import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import 'admin_api.dart';
import 'admin_models.dart';
import 'admin_providers.dart';

const menuPermissionsRoute = '/admin/menu-permissions';

class MenuPermissionsScreen extends ConsumerStatefulWidget {
  const MenuPermissionsScreen({super.key});

  @override
  ConsumerState<MenuPermissionsScreen> createState() => _MenuPermissionsScreenState();
}

class _MenuPermissionsScreenState extends ConsumerState<MenuPermissionsScreen> {
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
          title: 'Menu Permissions',
          subtitle: 'Grant a role access to individual menu items (child pages).',
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

    final treeAsync = ref.watch(menuPermissionsProvider(_roleId!));

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
                onPressed: _saving || treeAsync.valueOrNull == null
                    ? null
                    : () => _save(context),
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
          child: AsyncStateView<List<MenuPermissionNode>>(
            isLoading: treeAsync.isLoading,
            error: treeAsync.hasError ? treeAsync.error : null,
            value: treeAsync.valueOrNull,
            onRetry: () => ref.invalidate(menuPermissionsProvider(_roleId!)),
            data: (tree) {
              if (!_hydrated) {
                _permitted
                  ..clear()
                  ..addAll(_collectPermitted(tree));
                _hydrated = true;
              }
              if (tree.isEmpty) {
                return const EmptyState(message: 'No menus configured.');
              }
              return ListView(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                children: [for (final node in tree) _node(node, 0)],
              );
            },
          ),
        ),
      ],
    );
  }

  Iterable<String> _collectPermitted(List<MenuPermissionNode> nodes) sync* {
    for (final n in nodes) {
      if (n.isPermitted) yield n.id;
      yield* _collectPermitted(n.children);
    }
  }

  Widget _node(MenuPermissionNode node, int depth) {
    final isRoot = depth == 0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: EdgeInsets.only(left: depth * 20.0),
          child: isRoot
              // Root menus (modules) are owned by the Module Permissions screen;
              // shown here as headers only.
              ? Padding(
                  padding: const EdgeInsets.only(top: 10, bottom: 4),
                  child: Text(node.title,
                      style: const TextStyle(fontWeight: FontWeight.w800, color: Bo.text)),
                )
              : CheckboxListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  controlAffinity: ListTileControlAffinity.leading,
                  value: _permitted.contains(node.id),
                  title: Text(node.title),
                  subtitle: node.url == null
                      ? null
                      : Text(node.url!, style: const TextStyle(fontSize: 12, color: Bo.textSubtle)),
                  onChanged: (v) => setState(() {
                    if (v == true) {
                      _permitted.add(node.id);
                    } else {
                      _permitted.remove(node.id);
                    }
                  }),
                ),
        ),
        for (final c in node.children) _node(c, depth + 1),
      ],
    );
  }

  Future<void> _save(BuildContext context) async {
    setState(() => _saving = true);
    try {
      await ref
          .read(staffApiProvider)
          .adminSaveMenuPermissions(_roleId!, _permitted.toList());
      ref.invalidate(menuPermissionsProvider(_roleId!));
      _hydrated = false;
      if (context.mounted) AppToast.show(context, 'Menu permissions saved');
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }
}
