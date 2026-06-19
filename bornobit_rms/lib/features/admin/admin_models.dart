// DTOs for the Admin screens — mirror the Blazor admin pages (Users.razor,
// Roles.razor, MenuPermissions.razor, ModulePermissions.razor,
// NumberingScopes.razor, Tenants.razor, Modules.razor). JSON field names match
// the C# DTO property names (camelCase).
//
// NOTE (project rule): we never name a Dart symbol `Roles` — it shadows the
// domain Roles type. Role-list rows are `RoleRow`, the permission picker option
// is `PermissionRole`.

int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.now() : DateTime.parse(v.toString()).toLocal();
DateTime? _dtOrNull(dynamic v) => v == null ? null : DateTime.parse(v.toString()).toLocal();

/// A staff user row (mirrors UserDto).
class UserRow {
  final String id;
  final String userName;
  final String email;
  final String fullName;
  final bool isActive;
  final bool isSuperAdmin;
  final List<String> roles;
  final DateTime createdAtUtc;

  UserRow({
    required this.id,
    required this.userName,
    required this.email,
    required this.fullName,
    required this.isActive,
    required this.isSuperAdmin,
    required this.roles,
    required this.createdAtUtc,
  });

  factory UserRow.fromJson(Map<String, dynamic> j) => UserRow(
        id: _s(j['id']),
        userName: _s(j['userName']),
        email: _s(j['email']),
        fullName: _s(j['fullName']),
        isActive: j['isActive'] as bool? ?? false,
        isSuperAdmin: j['isSuperAdmin'] as bool? ?? false,
        roles: (j['roles'] as List? ?? []).map((e) => e.toString()).toList(),
        createdAtUtc: _dt(j['createdAtUtc']),
      );
}

/// A role list row (mirrors RoleListItemDto). Named RoleRow (never `Roles`).
class RoleRow {
  final String id;
  final String name;
  final String? description;
  final int userCount;
  final bool isSystem;

  RoleRow({
    required this.id,
    required this.name,
    required this.description,
    required this.userCount,
    required this.isSystem,
  });

  factory RoleRow.fromJson(Map<String, dynamic> j) => RoleRow(
        id: _s(j['id']),
        name: _s(j['name']),
        description: _sOrNull(j['description']),
        userCount: _i(j['userCount']),
        isSystem: j['isSystem'] as bool? ?? false,
      );
}

/// A role option for the permission pickers (mirrors RoleDto).
class PermissionRole {
  final String id;
  final String name;

  PermissionRole({required this.id, required this.name});

  factory PermissionRole.fromJson(Map<String, dynamic> j) =>
      PermissionRole(id: _s(j['id']), name: _s(j['name']));
}

/// A node in the menu-permissions tree (mirrors MenuPermissionNodeDto).
class MenuPermissionNode {
  final String id;
  final String title;
  final String? url;
  final String? icon;
  final int displayOrder;
  final bool isPermitted;
  final List<MenuPermissionNode> children;

  MenuPermissionNode({
    required this.id,
    required this.title,
    required this.url,
    required this.icon,
    required this.displayOrder,
    required this.isPermitted,
    required this.children,
  });

