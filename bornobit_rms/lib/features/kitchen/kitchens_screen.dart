import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'kitchen_api.dart';
import 'kitchen_models.dart';

const kitchensAdminRoute = '/admin/kitchens';

/// Lists physical kitchens (CRUD) and lets an admin route each station to a kitchen.
/// Mirrors the Blazor /admin/kitchens page. Admin only.
final _kitchensProvider = FutureProvider.autoDispose<List<Kitchen>>(
    (ref) => ref.read(staffApiProvider).kitchens(includeInactive: true));
final _stationsProvider = FutureProvider.autoDispose<List<KitchenStation>>(
    (ref) => ref.read(staffApiProvider).kitchenStations(includeInactive: true));

class KitchensScreen extends ConsumerWidget {
  const KitchensScreen({super.key});

  void _refresh(WidgetRef ref) {
    ref.invalidate(_kitchensProvider);
    ref.invalidate(_stationsProvider);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final kitchensAsync = ref.watch(_kitchensProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Kitchens',
          subtitle:
              'Physical kitchens grouping stations. Each gets its own display and printer, and a separate kitchen ticket per order.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context, ref),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Kitchen'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<Kitchen>>(
            isLoading: kitchensAsync.isLoading,
            error: kitchensAsync.hasError ? kitchensAsync.error : null,
            value: kitchensAsync.valueOrNull,
            onRetry: () => _refresh(ref),
            data: (kitchens) => SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _kitchensTable(context, ref, kitchens),
                  const SizedBox(height: 16),
                  _stationRouting(context, ref, kitchens.where((k) => k.isActive).toList()),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _kitchensTable(BuildContext context, WidgetRef ref, List<Kitchen> kitchens) {
    return DataTableCard(
      emptyMessage: "No kitchens yet. Click 'New Kitchen' to add one.",
      columns: const [
        DataColumn(label: Text('Order')),
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Code')),
        DataColumn(label: Text('Printer')),
        DataColumn(label: Text('Default')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final k in kitchens)
          DataRow(cells: [
            DataCell(Text('${k.displayOrder}')),
            DataCell(Row(mainAxisSize: MainAxisSize.min, children: [
              if (k.colorHex != null) ...[
                CircleAvatar(backgroundColor: _hexColor(k.colorHex!), radius: 6),
                const SizedBox(width: 6),
              ],
              Text(k.name, style: const TextStyle(fontWeight: FontWeight.w700)),
            ])),
            DataCell(Text(k.code ?? '—')),
            DataCell(Text(k.printerName ?? '(default)')),
            DataCell(Text(k.isDefault ? '✓' : '')),
            DataCell(k.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, ref, kitchen: k),
              ),
              IconButton(
                tooltip: k.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(k.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: k.isActive ? Bo.success : Bo.textSubtle),
                onPressed: k.isDefault && k.isActive
                    ? null
                    : () => _toggleActive(context, ref, k),
              ),
            ])),
          ]),
      ],
    );
  }

  Widget _stationRouting(BuildContext context, WidgetRef ref, List<Kitchen> activeKitchens) {
    final stationsAsync = ref.watch(_stationsProvider);
    return SectionCard(
      title: 'Station routing',
      child: stationsAsync.when(
        loading: () => const Padding(
          padding: EdgeInsets.all(24),
          child: Center(child: CircularProgressIndicator()),
        ),
        error: (e, _) => Padding(
          padding: const EdgeInsets.all(16),
          child: Text('$e', style: const TextStyle(color: Bo.danger)),
        ),
        data: (stations) => Column(
          children: [
            const Padding(
              padding: EdgeInsets.only(bottom: 8),
              child: Text(
                'Each station belongs to one kitchen. Unassigned stations fall back to the default kitchen.',
                style: TextStyle(color: Bo.textSubtle, fontSize: 12),
              ),
            ),
            for (final s in stations)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                    ),
                    SizedBox(
                      width: 220,
                      child: DropdownButton<String?>(
                        isExpanded: true,
                        value: activeKitchens.any((k) => k.id == s.kitchenId) ? s.kitchenId : null,
                        hint: const Text('(default kitchen)'),
                        items: [
                          const DropdownMenuItem<String?>(
                            value: null,
                            child: Text('(default kitchen)'),
                          ),
                          for (final k in activeKitchens)
                            DropdownMenuItem<String?>(value: k.id, child: Text(k.name)),
                        ],
                        onChanged: (kid) => _assignStation(context, ref, s, kid),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _assignStation(
      BuildContext context, WidgetRef ref, KitchenStation s, String? kitchenId) async {
    try {
      await ref.read(staffApiProvider).assignStationKitchen(s.id, kitchenId);
      _refresh(ref);
      if (context.mounted) AppToast.show(context, "'${s.name}' routing updated");
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  Future<void> _toggleActive(BuildContext context, WidgetRef ref, Kitchen k) async {
    try {
      await ref.read(staffApiProvider).kitchenSetActive(k.id, !k.isActive);
      _refresh(ref);
      if (context.mounted) {
        AppToast.show(context, k.isActive ? 'Kitchen deactivated' : 'Kitchen activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, WidgetRef ref, {Kitchen? kitchen}) {
    final nameCtrl = TextEditingController(text: kitchen?.name ?? '');
    final codeCtrl = TextEditingController(text: kitchen?.code ?? '');
    final colorCtrl = TextEditingController(text: kitchen?.colorHex ?? '');
    final printerCtrl = TextEditingController(text: kitchen?.printerName ?? '');
    final orderCtrl = TextEditingController(text: kitchen == null ? '' : '${kitchen.displayOrder}');
    final isEdit = kitchen != null;

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Kitchen' : 'New Kitchen',
        icon: Icons.restaurant,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final name = nameCtrl.text.trim();
          final code = codeCtrl.text.trim();
          final color = colorCtrl.text.trim();
          final printer = printerCtrl.text.trim();
          final order = int.tryParse(orderCtrl.text.trim()) ?? kitchen?.displayOrder ?? 0;
          if (isEdit) {
            await api.kitchenUpdate(kitchen.id,
                name: name,
                code: code.isEmpty ? null : code,
                colorHex: color.isEmpty ? null : color,
                printerName: printer.isEmpty ? null : printer,
                displayOrder: order);
          } else {
            await api.kitchenCreate(
                name: name,
                code: code.isEmpty ? null : code,
                colorHex: color.isEmpty ? null : color,
                printerName: printer.isEmpty ? null : printer,
                displayOrder: order);
          }
          _refresh(ref);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Kitchen updated' : 'Kitchen created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
            FormField2(label: 'Code (e.g. MAIN, BAR)', child: TextField(controller: codeCtrl)),
            FormField2(label: 'Accent colour (#hex)', child: TextField(controller: colorCtrl)),
            FormField2(
              label: 'Printer name (blank ⇒ default KOT printer)',
              child: TextField(controller: printerCtrl),
            ),
            FormField2(
              label: 'Display order',
              child: TextField(controller: orderCtrl, keyboardType: TextInputType.number),
            ),
          ],
        ),
      ),
    );
  }

  static Color _hexColor(String hex) {
    var h = hex.replaceAll('#', '').trim();
    if (h.length == 6) h = 'FF$h';
    final v = int.tryParse(h, radix: 16);
    return v == null ? Bo.textSubtle : Color(v);
  }
}
