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
      return;
    }
    // Access token expired/missing — try a silent refresh so a returning user with a valid
    // refresh token stays signed in (refresh tokens live for Jwt:RefreshTokenDays).
    if (await _store.readRefreshToken() != null && await refreshSession()) {
      final user = await _store.readUser();
      state = AuthState(status: AuthStatus.authenticated, user: user);
      return;
    }
    await _store.clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  Future<void> login(String emailOrUsername, String password) async {
    state = const AuthState(status: AuthStatus.authenticating);
    try {
      final data = await ref.read(staffApiProvider).login(emailOrUsername, password);
      final token = data['accessToken'] as String;
      final expiry = DateTime.parse(data['expiresAtUtc'] as String).toUtc();
      final user = AuthUser.fromJson(data['user'] as Map<String, dynamic>);
      await _store.save(token, expiry, user, refreshToken: data['refreshToken'] as String?);
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } on ApiException catch (e) {
      state = AuthState(status: AuthStatus.unauthenticated, error: e.message);
    } catch (_) {
      state = const AuthState(
          status: AuthStatus.unauthenticated, error: 'Login failed. Try again.');
    }
  }

  /// Exchange the stored refresh token for a fresh access token. Returns false when there is no
  /// refresh token or the server rejects it. Invoked by the Dio 401 interceptor (single-flight)
  /// and by [_restore] on launch. Does not flip the UI to unauthenticated on failure — the caller
  /// decides (the interceptor falls through to [onUnauthorized]).
  Future<bool> refreshSession() async {
    final rt = await _store.readRefreshToken();
    if (rt == null || rt.isEmpty) return false;
    try {
      final data = await ref.read(staffApiProvider).refresh(rt);
      final token = data['accessToken'] as String;
      final expiry = DateTime.parse(data['expiresAtUtc'] as String).toUtc();
      final user = AuthUser.fromJson(data['user'] as Map<String, dynamic>);
      await _store.save(token, expiry, user, refreshToken: data['refreshToken'] as String?);
      if (state.status != AuthStatus.authenticated) {
        state = AuthState(status: AuthStatus.authenticated, user: user);
      }
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    final rt = await _store.readRefreshToken();
    if (rt != null && rt.isNotEmpty) {
      // Best-effort server-side revoke so a leaked refresh token can't outlive sign-out.
      try {
        await ref.read(staffApiProvider).logout(rt);
      } catch (_) {}
    }
    await _store.clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Called by the Dio 401 interceptor only after a refresh attempt has already failed
  /// (token already cleared).
  void onUnauthorized() {
    state = const AuthState(
        status: AuthStatus.unauthenticated, error: 'Session expired. Please sign in again.');
  }
}