  factory MenuPermissionNode.fromJson(Map<String, dynamic> j) => MenuPermissionNode(
        id: _s(j['id']),
        title: _s(j['title']),
        url: _sOrNull(j['url']),
        icon: _sOrNull(j['icon']),
        displayOrder: _i(j['displayOrder']),
        isPermitted: j['isPermitted'] as bool? ?? false,
        children: (j['children'] as List? ?? [])
            .map((e) => MenuPermissionNode.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// A module (root menu) permission row (mirrors ModulePermissionDto).
class ModulePermissionRow {
  final String id;
  final String title;
  final String? icon;
  final int displayOrder;
  final bool isPermitted;
  final int permittedChildCount;

  ModulePermissionRow({
    required this.id,
    required this.title,
    required this.icon,
    required this.displayOrder,
    required this.isPermitted,
    required this.permittedChildCount,
  });

  factory ModulePermissionRow.fromJson(Map<String, dynamic> j) => ModulePermissionRow(
        id: _s(j['id']),
        title: _s(j['title']),
        icon: _sOrNull(j['icon']),
        displayOrder: _i(j['displayOrder']),
        isPermitted: j['isPermitted'] as bool? ?? false,
        permittedChildCount: _i(j['permittedChildCount']),
      );
}

/// Numbering cadence — matches the C# NumberingCadence enum (byte-backed: 1/2/3).
enum NumberingCadence {
  yearly(1, 'Yearly'),
  monthly(2, 'Monthly'),
  daily(3, 'Daily');

  final int value;
  final String label;
  const NumberingCadence(this.value, this.label);

  static NumberingCadence fromValue(dynamic v) {
    final n = v is num ? v.toInt() : int.tryParse(v?.toString() ?? '') ?? 1;
    return NumberingCadence.values.firstWhere((c) => c.value == n,
        orElse: () => NumberingCadence.yearly);
  }
}

/// A numbering scope row (mirrors NumberingScopeDto).
class NumberingScopeRow {
  final String id;
  final String code;
  final String name;
  final String prefix;
  final NumberingCadence cadence;
  final int digits;
  final bool resetByOutlet;
  final bool isActive;
  final DateTime createdAtUtc;

  NumberingScopeRow({
    required this.id,
    required this.code,
    required this.name,
    required this.prefix,
    required this.cadence,
    required this.digits,
    required this.resetByOutlet,
    required this.isActive,
    required this.createdAtUtc,
  });

  factory NumberingScopeRow.fromJson(Map<String, dynamic> j) => NumberingScopeRow(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        prefix: _s(j['prefix']),
        cadence: NumberingCadence.fromValue(j['cadence']),
        digits: _i(j['digits']),
        resetByOutlet: j['resetByOutlet'] as bool? ?? false,
        isActive: j['isActive'] as bool? ?? false,
        createdAtUtc: _dt(j['createdAtUtc']),
      );
}

/// A tenant row (mirrors TenantDto).
class TenantRow {
  final String id;
  final String name;
  final String subdomain;
  final String contactEmail;
  final bool isActive;
  final DateTime? licenseExpiresOnUtc;
  final DateTime createdAtUtc;

  TenantRow({
    required this.id,
    required this.name,
    required this.subdomain,
    required this.contactEmail,
    required this.isActive,
    required this.licenseExpiresOnUtc,
    required this.createdAtUtc,
  });

  factory TenantRow.fromJson(Map<String, dynamic> j) => TenantRow(
        id: _s(j['id']),
        name: _s(j['name']),
        subdomain: _s(j['subdomain']),
        contactEmail: _s(j['contactEmail']),
        isActive: j['isActive'] as bool? ?? false,
        licenseExpiresOnUtc: _dtOrNull(j['licenseExpiresOnUtc']),
        createdAtUtc: _dt(j['createdAtUtc']),
      );
}

/// A module admin row (mirrors ModuleDto).
class ModuleRow {
  final String id;
  final String title;
  final String? icon;
  final int displayOrder;
  final bool isActive;
  final String? requiredRole;
  final String? firstAccessibleUrl;
  final int accessibleMenuCount;

  ModuleRow({
    required this.id,
    required this.title,
    required this.icon,
    required this.displayOrder,
    required this.isActive,
    required this.requiredRole,
    required this.firstAccessibleUrl,
    required this.accessibleMenuCount,
  });

  factory ModuleRow.fromJson(Map<String, dynamic> j) => ModuleRow(
        id: _s(j['id']),
        title: _s(j['title']),
        icon: _sOrNull(j['icon']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? false,
        requiredRole: _sOrNull(j['requiredRole']),
        firstAccessibleUrl: _sOrNull(j['firstAccessibleUrl']),
        accessibleMenuCount: _i(j['accessibleMenuCount']),
      );
}
