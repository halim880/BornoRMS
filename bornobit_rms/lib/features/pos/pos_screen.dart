import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config/app_config.dart';
import '../../core/models/dtos.dart';
import '../../core/printing/print_service.dart';
import '../../core/theme/app_theme.dart';
import '../dashboard/widgets.dart';
import 'payment_dialog.dart';
import 'pos_dialogs.dart';
import 'pos_models.dart';
import 'pos_providers.dart';

class PosScreen extends ConsumerWidget {
  const PosScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return LayoutBuilder(
      builder: (context, c) {
        final wide = c.maxWidth >= 820;
        return Column(
          children: [
            const _ActiveOrdersBar(),
            const Divider(height: 1),
            Expanded(child: wide ? const _WideBody() : const _NarrowBody()),
          ],
        );
      },
    );
  }
}

// ---------------- active orders bar ----------------
class _ActiveOrdersBar extends ConsumerWidget {
  const _ActiveOrdersBar();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(posActiveOrdersProvider).valueOrNull ?? const [];
    final activeId = ref.watch(posControllerProvider).orderId;
    return Container(
      height: 56,
      color: Bo.surface,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: Row(
        children: [
          FilledButton.icon(
            onPressed: () => showDialog(context: context, builder: (_) => const NewOrderDialog()),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('New'),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: ListView(
              scrollDirection: Axis.horizontal,
              children: [
                for (final o in orders)
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3, vertical: 10),
                    child: ChoiceChip(
                      selected: o.id == activeId,
                      onSelected: (_) => ref.read(posControllerProvider.notifier).selectOrder(o.id),
                      label: Text('${o.tableNumber != null ? 'T${o.tableNumber}' : o.orderType.substring(0, 1)} · ${o.itemCount}× · ${money(o.total, o.currency)}',
                          style: const TextStyle(fontSize: 12)),
                    ),
                  ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Printer settings',
            icon: const Icon(Icons.print_outlined),
            onPressed: () => showDialog(context: context, builder: (_) => const PrinterSettingsDialog()),
          ),
        ],
      ),
    );
  }
}

// ---------------- wide: catalog + cart side by side ----------------
class _WideBody extends StatelessWidget {
  const _WideBody();
  @override
  Widget build(BuildContext context) {
    return Row(
      children: const [
        Expanded(child: _Catalog()),
        VerticalDivider(width: 1),
        SizedBox(width: 360, child: _CartPanel()),
      ],
    );
  }
}

// ---------------- narrow: catalog full, cart in a sheet ----------------
class _NarrowBody extends ConsumerWidget {
  const _NarrowBody();
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(posControllerProvider).detail;
    final count = detail?.lines.fold<int>(0, (a, l) => a + l.quantity) ?? 0;
    return Stack(
      children: [
        const Positioned.fill(child: _Catalog()),
        if (detail != null)
          Positioned(
            left: 12, right: 12, bottom: 12,
            child: FilledButton(
              onPressed: () => showModalBottomSheet(
                context: context,
                isScrollControlled: true,
                builder: (_) => SizedBox(height: MediaQuery.of(context).size.height * 0.85, child: const _CartPanel()),
              ),
              style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
              child: Text('View cart · $count item(s) · ${money(detail.grandTotal, detail.currency)}'),
            ),
          ),
      ],
    );
  }
}

