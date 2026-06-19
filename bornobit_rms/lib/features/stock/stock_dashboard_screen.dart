import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const stockDashboardRoute = '/stock/dashboard';

/// Stock → Dashboard. KPI cards + low-stock / out-of-stock / movers lists.
/// Mirrors the Blazor StockDashboard.razor page.
class StockDashboardScreen extends ConsumerWidget {
  const StockDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(stockDashboardProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Stock Dashboard',
          subtitle: 'Inventory health at a glance — valuation, low stock and consumption.',
          actions: [
            IconButton(
              tooltip: 'Refresh',
              onPressed: () => ref.invalidate(stockDashboardProvider),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<StockDashboard>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockDashboardProvider),
            data: (d) => _body(d),
          ),
        ),
      ],
    );
  }

  Widget _body(StockDashboard d) {
    const cur = 'Tk';
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(
            label: 'Stock Value',
            value: money(d.valuation.totalValue, cur),
            icon: Icons.account_balance_wallet,
            tint: Bo.successSoft,
          ),
          KpiCard(
            label: 'Items',
            value: count(d.summary.itemCount),
            icon: Icons.inventory_2,
            tint: Bo.primaryTint,
          ),
          KpiCard(
            label: 'Low Stock',
            value: count(d.summary.lowStockCount),
            icon: Icons.warning_amber,
            tint: Bo.warningSoft,
          ),
          KpiCard(
            label: 'Waste %',
            value: '${d.waste.overallPercent}%',
            icon: Icons.delete_outline,
            tint: Bo.dangerSoft,
          ),
        ]),
        const SizedBox(height: 16),
        SectionCard(
          title: 'Low Stock',
          icon: Icons.warning_amber,
          child: d.lowStock.isEmpty
              ? const EmptyStateInline('Nothing below reorder level.')
              : Column(
                  children: [
                    for (final i in d.lowStock.take(15))
                      _row(i.name, '${i.qtyOnHand} / ${i.reorderLevel} ${i.unitCode}', 'warning'),
                  ],
                ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: 'Out of Stock',
          icon: Icons.remove_shopping_cart,
          child: d.outOfStock.isEmpty
              ? const EmptyStateInline('Everything is in stock.')
              : Column(
                  children: [
                    for (final o in d.outOfStock.take(15))
                      _row(o.name, '${o.currentStock} ${o.unitCode}', 'danger'),
                  ],
                ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: 'Top Consumed Ingredients',
          icon: Icons.local_fire_department,
          child: d.consumption.isEmpty
              ? const EmptyStateInline('No consumption recorded yet.')
              : Column(
                  children: [
                    for (final c in d.consumption.take(10))
                      _row(c.name, '${c.qtyConsumed} ${c.unitCode} · ${money(c.value, cur)}', 'info'),
                  ],
                ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: 'Stock Value by Category',
          icon: Icons.donut_small,
          child: d.valuation.byCategory.isEmpty
              ? const EmptyStateInline('No valuation data.')
              : Column(
                  children: [
                    for (final c in d.valuation.byCategory)
                      _row(c.categoryName, money(c.value, cur), 'primary'),
                  ],
                ),
        ),
      ],
    );
  }

  Widget _row(String label, String value, String tone) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(child: Text(label, style: const TextStyle(color: Bo.text))),
          const SizedBox(width: 8),
          ToneChip(value, tone),
        ],
      ),
    );
  }
}

/// A compact inline empty placeholder for use inside a [SectionCard].
class EmptyStateInline extends StatelessWidget {
  final String message;
  const EmptyStateInline(this.message, {super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Text(message, style: const TextStyle(color: Bo.textSubtle)),
    );
  }
}
