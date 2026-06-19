import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/dtos.dart';

/// Persists the staff JWT + expiry + the logged-in user in the OS secure store.
class TokenStore {
  static const _kToken = 'access_token';
  static const _kExpiry = 'expires_at_utc';
  static const _kUser = 'auth_user';
  static const _kRefresh = 'refresh_token';

  final FlutterSecureStorage _storage;
  TokenStore([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  Future<void> save(String token, DateTime expiresAtUtc, AuthUser user, {String? refreshToken}) async {
    await _storage.write(key: _kToken, value: token);
    await _storage.write(key: _kExpiry, value: expiresAtUtc.toUtc().toIso8601String());
    await _storage.write(key: _kUser, value: jsonEncode(user.toJson()));
    // Rotation hands back a new refresh token each time; only overwrite when present so a
    // call that doesn't return one (shouldn't happen) can't wipe the stored token.
    if (refreshToken != null && refreshToken.isNotEmpty) {
      await _storage.write(key: _kRefresh, value: refreshToken);
    }
  }

  Future<void> clear() async {
    await _storage.delete(key: _kToken);
    await _storage.delete(key: _kExpiry);
    await _storage.delete(key: _kUser);
    await _storage.delete(key: _kRefresh);
  }

  Future<String?> readToken() => _storage.read(key: _kToken);

  Future<String?> readRefreshToken() => _storage.read(key: _kRefresh);

  Future<DateTime?> readExpiry() async {
    final raw = await _storage.read(key: _kExpiry);
    return raw == null ? null : DateTime.tryParse(raw)?.toUtc();
  }

  Future<AuthUser?> readUser() async {
    final raw = await _storage.read(key: _kUser);
    if (raw == null) return null;
    try {
      return AuthUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  /// True when a non-expired token exists (30s safety margin).
  Future<bool> hasValidToken() async {
    final token = await readToken();
    if (token == null) return false;
    final exp = await readExpiry();
    if (exp == null) return false;
    return exp.isAfter(DateTime.now().toUtc().add(const Duration(seconds: 30)));
  }
}
