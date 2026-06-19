import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'admin_api.dart';
import 'admin_models.dart';
import 'admin_providers.dart';

const numberingScopesRoute = '/admin/numbering-scopes';

const _pageSize = 12;

class NumberingScopesScreen extends ConsumerStatefulWidget {
  const NumberingScopesScreen({super.key});

  @override
  ConsumerState<NumberingScopesScreen> createState() => _NumberingScopesScreenState();
}

class _NumberingScopesScreenState extends ConsumerState<NumberingScopesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(numberingScopesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Numbering Scopes',
          subtitle: 'Document numbering: prefix, reset cadence and zero-padded width.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Scope'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<NumberingScopeRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(numberingScopesProvider),
            data: (scopes) => _table(context, scopes),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<NumberingScopeRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No numbering scopes yet. Click 'New Scope' to add one.",
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Prefix')),
        DataColumn(label: Text('Cadence')),
        DataColumn(label: Text('Digits')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final s in rows)
          DataRow(cells: [
            DataCell(Text(s.code, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(s.name)),
            DataCell(Text(s.prefix)),
            DataCell(Text(s.cadence.label)),
            DataCell(Text('${s.digits}')),
            DataCell(s.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, scope: s),
              ),
              IconButton(
                tooltip: s.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(s.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: s.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, s),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} scopes',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, NumberingScopeRow s) async {
    try {
      await ref.read(staffApiProvider).adminSetNumberingScopeActive(s.id, !s.isActive);
      ref.invalidate(numberingScopesProvider);
      if (context.mounted) {
        AppToast.show(context, s.isActive ? 'Scope deactivated' : 'Scope activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {NumberingScopeRow? scope}) {
    final codeCtrl = TextEditingController(text: scope?.code ?? '');
    final nameCtrl = TextEditingController(text: scope?.name ?? '');
    final prefixCtrl = TextEditingController(text: scope?.prefix ?? '');
    final digitsCtrl = TextEditingController(text: '${scope?.digits ?? 4}');
    var cadence = scope?.cadence ?? NumberingCadence.yearly;
    var resetByOutlet = scope?.resetByOutlet ?? false;
    final isEdit = scope != null;

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) => AppFormDialog(
          title: isEdit ? 'Edit Scope' : 'New Scope',
          icon: Icons.tag_outlined,
          onSave: () async {
            final api = ref.read(staffApiProvider);
            final digits = int.tryParse(digitsCtrl.text.trim()) ?? 4;
            if (isEdit) {
              await api.adminUpdateNumberingScope(scope.id,
                  name: nameCtrl.text.trim(),
                  prefix: prefixCtrl.text.trim(),
                  cadence: cadence,
                  digits: digits,
                  resetByOutlet: resetByOutlet);
            } else {
              await api.adminCreateNumberingScope(
                  code: codeCtrl.text.trim(),
                  name: nameCtrl.text.trim(),
                  prefix: prefixCtrl.text.trim(),
                  cadence: cadence,
                  digits: digits,
                  resetByOutlet: resetByOutlet);
            }
            ref.invalidate(numberingScopesProvider);
            if (context.mounted) AppToast.show(context, isEdit ? 'Scope updated' : 'Scope created');
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(
                label: 'Code',
                child: TextField(controller: codeCtrl, enabled: !isEdit),
              ),
              FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
              FormField2(label: 'Prefix', child: TextField(controller: prefixCtrl)),
              FormField2(
                label: 'Cadence',
                child: DropdownButtonFormField<NumberingCadence>(
                  initialValue: cadence,
                  items: [
                    for (final c in NumberingCadence.values)
                      DropdownMenuItem(value: c, child: Text(c.label)),
                  ],
                  onChanged: (v) => setLocal(() => cadence = v ?? cadence),
                ),
              ),
              FormField2(
                label: 'Digits (zero-padded width)',
                child: TextField(controller: digitsCtrl, keyboardType: TextInputType.number),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Reset numbering per outlet'),
                value: resetByOutlet,
                onChanged: (v) => setLocal(() => resetByOutlet = v),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
