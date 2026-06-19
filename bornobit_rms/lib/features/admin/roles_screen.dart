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

// NOTE: never name this `Roles` — that shadows the domain Roles type.
const rolesAdminRoute = '/admin/roles';

const _pageSize = 12;

class RolesAdminScreen extends ConsumerStatefulWidget {
  const RolesAdminScreen({super.key});

  @override
  ConsumerState<RolesAdminScreen> createState() => _RolesAdminScreenState();
}

class _RolesAdminScreenState extends ConsumerState<RolesAdminScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(adminRolesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Roles',
          subtitle: 'Manage roles. System roles cannot be renamed or deleted.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Role'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<RoleRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(adminRolesProvider),
            data: (roles) => _table(context, roles),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<RoleRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No roles yet. Click 'New Role' to add one.",
      columns: const [
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Description')),
        DataColumn(label: Text('Users')),
        DataColumn(label: Text('Type')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final r in rows)
          DataRow(cells: [
            DataCell(Text(r.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(r.description ?? '—')),
            DataCell(Text('${r.userCount}')),
            DataCell(r.isSystem
                ? const ToneChip('System', 'info')
                : const ToneChip('Custom', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, role: r),
              ),
              IconButton(
                tooltip: r.isSystem ? 'System roles cannot be deleted' : 'Delete',
                icon: const Icon(Icons.delete_outline, size: 18),
                color: r.isSystem ? Bo.textSubtle : Bo.danger,
                onPressed: r.isSystem ? null : () => _delete(context, r),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} roles',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _delete(BuildContext context, RoleRow r) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Delete role'),
        content: Text('Delete role "${r.name}"? This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Delete')),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(staffApiProvider).adminDeleteRole(r.id);
      ref.invalidate(adminRolesProvider);
      if (context.mounted) AppToast.show(context, 'Role deleted');
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {RoleRow? role}) {
    final nameCtrl = TextEditingController(text: role?.name ?? '');
    final descCtrl = TextEditingController(text: role?.description ?? '');
    final isEdit = role != null;
    final lockName = role?.isSystem ?? false;

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Role' : 'New Role',
        icon: Icons.shield_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final name = nameCtrl.text.trim();
          final desc = descCtrl.text.trim();
          if (isEdit) {
            await api.adminUpdateRole(role.id, name: name, description: desc);
          } else {
            await api.adminCreateRole(name: name, description: desc);
          }
          ref.invalidate(adminRolesProvider);
          if (context.mounted) AppToast.show(context, isEdit ? 'Role updated' : 'Role created');
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(
              label: 'Name',
              child: TextField(controller: nameCtrl, enabled: !lockName),
            ),
            FormField2(
              label: 'Description',
              child: TextField(controller: descCtrl, maxLines: 2),
            ),
          ],
        ),
      ),
    );
  }
}
