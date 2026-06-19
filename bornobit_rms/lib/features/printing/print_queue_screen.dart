import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/printing/print_job.dart';
import '../../core/printing/print_queue.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/app_toast.dart';

const printQueueRoute = '/operations/print-queue';

/// Operations → Print Queue. Shows ESC/POS jobs that couldn't reach the thermal
/// printer and are being auto-retried (every 25s). The operator can force a retry,
/// or delete a job that will never succeed (e.g. a printer that's been removed).
class PrintQueueScreen extends ConsumerWidget {
  const PrintQueueScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final jobs = ref.watch(printQueueProvider);
    final q = ref.read(printQueueProvider.notifier);

    return Column(
      children: [
        PageHeader(
          title: 'Print Queue',
          subtitle: jobs.isEmpty
              ? 'No pending print jobs — the printer is keeping up.'
              : '${jobs.length} job(s) waiting for the printer. Retrying automatically every 25s.',
          actions: [
            if (jobs.isNotEmpty) ...[
              TextButton.icon(
                onPressed: () async {
                  await q.retryNow();
                  if (context.mounted) AppToast.show(context, 'Retrying queued jobs…');
                },
                icon: const Icon(Icons.refresh, size: 18),
                label: const Text('Retry all'),
              ),
              TextButton.icon(
                onPressed: () => _confirmClear(context, q),
                icon: const Icon(Icons.delete_sweep_outlined, size: 18, color: Bo.danger),
                label: const Text('Clear all', style: TextStyle(color: Bo.danger)),
              ),
            ],
          ],
        ),
        Expanded(
          child: jobs.isEmpty
              ? const _Empty()
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: jobs.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (_, i) => _JobCard(job: jobs[i], q: q),
                ),
        ),
      ],
    );
  }

  Future<void> _confirmClear(BuildContext context, PrintQueueNotifier q) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Clear print queue?'),
        content: const Text('All pending print jobs will be discarded and will not be printed.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Bo.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Clear all'),
          ),
        ],
      ),
    );
    if (ok == true) await q.clear();
  }
}

class _Empty extends StatelessWidget {
  const _Empty();
  @override
  Widget build(BuildContext context) => const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.print_outlined, size: 48, color: Bo.textSubtle),
            SizedBox(height: 12),
            Text('Print queue is empty', style: TextStyle(color: Bo.textMuted)),
          ],
        ),
      );
}

class _JobCard extends StatelessWidget {
  final PrintJob job;
  final PrintQueueNotifier q;
  const _JobCard({required this.job, required this.q});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: Bo.border),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(color: Bo.dangerSoft, borderRadius: BorderRadius.circular(8)),
            child: Icon(job.isKot ? Icons.soup_kitchen_outlined : Icons.receipt_long_outlined,
                color: Bo.danger, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(children: [
                  Text('${job.orderNumber} · ${job.label}',
                      style: const TextStyle(fontWeight: FontWeight.w700)),
                  const SizedBox(width: 8),
                  Text('→ ${job.target}', style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                ]),
                const SizedBox(height: 2),
                Text(
                  '${job.attempts} attempt(s)${job.lastError != null ? ' · ${job.lastError}' : ''}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Bo.textMuted, fontSize: 12),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Retry now',
            icon: const Icon(Icons.refresh, color: Bo.primary),
            onPressed: () async {
              await q.retryNow();
              if (context.mounted) AppToast.show(context, 'Retrying…');
            },
          ),
          IconButton(
            tooltip: 'Delete job',
            icon: const Icon(Icons.delete_outline, color: Bo.danger),
            onPressed: () => q.remove(job.id),
          ),
        ],
      ),
    );
  }
}
