import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_form_dialog.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'delivery_api.dart';
import 'delivery_models.dart';
import 'delivery_providers.dart';

const ridersRoute = '/logistics/riders';

/// Delivery → Riders. CRUD roster of delivery riders (managed records, not logins).
class RidersScreen extends ConsumerStatefulWidget {
  const RidersScreen({super.key});

  @override
  ConsumerState<RidersScreen> createState() => _RidersScreenState();
}

class _RidersScreenState extends ConsumerState<RidersScreen> {
  @override
  Widget build(BuildContext context) {
    final async = ref.watch(ridersProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Riders',
          subtitle: 'Your delivery roster. Assign riders to orders on the dispatch board.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Rider'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<Rider>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(ridersProvider),
            data: (list) => _table(context, list),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<Rider> all) {
    return DataTableCard(
      emptyMessage: "No riders yet. Click 'New Rider' to add one.",
      columns: const [
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Phone')),
        DataColumn(label: Text('Vehicle')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final r in all)
          DataRow(cells: [
            DataCell(Text(r.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(r.phone, style: const TextStyle(color: Bo.textMuted))),
            DataCell(Text(r.vehicle?.isNotEmpty == true ? r.vehicle! : '—',
                style: const TextStyle(color: Bo.textMuted))),
            DataCell(r.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, rider: r),
              ),
              IconButton(
                tooltip: r.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(r.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: r.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, r),
              ),
            ])),
          ]),
      ],
    );
  }

  Future<void> _toggleActive(BuildContext context, Rider r) async {
    try {
      await ref.read(staffApiProvider).setRiderActive(r.id, !r.isActive);
      ref.invalidate(ridersProvider);
      if (context.mounted) {
        AppToast.show(context, r.isActive ? 'Rider deactivated' : 'Rider activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {Rider? rider}) {
    final isEdit = rider != null;
    final nameCtrl = TextEditingController(text: rider?.name ?? '');
    final phoneCtrl = TextEditingController(text: rider?.phone ?? '');
    final vehicleCtrl = TextEditingController(text: rider?.vehicle ?? '');

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Rider' : 'New Rider',
        icon: Icons.local_shipping_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final vehicle = vehicleCtrl.text.trim().isEmpty ? null : vehicleCtrl.text.trim();
          if (isEdit) {
            await api.updateRider(id: rider.id, name: nameCtrl.text.trim(), phone: phoneCtrl.text.trim(), vehicle: vehicle);
          } else {
            await api.createRider(name: nameCtrl.text.trim(), phone: phoneCtrl.text.trim(), vehicle: vehicle);
          }
          ref.invalidate(ridersProvider);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Rider updated' : 'Rider created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
            FormField2(label: 'Phone', child: TextField(controller: phoneCtrl)),
            FormField2(label: 'Vehicle (optional)', child: TextField(controller: vehicleCtrl)),
          ],
        ),
      ),
    );
  }
}
