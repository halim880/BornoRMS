import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import 'settings_models.dart';

/// App Settings HTTP surface — the versioned `/api/v1/staff/settings` routes
/// (SettingsEndpoints.cs). GET is staff-readable; the update is admin-only.
/// Lives as an extension so the feature stays self-contained.
extension SettingsApi on StaffApi {
  String get _base => '${AppConfig.apiPrefix}/staff/settings';

  /// GET current restaurant settings.
  Future<AppSettings> getSettings() => client.guard(() async {
        final res = await client.dio.get('$_base/');
        return AppSettings.fromJson(res.data as Map<String, dynamic>);
      });

  /// PUT updated settings (admin only). Returns the persisted values.
  Future<AppSettings> updateSettings(AppSettings settings) =>
      client.guard(() async {
        final res = await client.dio.put('$_base/', data: settings.toJson());
        return AppSettings.fromJson(res.data as Map<String, dynamic>);
      });
}
