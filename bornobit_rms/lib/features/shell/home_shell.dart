import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/fluent_icons.dart';
import '../dashboard/dashboard_screen.dart';
import '../orders/orders_screen.dart';
import '../pos/pos_screen.dart';

/// Below this width we switch from a permanent sidebar to a drawer (phones).
const _kWideBreakpoint = 900.0;

class HomeShell extends ConsumerWidget {
  const HomeShell({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final wide = constraints.maxWidth >= _kWideBreakpoint;
        return wide ? const _WideShell() : const _NarrowShell();
      },
    );
  }
}

// ---------------- wide (tablet landscape / desktop) ----------------
class _WideShell extends ConsumerWidget {
  const _WideShell();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final collapsed = ref.watch(navCollapsedProvider);
    final sel = ref.watch(selectedNavProvider);
    return Scaffold(
      body: Row(
        children: [
          SizedBox(width: collapsed ? 72 : 248, child: const _SidebarContent()),
          const VerticalDivider(width: 1),
          Expanded(
            child: Column(
              children: [
                _HeaderBar(title: sel.title, showMenuButton: true),
                const Divider(height: 1),
                Expanded(child: _ModuleContent(selection: sel)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------- narrow (phones) ----------------
class _NarrowShell extends ConsumerWidget {
  const _NarrowShell();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authControllerProvider).user;
    final sel = ref.watch(selectedNavProvider);
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Bo.surface,
        foregroundColor: Bo.text,
        elevation: 0,
        shape: const Border(bottom: BorderSide(color: Bo.border)),
        title: Text(sel.title, style: const TextStyle(fontWeight: FontWeight.w700)),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
            onPressed: () => _refreshCurrent(ref),
          ),
          PopupMenuButton<String>(
            icon: CircleAvatar(
              radius: 15,
              backgroundColor: Bo.primarySoft,
              child: Text(
                _initial(user?.fullName, user?.email),
                style: const TextStyle(color: Bo.primaryEmphasis, fontWeight: FontWeight.w700, fontSize: 13),
              ),
            ),
            onSelected: (v) {
              if (v == 'logout') ref.read(authControllerProvider.notifier).logout();
            },
            itemBuilder: (_) => [
              if (user != null)
                PopupMenuItem<String>(
                  enabled: false,
                  child: Text('${user.fullName.isNotEmpty ? user.fullName : user.email}\n${user.roles.join(', ')}'),
                ),
              const PopupMenuItem<String>(value: 'logout', child: Text('Sign out')),
            ],
          ),
        ],
      ),
      drawer: const Drawer(width: 248, child: _SidebarContent(inDrawer: true)),
      body: _ModuleContent(selection: sel),
    );
  }
}

/// Routes the selected menu url to a screen. Real modules render; everything
/// else gets a clean placeholder.
class _ModuleContent extends StatelessWidget {
  final NavSelection selection;
  const _ModuleContent({required this.selection});

  @override
  Widget build(BuildContext context) {
    switch (selection.url) {
      case dashboardRoute:
        return const DashboardScreen();
      case ordersRoute:
        return const OrdersScreen();
      case posRoute:
        return const PosScreen();
      default:
        return _ModulePlaceholder(selection: selection);
    }
  }
}

class _ModulePlaceholder extends StatelessWidget {
  final NavSelection selection;
  const _ModulePlaceholder({required this.selection});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 72,
              height: 72,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: Bo.slate100, borderRadius: BorderRadius.circular(Bo.radiusLg)),
              child: const Icon(Icons.construction_outlined, size: 36, color: Bo.slate400),
            ),
            const SizedBox(height: 16),
            Text(selection.title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: Bo.text)),
            const SizedBox(height: 6),
            const Text('This module is not built yet.',
                style: TextStyle(color: Bo.textSubtle)),
            const SizedBox(height: 2),
            Text(selection.url, style: const TextStyle(color: Bo.slate400, fontSize: 12)),
          ],
        ),
      ),
    );
  }
}

