import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/models/enums.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/async_view.dart';
import '../../core/widgets/snack.dart';

class ReadyScreen extends ConsumerWidget {
  const ReadyScreen({super.key});

  Future<void> _serve(BuildContext ctx, WidgetRef ref, String orderId) async {
    try {
      await ref.read(waiterApiProvider).changeStatus(orderId, OrderStatus.served);
      await refreshConsole(ref);
    } on ApiException catch (e) {
      if (ctx.mounted) showError(ctx, e.message);
    }
  }

  Future<void> _serveAll(BuildContext ctx, WidgetRef ref, List<String> ids) async {
    final api = ref.read(waiterApiProvider);
    var failed = 0;
    for (final id in ids) {
      try {
        await api.changeStatus(id, OrderStatus.served);
      } on ApiException {
        failed++;
      }
    }
    await refreshConsole(ref);
    if (ctx.mounted && failed > 0) showError(ctx, '$failed order(s) could not be served.');
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final console = ref.watch(consoleProvider);
    return AsyncView(
      value: console,
      onRetry: () => ref.invalidate(consoleProvider),
      data: (c) {
        final ready = c.ready;
        if (ready.isEmpty) {
          return const EmptyState(
              icon: Icons.room_service_outlined,
              title: 'Nothing waiting',
              description: 'Cooked orders ready to carry out appear here.');
        }
        return Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('${ready.length} ready',
                      style: const TextStyle(fontWeight: FontWeight.bold)),
                  FilledButton.icon(
                    onPressed: () =>
                        _serveAll(context, ref, ready.map((r) => r.orderId).toList()),
                    icon: const Icon(Icons.done_all, size: 18),
                    label: const Text('Serve all'),
                  ),
                ],
              ),
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: () => refreshConsole(ref),
                child: ListView.separated(
                  padding: const EdgeInsets.all(12),
                  itemCount: ready.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (_, i) {
                    final r = ready[i];
                    return Card(
                      margin: EdgeInsets.zero,
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(children: [
                                    Text(r.tableNumber ?? 'Takeaway',
                                        style: const TextStyle(fontWeight: FontWeight.w800)),
                                    const SizedBox(width: 8),
                                    Text(r.orderNumber,
                                        style: TextStyle(
                                            fontSize: 12, color: Colors.grey.shade600)),
                                    const Spacer(),
                                    Text('⏱ ${r.waitingMinutes}m',
                                        style: TextStyle(
                                            fontSize: 12, color: Colors.grey.shade500)),
                                  ]),
                                  const SizedBox(height: 4),
                                  Text(
                                    r.items.map((i) => '${i.quantity}× ${i.name}').join(', '),
                                    style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(width: 8),
                            FilledButton(
                              onPressed: () => _serve(context, ref, r.orderId),
                              child: const Text('Served'),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
