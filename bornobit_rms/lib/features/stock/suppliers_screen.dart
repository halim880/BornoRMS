import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'stock_api.dart';
import 'stock_models.dart';
import 'stock_providers.dart';

const suppliersRoute = '/stock/suppliers';

/// Stock → Suppliers. CRUD list of inventory suppliers.
/// Mirrors the Blazor Suppliers.razor page.
class SuppliersScreen extends ConsumerStatefulWidget {
  const SuppliersScreen({super.key});

  @override
  ConsumerState<SuppliersScreen> createState() => _SuppliersScreenState();
}

class _SuppliersScreenState extends ConsumerState<SuppliersScreen> {
  static const _pageSize = 15;
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(stockSuppliersProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Suppliers',
          subtitle: 'Vendors you raise purchase orders against.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Supplier'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<Supplier>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(stockSuppliersProvider),
            data: (list) => _table(context, list),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<Supplier> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No suppliers yet. Click 'New Supplier' to add one.",
      columns: const [
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Phone')),
        DataColumn(label: Text('Terms (days)'), numeric: true),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final s in rows)
          DataRow(cells: [
            DataCell(Text(s.code, style: const TextStyle(color: Bo.textSubtle))),
            DataCell(Text(s.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(s.phone?.isNotEmpty == true ? s.phone! : '—',
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text('${s.paymentTermsDays}')),
            DataCell(s.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, supplier: s),
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
        label: '${all.length} suppliers',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, Supplier s) async {
    try {
      await ref.read(staffApiProvider).stockSetSupplierActive(s.id, !s.isActive);
      ref.invalidate(stockSuppliersProvider);
      if (context.mounted) {
        AppToast.show(context, s.isActive ? 'Supplier deactivated' : 'Supplier activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {Supplier? supplier}) {
    final isEdit = supplier != null;
    final codeCtrl = TextEditingController(text: supplier?.code ?? '');
    final nameCtrl = TextEditingController(text: supplier?.name ?? '');
    final phoneCtrl = TextEditingController(text: supplier?.phone ?? '');
    final addressCtrl = TextEditingController(text: supplier?.address ?? '');
    final termsCtrl = TextEditingController(text: '${supplier?.paymentTermsDays ?? 0}');
    final notesCtrl = TextEditingController(text: supplier?.notes ?? '');

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Supplier' : 'New Supplier',
        icon: Icons.local_shipping_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final phone = phoneCtrl.text.trim().isEmpty ? null : phoneCtrl.text.trim();
          final address = addressCtrl.text.trim().isEmpty ? null : addressCtrl.text.trim();
          final notes = notesCtrl.text.trim().isEmpty ? null : notesCtrl.text.trim();
          final terms = int.tryParse(termsCtrl.text.trim()) ?? 0;
          if (isEdit) {
            await api.stockUpdateSupplier(supplier.id,
                name: nameCtrl.text.trim(), phone: phone, address: address, paymentTermsDays: terms, notes: notes);
          } else {
            await api.stockCreateSupplier(
                code: codeCtrl.text.trim(),
                name: nameCtrl.text.trim(),
                phone: phone,
                address: address,
                paymentTermsDays: terms,
                notes: notes);
          }
          ref.invalidate(stockSuppliersProvider);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Supplier updated' : 'Supplier created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (!isEdit) FormField2(label: 'Code', child: TextField(controller: codeCtrl)),
            FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
            FormField2(label: 'Phone', child: TextField(controller: phoneCtrl)),
            FormField2(
                label: 'Address',
                child: TextField(controller: addressCtrl, minLines: 1, maxLines: 3)),
            FormField2(
                label: 'Payment terms (days)',
                child: TextField(controller: termsCtrl, keyboardType: TextInputType.number)),
            FormField2(
                label: 'Notes',
                child: TextField(controller: notesCtrl, minLines: 1, maxLines: 3)),
          ],
        ),
      ),
    );
  }
}
