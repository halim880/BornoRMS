import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

/// A floating section panel: white surface, radius 16, soft shadow, no border.
/// Used by the order-tabs / categories / products / cart sections.
class PosPanel extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final EdgeInsetsGeometry margin;
  final double? radius;
  const PosPanel({
    super.key,
    required this.child,
    this.padding,
    this.margin = EdgeInsets.zero,
    this.radius,
  });

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return Container(
      margin: margin,
      padding: padding,
      decoration: BoxDecoration(
        color: a.surface,
        borderRadius: BorderRadius.circular(radius ?? AppColors.radiusPanel),
        boxShadow: AppColors.shadowSoft,
      ),
      child: child,
    );
  }
}

/// A deterministic light gradient derived from a category/product key — used for
/// product thumbnails and cart-line tiles so the same item always tints the same.
LinearGradient categoryGradient(String seed) {
  final h = (seed.hashCode % 360).abs().toDouble();
  return LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [
      HSLColor.fromAHSL(1, h, 0.55, 0.93).toColor(),
      HSLColor.fromAHSL(1, (h + 26) % 360, 0.50, 0.86).toColor(),
    ],
  );
}

/// A readable glyph color matching [categoryGradient].
Color categoryGlyph(String seed) {
  final h = (seed.hashCode % 360).abs().toDouble();
  return HSLColor.fromAHSL(1, h, 0.42, 0.42).toColor();
}

/// Grayscale filter for sold-out thumbnails.
const ColorFilter grayscaleFilter = ColorFilter.matrix(<double>[
  0.2126, 0.7152, 0.0722, 0, 0, //
  0.2126, 0.7152, 0.0722, 0, 0, //
  0.2126, 0.7152, 0.0722, 0, 0, //
  0, 0, 0, 1, 0, //
]);
