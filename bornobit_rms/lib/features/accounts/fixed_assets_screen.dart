import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const fixedAssetsRoute = '/accounts/fixed-assets';

const _pageSize = 12;

/// Accounts → Fixed Assets. Registered assets with cost, accumulated depreciation
/// and net book value. Mirrors the Blazor FixedAssets.razor page. Read-only here.
class FixedAssetsScreen extends ConsumerStatefulWidget {
  const FixedAssetsScreen({super.key});

  @override
  ConsumerState<FixedAssetsScreen> createState() => _FixedAssetsScreenState();
}

class _FixedAssetsScreenState extends ConsumerState<FixedAssetsScreen> {
  int _page = 1;

  String _statusLabel(String s) => s == 'FullyDepreciated' ? 'Fully Depreciated' : s;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(fixedAssetsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Fixed Assets',
          subtitle: 'Registered assets with straight-line depreciation and net book value.',
          actions: [RefreshAction(onPressed: () => ref.invalidate(fixedAssetsProvider))],
        ),
        Expanded(
          child: AsyncStateView<List<FixedAsset>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(fixedAssetsProvider),
            data: (rows) => _table(rows),
          ),
        ),
      ],
    );
  }

  Widget _table(List<FixedAsset> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();
    final totalCost = all.fold<double>(0, (s, a) => s + a.cost);
    final totalNbv = all.fold<double>(0, (s, a) => s + a.netBookValue);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Assets', value: count(all.length), icon: Icons.chair_alt, tint: Bo.primaryTint),
            KpiCard(label: 'Total Cost', value: money(totalCost, 'Tk'), icon: Icons.sell, tint: Bo.infoSoft),
            KpiCard(label: 'Net Book Value', value: money(totalNbv, 'Tk'), icon: Icons.account_balance, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: 'No fixed assets registered.',
            columns: const [
              DataColumn(label: Text('Number')),
              DataColumn(label: Text('Name')),
              DataColumn(label: Text('GL Account')),
              DataColumn(label: Text('Acquired')),
              DataColumn(label: Text('Cost'), numeric: true),
              DataColumn(label: Text('Accum. Dep.'), numeric: true),
              DataColumn(label: Text('NBV'), numeric: true),
              DataColumn(label: Text('Status')),
            ],
            rows: [
              for (final a in rows)
                DataRow(cells: [
                  DataCell(Text(a.assetNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(Text(a.name)),
                  DataCell(Text(a.glAccountName, style: const TextStyle(color: Bo.textMuted))),
                  DataCell(Text(shortDate(a.acquisitionDate))),
                  DataCell(Text(money(a.cost, 'Tk'))),
                  DataCell(Text(money(a.accumulatedDepreciation, 'Tk'),
                      style: const TextStyle(color: Bo.danger))),
                  DataCell(Text(money(a.netBookValue, 'Tk'),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(ToneChip(_statusLabel(a.status), accountsStatusTone(a.status))),
                ]),
            ],
            pager: Pager(
              page: page,
              totalPages: totalPages,
              label: '${all.length} assets',
              onPage: (p) => setState(() => _page = p),
            ),
          ),
        ),
      ],
    );
  }
}
