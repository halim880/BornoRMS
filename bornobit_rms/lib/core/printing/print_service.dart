import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../models/dtos.dart';
import '../providers/providers.dart';
import 'printer_settings.dart';
import 'receipt_builder.dart';

enum PrintResult { printed, pdf, failed }

class PrintOutcome {
  final PrintResult result;
  final String message;
  const PrintOutcome(this.result, this.message);
}

final printServiceProvider = Provider<PrintService>((ref) => PrintService(ref));

class PrintService {
  final Ref ref;
  PrintService(this.ref);

  Future<PrintOutcome> printReceipt(OrderDetail order) =>
      _print(order, isKot: false);

  Future<PrintOutcome> printKot(OrderDetail order) =>
      _print(order, isKot: true);

  Future<PrintOutcome> _print(OrderDetail order, {required bool isKot}) async {
    final settings = await ref.read(printerSettingsProvider.future);

    if (settings.configured) {
      try {
        final bytes = isKot
            ? await buildKotBytes(order, settings.headerName)
            : await buildReceiptBytes(order, settings.headerName);
        await _sendToPrinter(settings.host, settings.port, bytes);
        return PrintOutcome(PrintResult.printed, isKot ? 'KOT sent to printer' : 'Receipt sent to printer');
      } catch (e) {
        // fall through to PDF
      }
    }

    // Fallback: download the server PDF (authenticated) and open it.
    try {
      await _openPdf(order, isKot: isKot);
      return PrintOutcome(PrintResult.pdf,
          settings.configured ? 'Printer unreachable — opened PDF instead' : 'No printer set — opened PDF');
    } catch (e) {
      return PrintOutcome(PrintResult.failed, 'Could not print or open PDF: $e');
    }
  }

  Future<void> _sendToPrinter(String host, int port, List<int> bytes) async {
    final socket = await Socket.connect(host, port, timeout: const Duration(seconds: 4));
    try {
      socket.add(bytes);
      await socket.flush();
    } finally {
      await socket.close();
      socket.destroy();
    }
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
