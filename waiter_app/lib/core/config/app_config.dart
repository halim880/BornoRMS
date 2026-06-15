/// App-wide configuration. The API base URL is injected at build/run time so the
/// same binary can point at a dev machine on the LAN, staging, or prod:
///
///   flutter run --dart-define=API_BASE_URL=http://192.168.1.20:5000
///
/// A phone CANNOT reach `localhost` — that resolves to the phone itself. Use the
/// dev machine's LAN IP and make sure the API listens on it (and the firewall/port
/// is open). Android cleartext HTTP to a non-HTTPS LAN IP needs
/// `android:usesCleartextTraffic="true"` (already set in this project's manifest).
class AppConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000', // Android emulator -> host machine.
  );

  /// How often the floor/ready/requests data is re-fetched (mobile uses polling,
  /// not SignalR). Mirrors the Blazor console's live refresh.
  static const Duration pollInterval = Duration(seconds: 5);

  /// Resolves a product `imagePath` (e.g. "/img/products/x.jpg") to an absolute
  /// URL against the API host. Returns null for empty paths.
  static String? imageUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return baseUrl + (path.startsWith('/') ? path : '/$path');
  }
}
