import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../api/api_exception.dart';
import '../providers/providers.dart';
import 'snack.dart';

/// Downloads a protected PDF through the authenticated client and opens it in the
/// platform viewer. `url_launcher` is intentionally avoided — it cannot attach
/// the bearer header these endpoints require.
Future<void> openOrderPdf(
  BuildContext context,
  WidgetRef ref, {
  required String orderId,
  required String orderNumber,
  required bool kot,
}) async {
  final api = ref.read(waiterApiProvider);
  final path = '/waiter/orders/$orderId/${kot ? 'kot' : 'bill'}.pdf';
  try {
    final bytes = await api.pdfBytes(path);
    if (bytes.isEmpty) {
      if (context.mounted) showError(context, 'The document was empty.');
      return;
    }
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/$orderNumber-${kot ? 'kot' : 'bill'}.pdf');
    await file.writeAsBytes(bytes, flush: true);
    final result = await OpenFilex.open(file.path);
    if (result.type != ResultType.done && context.mounted) {
      showError(context, 'No app available to open the PDF.');
    }
  } on ApiException catch (e) {
    if (context.mounted) showError(context, e.message);
  }
}
