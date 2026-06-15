import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/responsive.dart';
import '../floor/floor_screen.dart';
import '../ready/ready_screen.dart';
import '../requests/requests_screen.dart';
import '../take_order/take_order_screen.dart';
import 'widget_strip.dart';

class HomeShell extends ConsumerWidget {
  const HomeShell({super.key});

  static const _titles = ['Floor', 'Take order', 'Ready', 'Requests'];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tab = ref.watch(selectedTabProvider);
    final console = ref.watch(consoleProvider);
    final dash = console.valueOrNull?.dashboard;
    final readyCount = console.valueOrNull?.ready.length ?? 0;
    final reqCount = console.valueOrNull?.requests.length ?? 0;
    final live = ref.watch(pollingEnabledProvider) && !console.hasError;
    final expanded = context.isExpanded;

    void selectTab(int i) => ref.read(selectedTabProvider.notifier).state = i;

    final content = Column(
      children: [
        if (dash != null) WidgetStrip(dash),
        const Divider(height: 1),
        Expanded(
          child: IndexedStack(
            index: tab,
            children: const [
              FloorScreen(),
              TakeOrderScreen(),
              ReadyScreen(),
              RequestsScreen(),
            ],
          ),
        ),
      ],
    );

    return Scaffold(
      appBar: AppBar(
        title: Row(children: [
          Text(_titles[tab]),
          const SizedBox(width: 10),
          _LiveDot(live: live),
        ]),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
            onPressed: () => refreshConsole(ref),
          ),
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: expanded
          ? Row(
              children: [
                NavigationRail(
                  selectedIndex: tab,
                  onDestinationSelected: selectTab,
                  labelType: NavigationRailLabelType.all,
                  destinations: [
                    const NavigationRailDestination(
                        icon: Icon(Icons.grid_view), label: Text('Floor')),
                    const NavigationRailDestination(
                        icon: Icon(Icons.add_shopping_cart), label: Text('Take order')),
                    NavigationRailDestination(
                        icon: _Badge(count: readyCount, child: const Icon(Icons.room_service)),
                        label: const Text('Ready')),
                    NavigationRailDestination(
                        icon: _Badge(count: reqCount, child: const Icon(Icons.notifications)),
                        label: const Text('Requests')),
                  ],
                ),
                const VerticalDivider(width: 1),
                Expanded(child: content),
              ],
            )
          : content,
      bottomNavigationBar: expanded
          ? null
          : NavigationBar(
              selectedIndex: tab,
              onDestinationSelected: selectTab,
              destinations: [
                const NavigationDestination(icon: Icon(Icons.grid_view), label: 'Floor'),
                const NavigationDestination(
                    icon: Icon(Icons.add_shopping_cart), label: 'Take order'),
                NavigationDestination(
                  icon: _Badge(count: readyCount, child: const Icon(Icons.room_service)),
                  label: 'Ready',
                ),
                NavigationDestination(
                  icon: _Badge(count: reqCount, child: const Icon(Icons.notifications)),
                  label: 'Requests',
                ),
              ],
            ),
    );
  }
}

class _LiveDot extends StatelessWidget {
  final bool live;
  const _LiveDot({required this.live});
  @override
  Widget build(BuildContext context) {
    final color = live ? Colors.green : Colors.orange;
    return Row(children: [
      Container(width: 8, height: 8, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
      const SizedBox(width: 4),
      Text(live ? 'Live' : 'Paused',
          style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w600)),
    ]);
  }
}

class _Badge extends StatelessWidget {
  final int count;
  final Widget child;
  const _Badge({required this.count, required this.child});
  @override
  Widget build(BuildContext context) {
    if (count <= 0) return child;
    return Badge(label: Text('$count'), child: child);
  }
}
