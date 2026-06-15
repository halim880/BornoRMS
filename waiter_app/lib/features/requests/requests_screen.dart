import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/providers/providers.dart';
import '../../core/widgets/async_view.dart';
import '../../core/widgets/snack.dart';

class RequestsScreen extends ConsumerWidget {
  const RequestsScreen({super.key});

  Future<void> _resolve(BuildContext ctx, WidgetRef ref, String id) async {
    try {
      await ref.read(waiterApiProvider).resolveRequest(id);
      await refreshConsole(ref);
    } on ApiException catch (e) {
      if (ctx.mounted) showError(ctx, e.message);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final console = ref.watch(consoleProvider);
    return AsyncView(
      value: console,
      onRetry: () => ref.invalidate(consoleProvider),
      data: (c) {
        final requests = c.requests;
        if (requests.isEmpty) {
          return const EmptyState(
              icon: Icons.notifications_none,
              title: 'No pending requests',
              description: 'Call-waiter, bill and other table requests show up here.');
        }
        return RefreshIndicator(
          onRefresh: () => refreshConsole(ref),
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: requests.length,
            separatorBuilder: (_, __) => const SizedBox(height: 8),
            itemBuilder: (_, i) {
              final r = requests[i];
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
                              Text('Table ${r.tableNumber}',
                                  style: const TextStyle(fontWeight: FontWeight.w800)),
                              const SizedBox(width: 8),
                              Container(
                                padding:
                                    const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                decoration: BoxDecoration(
                                  color: Colors.amber.shade100,
                                  borderRadius: BorderRadius.circular(999),
                                ),
                                child: Text(r.type.label,
                                    style: TextStyle(
                                        fontSize: 11,
                                        fontWeight: FontWeight.w700,
                                        color: Colors.amber.shade900)),
                              ),
                              const Spacer(),
                              Text('⏱ ${r.waitingMinutes}m',
                                  style:
                                      TextStyle(fontSize: 12, color: Colors.grey.shade500)),
                            ]),
                            if (r.note != null && r.note!.isNotEmpty) ...[
                              const SizedBox(height: 4),
                              Text(r.note!,
                                  style:
                                      TextStyle(fontSize: 13, color: Colors.grey.shade700)),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      FilledButton(
                        onPressed: () => _resolve(context, ref, r.id),
                        child: const Text('Resolve'),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        );
      },
    );
  }
}
