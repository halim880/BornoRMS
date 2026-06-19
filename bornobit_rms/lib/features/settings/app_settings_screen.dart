import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';
import 'settings_api.dart';
import 'settings_models.dart';
import 'settings_providers.dart';

const appSettingsRoute = '/settings/app';

/// Editable restaurant settings (billing defaults) — the Flutter counterpart of
/// the Blazor AppSettings page, backed by /api/v1/staff/settings. Admin saves;
/// any staff can read.
class AppSettingsScreen extends ConsumerStatefulWidget {
  const AppSettingsScreen({super.key});

  @override
  ConsumerState<AppSettingsScreen> createState() => _AppSettingsScreenState();
}

class _AppSettingsScreenState extends ConsumerState<AppSettingsScreen> {
  final _currencyCtrl = TextEditingController();
  final _vatCtrl = TextEditingController();
  final _serviceCtrl = TextEditingController();
  final _highDiscountCtrl = TextEditingController();
  bool _tipEnabled = false;
  bool _priceIncludesTax = false;

  bool _seeded = false; // populate controllers once when data first arrives
  bool _saving = false;

  @override
  void dispose() {
    _currencyCtrl.dispose();
    _vatCtrl.dispose();
    _serviceCtrl.dispose();
    _highDiscountCtrl.dispose();
    super.dispose();
  }

  void _seed(AppSettings s) {
    if (_seeded) return;
    _seeded = true;
    _currencyCtrl.text = s.currency;
    _vatCtrl.text = _fmt(s.vatPercent);
    _serviceCtrl.text = _fmt(s.serviceChargePercent);
    _highDiscountCtrl.text = _fmt(s.highDiscountThresholdPercent);
    _tipEnabled = s.tipEnabled;
    _priceIncludesTax = s.priceIncludesTax;
  }

  static String _fmt(double v) =>
      v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(appSettingsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const PageHeader(
          title: 'App Settings',
          subtitle:
              'Restaurant-wide billing defaults — currency, tax/VAT, service charge, and discount rules.',
        ),
        Expanded(
          child: AsyncStateView<AppSettings>(
            isLoading: async.isLoading,
            error: async.hasError ? async.error : null,
            value: async.valueOrNull,
            onRetry: () => ref.invalidate(appSettingsProvider),
            data: (settings) {
              _seed(settings);
              return _form(context);
            },
          ),
        ),
      ],
    );
  }

  Widget _form(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SectionCard(
            title: 'Currency & display',
            icon: Icons.attach_money,
            child: _Field(
              label: 'Currency symbol / code',
              hint: 'e.g. Tk, ৳, BDT',
              child: TextField(
                controller: _currencyCtrl,
                decoration: const InputDecoration(hintText: 'Tk'),
              ),
            ),
          ),
          const SizedBox(height: 12),
          SectionCard(
            title: 'Tax & VAT',
            icon: Icons.receipt_long,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Field(
                  label: 'VAT %',
                  hint: 'Value-added tax applied at billing (0–100).',
                  child: _numField(_vatCtrl),
                ),
                const SizedBox(height: 4),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  value: _priceIncludesTax,
                  onChanged: (v) => setState(() => _priceIncludesTax = v),
                  title: const Text('Prices include tax',
                      style: TextStyle(fontWeight: FontWeight.w600, color: Bo.text)),
                  subtitle: const Text(
                      'When on, listed prices are tax-inclusive (no VAT added on top).',
                      style: TextStyle(color: Bo.textMuted, fontSize: 12)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          SectionCard(
            title: 'Service & tips',
            icon: Icons.room_service,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Field(
                  label: 'Service charge %',
                  hint: 'Auto-added service charge (0–100).',
                  child: _numField(_serviceCtrl),
                ),
                const SizedBox(height: 4),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  value: _tipEnabled,
                  onChanged: (v) => setState(() => _tipEnabled = v),
                  title: const Text('Enable tips',
                      style: TextStyle(fontWeight: FontWeight.w600, color: Bo.text)),
                  subtitle: const Text('Allow staff to record customer tips at checkout.',
                      style: TextStyle(color: Bo.textMuted, fontSize: 12)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          SectionCard(
            title: 'Discount controls',
            icon: Icons.percent,
            child: _Field(
              label: 'High-discount threshold %',
              hint: 'Discounts at or above this percent are flagged as high (0–100).',
              child: _numField(_highDiscountCtrl),
            ),
          ),
          const SizedBox(height: 20),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton.icon(
              onPressed: _saving ? null : _save,
              icon: _saving
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.save_outlined, size: 18),
              label: Text(_saving ? 'Saving…' : 'Save settings'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _numField(TextEditingController ctrl) => TextField(
        controller: ctrl,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
      );

  Future<void> _save() async {
    final currency = _currencyCtrl.text.trim();
    if (currency.isEmpty) {
      AppToast.show(context, 'Currency is required', type: ToastType.error);
      return;
    }
    final updated = AppSettings(
      currency: currency,
      vatPercent: double.tryParse(_vatCtrl.text.trim()) ?? 0,
      serviceChargePercent: double.tryParse(_serviceCtrl.text.trim()) ?? 0,
      highDiscountThresholdPercent: double.tryParse(_highDiscountCtrl.text.trim()) ?? 0,
      tipEnabled: _tipEnabled,
      priceIncludesTax: _priceIncludesTax,
    );

    setState(() => _saving = true);
    try {
      await ref.read(staffApiProvider).updateSettings(updated);
      ref.invalidate(appSettingsProvider);
      if (mounted) AppToast.show(context, 'Settings saved');
    } catch (e) {
      if (mounted) AppToast.show(context, e.toString(), type: ToastType.error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }
}

/// A labelled field with an optional helper hint, used inside the setting cards.
class _Field extends StatelessWidget {
  final String label;
  final String? hint;
  final Widget child;
  const _Field({required this.label, required this.child, this.hint});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Bo.textMuted)),
        const SizedBox(height: 6),
        child,
        if (hint != null) ...[
          const SizedBox(height: 6),
          Text(hint!, style: const TextStyle(fontSize: 12, color: Bo.textSubtle)),
        ],
      ],
    );
  }
}
