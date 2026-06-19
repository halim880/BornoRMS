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

const usersRoute = '/admin/users';

const _pageSize = 12;

class UsersScreen extends ConsumerStatefulWidget {
  const UsersScreen({super.key});

  @override
  ConsumerState<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends ConsumerState<UsersScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(adminUsersProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Users',
          subtitle: 'Manage staff accounts, roles and access.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New User'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<UserRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(adminUsersProvider),
            data: (users) => _table(context, users),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<UserRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No users yet. Click 'New User' to add one.",
      columns: const [
        DataColumn(label: Text('Username')),
        DataColumn(label: Text('Full name')),
        DataColumn(label: Text('Email')),
        DataColumn(label: Text('Roles')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final u in rows)
          DataRow(cells: [
            DataCell(Row(children: [
              Text(u.userName, style: const TextStyle(fontWeight: FontWeight.w700)),
              if (u.isSuperAdmin) ...[
                const SizedBox(width: 6),
                const ToneChip('Super', 'primary'),
              ],
            ])),
            DataCell(Text(u.fullName)),
            DataCell(Text(u.email)),
            DataCell(Text(u.roles.isEmpty ? '—' : u.roles.join(', '))),
            DataCell(u.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: u.isSuperAdmin ? null : () => _openForm(context, user: u),
              ),
              IconButton(
                tooltip: 'Reset password',
                icon: const Icon(Icons.key_outlined, size: 18),
                onPressed: () => _resetPassword(context, u),
              ),
              IconButton(
                tooltip: u.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(u.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: u.isActive ? Bo.success : Bo.textSubtle),
                onPressed: u.isSuperAdmin ? null : () => _toggleActive(context, u),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} users',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, UserRow u) async {
    try {
      await ref.read(staffApiProvider).adminSetUserActive(u.id, !u.isActive);
      ref.invalidate(adminUsersProvider);
      if (context.mounted) {
        AppToast.show(context, u.isActive ? 'User deactivated' : 'User activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  Future<void> _resetPassword(BuildContext context, UserRow u) async {
    try {
      final pwd = await ref.read(staffApiProvider).adminResetPassword(u.id);
      if (context.mounted) _showPassword(context, u.userName, pwd);
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _showPassword(BuildContext context, String userName, String password) {
    showDialog<void>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('New password'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Temporary password for "$userName":',
                style: const TextStyle(color: Bo.textMuted)),
            const SizedBox(height: 10),
            SelectableText(password,
                style: const TextStyle(
                    fontFamily: 'monospace', fontWeight: FontWeight.w700, fontSize: 16)),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
        ],
      ),
    );
  }

  void _openForm(BuildContext context, {UserRow? user}) {
    final userCtrl = TextEditingController(text: user?.userName ?? '');
    final emailCtrl = TextEditingController(text: user?.email ?? '');
    final nameCtrl = TextEditingController(text: user?.fullName ?? '');
    final pwdCtrl = TextEditingController();
    final selected = <String>{...?user?.roles};
    final isEdit = user != null;

    final rolesAsync = ref.read(adminRolesProvider);
    final roleNames = (rolesAsync.valueOrNull ?? [])
        .where((r) => !r.isSystem || r.name != 'SuperAdmin')
        .map((r) => r.name)
        .where((n) => n != 'SuperAdmin')
        .toList();

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) => AppFormDialog(
          title: isEdit ? 'Edit User' : 'New User',
          icon: Icons.person_outline,
          onSave: () async {
            final api = ref.read(staffApiProvider);
            final roles = selected.toList();
            if (isEdit) {
              await api.adminUpdateUser(user.id,
                  userName: userCtrl.text.trim(),
                  email: emailCtrl.text.trim(),
                  fullName: nameCtrl.text.trim(),
                  roles: roles);
              ref.invalidate(adminUsersProvider);
              if (context.mounted) AppToast.show(context, 'User updated');
            } else {
              final generated = await api.adminCreateUser(
                  userName: userCtrl.text.trim(),
                  email: emailCtrl.text.trim(),
                  fullName: nameCtrl.text.trim(),
                  roles: roles,
                  initialPassword: pwdCtrl.text.trim());
              ref.invalidate(adminUsersProvider);
              if (context.mounted) {
                if (generated != null && pwdCtrl.text.trim().isEmpty) {
                  _showPassword(context, userCtrl.text.trim(), generated);
                } else {
                  AppToast.show(context, 'User created');
                }
              }
            }
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(label: 'Username', child: TextField(controller: userCtrl)),
              FormField2(
                  label: 'Email',
                  child: TextField(
                      controller: emailCtrl, keyboardType: TextInputType.emailAddress)),
              FormField2(label: 'Full name', child: TextField(controller: nameCtrl)),
              if (!isEdit)
                FormField2(
                  label: 'Initial password (optional — generated if blank)',
                  child: TextField(controller: pwdCtrl, obscureText: true),
                ),
              FormField2(
                label: 'Roles',
                child: roleNames.isEmpty
                    ? const Text('No assignable roles.', style: TextStyle(color: Bo.textSubtle))
                    : Wrap(
                        spacing: 8,
                        runSpacing: 4,
                        children: [
                          for (final r in roleNames)
                            FilterChip(
                              label: Text(r),
                              selected: selected.contains(r),
                              onSelected: (v) => setLocal(() {
                                if (v) {
                                  selected.add(r);
                                } else {
                                  selected.remove(r);
                                }
                              }),
                            ),
                        ],
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