void _refreshCurrent(WidgetRef ref) {
  switch (ref.read(selectedNavProvider).url) {
    case ordersRoute:
      ref.invalidate(ordersProvider);
      break;
    case dashboardRoute:
    default:
      ref.read(dashboardProvider.notifier).refresh();
  }
}

// ---------------- shared sidebar (DB-driven) ----------------
class _SidebarContent extends ConsumerWidget {
  final bool inDrawer;
  const _SidebarContent({this.inDrawer = false});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final collapsed = !inDrawer && ref.watch(navCollapsedProvider);
    final menu = ref.watch(menuProvider);

    return Container(
      color: Bo.slate900,
      child: SafeArea(
        right: false,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 18, 16, 18),
              child: Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(Bo.radiusMd),
                    child: Image.asset(
                      'assets/brand/app-logo-256.png',
                      width: 36,
                      height: 36,
                      filterQuality: FilterQuality.medium,
                    ),
                  ),
                  if (!collapsed) ...[
                    const SizedBox(width: 12),
                    const Expanded(
                      child: Text('BornoBit RMS',
                          style: TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 16),
                          overflow: TextOverflow.ellipsis),
                    ),
                  ],
                ],
              ),
            ),
            const Divider(height: 1, color: Bo.slate700),
            Expanded(
              child: menu.when(
                loading: () => const Center(
                  child: SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2, color: Bo.slate400)),
                ),
                error: (_, __) => _MenuList(
                  items: [
                    MenuItem(
                      id: 'fallback-dashboard',
                      title: 'Dashboard',
                      url: dashboardRoute,
                      icon: 'DataPie',
                      displayOrder: 0,
                      requiredRole: null,
                      children: const [],
                    ),
                  ],
                  collapsed: collapsed,
                ),
                data: (items) => _MenuList(items: items, collapsed: collapsed),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MenuList extends ConsumerWidget {
  final List<MenuItem> items;
  final bool collapsed;
  const _MenuList({required this.items, required this.collapsed});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (collapsed) {
      final sel = ref.watch(selectedNavProvider);
      return ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          for (final item in items.where(_isRenderable))
            _RailIcon(
              item: item,
              active: _containsUrl(item, sel.url),
              onTap: () {
                if (item.hasChildren || item.url == null) {
                  ref.read(navCollapsedProvider.notifier).state = false;
                } else {
                  ref.read(selectedNavProvider.notifier).state = NavSelection(item.url!, item.title);
                }
              },
            ),
        ],
      );
    }
    return ListView(
      padding: const EdgeInsets.symmetric(vertical: 8),
      children: _buildMenuNodes(items, 0),
    );
  }
}

class _RailIcon extends StatelessWidget {
  final MenuItem item;
  final bool active;
  final VoidCallback onTap;
  const _RailIcon({required this.item, required this.active, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: item.title,
      child: InkWell(
        onTap: onTap,
        child: Container(
          margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
          padding: const EdgeInsets.symmetric(vertical: 11),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: active ? Bo.primary : Colors.transparent,
            borderRadius: BorderRadius.circular(Bo.radiusMd),
          ),
          child: Icon(fluentIcon(item.icon), size: 20, color: active ? Colors.white : Bo.slate300),
        ),
      ),
    );
  }
}

/// An expandable group node. Built only for items that have children
/// (see [_buildMenuNodes], which mirrors the web NavMenu's render rule).
class _MenuNode extends ConsumerWidget {
  final MenuItem item;
  final int depth;
  const _MenuNode({required this.item, required this.depth});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selUrl = ref.watch(selectedNavProvider).url;
    return Theme(
      data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
      child: ExpansionTile(
        dense: true,
        initiallyExpanded: _containsUrl(item, selUrl),
        tilePadding: EdgeInsets.only(left: 16.0 + depth * 12, right: 12),
        childrenPadding: EdgeInsets.zero,
        iconColor: Bo.slate300,
        collapsedIconColor: Bo.slate400,
        leading: Icon(fluentIcon(item.icon), size: 20, color: Bo.slate300),
        title: Text(item.title,
            style: const TextStyle(color: Bo.slate200, fontSize: 13, fontWeight: FontWeight.w600),
            overflow: TextOverflow.ellipsis),
        children: _buildMenuNodes(item.children, depth + 1),
      ),
    );
  }
}

