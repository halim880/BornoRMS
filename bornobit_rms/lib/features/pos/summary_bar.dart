import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../dashboard/widgets.dart' show money;

/// Bill breakdown for the cart panel: subtotal, optional discount, VAT, a dashed
/// rule, then the large tabular "Total payable". With no active order ([detail]
/// is null) it renders a zeroed breakdown so the cart footer stays visible.
class SummaryBar extends StatelessWidget {
  final OrderDetail? detail;
  const SummaryBar({super.key, required this.detail});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final t = AppLocalizations.of(context);
    final d = detail;
    final cur = d?.currency ?? 'Tk';

    return Column(
      children: [
        _row(context, t.billSubtotal, money(d?.subtotal ?? 0, cur)),
        if (d != null && d.discountAmount != 0)
          _row(context, t.billDiscount, '-${money(d.discountAmount, cur)}', color: a.success),
        if (d != null && d.taxAmount != 0) _row(context, t.billVat, money(d.taxAmount, cur)),
        if (d != null && d.roundingAdjustment != 0)
          _row(context, t.billRounding, money(d.roundingAdjustment, cur)),
        const SizedBox(height: 8),
        _DashedDivider(color: a.borderStrong),
        const SizedBox(height: 8),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(t.billTotalPayable, style: Theme.of(context).textTheme.bodyLarge),
            Text(
              money(d?.grandTotal ?? 0, cur),
              style: AppColors.displayTotal.copyWith(color: a.textPrimary),
            ),
          ],
        ),
      ],
    );
  }

  Widget _row(BuildContext context, String label, String value, {Color? color}) {
    final a = context.appColors;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: Theme.of(context).textTheme.bodyMedium),
          Text(
            value,
            style: AppColors.priceText.copyWith(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: color ?? a.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}

class _DashedDivider extends StatelessWidget {
  final Color color;
  const _DashedDivider({required this.color});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, c) {
        const dash = 5.0;
        const gap = 4.0;
        final count = (c.maxWidth / (dash + gap)).floor();
        return Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: List.generate(
            count,
            (_) => SizedBox(width: dash, height: 1, child: ColoredBox(color: color)),
          ),
        );
      },
    );
  }
}
