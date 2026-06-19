import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const foodCostRoute = '/accounts/reports/food-cost';

/// Accounts → Reports → Food Cost. Consumption-based food-cost % report. Mirrors
/// the Blazor FoodCost.razor page. Read-only.
class FoodCostScreen extends ConsumerWidget {
  const FoodCostScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(foodCostProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Food Cost',
          subtitle: 'Consumption-based COGS and food-cost % over a date range.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(foodCostProvider))],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<FoodCostReport>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(foodCostProvider),
            data: (r) => _body(r),
          ),
        ),
      ],
    );
  }

  Widget _body(FoodCostReport r) {
    final cur = r.currency;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(label: 'Net Sales', value: money(r.netSales, cur), icon: Icons.trending_up, tint: Bo.successSoft),
          KpiCard(label: 'COGS', value: money(r.cogs, cur), icon: Icons.inventory_2, tint: Bo.warningSoft),
          KpiCard(label: 'Food Cost %', value: '${r.foodCostPercent.toStringAsFixed(2)}%', icon: Icons.percent, tint: Bo.primaryTint),
          KpiCard(label: 'Wastage', value: money(r.wastage, cur), icon: Icons.delete_outline, tint: Bo.dangerSoft),
          KpiCard(label: 'Inventory Value', value: money(r.inventoryValue, cur), icon: Icons.warehouse, tint: Bo.infoSoft),
        ]),
        const SizedBox(height: 16),
        SectionCard(
          title: 'COGS by Category',
          child: r.categories.isEmpty
              ? const Text('No consumption in this period.', style: TextStyle(color: Bo.textSubtle))
              : Column(children: [
                  for (final c in r.categories)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Row(children: [
                        Expanded(child: Text(c.category)),
                        Text(money(c.cogs, cur), style: const TextStyle(fontWeight: FontWeight.w600)),
                      ]),
                    ),
                ]),
        ),
      ],
    );
  }
}
