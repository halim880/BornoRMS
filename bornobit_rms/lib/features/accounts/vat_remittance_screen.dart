import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';
import 'accounts_models.dart';
import 'accounts_providers.dart';
import 'widgets.dart';

const vatRemittanceRoute = '/accounts/reports/vat-remittance';

/// Accounts → Reports → VAT Remittance. Shows the live VAT Payable GL balance —
/// what is owed to the authority. Mirrors the Blazor VatRemittance.razor page.
/// The remittance write (Dr VAT Payable / Cr cash) stays in the staff console.
class VatRemittanceScreen extends ConsumerWidget {
  const VatRemittanceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(vatRemittanceProvider);

    return Column(
      children: [
        PageHeader(
          title: 'VAT Remittance',
          subtitle: 'Output VAT owed to the authority (the VAT Payable GL balance).',
          actions: [RefreshAction(onPressed: () => ref.invalidate(vatRemittanceProvider))],
        ),
        Expanded(
          child: AsyncStateView<GlAccountBalance>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(vatRemittanceProvider),
            data: (b) => _body(b),
          ),
        ),
      ],
    );
  }

  Widget _body(GlAccountBalance b) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        KpiGrid(children: [
          KpiCard(
            label: 'VAT Payable (${b.code})',
            value: money(b.balance, 'Tk'),
            icon: Icons.account_balance,
            tint: b.balance > 0 ? Bo.dangerSoft : Bo.successSoft,
          ),
          KpiCard(label: 'Total Debited', value: money(b.debit, 'Tk'), icon: Icons.remove_circle_outline, tint: Bo.infoSoft),
          KpiCard(label: 'Total Credited', value: money(b.credit, 'Tk'), icon: Icons.add_circle_outline, tint: Bo.primaryTint),
        ]),
        const SizedBox(height: 16),
        SectionCard(
          title: b.name,
          child: const Text(
            'Record the actual remittance (Dr VAT Payable / Cr cash) from the staff console. '
            'This screen reflects the current outstanding liability.',
            style: TextStyle(color: Bo.textMuted),
          ),
        ),
      ],
    );
  }
}
