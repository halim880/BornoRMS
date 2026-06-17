import 'dart:io' show Platform;

/// App-wide configuration. The app runs on Windows desktop, Android/iOS tablets,
/// and phones, so the API base URL adapts per platform and can always be
/// overridden at run/build time:
///
///   flutter run --dart-define=API_BASE_URL=http://192.168.1.20:5000
///
/// Defaults:
///   - Android emulator → 10.0.2.2 (the host machine; `localhost` would be the device itself)
///   - Desktop / iOS simulator → localhost
///   - Physical devices → MUST pass --dart-define with the dev machine's LAN IP.
class AppConfig {
  static const String _override = String.fromEnvironment('API_BASE_URL');

  static String get baseUrl {
    if (_override.isNotEmpty) return _override;
    if (Platform.isAndroid) return 'http://10.0.2.2:5000';
    return 'http://localhost:5000';
  }

  /// All staff endpoints live under the versioned group (/api/v1/...).
  static const String apiPrefix = '/api/v1';

  /// Dashboard re-fetch cadence. The Blazor console pushes live updates over
  /// SignalR; Phase 1 mirrors that with polling (SignalR is deferred).
  static const Duration pollInterval = Duration(seconds: 15);

  /// Resolves a product `imagePath` (e.g. "/img/products/x.jpg") to an absolute
  /// URL against the API host. Returns null for empty paths.
  static String? imageUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return baseUrl + (path.startsWith('/') ? path : '/$path');
  }
}
