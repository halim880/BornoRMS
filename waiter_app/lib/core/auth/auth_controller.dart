import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../api/api_exception.dart';
import '../models/dtos.dart';
import '../providers/providers.dart';
import 'auth_state.dart';
import 'token_store.dart';

final authControllerProvider =
    NotifierProvider<AuthController, AuthState>(AuthController.new);

class AuthController extends Notifier<AuthState> {
  TokenStore get _store => ref.read(tokenStoreProvider);

  @override
  AuthState build() {
    _restore();
    return const AuthState.unknown();
  }

  Future<void> _restore() async {
    if (await _store.hasValidToken()) {
      final user = await _store.readUser();
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } else {
      await _store.clear();
      state = const AuthState(status: AuthStatus.unauthenticated);
    }
  }

  Future<void> login(String emailOrUsername, String password) async {
    state = const AuthState(status: AuthStatus.authenticating);
    try {
      final data = await ref.read(waiterApiProvider).login(emailOrUsername, password);
      final token = data['accessToken'] as String;
      final expiry = DateTime.parse(data['expiresAtUtc'] as String).toUtc();
      final user = AuthUser.fromJson(data['user'] as Map<String, dynamic>);
      await _store.save(token, expiry, user);
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } on ApiException catch (e) {
      state = AuthState(status: AuthStatus.unauthenticated, error: e.message);
    } catch (_) {
      state = const AuthState(
          status: AuthStatus.unauthenticated, error: 'Login failed. Try again.');
    }
  }

  Future<void> logout() async {
    await _store.clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Called by the Dio 401 interceptor (token already cleared).
  void onUnauthorized() {
    state = const AuthState(
        status: AuthStatus.unauthenticated, error: 'Session expired. Please sign in again.');
  }
}
