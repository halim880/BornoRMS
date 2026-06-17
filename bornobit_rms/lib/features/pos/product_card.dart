import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config/app_config.dart';
import '../../core/theme/app_colors.dart';
import '../dashboard/widgets.dart' show money;
import 'pos_dialogs.dart';
import 'pos_models.dart';
import 'pos_providers.dart';
import 'pos_section.dart';

class ProductCard extends ConsumerStatefulWidget {
  final PosProduct product;
  final PosAvailability? availability;
  const ProductCard({super.key, required this.product, required this.availability});

  @override
  ConsumerState<ProductCard> createState() => _ProductCardState();
}

class _ProductCardState extends ConsumerState<ProductCard> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final text = Theme.of(context).textTheme;
    final p = widget.product;
    final out = widget.availability?.isOutOfStock ?? false;
    final low = widget.availability?.isLowStock ?? false;
    final img = AppConfig.imageUrl(p.imagePath);
    final active = _hover && !out;

    Widget thumb = Stack(
      fit: StackFit.expand,
      children: [
        DecoratedBox(decoration: BoxDecoration(gradient: categoryGradient(p.categoryId))),
        Center(
          child: img != null
              ? Image.network(img, fit: BoxFit.cover,
                  errorBuilder: (_, __, ___) => Icon(Icons.restaurant_menu, color: categoryGlyph(p.categoryId)))
              : Icon(Icons.restaurant_menu, size: 30, color: categoryGlyph(p.categoryId)),
        ),
      ],
    );
    if (out) thumb = ColorFiltered(colorFilter: grayscaleFilter, child: thumb);

    return MouseRegion(
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: Opacity(
        opacity: out ? 0.62 : 1,
        child: InkWell(
          borderRadius: BorderRadius.circular(AppColors.radiusCard),
          onTap: out ? null : () => addProduct(context, ref, p),
          child: Container(
            clipBehavior: Clip.antiAlias,
            decoration: BoxDecoration(
              color: a.surface,
              borderRadius: BorderRadius.circular(AppColors.radiusCard),
              border: Border.all(color: active ? a.accent : a.borderStrong),
            ),
            child: Stack(
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(child: thumb),
                    Padding(
                      padding: const EdgeInsets.all(8),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(p.name,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: text.bodyLarge),
                          const SizedBox(height: 2),
                          Row(
                            children: [
                              if (p.hasVariants)
                                Text('from ', style: text.bodySmall),
                              Flexible(
                                child: Text(
                                  money(p.fromPrice, p.currency),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: AppColors.priceText
                                      .copyWith(fontSize: 12, color: a.textPrimary),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                // sold-out pill
                if (out)
                  Positioned(
                    top: 6,
                    right: 6,
                    child: _Pill(label: 'Sold out', bg: a.textPrimary, fg: a.onAccent),
                  )
                else if (low)
                  Positioned(
                    top: 6,
                    left: 6,
                    child: _Pill(label: 'Low', bg: a.warning, fg: a.onAccent),
                  ),
                // quick-add
                if (active)
                  Positioned(
                    top: 6,
                    right: 6,
                    child: Container(
                      width: 26,
                      height: 26,
                      decoration: BoxDecoration(color: a.accent, shape: BoxShape.circle),
                      child: Icon(Icons.add, size: 18, color: a.onAccent),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  final String label;
  final Color bg;
  final Color fg;
  const _Pill({required this.label, required this.bg, required this.fg});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Text(label,
          style: TextStyle(color: fg, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}

/// Resolve variant + options, then add to the active order. Mirrors the prior
/// inline flow; lifted out so [ProductCard] and others can call it.
Future<void> addProduct(BuildContext context, WidgetRef ref, PosProduct product) async {
  if (ref.read(posControllerProvider).orderId == null) {
    ScaffoldMessenger.of(context)
        .showSnackBar(const SnackBar(content: Text('Start an order first (tap +).')));
    return;
  }
  String? variantId;
  if (product.hasVariants) {
    final v = await showVariantPicker(context, product);
    if (v == null) return;
    variantId = v.id;
  }
  List<String> optionIds = const [];
  if (product.hasOptions) {
    final groups = await ref.read(posOptionGroupsProvider(product.id).future);
    if (groups.isNotEmpty) {
      if (!context.mounted) return;
      final picked = await showOptionPicker(context, product, groups);
      if (picked == null) return;
      optionIds = picked;
    }
  }
  try {
    await ref
        .read(posControllerProvider.notifier)
        .addItem(menuItemId: product.id, variantId: variantId, optionIds: optionIds);
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Added ${product.name}'), duration: const Duration(milliseconds: 900)),
      );
    }
  } catch (e) {
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
    }
  }
}
