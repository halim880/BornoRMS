import '../../core/api/staff_api.dart';
import '../../core/config/app_config.dart';
import 'admin_models.dart';

/// Admin HTTP surface — the versioned `/api/v1/staff/admin/*` routes
/// (AdminEndpoints.cs). Lives as an extension so the feature stays
/// self-contained.
extension AdminApi on StaffApi {
  String get _base => '${AppConfig.apiPrefix}/staff/admin';

  // ---------- users ----------
  Future<List<UserRow>> adminUsers({bool includeInactive = true}) => client.guard(() async {
        final res = await client.dio
            .get('$_base/users', queryParameters: {'includeInactive': includeInactive});
        return (res.data as List)
            .map((e) => UserRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  /// Returns the generated password (when none supplied) for display.
  Future<String?> adminCreateUser({
    required String userName,
    required String email,
    required String fullName,
    required List<String> roles,
    String? initialPassword,
  }) =>
      client.guard(() async {
        final res = await client.dio.post('$_base/users', data: {
          'userName': userName,
          'email': email,
          'fullName': fullName,
          'roles': roles,
          if (initialPassword != null && initialPassword.isNotEmpty)
            'initialPassword': initialPassword,
        });
        final data = res.data as Map<String, dynamic>?;
        return data?['generatedPassword']?.toString();
      });

  Future<void> adminUpdateUser(
    String id, {
    required String userName,
    required String email,
    required String fullName,
    required List<String> roles,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/users/$id', data: {
          'userName': userName,
          'email': email,
          'fullName': fullName,
          'roles': roles,
        });
      });

  Future<String> adminResetPassword(String id) => client.guard(() async {
        final res = await client.dio.post('$_base/users/$id/reset-password');
        return (res.data as Map<String, dynamic>)['password']?.toString() ?? '';
      });

  Future<void> adminSetUserActive(String id, bool isActive) => client.guard(() async {
        await client.dio.post('$_base/users/$id/active', data: {'isActive': isActive});
      });

  // ---------- roles ----------
  Future<List<RoleRow>> adminRoles() => client.guard(() async {
        final res = await client.dio.get('$_base/roles');
        return (res.data as List)
            .map((e) => RoleRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminCreateRole({required String name, String? description}) =>
      client.guard(() async {
        await client.dio.post('$_base/roles', data: {
          'name': name,
          if (description != null && description.isNotEmpty) 'description': description,
        });
      });

  Future<void> adminUpdateRole(String id, {required String name, String? description}) =>
      client.guard(() async {
        await client.dio.patch('$_base/roles/$id', data: {
          'name': name,
          if (description != null && description.isNotEmpty) 'description': description,
        });
      });

  Future<void> adminDeleteRole(String id) => client.guard(() async {
        await client.dio.delete('$_base/roles/$id');
      });

  /// Roles for the permission pickers (excludes SuperAdmin).
  Future<List<PermissionRole>> adminPermissionRoles() => client.guard(() async {
        final res = await client.dio.get('$_base/permission-roles');
        return (res.data as List)
            .map((e) => PermissionRole.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  // ---------- menu permissions ----------
  Future<List<MenuPermissionNode>> adminMenuPermissions(String roleId) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_base/menu-permissions', queryParameters: {'roleId': roleId});
        return (res.data as List)
            .map((e) => MenuPermissionNode.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminSaveMenuPermissions(String roleId, List<String> permittedMenuIds) =>
      client.guard(() async {
        await client.dio.post('$_base/menu-permissions',
            data: {'roleId': roleId, 'permittedMenuIds': permittedMenuIds});
      });

  // ---------- module permissions ----------
  Future<List<ModulePermissionRow>> adminModulePermissions(String roleId) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_base/module-permissions', queryParameters: {'roleId': roleId});
        return (res.data as List)
            .map((e) => ModulePermissionRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminSaveModulePermissions(String roleId, List<String> permittedModuleIds) =>
      client.guard(() async {
        await client.dio.post('$_base/module-permissions',
            data: {'roleId': roleId, 'permittedModuleIds': permittedModuleIds});
      });

  // ---------- numbering scopes ----------
  Future<List<NumberingScopeRow>> adminNumberingScopes() => client.guard(() async {
        final res = await client.dio.get('$_base/numbering-scopes');
        return (res.data as List)
            .map((e) => NumberingScopeRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminCreateNumberingScope({
    required String code,
    required String name,
    required String prefix,
    required NumberingCadence cadence,
    required int digits,
    required bool resetByOutlet,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/numbering-scopes', data: {
          'code': code,
          'name': name,
          'prefix': prefix,
          'cadence': cadence.value,
          'digits': digits,
          'resetByOutlet': resetByOutlet,
        });
      });

  Future<void> adminUpdateNumberingScope(
    String id, {
    required String name,
    required String prefix,
    required NumberingCadence cadence,
    required int digits,
    required bool resetByOutlet,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/numbering-scopes/$id', data: {
          'name': name,
          'prefix': prefix,
          'cadence': cadence.value,
          'digits': digits,
          'resetByOutlet': resetByOutlet,
        });
      });

  Future<void> adminSetNumberingScopeActive(String id, bool isActive) =>
      client.guard(() async {
        await client.dio
            .post('$_base/numbering-scopes/$id/active', data: {'isActive': isActive});
      });

  // ---------- tenants ----------
  Future<List<TenantRow>> adminTenants({bool includeInactive = true}) =>
      client.guard(() async {
        final res = await client.dio
            .get('$_base/tenants', queryParameters: {'includeInactive': includeInactive});
        return (res.data as List)
            .map((e) => TenantRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminCreateTenant({
    required String name,
    required String subdomain,
    required String contactEmail,
    DateTime? licenseExpiresOnUtc,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/tenants', data: {
          'name': name,
          'subdomain': subdomain,
          'contactEmail': contactEmail,
          if (licenseExpiresOnUtc != null)
            'licenseExpiresOnUtc': licenseExpiresOnUtc.toUtc().toIso8601String(),
        });
      });

  Future<void> adminUpdateTenant(
    String id, {
    required String name,
    required String contactEmail,
    DateTime? licenseExpiresOnUtc,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/tenants/$id', data: {
          'name': name,
          'contactEmail': contactEmail,
          if (licenseExpiresOnUtc != null)
            'licenseExpiresOnUtc': licenseExpiresOnUtc.toUtc().toIso8601String(),
        });
      });

  Future<void> adminSetTenantActive(String id, bool isActive) => client.guard(() async {
        await client.dio.post('$_base/tenants/$id/active', data: {'isActive': isActive});
      });

  // ---------- modules ----------
  Future<List<ModuleRow>> adminModules() => client.guard(() async {
        final res = await client.dio.get('$_base/modules');
        return (res.data as List)
            .map((e) => ModuleRow.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<void> adminCreateModule({
    required String title,
    String? icon,
    int? displayOrder,
    String? requiredRole,
  }) =>
      client.guard(() async {
        await client.dio.post('$_base/modules', data: {
          'title': title,
          if (icon != null && icon.isNotEmpty) 'icon': icon,
          if (displayOrder != null) 'displayOrder': displayOrder,
          if (requiredRole != null && requiredRole.isNotEmpty) 'requiredRole': requiredRole,
        });
      });

  Future<void> adminUpdateModule(
    String id, {
    required String title,
    String? icon,
    required int displayOrder,
    String? requiredRole,
  }) =>
      client.guard(() async {
        await client.dio.patch('$_base/modules/$id', data: {
          'title': title,
          if (icon != null && icon.isNotEmpty) 'icon': icon,
          'displayOrder': displayOrder,
          if (requiredRole != null && requiredRole.isNotEmpty) 'requiredRole': requiredRole,
        });
      });

  Future<void> adminSetModuleActive(String id, bool isActive) => client.guard(() async {
        await client.dio.post('$_base/modules/$id/active', data: {'isActive': isActive});
      });
}
