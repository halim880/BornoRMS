import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Coarse online/offline signal from the OS network stack. This reports *link
/// presence* (Wi-Fi/ethernet/mobile up), not end-to-end API reachability — good
/// enough to drive the offline banner and to know when to drain a write queue.
/// Defaults to `true` until the first reading so we never show "offline" on boot.
final connectivityProvider = StreamProvider<bool>((ref) async* {
  final connectivity = Connectivity();

  bool isOnline(List<ConnectivityResult> results) =>
      results.any((r) => r != ConnectivityResult.none);

  try {
    yield isOnline(await connectivity.checkConnectivity());
  } catch (_) {
    yield true; // platform without connectivity support → assume online
  }

  await for (final results in connectivity.onConnectivityChanged) {
    yield isOnline(results);
  }
});
