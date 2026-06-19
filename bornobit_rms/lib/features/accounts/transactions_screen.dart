import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
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

const transactionsRoute = '/accounts/transactions';

/// Accounts → Transactions. Filterable cash-book transactions with period summary
/// cards. Mirrors the Blazor Transactions.razor page.
class TransactionsScreen extends ConsumerWidget {
  const TransactionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(transactionsProvider);
    final summary = ref.watch(transactionsSummaryProvider);

    void reload() {
      ref.invalidate(transactionsProvider);
      ref.invalidate(transactionsSummaryProvider);
    }

    return Column(
      children: [
        PageHeader(
          title: 'Transactions',
          subtitle: 'Cash-book income and expense entries over a date range.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context, ref),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Transaction'),
            ),
            const SizedBox(width: 8),
            RefreshAction(onPressed: reload),
          ],
        ),
        const AccountsRangeSelector(),
        summary.maybeWhen(
          data: (s) => Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: KpiGrid(children: [
              KpiCard(label: 'Income', value: money(s.totalIncome, 'Tk'), icon: Icons.south_west, tint: Bo.successSoft),
              KpiCard(label: 'Expense', value: money(s.totalExpense, 'Tk'), icon: Icons.north_east, tint: Bo.dangerSoft),
              KpiCard(label: 'Net', value: money(s.net, 'Tk'), icon: Icons.account_balance_wallet, tint: Bo.primaryTint),
            ]),
          ),
          orElse: () => const SizedBox.shrink(),
        ),
        Expanded(
          child: AsyncStateView<Paged<TransactionRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: reload,
            data: (paged) => _table(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _table(WidgetRef ref, Paged<TransactionRow> paged) {
    return DataTableCard(
      emptyMessage: 'No transactions in this period.',
      columns: const [
        DataColumn(label: Text('Number')),
        DataColumn(label: Text('Date')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Category')),
        DataColumn(label: Text('Account')),
        DataColumn(label: Text('Amount'), numeric: true),
        DataColumn(label: Text('Reference')),
      ],
      rows: [
        for (final t in paged.items)
          DataRow(cells: [
            DataCell(Text(t.number, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(shortDate(t.occurredOn))),
            DataCell(ToneChip(t.type, financeTypeTone(t.type))),
            DataCell(Text(t.categoryName)),
            DataCell(Text(t.cashAccountName, style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(money(t.amount, 'Tk'),
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    color: t.type == 'Income' ? Bo.success : Bo.danger))),
            DataCell(Text(t.reference?.isNotEmpty == true ? t.reference! : '—',
                style: const TextStyle(color: Bo.textSubtle))),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} transactions',
        onPage: (p) => ref.read(transactionsPageProvider.notifier).state = p,
      ),
    );
  }

  void _openForm(BuildContext context, WidgetRef ref) {
    final amountCtrl = TextEditingController();
    final refCtrl = TextEditingController();
    final notesCtrl = TextEditingController();
    String type = 'Expense';
    String? categoryId;
    String? cashAccountId;
    DateTime occurredOn = DateTime.now();

    final cats = ref.read(financeCategoriesProvider).valueOrNull ?? const <FinanceCategory>[];
    final accts = ref.read(cashAccountsProvider).valueOrNull ?? const <CashAccount>[];

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) {
          final typeCats = cats.where((c) => c.type == type && c.isActive).toList();
          return AppFormDialog(
            title: 'New Transaction',
            icon: Icons.receipt_long_outlined,
            onSave: () async {
              if (categoryId == null) throw 'Pick a category.';
              if (cashAccountId == null) throw 'Pick a cash account.';
              final amount = double.tryParse(amountCtrl.text.trim()) ?? 0;
              if (amount <= 0) throw 'Amount must be greater than zero.';
              await ref.read(staffApiProvider).createTransaction(
                    occurredOn: occurredOn,
                    type: type,
                    cashAccountId: cashAccountId!,
                    categoryId: categoryId!,
                    amount: amount,
                    reference: refCtrl.text.trim().isEmpty ? null : refCtrl.text.trim(),
                    notes: notesCtrl.text.trim().isEmpty ? null : notesCtrl.text.trim(),
                  );
              ref.invalidate(transactionsProvider);
              ref.invalidate(transactionsSummaryProvider);
              ref.invalidate(cashAccountsProvider);
              if (context.mounted) AppToast.show(context, 'Transaction created');
              return true;
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                FormField2(
                  label: 'Type',
                  child: SegmentedButton<String>(
                    segments: const [
                      ButtonSegment(value: 'Income', label: Text('Income')),
                      ButtonSegment(value: 'Expense', label: Text('Expense')),
                    ],
                    selected: {type},
                    onSelectionChanged: (s) => setLocal(() {
                      type = s.first;
                      categoryId = null;
                    }),
                  ),
                ),
                FormField2(
                  label: 'Category',
                  child: DropdownButtonFormField<String>(
                    initialValue: categoryId,
                    isExpanded: true,
                    items: [
                      for (final c in typeCats)
                        DropdownMenuItem(value: c.id, child: Text(c.name)),
                    ],
                    onChanged: (v) => setLocal(() => categoryId = v),
                  ),
                ),
                FormField2(
                  label: 'Cash account',
                  child: DropdownButtonFormField<String>(
                    initialValue: cashAccountId,
                    isExpanded: true,
                    items: [
                      for (final a in accts.where((a) => a.isActive))
                        DropdownMenuItem(value: a.id, child: Text(a.name)),
                    ],
                    onChanged: (v) => setLocal(() => cashAccountId = v),
                  ),
                ),
                FormField2(
                  label: 'Amount',
                  child: TextField(
                      controller: amountCtrl,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true)),
                ),
                FormField2(
                  label: 'Date',
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: ctx,
                        initialDate: occurredOn,
                        firstDate: DateTime(2000),
                        lastDate: DateTime(2100),
                      );
                      if (picked != null) setLocal(() => occurredOn = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(),
                      child: Text(shortDate(occurredOn)),
                    ),
                  ),
                ),
                FormField2(label: 'Reference', child: TextField(controller: refCtrl)),
                FormField2(
                    label: 'Notes',
                    child: TextField(controller: notesCtrl, minLines: 1, maxLines: 3)),
              ],
            ),
          );
        },
      ),
    );
  }
}
