import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'accounts_api.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const cashAccountsRoute = '/accounts/cash-accounts';

const _pageSize = 12;

/// Accounts → Cash Accounts. Each account shows its running balance. Mirrors the
/// Blazor CashAccounts.razor page.
class CashAccountsScreen extends ConsumerStatefulWidget {
  const CashAccountsScreen({super.key});

  @override
  ConsumerState<CashAccountsScreen> createState() => _CashAccountsScreenState();
}

class _CashAccountsScreenState extends ConsumerState<CashAccountsScreen> {
  int _page = 1;

  static const _kinds = ['Cash', 'MobileWallet', 'Bank'];

  String _kindLabel(String k) => switch (k) {
        'MobileWallet' => 'Mobile Wallet',
        _ => k,
      };

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(cashAccountsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Cash Accounts',
          subtitle: 'Where money physically sits — cash, mobile wallets, bank accounts.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Account'),
            ),
            const SizedBox(width: 8),
            RefreshAction(onPressed: () => ref.invalidate(cashAccountsProvider)),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<CashAccount>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(cashAccountsProvider),
            data: (accounts) => _table(accounts),
          ),
        ),
      ],
    );
  }

  Widget _table(List<CashAccount> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();
    final totalBalance = all.fold<double>(0, (s, a) => s + a.balance);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
          child: KpiGrid(children: [
            KpiCard(label: 'Accounts', value: count(all.length), icon: Icons.account_balance, tint: Bo.primaryTint),
            KpiCard(label: 'Total Balance', value: money(totalBalance, 'Tk'), icon: Icons.account_balance_wallet, tint: Bo.successSoft),
          ]),
        ),
        Expanded(
          child: DataTableCard(
            emptyMessage: "No cash accounts yet. Click 'New Account' to add one.",
            columns: const [
              DataColumn(label: Text('Name')),
              DataColumn(label: Text('Kind')),
              DataColumn(label: Text('Opening'), numeric: true),
              DataColumn(label: Text('Balance'), numeric: true),
              DataColumn(label: Text('Status')),
            ],
            rows: [
              for (final a in rows)
                DataRow(cells: [
                  DataCell(Text(a.name, style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(Text(_kindLabel(a.kind), style: const TextStyle(color: Bo.textMuted))),
                  DataCell(Text(money(a.openingBalance, 'Tk'))),
                  DataCell(Text(money(a.balance, 'Tk'),
                      style: const TextStyle(fontWeight: FontWeight.w700))),
                  DataCell(a.isActive
                      ? const ToneChip('Active', 'success')
                      : const ToneChip('Inactive', 'neutral')),
                ]),
            ],
            pager: Pager(
              page: page,
              totalPages: totalPages,
              label: '${all.length} accounts',
              onPage: (p) => setState(() => _page = p),
            ),
          ),
        ),
      ],
    );
  }

  void _openForm(BuildContext context) {
    final nameCtrl = TextEditingController();
    final openingCtrl = TextEditingController(text: '0');
    String kind = 'Cash';

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) => AppFormDialog(
          title: 'New Cash Account',
          icon: Icons.account_balance_outlined,
          onSave: () async {
            final name = nameCtrl.text.trim();
            if (name.isEmpty) throw 'Name is required.';
            await ref.read(staffApiProvider).createCashAccount(
                  name: name,
                  kind: kind,
                  openingBalance: double.tryParse(openingCtrl.text.trim()) ?? 0,
                );
            ref.invalidate(cashAccountsProvider);
            if (context.mounted) AppToast.show(context, 'Cash account created');
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
              FormField2(
                label: 'Kind',
                child: DropdownButtonFormField<String>(
                  initialValue: kind,
                  isExpanded: true,
                  items: [
                    for (final k in _kinds)
                      DropdownMenuItem(value: k, child: Text(_kindLabel(k))),
                  ],
                  onChanged: (v) => setLocal(() => kind = v ?? 'Cash'),
                ),
              ),
              FormField2(
                label: 'Opening balance',
                child: TextField(
                    controller: openingCtrl,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
