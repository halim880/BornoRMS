import 'dart:ui' as ui;

import 'package:esc_pos_utils_plus/esc_pos_utils_plus.dart';
import 'package:flutter/material.dart';
import 'package:image/image.dart' as img;
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../models/dtos.dart';

/// Bengali-capable receipt printing. ESC/POS `text()` is ASCII-only and cannot
/// render Bangla, so we lay the receipt out with [TextPainter] (which shapes
/// Bengali correctly via the bundled Noto Sans Bengali font), rasterize it to a
/// monochrome bitmap, and send it as an ESC/POS raster image. Used when the app
/// language is Bengali; English keeps the faster text path in receipt_builder.dart.

const double _paperWidth = 384; // 58mm @ 203 dpi ≈ 384 dots
const double _margin = 8;
const String _font = 'NotoSansBengali'; // covers Latin + Bengali

final _money = NumberFormat('#,##0.00');
final _dt = DateFormat('dd/MM/yyyy HH:mm');

String _sym(String raw) {
  switch (raw.trim().toLowerCase()) {
    case 'tk':
    case 'tk.':
    case 'bdt':
    case 'taka':
    case '৳':
      return '৳';
    default:
      return raw.trim();
  }
}

/// One printed row. Either a horizontal rule, a single (optionally centered)
/// text, or a label/value two-column row.
class _Line {
  final String? left;
  final String? right;
  final double size;
  final bool bold;
  final TextAlign align;
  final bool rule;
  const _Line({
    this.left,
    this.right,
    this.size = 22,
    this.bold = false,
    this.align = TextAlign.left,
    this.rule = false,
  });
  const _Line.rule() : this(rule: true);
}

Future<List<int>> buildImageReceiptBytes(
  OrderDetail o,
  String headerName,
  AppLocalizations t, {
  required bool kot,
}) async {
  final lines = kot ? _kotLines(o, headerName, t) : _receiptLines(o, headerName, t);
  final image = await _render(lines);

  final profile = await CapabilityProfile.load();
  final g = Generator(PaperSize.mm58, profile);
  final bytes = <int>[];
  bytes.addAll(g.imageRaster(image));
  bytes.addAll(g.feed(2));
  bytes.addAll(g.cut());
  return bytes;
}

List<_Line> _receiptLines(OrderDetail o, String headerName, AppLocalizations t) {
  final cur = _sym(o.currency);
  String m(num v) => '${_money.format(v)} $cur';
  final lines = <_Line>[
    _Line(left: headerName, size: 34, bold: true, align: TextAlign.center),
    _Line(left: t.posReceipt, size: 22, align: TextAlign.center),
    const _Line.rule(),
    _Line(left: 'Order: ${o.orderNumber}', size: 22, bold: true),
    _Line(left: _dt.format(o.orderedAtUtc), size: 20),
    _Line(left: '${o.orderType}${o.tableNumber != null ? '  Table ${o.tableNumber}' : ''}', size: 20),
  ];
  if (o.customerName != null && o.customerName!.isNotEmpty) {
    lines.add(_Line(left: o.customerName!, size: 20));
  }
  lines.add(const _Line.rule());
  for (final l in o.lines) {
    lines.add(_Line(left: '${l.quantity} x ${l.name}', right: _money.format(l.lineTotal), size: 22));
    for (final mod in l.modifiers) {
      lines.add(_Line(left: '   + ${mod.optionName}', size: 18));
    }
  }
  lines.add(const _Line.rule());
  lines.add(_Line(left: t.billSubtotal, right: m(o.subtotal), size: 20));
  if (o.discountAmount != 0) lines.add(_Line(left: t.billDiscount, right: '-${m(o.discountAmount)}', size: 20));
  if (o.taxAmount != 0) lines.add(_Line(left: t.billVat, right: m(o.taxAmount), size: 20));
  if (o.roundingAdjustment != 0) lines.add(_Line(left: t.billRounding, right: m(o.roundingAdjustment), size: 20));
  lines.add(_Line(left: t.billTotalPayable, right: m(o.grandTotal), size: 24, bold: true));
  if (o.isPaid) {
    lines.add(const _Line.rule());
    lines.add(_Line(left: t.billPaid, right: m(o.amountPaid), size: 20));
  } else if (o.balanceDue > 0) {
    lines.add(_Line(left: t.billBalanceDue, right: m(o.balanceDue), size: 22, bold: true));
  }
  lines.add(_Line(left: t.posOrderSettled, size: 20, align: TextAlign.center));
  return lines;
}

