import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../theme/app_theme.dart';
import 'connectivity.dart';
import 'live_connection.dart';

/// Compact header chip showing the real-time connection health, so staff can tell
/// at a glance whether the floor is live, falling back to polling, or offline.
class LiveIndicator extends ConsumerWidget {
  const LiveIndicator({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final online = ref.watch(connectivityProvider).valueOrNull ?? true;
    final live = ref.watch(liveControllerProvider);

    late final Color color;
    late final String label;
    if (!online) {
      color = Bo.slate400;
      label = 'Offline';
    } else {
      switch (live) {
        case LiveStatus.connected:
          color = const Color(0xFF16A34A); // success green
          label = 'Live';
        case LiveStatus.connecting:
          color = const Color(0xFFD97706); // amber
          label = 'Connecting';
        case LiveStatus.reconnecting:
          color = const Color(0xFFD97706);
          label = 'Reconnecting';
        case LiveStatus.disconnected:
          color = const Color(0xFFD97706);
          label = 'Polling'; // socket down → fallback timer still refreshing
      }
    }

    return Tooltip(
      message: !online
          ? 'No network connection — showing last known data'
          : 'Real-time updates: $label',
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(color: color, shape: BoxShape.circle),
            ),
            const SizedBox(width: 6),
            Text(label,
                style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    );
  }
}

/// Thin full-width bar shown only while the device has no network link. Read
/// screens keep their last snapshot underneath (see `PollingNotifier`), so this
/// is an awareness cue, not an error state.
class OfflineBanner extends ConsumerWidget {
  const OfflineBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final online = ref.watch(connectivityProvider).valueOrNull ?? true;
    if (online) return const SizedBox.shrink();

    return Material(
      color: const Color(0xFFFEF3C7), // amber-100
      child: SizedBox(
        width: double.infinity,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 12),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: const [
              Icon(Icons.wifi_off_rounded, size: 16, color: Color(0xFF92400E)),
              SizedBox(width: 8),
              Text('Offline — showing last known data. Changes will sync when reconnected.',
                  style: TextStyle(color: Color(0xFF92400E), fontSize: 12, fontWeight: FontWeight.w600)),
            ],
          ),
        ),
      ),
    );
  }
}
