import 'dart:ui' show Locale;

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Supported app languages. English is the default; Bengali is opt-in.
const supportedLocales = <Locale>[Locale('en'), Locale('bn')];

/// Persists the chosen UI language locally (device preference, not server state).
/// Reuses the existing secure store — the value isn't sensitive, but it keeps us
/// from adding a second storage dependency.
class LocaleStore {
  static const _key = 'app_locale';
  final FlutterSecureStorage _storage;
  LocaleStore([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  Future<String?> read() => _storage.read(key: _key);
  Future<void> write(String code) => _storage.write(key: _key, value: code);
  Future<void> clear() => _storage.delete(key: _key);
}

final localeStoreProvider = Provider<LocaleStore>((ref) => LocaleStore());

/// Current UI locale. `null` → follow the default (English, the first supported
/// locale). Set to `Locale('bn')` for Bengali. Restored from storage on build.
final localeProvider = NotifierProvider<LocaleController, Locale?>(LocaleController.new);

class LocaleController extends Notifier<Locale?> {
  @override
  Locale? build() {
    _restore();
    return null;
  }

  Future<void> _restore() async {
    final code = await ref.read(localeStoreProvider).read();
    if (code != null && supportedLocales.any((l) => l.languageCode == code)) {
      state = Locale(code);
    }
  }

  /// Switch language and persist. Pass `null` to fall back to the default.
  Future<void> setLocale(Locale? locale) async {
    state = locale;
    final store = ref.read(localeStoreProvider);
    if (locale == null) {
      await store.clear();
    } else {
      await store.write(locale.languageCode);
    }
  }

  /// True when Bengali is active — used to pick the image-based receipt path.
  bool get isBengali => state?.languageCode == 'bn';
}

/// Convenience: is the app currently showing Bengali?
final isBengaliProvider = Provider<bool>((ref) {
  final locale = ref.watch(localeProvider);
  return locale?.languageCode == 'bn';
});
