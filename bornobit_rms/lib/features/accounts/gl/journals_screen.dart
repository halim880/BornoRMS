import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/dtos.dart';
import '../../../core/providers/providers.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/app_form_dialog.dart';
import '../../../core/widgets/app_page.dart';
import '../../../core/widgets/app_toast.dart';
import '../../dashboard/widgets.dart';
import '../accounts_api.dart';
import '../accounts_models.dart';
import '../accounts_providers.dart';
import '../widgets.dart';

const journalsRoute = '/accounts/gl/journal';

/// Accounts → GL → Journal. Paged journal entries with a create-entry dialog.
/// Mirrors the Blazor Journals.razor page.
class JournalsScreen extends ConsumerWidget {
  const JournalsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(journalEntriesProvider);

    return Column(
      children: [
        PageHeader(
          title: 'Journal',
          subtitle: 'Double-entry journal entries over a date range.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context, ref),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Entry'),
            ),
            const SizedBox(width: 8),
            RefreshAction(onPressed: () => ref.invalidate(journalEntriesProvider)),
          ],
        ),
        const AccountsRangeSelector(),
        Expanded(
          child: AsyncStateView<Paged<JournalEntryRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(journalEntriesProvider),
            data: (paged) => _table(ref, paged),
          ),
        ),
      ],
    );
  }

  Widget _table(WidgetRef ref, Paged<JournalEntryRow> paged) {
    return DataTableCard(
      emptyMessage: 'No journal entries in this period.',
      columns: const [
        DataColumn(label: Text('Entry #')),
        DataColumn(label: Text('Date')),
        DataColumn(label: Text('Voucher')),
        DataColumn(label: Text('Reference')),
        DataColumn(label: Text('Narration')),
        DataColumn(label: Text('Debit'), numeric: true),
        DataColumn(label: Text('Credit'), numeric: true),
        DataColumn(label: Text('Lines'), numeric: true),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final e in paged.items)
          DataRow(cells: [
            DataCell(Text(e.entryNumber, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(shortDate(e.entryDate))),
            DataCell(Text(e.voucherType, style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(e.reference?.isNotEmpty == true ? e.reference! : '—',
                style: const TextStyle(color: Bo.textSubtle))),
            DataCell(SizedBox(
                width: 200,
                child: Text(e.narration?.isNotEmpty == true ? e.narration! : '—',
                    overflow: TextOverflow.ellipsis))),
            DataCell(Text(money(e.totalDebit, 'Tk'))),
            DataCell(Text(money(e.totalCredit, 'Tk'))),
            DataCell(Text('${e.lineCount}')),
            DataCell(ToneChip(e.status, accountsStatusTone(e.status))),
          ]),
      ],
      pager: Pager(
        page: paged.page,
        totalPages: paged.totalPages,
        label: '${paged.totalCount} entries',
        onPage: (p) => ref.read(journalPageProvider.notifier).state = p,
      ),
    );
  }

  void _openForm(BuildContext context, WidgetRef ref) {
    final refCtrl = TextEditingController();
    final narrationCtrl = TextEditingController();
    String voucherType = 'Journal';
    bool postImmediately = true;
    DateTime entryDate = DateTime.now();
    // Start with two empty lines (a journal needs at least two).
    final lines = <_JournalLineDraft>[_JournalLineDraft(), _JournalLineDraft()];

    final accounts = ref.read(postableAccountsProvider).valueOrNull ?? const <GlAccount>[];

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) {
          double totalDr = 0, totalCr = 0;
          for (final l in lines) {
            totalDr += double.tryParse(l.debit.text.trim()) ?? 0;
            totalCr += double.tryParse(l.credit.text.trim()) ?? 0;
          }
          final balanced = totalDr == totalCr && totalDr > 0;

          return AppFormDialog(
            title: 'New Journal Entry',
            icon: Icons.menu_book_outlined,
            maxWidth: 720,
            onSave: () async {
              final payload = <Map<String, dynamic>>[];
              for (final l in lines) {
                if (l.accountId == null) continue;
                final dr = double.tryParse(l.debit.text.trim()) ?? 0;
                final cr = double.tryParse(l.credit.text.trim()) ?? 0;
                if (dr == 0 && cr == 0) continue;
                payload.add({
                  'accountId': l.accountId,
                  'debit': dr,
                  'credit': cr,
                  if (l.narration.text.trim().isNotEmpty) 'narration': l.narration.text.trim(),
                });
              }
              if (payload.length < 2) throw 'A journal entry needs at least two lines.';
              if (!balanced) throw 'The entry is not balanced (debits must equal credits).';
              await ref.read(staffApiProvider).createJournalEntry(
                    entryDate: entryDate,
                    voucherType: voucherType,
                    reference: refCtrl.text.trim().isEmpty ? null : refCtrl.text.trim(),
                    narration: narrationCtrl.text.trim().isEmpty ? null : narrationCtrl.text.trim(),
                    lines: payload,
                    postImmediately: postImmediately,
                  );
              ref.invalidate(journalEntriesProvider);
              if (context.mounted) AppToast.show(context, 'Journal entry created');
              return true;
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(children: [
                  Expanded(
                    child: FormField2(
                      label: 'Voucher type',
                      child: DropdownButtonFormField<String>(
                        initialValue: voucherType,
                        isExpanded: true,
                        items: const [
                          DropdownMenuItem(value: 'Journal', child: Text('Journal')),
                          DropdownMenuItem(value: 'Payment', child: Text('Payment')),
                          DropdownMenuItem(value: 'Receipt', child: Text('Receipt')),
                          DropdownMenuItem(value: 'Contra', child: Text('Contra')),
                        ],
                        onChanged: (v) => setLocal(() => voucherType = v ?? 'Journal'),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FormField2(
                      label: 'Date',
                      child: InkWell(
                        onTap: () async {
                          final picked = await showDatePicker(
                            context: ctx,
                            initialDate: entryDate,
                            firstDate: DateTime(2000),
                            lastDate: DateTime(2100),
                          );
                          if (picked != null) setLocal(() => entryDate = picked);
                        },
                        child: InputDecorator(
                          decoration: const InputDecoration(),
                          child: Text(shortDate(entryDate)),
                        ),
                      ),
                    ),
                  ),
                ]),
                FormField2(label: 'Reference', child: TextField(controller: refCtrl)),
                FormField2(
                    label: 'Narration',
                    child: TextField(controller: narrationCtrl, minLines: 1, maxLines: 2)),
                const SizedBox(height: 4),
                const Text('Lines', style: TextStyle(fontWeight: FontWeight.w700, color: Bo.textMuted)),
                const SizedBox(height: 6),
                for (var i = 0; i < lines.length; i++)
                  _lineRow(ctx, setLocal, lines, i, accounts),
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton.icon(
                    onPressed: () => setLocal(() => lines.add(_JournalLineDraft())),
                    icon: const Icon(Icons.add, size: 16),
                    label: const Text('Add line'),
                  ),
                ),
                const Divider(),
                Row(children: [
                  const Expanded(child: Text('Totals', style: TextStyle(fontWeight: FontWeight.w800))),
                  Text('Dr ${money(totalDr, 'Tk')}   Cr ${money(totalCr, 'Tk')}',
                      style: TextStyle(
                          fontWeight: FontWeight.w800,
                          color: balanced ? Bo.success : Bo.danger)),
                ]),
                if (!balanced)
                  const Padding(
                    padding: EdgeInsets.only(top: 4),
                    child: Text('Debits must equal credits and be greater than zero.',
                        style: TextStyle(color: Bo.danger, fontSize: 12)),
                  ),
                Row(children: [
                  Checkbox(
                    value: postImmediately,
                    onChanged: (v) => setLocal(() => postImmediately = v ?? false),
                  ),
                  const Text('Post immediately'),
                ]),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _lineRow(BuildContext ctx, void Function(void Function()) setLocal,
      List<_JournalLineDraft> lines, int i, List<GlAccount> accounts) {
    final l = lines[i];
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            flex: 4,
            child: DropdownButtonFormField<String>(
              initialValue: l.accountId,
              isExpanded: true,
              decoration: const InputDecoration(isDense: true, hintText: 'Account'),
              items: [
                for (final a in accounts)
                  DropdownMenuItem(value: a.id, child: Text('${a.code} ${a.name}', overflow: TextOverflow.ellipsis)),
              ],
              onChanged: (v) => setLocal(() => l.accountId = v),
            ),
          ),
          const SizedBox(width: 6),
          Expanded(
            flex: 2,
            child: TextField(
              controller: l.debit,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(isDense: true, hintText: 'Debit'),
              onChanged: (_) => setLocal(() {}),
            ),
          ),
          const SizedBox(width: 6),
          Expanded(
            flex: 2,
            child: TextField(
              controller: l.credit,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(isDense: true, hintText: 'Credit'),
              onChanged: (_) => setLocal(() {}),
            ),
          ),
          IconButton(
            tooltip: 'Remove',
            icon: const Icon(Icons.close, size: 18),
            onPressed: lines.length > 2 ? () => setLocal(() => lines.removeAt(i)) : null,
          ),
        ],
      ),
    );
  }
}

class _JournalLineDraft {
  String? accountId;
  final TextEditingController debit = TextEditingController();
  final TextEditingController credit = TextEditingController();
  final TextEditingController narration = TextEditingController();
}