// ---------------- catalog (category rail + grid) ----------------
class _Catalog extends ConsumerWidget {
  const _Catalog();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final catalogAsync = ref.watch(posCatalogProvider);
    return catalogAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => Center(child: Text('Catalog: $e', style: const TextStyle(color: Bo.textMuted))),
      data: (catalog) {
        final selectedCat = ref.watch(posCategoryProvider);
        final search = ref.watch(posSearchProvider).toLowerCase();
        var products = catalog.products;
        if (selectedCat != null) products = products.where((p) => p.categoryId == selectedCat).toList();
        if (search.isNotEmpty) products = products.where((p) => p.name.toLowerCase().contains(search) || p.code.toLowerCase().contains(search)).toList();
        products = products.toList()..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));

        return Column(
          children: [
            // search + categories
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
              child: TextField(
                decoration: const InputDecoration(hintText: 'Search products', prefixIcon: Icon(Icons.search), isDense: true),
                onChanged: (v) => ref.read(posSearchProvider.notifier).state = v,
              ),
            ),
            SizedBox(
              height: 40,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                children: [
                  Padding(
                    padding: const EdgeInsets.only(right: 6),
                    child: ChoiceChip(label: const Text('All'), selected: selectedCat == null, onSelected: (_) => ref.read(posCategoryProvider.notifier).state = null),
                  ),
                  for (final cat in catalog.categories)
                    Padding(
                      padding: const EdgeInsets.only(right: 6),
                      child: ChoiceChip(label: Text(cat.name), selected: selectedCat == cat.id, onSelected: (_) => ref.read(posCategoryProvider.notifier).state = cat.id),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 6),
            Expanded(
              child: GridView.builder(
                padding: const EdgeInsets.all(12),
                gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                  maxCrossAxisExtent: 170, mainAxisExtent: 150, crossAxisSpacing: 10, mainAxisSpacing: 10),
                itemCount: products.length,
                itemBuilder: (_, i) => _ProductCard(product: products[i], availability: catalog.availability[products[i].id]),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _ProductCard extends ConsumerWidget {
  final PosProduct product;
  final PosAvailability? availability;
  const _ProductCard({required this.product, required this.availability});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final out = availability?.isOutOfStock ?? false;
    final low = availability?.isLowStock ?? false;
    final img = AppConfig.imageUrl(product.imagePath);

    return Opacity(
      opacity: out ? 0.5 : 1,
      child: InkWell(
        borderRadius: BorderRadius.circular(Bo.radiusMd),
        onTap: out ? null : () => _addProduct(context, ref, product),
        child: Container(
          decoration: BoxDecoration(
            color: Bo.surface,
            borderRadius: BorderRadius.circular(Bo.radiusMd),
            border: Border.all(color: Bo.border),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: ClipRRect(
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(Bo.radiusMd)),
                  child: img != null
                      ? Image.network(img, fit: BoxFit.cover, errorBuilder: (_, __, ___) => _imgFallback())
                      : _imgFallback(),
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(product.name, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                    const SizedBox(height: 2),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('${product.hasVariants ? 'from ' : ''}${money(product.fromPrice, product.currency)}',
                            style: const TextStyle(color: Bo.primary, fontWeight: FontWeight.w700, fontSize: 12)),
                        if (out) const ToneChip('Out', 'danger') else if (low) const ToneChip('Low', 'warning'),
                      ],
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

  Widget _imgFallback() => Container(color: Bo.slate100, child: const Icon(Icons.restaurant_menu, color: Bo.slate400));
}

/// Resolve variant + options, then add to the active order.
Future<void> _addProduct(BuildContext context, WidgetRef ref, PosProduct product) async {
  if (ref.read(posControllerProvider).orderId == null) {
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Start an order first (press New).')));
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
    await ref.read(posControllerProvider.notifier).addItem(menuItemId: product.id, variantId: variantId, optionIds: optionIds);
  } catch (e) {
    if (context.mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
  }
}

// ---------------- cart / checkout ----------------
class _CartPanel extends ConsumerWidget {
  const _CartPanel();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(posControllerProvider);
    final detail = state.detail;

    if (detail == null) {
      return Container(
        color: Bo.surface,
        child: const Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text('Pick an order above, or press New to start one.', textAlign: TextAlign.center, style: TextStyle(color: Bo.textSubtle)),
          ),
        ),
      );
    }

    final c = ref.read(posControllerProvider.notifier);
    return Container(
      color: Bo.surface,
      child: Column(
        children: [
          // header
          ListTile(
            title: Text(detail.orderNumber, style: const TextStyle(fontWeight: FontWeight.w700)),
            subtitle: Text('${detail.orderType}${detail.tableNumber != null ? ' · T${detail.tableNumber}' : ''} · ${detail.customerName ?? 'Walk-in'}'),
            trailing: IconButton(
              icon: const Icon(Icons.edit_outlined),
              onPressed: () => showDialog(context: context, builder: (_) => const NewOrderDialog(edit: true)),
            ),
          ),
          const Divider(height: 1),
          // lines
          Expanded(
            child: detail.lines.isEmpty
                ? const Center(child: Text('No items yet', style: TextStyle(color: Bo.textSubtle)))
                : ListView.separated(
                    itemCount: detail.lines.length,
                    separatorBuilder: (_, __) => const Divider(height: 1),
                    itemBuilder: (_, i) => _CartLineTile(line: detail.lines[i], currency: detail.currency, controller: c),
                  ),
          ),
          const Divider(height: 1),
          // totals
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                _totalRow('Subtotal', money(detail.subtotal, detail.currency)),
                if (detail.discountAmount != 0) _totalRow('Discount', '-${money(detail.discountAmount, detail.currency)}'),
                if (detail.roundingAdjustment != 0) _totalRow('Rounding', money(detail.roundingAdjustment, detail.currency)),
                _totalRow('Total', money(detail.grandTotal, detail.currency), bold: true),
              ],
            ),
          ),
          // actions
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(child: OutlinedButton.icon(onPressed: () => _print(context, ref, detail, kot: false), icon: const Icon(Icons.receipt_long, size: 18), label: const Text('Receipt'))),
                    const SizedBox(width: 8),
                    Expanded(child: OutlinedButton.icon(onPressed: detail.lines.isEmpty ? null : () => _print(context, ref, detail, kot: true), icon: const Icon(Icons.soup_kitchen_outlined, size: 18), label: const Text('KOT'))),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    OutlinedButton(
                      onPressed: () => showCancelDialog(context, ref),
                      style: OutlinedButton.styleFrom(foregroundColor: Bo.danger),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: FilledButton(
                        onPressed: detail.lines.isEmpty ? null : () => _checkout(context, ref),
                        style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
                        child: Text('Checkout · ${money(detail.grandTotal, detail.currency)}'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _totalRow(String label, String value, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.w700 : FontWeight.w400, color: Bo.textMuted)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.w800 : FontWeight.w600, fontSize: bold ? 16 : 14)),
        ]),
      );

  Future<void> _checkout(BuildContext context, WidgetRef ref) async {
    final result = await showDialog(context: context, builder: (_) => const PaymentDialog());
    if (result != null) {
      // Paid in full — print the receipt from the (now-paid) detail, then clear.
      final detail = ref.read(posControllerProvider).detail;
      if (detail != null && context.mounted) {
        await _print(context, ref, detail, kot: false);
      }
      ref.read(posControllerProvider.notifier).clearSelection();
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order settled.')));
      }
    }
  }

  Future<void> _print(BuildContext context, WidgetRef ref, OrderDetail detail, {required bool kot}) async {
    final svc = ref.read(printServiceProvider);
    final outcome = kot ? await svc.printKot(detail) : await svc.printReceipt(detail);
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(outcome.message)));
    }
  }
}

class _CartLineTile extends StatelessWidget {
  final OrderLine line;
  final String currency;
  final dynamic controller;
  const _CartLineTile({required this.line, required this.currency, required this.controller});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(line.name, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                for (final m in line.modifiers)
                  Text('+ ${m.optionName}', style: const TextStyle(color: Bo.textSubtle, fontSize: 11)),
                Text(money(line.lineTotal, currency), style: const TextStyle(color: Bo.textMuted, fontSize: 12)),
              ],
            ),
          ),
          IconButton(visualDensity: VisualDensity.compact, icon: const Icon(Icons.remove_circle_outline, size: 20), onPressed: () => controller.changeQty(line, -1)),
          Text('${line.quantity}', style: const TextStyle(fontWeight: FontWeight.w700)),
          IconButton(visualDensity: VisualDensity.compact, icon: const Icon(Icons.add_circle_outline, size: 20), onPressed: () => controller.changeQty(line, 1)),
          IconButton(visualDensity: VisualDensity.compact, icon: const Icon(Icons.close, size: 18, color: Bo.danger), onPressed: () => controller.removeLine(line)),
        ],
      ),
    );
  }
}
