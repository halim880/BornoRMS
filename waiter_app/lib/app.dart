import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/auth/auth_controller.dart';
import 'core/auth/auth_state.dart';
import 'core/providers/providers.dart';
import 'features/auth/login_screen.dart';
import 'features/shell/home_shell.dart';

class WaiterApp extends StatelessWidget {
  const WaiterApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'BornoBit Waiter',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: const Color(0xFFCB3A1A),
        scaffoldBackgroundColor: const Color(0xFFF5F5F7),
      ),
      home: const _AuthGate(),
    );
  }
}

/// Routes between login and the console based on auth state, and pauses polling
/// while the app is backgrounded.
class _AuthGate extends ConsumerStatefulWidget {
  const _AuthGate();
  @override
  ConsumerState<_AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends ConsumerState<_AuthGate> {
  late final AppLifecycleListener _lifecycle;

  @override
  void initState() {
    super.initState();
    _lifecycle = AppLifecycleListener(
      onStateChange: (state) {
        ref.read(pollingEnabledProvider.notifier).state =
            state == AppLifecycleState.resumed;
      },
    );
  }

  @override
  void dispose() {
    _lifecycle.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final status = ref.watch(authControllerProvider).status;
    switch (status) {
      case AuthStatus.authenticated:
        return const HomeShell();
      case AuthStatus.unknown:
        return const Scaffold(body: Center(child: CircularProgressIndicator()));
      default:
        return const LoginScreen();
    }
  }
}
