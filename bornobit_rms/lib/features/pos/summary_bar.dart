import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_colors.dart';
import '../dashboard/widgets.dart' show money;

/// Bill breakdown for the cart panel: subtotal, optional discount, VAT, a dashed
/// rule, then the large tabular "Total payable".
class SummaryBar extends StatelessWidget {
  final OrderDetail detail;
  const SummaryBar({super.key, required this.detail});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final cur = detail.currency;

    return Column(
      children: [
        _row(context, 'Subtotal', money(detail.subtotal, cur)),
        if (detail.discountAmount != 0)
          _row(context, 'Discount', '-${money(detail.discountAmount, cur)}', color: a.success),
        if (detail.taxAmount != 0) _row(context, 'VAT (5%)', money(detail.taxAmount, cur)),
        if (detail.roundingAdjustment != 0)
          _row(context, 'Rounding', money(detail.roundingAdjustment, cur)),
        const SizedBox(height: 8),
        _DashedDivider(color: a.borderStrong),
        const SizedBox(height: 8),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text('Total payable', style: Theme.of(context).textTheme.bodyLarge),
            Text(
              money(detail.grandTotal, cur),
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
