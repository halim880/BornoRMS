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

const tenantsRoute = '/admin/tenants';

const _pageSize = 12;

class TenantsScreen extends ConsumerStatefulWidget {
  const TenantsScreen({super.key});

  @override
  ConsumerState<TenantsScreen> createState() => _TenantsScreenState();
}

class _TenantsScreenState extends ConsumerState<TenantsScreen> {
  int _page = 1;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(tenantsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeader(
          title: 'Tenants',
          subtitle: 'Manage tenant records, subdomains and license expiry.',
          actions: [
            FilledButton.icon(
              onPressed: () => _openForm(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('New Tenant'),
            ),
          ],
        ),
        Expanded(
          child: AsyncStateView<List<TenantRow>>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(tenantsProvider),
            data: (tenants) => _table(context, tenants),
          ),
        ),
      ],
    );
  }

  Widget _table(BuildContext context, List<TenantRow> all) {
    final totalPages = (all.length / _pageSize).ceil();
    final page = _page.clamp(1, totalPages == 0 ? 1 : totalPages);
    final rows = all.skip((page - 1) * _pageSize).take(_pageSize).toList();

    return DataTableCard(
      emptyMessage: "No tenants yet. Click 'New Tenant' to add one.",
      columns: const [
        DataColumn(label: Text('Name')),
        DataColumn(label: Text('Subdomain')),
        DataColumn(label: Text('Contact')),
        DataColumn(label: Text('License expires')),
        DataColumn(label: Text('Status')),
        DataColumn(label: Text('Actions')),
      ],
      rows: [
        for (final t in rows)
          DataRow(cells: [
            DataCell(Text(t.name, style: const TextStyle(fontWeight: FontWeight.w700))),
            DataCell(Text(t.subdomain)),
            DataCell(Text(t.contactEmail)),
            DataCell(Text(t.licenseExpiresOnUtc == null
                ? '—'
                : shortDate(t.licenseExpiresOnUtc!))),
            DataCell(t.isActive
                ? const ToneChip('Active', 'success')
                : const ToneChip('Inactive', 'neutral')),
            DataCell(Row(children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _openForm(context, tenant: t),
              ),
              IconButton(
                tooltip: t.isActive ? 'Deactivate' : 'Activate',
                icon: Icon(t.isActive ? Icons.toggle_on : Icons.toggle_off,
                    size: 22, color: t.isActive ? Bo.success : Bo.textSubtle),
                onPressed: () => _toggleActive(context, t),
              ),
            ])),
          ]),
      ],
      pager: Pager(
        page: page,
        totalPages: totalPages,
        label: '${all.length} tenants',
        onPage: (p) => setState(() => _page = p),
      ),
    );
  }

  Future<void> _toggleActive(BuildContext context, TenantRow t) async {
    try {
      await ref.read(staffApiProvider).adminSetTenantActive(t.id, !t.isActive);
      ref.invalidate(tenantsProvider);
      if (context.mounted) {
        AppToast.show(context, t.isActive ? 'Tenant deactivated' : 'Tenant activated');
      }
    } catch (e) {
      if (context.mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    }
  }

  void _openForm(BuildContext context, {TenantRow? tenant}) {
    final nameCtrl = TextEditingController(text: tenant?.name ?? '');
    final subCtrl = TextEditingController(text: tenant?.subdomain ?? '');
    final emailCtrl = TextEditingController(text: tenant?.contactEmail ?? '');
    var license = tenant?.licenseExpiresOnUtc;
    final isEdit = tenant != null;

    showDialog<bool>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setLocal) => AppFormDialog(
          title: isEdit ? 'Edit Tenant' : 'New Tenant',
          icon: Icons.apartment_outlined,
          onSave: () async {
            final api = ref.read(staffApiProvider);
            if (isEdit) {
              await api.adminUpdateTenant(tenant.id,
                  name: nameCtrl.text.trim(),
                  contactEmail: emailCtrl.text.trim(),
                  licenseExpiresOnUtc: license);
            } else {
              await api.adminCreateTenant(
                  name: nameCtrl.text.trim(),
                  subdomain: subCtrl.text.trim(),
                  contactEmail: emailCtrl.text.trim(),
                  licenseExpiresOnUtc: license);
            }
            ref.invalidate(tenantsProvider);
            if (context.mounted) {
              AppToast.show(context, isEdit ? 'Tenant updated' : 'Tenant created');
            }
            return true;
          },
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormField2(label: 'Name', child: TextField(controller: nameCtrl)),
              FormField2(
                label: 'Subdomain',
                child: TextField(controller: subCtrl, enabled: !isEdit),
              ),
              FormField2(
                  label: 'Contact email',
                  child: TextField(
                      controller: emailCtrl, keyboardType: TextInputType.emailAddress)),
              FormField2(
                label: 'License expires (optional)',
                child: InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: ctx,
                      initialDate: license ?? DateTime.now(),
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2100),
                    );
                    if (picked != null) setLocal(() => license = picked);
                  },
                  child: InputDecorator(
                    decoration: const InputDecoration(border: OutlineInputBorder()),
                    child: Row(
                      children: [
                        Text(license == null ? 'No expiry' : shortDate(license!)),
                        const Spacer(),
                        if (license != null)
                          IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () => setLocal(() => license = null),
                          ),
                        const Icon(Icons.calendar_today_outlined, size: 18),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
