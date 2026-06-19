import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../dashboard/widgets.dart' show money;
import 'cart_panel.dart';
import 'order_tabs.dart';
import 'pos_providers.dart';
import 'product_grid.dart';

/// POS work area. The dark sidebar + top app bar are owned by the app shell
/// (home_shell.dart); this screen renders only the order-tabs + catalog + cart.
class PosScreen extends StatelessWidget {
  const PosScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final a = context.appColors;
    return ColoredBox(
      color: a.canvas,
      child: CustomPaint(
        painter: _DotGridPainter(a.dotGrid),
        child: LayoutBuilder(
          builder: (context, c) =>
              c.maxWidth >= 820 ? const _WideBody() : const _NarrowBody(),
        ),
      ),
    );
  }
}

// ---------------- wide: catalog (flex) + cart (fixed) ----------------
class _WideBody extends StatelessWidget {
  const _WideBody();
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          const OrderTabs(),
          const SizedBox(height: 16),
          Expanded(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: const [
                Expanded(child: CatalogView()),
                SizedBox(width: 16),
                SizedBox(width: 372, child: CartPanel()),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------- narrow: catalog full, cart in a sheet ----------------
class _NarrowBody extends ConsumerWidget {
  const _NarrowBody();
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = context.appColors;
    final detail = ref.watch(posControllerProvider).detail;
    final count = detail?.lines.fold<int>(0, (acc, l) => acc + l.quantity) ?? 0;

    return Stack(
      children: [
        Positioned.fill(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 12),
            child: Column(
              children: const [
                OrderTabs(),
                SizedBox(height: 12),
                Expanded(child: CatalogView()),
              ],
            ),
          ),
        ),
        if (detail != null)
          Positioned(
            left: 12,
            right: 12,
            bottom: 12,
            child: FilledButton(
              onPressed: () => showModalBottomSheet(
                context: context,
                isScrollControlled: true,
                backgroundColor: Colors.transparent,
                builder: (_) => Padding(
                  padding: const EdgeInsets.all(12),
                  child: SizedBox(
                    height: MediaQuery.of(context).size.height * 0.85,
                    child: const CartPanel(),
                  ),
                ),
              ),
              style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(AppLocalizations.of(context).posViewCart(count)),
                  Text(
                    money(detail.grandTotal, detail.currency),
                    style: AppColors.priceText.copyWith(color: a.onAccent),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

/// Paints the canvas-texture dots behind the work area.
class _DotGridPainter extends CustomPainter {
  final Color color;
  _DotGridPainter(this.color);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = color;
    const gap = 22.0;
    for (var y = gap; y < size.height; y += gap) {
      for (var x = gap; x < size.width; x += gap) {
        canvas.drawCircle(Offset(x, y), 1, paint);
      }
    }
  }

  @override
  bool shouldRepaint(_DotGridPainter old) => old.color != color;
}
