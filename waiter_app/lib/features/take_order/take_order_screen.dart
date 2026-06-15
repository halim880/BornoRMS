import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/format.dart';
import '../../core/widgets/responsive.dart';
import '../../core/widgets/snack.dart';
import 'cart_controller.dart';
import 'cart_panel.dart';
import 'take_order_providers.dart';
import 'variant_picker_dialog.dart';

class TakeOrderScreen extends ConsumerWidget {
  const TakeOrderScreen({super.key});

  Future<void> _loadForEdit(BuildContext ctx, WidgetRef ref, String orderId) async {
    try {
      final order = await ref.read(waiterApiProvider).order(orderId);
      ref.read(cartProvider.notifier).loadForEdit(order);
    } on ApiException catch (e) {
      if (ctx.mounted) showError(ctx, e.message);
    }
  }

  Future<void> _onProductTap(BuildContext ctx, WidgetRef ref, Product p) async {
    if (p.hasVariants) {
      final v = await pickVariant(ctx, p);
      if (v == null) return;
      ref.read(cartProvider.notifier).add(p, variant: v);
    } else {
      ref.read(cartProvider.notifier).add(p);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Apply a floor "Take order" target once, then clear it.
    ref.listen(takeOrderTargetProvider, (_, target) {
      if (target == null) return;
      ref.read(cartProvider.notifier).applyTarget(
          target.tableId, target.tableNumber, target.sessionId, target.guests);
      WidgetsBinding.instance.addPostFrameCallback((_) {
        ref.read(takeOrderTargetProvider.notifier).state = null;
      });
    });

    final cart = ref.watch(cartProvider);
    final categories = ref.watch(categoriesProvider).valueOrNull ?? const <ProductCategory>[];
    final products = ref.watch(activeProductsProvider);
    final selectedCat = ref.watch(selectedCategoryProvider);
    final availMap = ref.watch(availabilityMapProvider);

    final visibleCats = categories
        .where((c) => c.isActive && products.any((p) => p.productCategoryId == c.id))
        .toList()
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));

    final filtered = selectedCat == null
        ? products
        : products.where((p) => p.productCategoryId == selectedCat).toList();

    final grid = filtered.isEmpty
        ? Center(
            child: Text('No products in this category',
                style: TextStyle(color: Colors.grey.shade600)))
        : LayoutBuilder(builder: (ctx, box) {
            final cols = gridColumns(box.maxWidth, 150);
            return GridView.builder(
              padding: const EdgeInsets.all(10),
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: cols,
                childAspectRatio: 0.82,
                crossAxisSpacing: 8,
                mainAxisSpacing: 8,
              ),
              itemCount: filtered.length,
              itemBuilder: (_, i) {
                final p = filtered[i];
                final avail = availMap[p.id];
                final out = avail?.isOutOfStock ?? false;
                return _ProductCard(
                  p: p,
                  outOfStock: out,
                  lowStock: avail?.isLowStock ?? false,
                  availableStock: avail?.availableStock,
                  onTap: out ? null : () => _onProductTap(context, ref, p),
                );
              },
            );
          });

    final header = Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        _RunningOrdersBar(onTapOrder: (id) => _loadForEdit(context, ref, id)),
        if (cart.isEdit)
          _EditBanner(cart: cart, onExit: () => ref.read(cartProvider.notifier).reset()),
      ],
    );

    // Expanded: web-style 3-pane (category rail | product grid | cart panel).
    if (context.isExpanded) {
      return Column(
        children: [
          header,
          Expanded(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                SizedBox(
                  width: 180,
                  child: _CategoryRail(cats: visibleCats, selected: selectedCat),
                ),
                const VerticalDivider(width: 1),
                Expanded(child: grid),
                const VerticalDivider(width: 1),
                const SizedBox(width: 360, child: CartPanelBody()),
              ],
            ),
          ),
        ],
      );
    }

    // Compact/medium: chips + grid + bottom cart bar (sheet).
    return Column(
      children: [
        header,
        SizedBox(
          height: 48,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            children: [
              Padding(
                padding: const EdgeInsets.only(right: 6),
                child: ChoiceChip(
                  label: const Text('All'),
                  selected: selectedCat == null,
                  onSelected: (_) => ref.read(selectedCategoryProvider.notifier).state = null,
                ),
              ),
              ...visibleCats.map((c) => Padding(
                    padding: const EdgeInsets.only(right: 6),
                    child: ChoiceChip(
                      label: Text(c.name),
                      selected: selectedCat == c.id,
                      onSelected: (_) =>
                          ref.read(selectedCategoryProvider.notifier).state = c.id,
                    ),
                  )),
            ],
          ),
        ),
        const Divider(height: 1),
        Expanded(child: grid),
        _CartBar(cart: cart),
      ],
    );
  }
}

/// Vertical category list for the expanded 3-pane layout.
class _CategoryRail extends ConsumerWidget {
  final List<ProductCategory> cats;
  final String? selected;
  const _CategoryRail({required this.cats, required this.selected});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    Widget item(String label, String? id) {
      final sel = selected == id;
      return InkWell(
        onTap: () => ref.read(selectedCategoryProvider.notifier).state = id,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
          color: sel ? const Color(0xFFCB3A1A).withValues(alpha: 0.10) : null,
          child: Text(label,
              style: TextStyle(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                  color: sel ? const Color(0xFFCB3A1A) : null)),
        ),
      );
    }

    return ListView(
      children: [
        const Padding(
          padding: EdgeInsets.fromLTRB(12, 12, 12, 8),
          child: Text('Category',
              style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
        ),
        item('All', null),
        ...cats.map((c) => item(c.name, c.id)),
      ],
    );
  }
}

