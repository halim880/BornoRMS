import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';

final _money = NumberFormat('#,##0.00');
final _int = NumberFormat('#,##0');
final _time = DateFormat('dd/MM HH:mm'); // project rule: dd/MM/yyyy date style

String money(num v, String currency) => '${_money.format(v)} $currency';
String count(num v) => _int.format(v);
String shortDateTime(DateTime d) => _time.format(d);

/// A titled white panel — the Flutter equivalent of the console's `.bo-panel`.
class SectionCard extends StatelessWidget {
  final String title;
  final Widget child;
  final Widget? trailing;
  final IconData? icon;
  const SectionCard({super.key, required this.title, required this.child, this.trailing, this.icon});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                if (icon != null) ...[Icon(icon, size: 18, color: Bo.textMuted), const SizedBox(width: 8)],
                Expanded(
                  child: Text(title,
                      style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: Bo.text)),
                ),
                if (trailing != null) trailing!,
              ],
            ),
            const SizedBox(height: 12),
            child,
          ],
        ),
      ),
    );
  }
}

/// A KPI tile: big value + label + optional sub-stats row.
class KpiCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color tint;
  final List<Widget> stats;
  const KpiCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.tint,
    this.stats = const [],
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 36,
                  height: 36,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: tint, borderRadius: BorderRadius.circular(Bo.radiusMd)),
                  child: Icon(icon, size: 20, color: Bo.primaryEmphasis),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(label,
                      style: const TextStyle(color: Bo.textSubtle, fontSize: 13, fontWeight: FontWeight.w600)),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text(value,
                style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: Bo.text)),
            if (stats.isNotEmpty) ...[
              const SizedBox(height: 10),
              Wrap(spacing: 8, runSpacing: 6, children: stats),
            ],
          ],
        ),
      ),
    );
  }
}

/// A small label:value pill used inside KPI cards.
class MiniStat extends StatelessWidget {
  final String label;
  final String value;
  final String tone;
  const MiniStat(this.label, this.value, {super.key, this.tone = 'neutral'});

  @override
  Widget build(BuildContext context) {
    final c = toneColors(tone);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(color: c.bg, borderRadius: BorderRadius.circular(Bo.radiusSm)),
      child: Text('$value $label',
          style: TextStyle(color: c.fg, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

/// A status chip rendered with a semantic tone.
class ToneChip extends StatelessWidget {
  final String text;
  final String tone;
  const ToneChip(this.text, this.tone, {super.key});

  @override
  Widget build(BuildContext context) {
    final c = toneColors(tone);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: c.bg, borderRadius: BorderRadius.circular(Bo.radiusSm)),
      child: Text(text, style: TextStyle(color: c.fg, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

// ---- tone/label mappings (mirror the Blazor view helpers) ----
String orderStatusTone(String s) => switch (s) {
      'Placed' => 'neutral',
      'Confirmed' => 'info',
      'Preparing' => 'warning',
      'Ready' => 'primary',
      'Served' => 'info',
      'Completed' => 'success',
      'Cancelled' => 'danger',
      _ => 'neutral',
    };

String tableStatusTone(String s) => switch (s) {
      'Available' => 'success',
      'Occupied' => 'primary',
      'Reserved' => 'info',
      'WaitingPayment' => 'warning',
      _ => 'neutral',
    };

String tableStatusLabel(String s) => switch (s) {
      'WaitingPayment' => 'Awaiting payment',
      _ => s,
    };

String requestLabel(String t) => switch (t) {
      'CallWaiter' => 'Call Waiter',
      'RequestBill' => 'Request Bill',
      'NeedWater' => 'Need Water',
      'NeedTissue' => 'Need Tissue',
      _ => t,
    };
