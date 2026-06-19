import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import 'settings_api.dart';
import 'settings_models.dart';

/// The current restaurant settings. Refresh by
/// `ref.invalidate(appSettingsProvider)` after a save.
final appSettingsProvider = FutureProvider<AppSettings>(
    (ref) => ref.read(staffApiProvider).getSettings());
