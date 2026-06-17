import 'dart:async';

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Visual flavour of a toast — drives the accent colour + leading icon.
enum ToastType { success, error, info }

/// Lightweight top-right toast notifications, rendered into the root [Overlay]
/// so they float above any page/dialog. Replaces the default Material
/// [SnackBar] for in-app feedback (e.g. "Added Burger").
///
/// Usage: `AppToast.show(context, 'Added Burger');`
class AppToast {
  AppToast._();

  /// Toasts currently on screen, top-most first. Drives vertical stacking.
  static final List<_ToastHandle> _live = [];

  static void show(
    BuildContext context,
    String message, {
    ToastType type = ToastType.success,
    Duration duration = const Duration(milliseconds: 2400),
  }) {
    final overlay = Overlay.of(context, rootOverlay: true);
    final handle = _ToastHandle();
    handle.entry = OverlayEntry(
      builder: (_) => _ToastCard(
        message: message,
        type: type,
        visibleDuration: duration,
        positionOf: () => _live.indexOf(handle),
        onRemove: () => _remove(handle),
      ),
    );
    _live.insert(0, handle);
    overlay.insert(handle.entry);
    // Nudge existing toasts down to make room for the new top entry.
    for (final h in _live) {
      h.entry.markNeedsBuild();
    }
  }

  static void _remove(_ToastHandle handle) {
    if (!_live.remove(handle)) return;
    handle.entry.remove();
    for (final h in _live) {
      h.entry.markNeedsBuild();
    }
  }
}

class _ToastHandle {
  late OverlayEntry entry;
}

class _ToastCard extends StatefulWidget {
  const _ToastCard({
    required this.message,
    required this.type,
    required this.visibleDuration,
    required this.positionOf,
    required this.onRemove,
  });

  final String message;
  final ToastType type;
  final Duration visibleDuration;
  final int Function() positionOf;
  final VoidCallback onRemove;

  @override
  State<_ToastCard> createState() => _ToastCardState();
}

class _ToastCardState extends State<_ToastCard>
    with SingleTickerProviderStateMixin {
  static const double _slot = 64; // per-toast vertical stride
  late final AnimationController _anim;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _anim = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 220),
    )..forward();
    _timer = Timer(widget.visibleDuration, _hide);
  }

  Future<void> _hide() async {
    if (!mounted) return;
    await _anim.reverse();
    widget.onRemove();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _anim.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    final index = widget.positionOf();
    final (accent, tint, icon) = switch (widget.type) {
      ToastType.success => (a.success, a.successTint, Icons.check_circle_rounded),
      ToastType.error => (a.danger, a.dangerTint, Icons.error_rounded),
      ToastType.info => (a.accent, a.accentTint, Icons.info_rounded),
    };

    return AnimatedPositioned(
      duration: const Duration(milliseconds: 200),
      curve: Curves.easeOut,
      top: 16 + index * _slot,
      right: 16,
      child: AnimatedBuilder(
        animation: _anim,
        builder: (_, child) {
          final t = Curves.easeOutCubic.transform(_anim.value);
          return Opacity(
            opacity: _anim.value,
            child: Transform.translate(
              offset: Offset((1 - t) * 40, 0),
              child: child,
            ),
          );
        },
        child: Material(
          color: Colors.transparent,
          child: Container(
            constraints: const BoxConstraints(maxWidth: 360, minWidth: 200),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
            decoration: BoxDecoration(
              color: a.surface,
              borderRadius: BorderRadius.circular(AppColors.radiusButton),
              border: Border.all(color: a.border),
              boxShadow: AppColors.shadowLifted,
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 30,
                  height: 30,
                  decoration: BoxDecoration(color: tint, shape: BoxShape.circle),
                  child: Icon(icon, size: 18, color: accent),
                ),
                const SizedBox(width: 10),
                Flexible(
                  child: Text(
                    widget.message,
                    style: TextStyle(
                      color: a.textPrimary,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
