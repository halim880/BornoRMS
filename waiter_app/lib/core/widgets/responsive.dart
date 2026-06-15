import 'package:flutter/widgets.dart';

enum Breakpoint { compact, medium, expanded }

extension ResponsiveContext on BuildContext {
  /// Current layout breakpoint from the window width.
  Breakpoint get bp {
    final w = MediaQuery.sizeOf(this).width;
    if (w < 600) return Breakpoint.compact;
    if (w < 960) return Breakpoint.medium;
    return Breakpoint.expanded;
  }

  /// Wide layout = show side panels + NavigationRail (mirrors the web wide layout).
  bool get isExpanded => bp == Breakpoint.expanded;
}

/// Column count for a grid given the available width and a minimum tile width.
int gridColumns(double width, double minTile) {
  final n = (width / minTile).floor();
  return n < 1 ? 1 : n;
}
