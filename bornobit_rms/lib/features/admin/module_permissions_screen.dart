import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import 'admin_api.dart';
import 'admin_models.dart';
import 'admin_providers.dart';

const modulePermissionsRoute = '/admin/module-permissions';

class ModulePermissionsScreen extends ConsumerStatefulWidget {
  const ModulePermissionsScreen({super.key});

  @override
  ConsumerState<ModulePermissionsScreen> createState() => _ModulePermissionsScreenState();
}

class _ModulePermissionsScreenState extends ConsumerState<ModulePermissionsScreen> {
  String? _roleId;
  final Set<String> _permitted = {};
  bool _saving = false;
  bool _hydrated = false;

  @override
  Widget build(BuildContext context) {
    final rolesAsync = ref.watch(permissionRolesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Module Permissions',
          subtitle: 'Grant a role access to top-level modules (navigation groups).',
        ),
        Expanded(
          child: AsyncStateView<List<PermissionRole>>(
            isLoading: rolesAsync.isLoading,
            error: rolesAsync.hasError ? rolesAsync.error : null,
            value: rolesAsync.valueOrNull,
            onRetry: () => ref.invalidate(permissionRolesProvider),
            data: (roles) => _body(context, roles),
          ),
        ),
      ],
    );
  }

  Widget _body(BuildContext context, List<PermissionRole> roles) {
    _roleId ??= roles.isEmpty ? null : roles.first.id;
    if (_roleId == null) {
      return const EmptyState(message: 'No roles available.');
    }

    final listAsync = ref.watch(modulePermissionsProvider(_roleId!));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: Row(
            children: [
              const Text('Role:', style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(width: 10),
              DropdownButton<String>(
                value: _roleId,
                items: [
                  for (final r in roles) DropdownMenuItem(value: r.id, child: Text(r.name)),
                ],
                onChanged: (v) => setState(() {
                  _roleId = v;
                  _hydrated = false;
                  _permitted.clear();
                }),
              ),
              const Spacer(),
              FilledButton.icon(
                onPressed:
                    _saving || listAsync.valueOrNull == null ? null : () => _save(context),
                icon: _saving
                    ? const SizedBox(
                        width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.save_outlined, size: 18),
                label: const Text('Save'),
              ),
            ],
          ),
        ),
        Expanded(
          child: AsyncStateView<List<ModulePermissionRow>>(
            isLoading: listAsync.isLoading,
            error: listAsync.hasError ? listAsync.error : null,
            value: listAsync.valueOrNull,
            onRetry: () => ref.invalidate(modulePermissionsProvider(_roleId!)),
            data: (mods) {
              if (!_hydrated) {
                _permitted
                  ..clear()
                  ..addAll(mods.where((m) => m.isPermitted).map((m) => m.id));
                _hydrated = true;
              }
              if (mods.isEmpty) {
                return const EmptyState(message: 'No modules configured.');
              }
              return ListView(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                children: [
                  for (final m in mods)
                    CheckboxListTile(
                      controlAffinity: ListTileControlAffinity.leading,
                      value: _permitted.contains(m.id),
                      title: Text(m.title,
                          style: const TextStyle(fontWeight: FontWeight.w600)),
                      subtitle: Text('${m.permittedChildCount} permitted page(s)',
                          style: const TextStyle(fontSize: 12, color: Bo.textSubtle)),
                      onChanged: (v) => setState(() {
                        if (v == true) {
                          _permitted.add(m.id);
                        } else {
                          _permitted.remove(m.id);
                        }
                      }),
                    ),
                ],
              );
            },
          ),
        ),
      ],
    );
  }

  Future<void> _save(BuildContext context) async {
    setState(() => _saving = true);
    try {
      await ref
          .read(staffApiProvider)
          .adminSaveModulePermissions(_roleId!, _permitted.toList());
      ref.invalidate(modulePermissionsProvider(_roleId!));
      _hydrated = false;
      if (context.mounted) AppToast.show(context, 'Module permissions saved');
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }
}
