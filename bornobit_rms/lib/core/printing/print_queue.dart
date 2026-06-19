import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';

import 'print_job.dart';
import 'print_service.dart' show sendBytesToPrinter;

/// Persistent offline print queue. When the thermal printer is unreachable a job
/// is parked here instead of being lost, and a background timer keeps re-sending
/// it until the printer answers (or the operator deletes it). Jobs survive app
/// restarts via a JSON file in the app-support directory.
///
/// Kept alive for the whole session (not autoDispose) so the retry timer runs
/// regardless of which screen is showing.
final printQueueProvider =
    NotifierProvider<PrintQueueNotifier, List<PrintJob>>(PrintQueueNotifier.new);

class PrintQueueNotifier extends Notifier<List<PrintJob>> {
  static const _retryEvery = Duration(seconds: 25);
  Timer? _timer;
  File? _file;
  bool _flushing = false;

  @override
  List<PrintJob> build() {
    ref.onDispose(() {
      _timer?.cancel();
      _timer = null;
    });
    // Fire-and-forget load; state starts empty and fills in when the file is read.
    unawaited(_load());
    return const [];
  }

  Future<File> _ensureFile() async {
    if (_file != null) return _file!;
    final dir = await getApplicationSupportDirectory();
    return _file = File('${dir.path}/print_queue.json');
  }

  Future<void> _load() async {
    try {
      final f = await _ensureFile();
      if (await f.exists()) {
        final raw = await f.readAsString();
        if (raw.trim().isNotEmpty) {
          final list = (jsonDecode(raw) as List)
              .map((e) => PrintJob.fromJson(e as Map<String, dynamic>))
              .toList();
          state = list;
        }
      }
    } catch (_) {
      // A corrupt queue file should never crash startup; just start empty.
    }
    _ensureTimer();
    if (state.isNotEmpty) unawaited(flush());
  }

  Future<void> _persist() async {
    try {
      final f = await _ensureFile();
      await f.writeAsString(jsonEncode(state.map((e) => e.toJson()).toList()));
    } catch (_) {
      // Persistence is best-effort; an unwritable temp dir shouldn't block printing.
    }
  }

  void _ensureTimer() {
    _timer ??= Timer.periodic(_retryEvery, (_) {
      if (state.isNotEmpty) unawaited(flush());
    });
  }

  /// Park a job and immediately attempt to send it.
  Future<void> enqueue(PrintJob job) async {
    state = [...state, job];
    await _persist();
    _ensureTimer();
    await flush();
  }

  /// Attempt every parked job once. Successful jobs are removed; failures have
  /// their attempt count + last error updated. Re-entrancy is guarded so the
  /// timer and a manual retry can't overlap.
  Future<void> flush() async {
    if (_flushing) return;
    _flushing = true;
    try {
      for (final job in [...state]) {
        try {
          await sendBytesToPrinter(job.host, job.port, base64Decode(job.bytesB64));
          state = state.where((j) => j.id != job.id).toList();
        } catch (e) {
          state = [
            for (final j in state)
              if (j.id == job.id) j.copyWith(attempts: j.attempts + 1, lastError: e.toString()) else j
          ];
        }
        await _persist();
      }
    } finally {
      _flushing = false;
    }
  }

  Future<void> retryNow() => flush();

  Future<void> remove(String id) async {
    state = state.where((j) => j.id != id).toList();
    await _persist();
  }

  Future<void> clear() async {
    state = const [];
    await _persist();
  }
}
