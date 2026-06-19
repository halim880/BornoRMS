import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'accounts_api.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const accountCategoriesRoute = '/accounts/categories';

const _pageSize = 14;

/// Accounts → Categories. Income / expense categories that classify transactions.
/// Mirrors the Blazor Categories.razor page.
class AccountCategoriesScreen extends ConsumerStatefulWidget {
  const AccountCategoriesScreen({super.key});

  @override
  ConsumerState<AccountCategoriesScreen> createState() => _AccountCategoriesScreenState();
}

class _AccountCategoriesScreenState extends ConsumerState<AccountCategoriesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(financeCategoriesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Categories',
          subtitle: 'Income and expense categories that classify cash-book transactions.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Category'),
            ),
            const SizedBox(width: 8),
            RefreshAction(onPressed: () => ref.invalidate(financeCategoriesProvider)),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<FinanceCategory>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(financeCategoriesProvider),
            data: (cats) => _table(cats),
          ),
        ),
      ],
    );
  }

  Widget _table(List<FinanceCategory> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No categories yet. Click 'New Category' to add one.",
      columns: const [
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Status')),
      ],
      rows: [
        for (final c in rows)
          DataRow(cells: [
            DataCell(Text(c.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(ToneChip(c.type, financeTypeTone(c.type))),
            DataCell(c.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} categories',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  void _openForm(BuildContext context) {
    final nameCtrl = TextEditingController();
    String type = 'Expense';

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) => AppFormDialog(
          title: 'New Category',
          icon: Icons.category_outlined,
          onSave: () async {
            final name = nameCtrl.text.trim();
            if (name.isEmpty) throw 'Name is required.';
            await ref.read(staffApiProvider).createCategory(name: name, type: type);
            ref.invalidate(financeCategoriesProvider);
            if (context.mounted) AppToast.show(context, 'Category created');
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
              FormField2(
                label: 'Type',
                child: SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'Income', label: Text('Income')),
                    ButtonSegment(value: 'Expense', label: Text('Expense')),
                  ],
                  selected: {type},
                  onSelectionChanged: (s) => setLocal(() => type = s.first),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
