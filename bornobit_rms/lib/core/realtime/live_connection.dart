import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../auth/auth_controller.dart';
import '../auth/auth_state.dart';
import '../config/app_config.dart';
import '../providers/providers.dart';

/// Well-known "changed" scopes pushed by the API `LiveHub` (mirror of the backend
/// `LiveScopes`). A tick carries no data — it only tells clients *what kind* of thing
/// changed so each screen can re-run its own authenticated query selectively.
class LiveScope {
  static const all = 'all';
  static const orders = 'orders';
  static const kitchen = 'kitchen';
  static const requests = 'requests';
  static const tables = 'tables';
  static const sessions = 'sessions';
  static const payments = 'payments';
  static const inventory = 'inventory';
  static const delivery = 'delivery';
}

enum LiveStatus { disconnected, connecting, connected, reconnecting }

/// Owns the SignalR connection to `/hubs/live`. Connects whenever the user is
/// authenticated and re-broadcasts every incoming scope tick on [scopeStream];
/// `PollingNotifier`s subscribe and refresh themselves on a matching scope. The
/// 15s/fallback poll stays as a backstop, so a dropped socket degrades gracefully.
final liveControllerProvider =
    NotifierProvider<LiveController, LiveStatus>(LiveController.new);

/// Per-tick scope stream the polling notifiers listen to. Watching the controller
/// keeps the connection alive and re-subscribes if it is rebuilt (e.g. on login).
final liveTickProvider = StreamProvider<String>((ref) {
  return ref.watch(liveControllerProvider.notifier).scopeStream;
});

class LiveController extends Notifier<LiveStatus> {
  HubConnection? _conn;
  final StreamController<String> _scopes = StreamController<String>.broadcast();
  bool _disposed = false;

  Stream<String> get scopeStream => _scopes.stream;

  @override
  LiveStatus build() {
    final authed =
        ref.watch(authControllerProvider).status == AuthStatus.authenticated;

    ref.onDispose(() {
      _disposed = true;
      _conn?.stop();
      _conn = null;
      _scopes.close();
    });

    if (authed) {
      Future.microtask(_connect);
      return LiveStatus.connecting;
    }
    return LiveStatus.disconnected;
  }

  Future<void> _connect() async {
    if (_disposed || _conn != null) return;

    final store = ref.read(tokenStoreProvider);
    final hub = HubConnectionBuilder()
        .withUrl(
          '${AppConfig.baseUrl}/hubs/live',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => (await store.readToken()) ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();
    _conn = hub;

    hub.on('changed', (args) {
      if (_disposed || _scopes.isClosed) return;
      final scope = (args != null && args.isNotEmpty)
          ? (args[0]?.toString() ?? LiveScope.all)
          : LiveScope.all;
      _scopes.add(scope);
    });
    hub.onclose(({error}) {
      if (!_disposed) state = LiveStatus.disconnected;
    });
    hub.onreconnecting(({error}) {
      if (!_disposed) state = LiveStatus.reconnecting;
    });
    hub.onreconnected(({connectionId}) {
      if (!_disposed) state = LiveStatus.connected;
    });

    try {
      await hub.start();
      if (!_disposed) state = LiveStatus.connected;
    } catch (_) {
      // Hub unreachable (e.g. older backend) — fall back to polling and retry.
      _conn = null;
      if (!_disposed) {
        state = LiveStatus.disconnected;
        Future.delayed(const Duration(seconds: 5), () {
          if (!_disposed) _connect();
        });
      }
    }
  }
}
