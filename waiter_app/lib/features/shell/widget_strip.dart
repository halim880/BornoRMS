import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/widgets/format.dart';

/// Horizontal scroller of the waiter dashboard counters (top of the Blazor console).
class WidgetStrip extends StatelessWidget {
  final WaiterDashboard d;
  const WidgetStrip(this.d, {super.key});

  @override
  Widget build(BuildContext context) {
    final items = <_W>[
      _W('My tables', '${d.myTables}', null),
      _W('Available', '${d.availableTables}', Colors.green),
      _W('Occupied', '${d.occupiedTables}', Colors.orange),
      _W('Requests', '${d.pendingRequests}', Colors.blue),
      _W('Ready', '${d.readyToServeOrders}', const Color(0xFFCB3A1A)),
      _W('Bills waiting', '${d.billsWaiting}', Colors.red),
      _W('My sessions', '${d.myActiveSessions}', null),
      _W('My revenue', money(d.myRevenueServedToday, currency: d.currency), null),
    ];
    // Wrap so every counter fills the width and flows onto more rows on narrow
    // screens — nothing clipped at any width.
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: items.map(_chip).toList(),
      ),
    );
  }

  Widget _chip(_W w) => Container(
        width: 150,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10),
          border: Border(left: BorderSide(color: w.color ?? Colors.grey.shade300, width: 3)),
          boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 4)],
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(w.value,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
            const SizedBox(height: 2),
            Text(w.label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                    fontSize: 10,
                    color: Colors.grey.shade600,
                    letterSpacing: 0.4,
                    fontWeight: FontWeight.w600)),
          ],
        ),
      );
}

class _W {
  final String label;
  final String value;
  final Color? color;
  _W(this.label, this.value, this.color);
}
