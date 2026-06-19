import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import 'reports_providers.dart';

/// A row of preset date-range chips (Today / Yesterday / Last 7 days / This
/// month) driving [reportsRangeProvider]. Shared by every report screen.
class ReportsRangeSelector extends ConsumerWidget {
  const ReportsRangeSelector({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(reportsRangeProvider);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          for (final r in DashboardRange.values)
            ChoiceChip(
              label: Text(r.label),
              selected: selected == r,
              onSelected: (_) => ref.read(reportsRangeProvider.notifier).state = r,
            ),
        ],
      ),
    );
  }
}

/// Compact icon button used in report page headers.
class RefreshAction extends StatelessWidget {
  final VoidCallback onPressed;
  const RefreshAction({super.key, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: 'Refresh',
      icon: const Icon(Icons.refresh, color: Bo.textMuted),
      onPressed: onPressed,
    );
  }
}
