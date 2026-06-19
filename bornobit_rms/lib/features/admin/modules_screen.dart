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

const modulesRoute = '/admin/modules';

const _pageSize = 12;

class ModulesScreen extends ConsumerStatefulWidget {
  const ModulesScreen({super.key});

  @override
  ConsumerState<ModulesScreen> createState() => _ModulesScreenState();
}

class _ModulesScreenState extends ConsumerState<ModulesScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(modulesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Modules',
          subtitle: 'Top-level navigation modules. SuperAdmin only.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Module'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<ModuleRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(modulesProvider),
            data: (mods) => _table(context, mods),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<ModuleRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No modules yet. Click 'New Module' to add one.",
      columns: const [
        DataColumn(label: Text('Order')),
        DataColumn(label: Text('Title')),
        DataColumn(label: Text('Icon')),
        DataColumn(label: Text('Required role')),
        DataColumn(label: Text('Pages')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final m in rows)
          DataRow(cells: [
            DataCell(Text('${m.displayOrder}')),
            DataCell(Text(m.title, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(m.icon ?? '—')),
            DataCell(Text(m.requiredRole ?? '—')),
            DataCell(Text('${m.accessibleMenuCount}')),
            DataCell(m.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, module: m),
              ),
              IconButton(
                tooltip: m.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(m.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: m.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, m),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} modules',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, ModuleRow m) async {
    try {
      await ref.read(staffApiProvider).adminSetModuleActive(m.id, !m.isActive);
      ref.invalidate(modulesProvider);
      if (context.mounted) {
        AppToast.show(context, m.isActive ? 'Module deactivated' : 'Module activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {ModuleRow? module}) {
    final titleCtrl = TextEditingController(text: module?.title ?? '');
    final iconCtrl = TextEditingController(text: module?.icon ?? '');
    final orderCtrl = TextEditingController(
        text: module == null ? '' : '${module.displayOrder}');
    final roleCtrl = TextEditingController(text: module?.requiredRole ?? '');
    final isEdit = module != null;

    showDialog<bool>(
      context: context,
      builder: (_) => AppFormDialog(
        title: isEdit ? 'Edit Module' : 'New Module',
        icon: Icons.widgets_outlined,
        onSave: () async {
          final api = ref.read(staffApiProvider);
          final title = titleCtrl.text.trim();
          final icon = iconCtrl.text.trim();
          final role = roleCtrl.text.trim();
          final order = int.tryParse(orderCtrl.text.trim());
          if (isEdit) {
            await api.adminUpdateModule(module.id,
                title: title,
                icon: icon,
                displayOrder: order ?? module.displayOrder,
                requiredRole: role);
          } else {
            await api.adminCreateModule(
                title: title, icon: icon, displayOrder: order, requiredRole: role);
          }
          ref.invalidate(modulesProvider);
          if (context.mounted) {
            AppToast.show(context, isEdit ? 'Module updated' : 'Module created');
          }
          return true;
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField2(label: 'Title', child: TextField(controller: titleCtrl)),
            FormField2(
              label: 'Icon (Fluent-style name)',
              child: TextField(controller: iconCtrl),
            ),
            FormField2(
              label: 'Display order (optional — appended if blank)',
              child: TextField(controller: orderCtrl, keyboardType: TextInputType.number),
            ),
            FormField2(
              label: 'Required role (optional)',
              child: TextField(controller: roleCtrl),
            ),
          ],
        ),
      ),
    );
  }
}
