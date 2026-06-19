import 'dart:convert';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../../l10n/app_localizations.dart';
import '../../l10n/app_localizations_bn.dart';
import '../i18n/locale_controller.dart';
import '../models/dtos.dart';
import '../providers/providers.dart';
import 'print_job.dart';
import 'print_queue.dart';
import 'printer_settings.dart';
import 'receipt_builder.dart';
import 'receipt_image.dart';

enum PrintResult { printed, queued, pdf, failed }

/// Opens a raw TCP socket to an ESC/POS printer and writes [bytes]. Shared by the
/// live print path and the offline retry queue. Throws on connect/write failure.
Future<void> sendBytesToPrinter(String host, int port, List<int> bytes) async {
  final socket = await Socket.connect(host, port, timeout: const Duration(seconds: 4));
  try {
    socket.add(bytes);
    await socket.flush();
  } finally {
    await socket.close();
    socket.destroy();
  }
}

class PrintOutcome {
  final PrintResult result;
  final String message;
  const PrintOutcome(this.result, this.message);
}

final printServiceProvider = Provider<PrintService>((ref) => PrintService(ref));

class PrintService {
  final Ref ref;
  PrintService(this.ref);

  // Monotonic suffix so two jobs enqueued in the same millisecond get distinct ids.
  static int _seq = 0;
  String _newId() => '${DateTime.now().microsecondsSinceEpoch}-${_seq++}';

  Future<PrintOutcome> printReceipt(OrderDetail order) =>
      _print(order, isKot: false);

  Future<PrintOutcome> printKot(OrderDetail order) =>
      _print(order, isKot: true);

  /// Open the server-rendered PDF without touching the thermal printer — used as
  /// an explicit "preview" action.
  Future<PrintOutcome> previewPdf(OrderDetail order, {required bool isKot}) async {
    try {
      await _openPdf(order, isKot: isKot);
      return const PrintOutcome(PrintResult.pdf, 'Opened PDF preview');
    } catch (e) {
      return PrintOutcome(PrintResult.failed, 'Could not open PDF: $e');
    }
  }

  Future<PrintOutcome> _print(OrderDetail order, {required bool isKot}) async {
    final settings = await ref.read(printerSettingsProvider.future);

    if (settings.configured) {
      List<int>? bytes;
      try {
        bytes = await _buildBytes(order, settings.headerName, isKot: isKot);
      } catch (_) {
        bytes = null; // couldn't render — fall through to PDF
      }

      if (bytes != null) {
        try {
          await sendBytesToPrinter(settings.host, settings.port, bytes);
          return PrintOutcome(PrintResult.printed, isKot ? 'KOT sent to printer' : 'Receipt sent to printer');
        } catch (e) {
          // Printer unreachable: park the job in the retry queue instead of losing
          // it (or silently dropping to PDF, which never reaches the kitchen).
          await ref.read(printQueueProvider.notifier).enqueue(PrintJob(
                id: _newId(),
                orderId: order.id,
                orderNumber: order.orderNumber,
                isKot: isKot,
                host: settings.host,
                port: settings.port,
                bytesB64: base64Encode(bytes),
                createdAtMs: DateTime.now().millisecondsSinceEpoch,
                attempts: 1,
                lastError: e.toString(),
              ));
          return PrintOutcome(PrintResult.queued,
              isKot ? 'Printer offline — KOT queued for retry' : 'Printer offline — receipt queued for retry');
        }
      }
    }

    // No printer configured (or render failed): download the server PDF and open it.
    try {
      await _openPdf(order, isKot: isKot);
      return PrintOutcome(PrintResult.pdf,
          settings.configured ? 'Could not build receipt — opened PDF instead' : 'No printer set — opened PDF');
    } catch (e) {
      return PrintOutcome(PrintResult.failed, 'Could not print or open PDF: $e');
    }
  }

  // Bengali can't render through the ASCII ESC/POS text path, so render it as a
  // raster image; English keeps the faster native-text path.
  Future<List<int>> _buildBytes(OrderDetail order, String headerName, {required bool isKot}) async {
    if (ref.read(isBengaliProvider)) {
      final AppLocalizations t = AppLocalizationsBn();
      return buildImageReceiptBytes(order, headerName, t, kot: isKot);
    }
    return isKot
        ? buildKotBytes(order, headerName)
        : buildReceiptBytes(order, headerName);
  }

  Future<void> _openPdf(OrderDetail order, {required bool isKot}) async {
    final dio = ref.read(apiClientProvider).dio;
    final path = isKot
        ? '/api/v1/staff/pos/orders/${order.id}/kot.pdf'
        : '/admin/orders/${order.id}/pos-receipt.pdf';
    final res = await dio.get<List<int>>(path,
        options: Options(responseType: ResponseType.bytes));
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/${order.orderNumber}-${isKot ? 'kot' : 'receipt'}.pdf');
    await file.writeAsBytes(res.data ?? const []);
    await OpenFilex.open(file.path);
  }
}