List<_Line> _kotLines(OrderDetail o, String headerName, AppLocalizations t) {
  final lines = <_Line>[
    _Line(left: t.posSendToKitchen, size: 32, bold: true, align: TextAlign.center),
    const _Line.rule(),
    _Line(left: 'Order: ${o.orderNumber}', size: 24, bold: true),
    _Line(left: '${o.orderType}${o.tableNumber != null ? '  Table ${o.tableNumber}' : ''}', size: 22),
    _Line(left: _dt.format(o.orderedAtUtc), size: 20),
    const _Line.rule(),
  ];
  for (final l in o.lines) {
    lines.add(_Line(left: '${l.quantity} x ${l.name}', size: 26, bold: true));
    for (final mod in l.modifiers) {
      lines.add(_Line(left: '   + ${mod.optionName}', size: 20));
    }
    if (l.notes != null && l.notes!.isNotEmpty) {
      lines.add(_Line(left: '   * ${l.notes}', size: 20));
    }
  }
  return lines;
}

/// Lays the lines out top-to-bottom with TextPainter and rasterizes to a
/// monochrome `img.Image` suitable for ESC/POS raster printing.
Future<img.Image> _render(List<_Line> lines) async {
  const contentWidth = _paperWidth - 2 * _margin;
  final painters = <_Line, ({TextPainter? left, TextPainter? right})>{};
  double height = _margin;

  TextPainter mk(String text, _Line l, {TextAlign align = TextAlign.left}) {
    final tp = TextPainter(
      text: TextSpan(
        text: text,
        style: TextStyle(
          fontFamily: _font,
          fontSize: l.size,
          height: 1.15,
          fontWeight: l.bold ? FontWeight.w700 : FontWeight.w400,
          color: const Color(0xFF000000),
        ),
      ),
      textAlign: align,
      textDirection: ui.TextDirection.ltr,
    );
    return tp;
  }

  // Measure pass.
  for (final l in lines) {
    if (l.rule) {
      height += 10;
      continue;
    }
    final left = mk(l.left ?? '', l, align: l.align);
    if (l.right != null) {
      // Two columns: left flexes, right hugs.
      final right = mk(l.right!, l);
      right.layout();
      left.layout(maxWidth: contentWidth - right.width - 6);
      painters[l] = (left: left, right: right);
      height += (left.height > right.height ? left.height : right.height) + 4;
    } else {
      left.layout(maxWidth: contentWidth);
      painters[l] = (left: left, right: null);
      height += left.height + 4;
    }
  }
  height += _margin;

  final recorder = ui.PictureRecorder();
  final canvas = Canvas(recorder);
  canvas.drawRect(
    Rect.fromLTWH(0, 0, _paperWidth, height),
    Paint()..color = const Color(0xFFFFFFFF),
  );

  double y = _margin;
  final rulePaint = Paint()
    ..color = const Color(0xFF000000)
    ..strokeWidth = 1;
  for (final l in lines) {
    if (l.rule) {
      canvas.drawLine(Offset(_margin, y + 4), Offset(_paperWidth - _margin, y + 4), rulePaint);
      y += 10;
      continue;
    }
    final p = painters[l]!;
    final left = p.left!;
    if (p.right != null) {
      left.paint(canvas, Offset(_margin, y));
      p.right!.paint(canvas, Offset(_paperWidth - _margin - p.right!.width, y));
      y += (left.height > p.right!.height ? left.height : p.right!.height) + 4;
    } else {
      // Honor centering by re-laying within full content width.
      double dx = _margin;
      if (l.align == TextAlign.center) dx = _margin + (contentWidth - left.width) / 2;
      left.paint(canvas, Offset(dx, y));
      y += left.height + 4;
    }
  }

  final picture = recorder.endRecording();
  final uiImage = await picture.toImage(_paperWidth.toInt(), height.ceil());
  final png = await uiImage.toByteData(format: ui.ImageByteFormat.png);
  final decoded = img.decodePng(png!.buffer.asUint8List());
  return decoded ?? img.Image(width: _paperWidth.toInt(), height: height.ceil());
}
