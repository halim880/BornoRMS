import 'package:esc_pos_utils_plus/esc_pos_utils_plus.dart';
import 'package:intl/intl.dart';

import '../models/dtos.dart';

final _money = NumberFormat('#,##0.00');
final _dt = DateFormat('dd/MM/yyyy HH:mm');

String _m(num v) => _money.format(v);

/// Builds 58mm ESC/POS bytes for a POS receipt from the order detail.
Future<List<int>> buildReceiptBytes(OrderDetail o, String headerName) async {
  final profile = await CapabilityProfile.load();
  final g = Generator(PaperSize.mm58, profile);
  final bytes = <int>[];

  bytes.addAll(g.text(headerName,
      styles: const PosStyles(align: PosAlign.center, bold: true, height: PosTextSize.size2, width: PosTextSize.size2)));
  bytes.addAll(g.text('Sales Receipt', styles: const PosStyles(align: PosAlign.center)));
  bytes.addAll(g.hr());

  bytes.addAll(g.text('Order: ${o.orderNumber}', styles: const PosStyles(bold: true)));
  bytes.addAll(g.text(_dt.format(o.orderedAtUtc)));
  bytes.addAll(g.text('Type : ${o.orderType}${o.tableNumber != null ? '  Table ${o.tableNumber}' : ''}'));
  if (o.customerName != null && o.customerName!.isNotEmpty) {
    bytes.addAll(g.text('Cust : ${o.customerName}'));
  }
  bytes.addAll(g.hr());

  for (final l in o.lines) {
    bytes.addAll(g.row([
      PosColumn(text: '${l.quantity} x ${l.name}', width: 8),
      PosColumn(text: _m(l.lineTotal), width: 4, styles: const PosStyles(align: PosAlign.right)),
    ]));
    for (final mod in l.modifiers) {
      bytes.addAll(g.text('  + ${mod.optionName}', styles: const PosStyles(fontType: PosFontType.fontB)));
    }
  }
  bytes.addAll(g.hr());

  void totalRow(String label, num value, {bool bold = false}) {
    bytes.addAll(g.row([
      PosColumn(text: label, width: 8, styles: PosStyles(bold: bold)),
      PosColumn(text: '${_m(value)} ${o.currency}', width: 4, styles: PosStyles(align: PosAlign.right, bold: bold)),
    ]));
  }

  totalRow('Subtotal', o.subtotal);
  if (o.discountAmount != 0) totalRow('Discount', -o.discountAmount);
  if (o.taxAmount != 0) totalRow('Tax', o.taxAmount);
  if (o.serviceChargeAmount != 0) totalRow('Service', o.serviceChargeAmount);
  totalRow('TOTAL', o.grandTotal, bold: true);

  if (o.isPaid) {
    bytes.addAll(g.hr(ch: '='));
    totalRow('Paid', o.amountPaid);
    if (o.paymentMethod != null) bytes.addAll(g.text('Method: ${o.paymentMethod}'));
  } else if (o.balanceDue > 0) {
    totalRow('Balance due', o.balanceDue, bold: true);
  }

  bytes.addAll(g.feed(1));
  bytes.addAll(g.text('Thank you!', styles: const PosStyles(align: PosAlign.center)));
  bytes.addAll(g.feed(2));
  bytes.addAll(g.cut());
  return bytes;
}

/// Builds 58mm ESC/POS bytes for a kitchen order ticket (items only, no prices).
Future<List<int>> buildKotBytes(OrderDetail o, String headerName) async {
  final profile = await CapabilityProfile.load();
  final g = Generator(PaperSize.mm58, profile);
  final bytes = <int>[];

  bytes.addAll(g.text('KITCHEN ORDER',
      styles: const PosStyles(align: PosAlign.center, bold: true, height: PosTextSize.size2, width: PosTextSize.size2)));
  bytes.addAll(g.hr());
  bytes.addAll(g.text('Order: ${o.orderNumber}', styles: const PosStyles(bold: true)));
  bytes.addAll(g.text('${o.orderType}${o.tableNumber != null ? '  Table ${o.tableNumber}' : ''}'));
  bytes.addAll(g.text(_dt.format(o.orderedAtUtc)));
  bytes.addAll(g.hr());

  for (final l in o.lines) {
    bytes.addAll(g.text('${l.quantity} x ${l.name}',
        styles: const PosStyles(bold: true, height: PosTextSize.size2)));
    for (final mod in l.modifiers) {
      bytes.addAll(g.text('   + ${mod.optionName}'));
    }
    if (l.notes != null && l.notes!.isNotEmpty) {
      bytes.addAll(g.text('   * ${l.notes}'));
    }
  }
  bytes.addAll(g.feed(2));
  bytes.addAll(g.cut());
  return bytes;
}
