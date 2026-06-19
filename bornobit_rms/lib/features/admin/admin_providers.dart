import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/providers.dart';
import 'admin_api.dart';
import 'admin_models.dart';

/// All staff users (active + inactive). Invalidate after a mutation.
final adminUsersProvider =
    FutureProvider<List<UserRow>>((ref) => ref.read(staffApiProvider).adminUsers());

/// All roles with user counts. Also feeds the user create/edit role picker.
final adminRolesProvider =
    FutureProvider<List<RoleRow>>((ref) => ref.read(staffApiProvider).adminRoles());

/// Roles for the permission pickers (excludes SuperAdmin).
final permissionRolesProvider = FutureProvider<List<PermissionRole>>(
    (ref) => ref.read(staffApiProvider).adminPermissionRoles());

/// Menu-permission tree for the role selected on the Menu Permissions screen.
final menuPermissionsProvider = FutureProvider.family<List<MenuPermissionNode>, String>(
    (ref, roleId) => ref.read(staffApiProvider).adminMenuPermissions(roleId));

/// Module-permission rows for the role selected on the Module Permissions screen.
final modulePermissionsProvider = FutureProvider.family<List<ModulePermissionRow>, String>(
    (ref, roleId) => ref.read(staffApiProvider).adminModulePermissions(roleId));

/// All numbering scopes (active + inactive).
final numberingScopesProvider = FutureProvider<List<NumberingScopeRow>>(
    (ref) => ref.read(staffApiProvider).adminNumberingScopes());

/// All tenants (active + inactive).
final tenantsProvider =
    FutureProvider<List<TenantRow>>((ref) => ref.read(staffApiProvider).adminTenants());

/// All modules (root menus, active + inactive).
final modulesProvider =
    FutureProvider<List<ModuleRow>>((ref) => ref.read(staffApiProvider).adminModules());