class _LeafTile extends ConsumerWidget {
  final MenuItem item;
  final int depth;
  const _LeafTile({required this.item, required this.depth});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selUrl = ref.watch(selectedNavProvider).url;
    final active = item.url != null && item.url == selUrl;
    final color = active ? Colors.white : Bo.slate300;

    return InkWell(
      onTap: () {
        // Built only for items with a non-empty URL (see _buildMenuNodes).
        ref.read(selectedNavProvider.notifier).state = NavSelection(item.url!, item.title);
        if (Scaffold.of(context).hasDrawer && Scaffold.of(context).isDrawerOpen) {
          Navigator.of(context).pop();
        }
      },
      child: Container(
        margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        padding: EdgeInsets.only(left: 12.0 + depth * 12, right: 12, top: 10, bottom: 10),
        decoration: BoxDecoration(
          color: active ? Bo.primary : Colors.transparent,
          borderRadius: BorderRadius.circular(Bo.radiusMd),
        ),
        child: Row(
          children: [
            Icon(fluentIcon(item.icon), size: 20, color: color),
            const SizedBox(width: 12),
            Expanded(
              child: Text(item.title,
                  style: TextStyle(color: color, fontWeight: active ? FontWeight.w600 : FontWeight.w400),
                  overflow: TextOverflow.ellipsis),
            ),
          ],
        ),
      ),
    );
  }
}

class _HeaderBar extends ConsumerWidget {
  final String title;
  final bool showMenuButton;
  const _HeaderBar({required this.title, this.showMenuButton = false});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authControllerProvider).user;
    return Container(
      height: 60,
      color: Bo.surface,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Row(
        children: [
          if (showMenuButton)
            IconButton(
              tooltip: 'Toggle menu',
              icon: const Icon(Icons.menu),
              onPressed: () => ref.read(navCollapsedProvider.notifier).update((v) => !v),
            ),
          const SizedBox(width: 8),
          Text(title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: Bo.text)),
          const Spacer(),
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
            onPressed: () => _refreshCurrent(ref),
          ),
          const SizedBox(width: 8),
          if (user != null) ...[
            CircleAvatar(
              radius: 16,
              backgroundColor: Bo.primarySoft,
              child: Text(_initial(user.fullName, user.email),
                  style: const TextStyle(color: Bo.primaryEmphasis, fontWeight: FontWeight.w700)),
            ),
            const SizedBox(width: 8),
            Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(user.fullName.isNotEmpty ? user.fullName : user.email,
                    style: const TextStyle(fontWeight: FontWeight.w600, color: Bo.text, fontSize: 13)),
                Text(user.roles.join(', '), style: const TextStyle(color: Bo.textSubtle, fontSize: 11)),
              ],
            ),
            const SizedBox(width: 8),
          ],
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
    );
  }
}

/// Mirrors the web NavMenu render rule (RenderRoot/RenderChild): a node renders
/// as an expandable group iff it has children; else as a leaf link iff it has a
/// non-empty URL; otherwise it is dropped (hides URL-less leaves).
List<Widget> _buildMenuNodes(List<MenuItem> items, int depth) {
  final widgets = <Widget>[];
  for (final item in items) {
    if (item.children.isNotEmpty) {
      widgets.add(_MenuNode(item: item, depth: depth));
    } else if (item.url != null && item.url!.isNotEmpty) {
      widgets.add(_LeafTile(item: item, depth: depth));
    }
  }
  return widgets;
}

/// A node the web would render at all (group, or URL-bearing leaf).
bool _isRenderable(MenuItem item) =>
    item.children.isNotEmpty || (item.url != null && item.url!.isNotEmpty);

bool _containsUrl(MenuItem item, String url) =>
    item.url == url || item.children.any((c) => _containsUrl(c, url));

String _initial(String? fullName, String? email) {
  final s = (fullName != null && fullName.isNotEmpty) ? fullName : (email ?? '?');
  return s.isEmpty ? '?' : s.characters.first.toUpperCase();
}