class _RunningOrdersBar extends ConsumerWidget {
  final void Function(String orderId) onTapOrder;
  const _RunningOrdersBar({required this.onTapOrder});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(activeOrdersProvider).valueOrNull ?? const <ActiveOrder>[];
    final editingId = ref.watch(cartProvider).editingOrderId;
    return Container(
      color: Colors.white,
      height: 64,
      child: Row(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10),
            child: Text('Running',
                style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: Colors.grey.shade600,
                    letterSpacing: 0.5)),
          ),
          Expanded(
            child: orders.isEmpty
                ? Text('None right now',
                    style: TextStyle(fontSize: 13, color: Colors.grey.shade400))
                : ListView.separated(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    itemCount: orders.length,
                    separatorBuilder: (_, __) => const SizedBox(width: 6),
                    itemBuilder: (_, i) {
                      final o = orders[i];
                      final active = o.id == editingId;
                      return InkWell(
                        onTap: () => onTapOrder(o.id),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: active ? const Color(0xFFCB3A1A).withValues(alpha: 0.12) : Colors.grey.shade100,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                                color: active ? const Color(0xFFCB3A1A) : Colors.grey.shade300),
                          ),
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            mainAxisSize: MainAxisSize.min,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(o.tableNumber ?? o.orderType.label,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                      fontSize: 12, fontWeight: FontWeight.w800)),
                              Text('${money(o.total, currency: o.currency)} · ${o.status.label}',
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(fontSize: 10, color: Colors.grey.shade600)),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
          ),
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh, size: 20),
            onPressed: () => ref.read(activeOrdersProvider.notifier).refresh(),
          ),
        ],
      ),
    );
  }
}

class _EditBanner extends StatelessWidget {
  final CartState cart;
  final VoidCallback onExit;
  const _EditBanner({required this.cart, required this.onExit});
  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: const Color(0xFFCB3A1A).withValues(alpha: 0.1),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Row(
        children: [
          const Icon(Icons.edit, size: 16, color: Color(0xFFCB3A1A)),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              'Editing ${cart.editingOrderNumber} · ${cart.tableNumber ?? 'Takeaway'}',
              style: const TextStyle(fontWeight: FontWeight.w600, color: Color(0xFFCB3A1A)),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close, size: 18),
            onPressed: onExit,
            tooltip: 'Back to new order',
          ),
        ],
      ),
    );
  }
}

class _ProductCard extends StatelessWidget {
  final Product p;
  final bool outOfStock;
  final bool lowStock;
  final double? availableStock;
  final VoidCallback? onTap;
  const _ProductCard({
    required this.p,
    required this.outOfStock,
    required this.lowStock,
    required this.availableStock,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Opacity(
        opacity: outOfStock ? 0.5 : 1,
        child: Container(
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: Colors.grey.shade200),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    ClipRRect(
                      borderRadius: const BorderRadius.vertical(top: Radius.circular(10)),
                      child: _thumb(),
                    ),
                    if (outOfStock)
                      _badge('Out', Colors.red)
                    else if (lowStock)
                      _badge('Low ${availableStock?.toStringAsFixed(0) ?? ''}', Colors.orange),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(p.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                    Text(
                      p.hasVariants
                          ? 'from ${money(p.minPrice, currency: p.currency)}'
                          : money(p.price, currency: p.currency),
                      style: const TextStyle(
                          fontSize: 12, fontWeight: FontWeight.w700, color: Color(0xFFCB3A1A)),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _thumb() {
    final url = AppConfig.imageUrl(p.imagePath);
    final fallback = Container(
      color: Colors.grey.shade100,
      alignment: Alignment.center,
      child: Icon(Icons.fastfood, color: Colors.grey.shade400),
    );
    if (url == null) return fallback;
    return Image.network(
      url,
      fit: BoxFit.cover,
      errorBuilder: (_, __, ___) => fallback,
      loadingBuilder: (ctx, child, progress) =>
          progress == null ? child : Container(color: Colors.grey.shade100),
    );
  }

  Widget _badge(String text, Color color) => Positioned(
        top: 6,
        left: 6,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
          decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(999)),
          child: Text(text,
              style: const TextStyle(fontSize: 10, color: Colors.white, fontWeight: FontWeight.bold)),
        ),
      );
}

class _CartBar extends StatelessWidget {
  final CartState cart;
  const _CartBar({required this.cart});
  @override
  Widget build(BuildContext context) {
    return Material(
      elevation: 8,
      child: SafeArea(
        top: false,
        child: InkWell(
          onTap: () => showCartPanel(context),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                Badge(
                  isLabelVisible: cart.itemCount > 0,
                  label: Text('${cart.itemCount}'),
                  child: const Icon(Icons.shopping_cart_outlined),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Text(
                    cart.lines.isEmpty
                        ? 'Tap items to add'
                        : '${cart.itemCount} item(s) · ${money(cart.total, currency: cart.currency)}',
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                ),
                FilledButton(
                  onPressed: cart.lines.isEmpty ? null : () => showCartPanel(context),
                  child: Text(cart.isEdit ? 'Review / save' : 'Review'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
