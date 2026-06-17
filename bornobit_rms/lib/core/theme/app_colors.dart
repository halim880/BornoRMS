import 'package:flutter/material.dart';

/// Custom design tokens the Material [ColorScheme] can't express cleanly:
/// tints, hairline borders, the fixed dark nav surface, and semantic
/// container/border pairs. Read at runtime via
/// `Theme.of(context).extension<AppColors>()!`.
///
/// Radii, shadows and the two off-grid text styles (cart total, price) are
/// exposed as static members since they don't participate in colour lerp.
@immutable
class AppColors extends ThemeExtension<AppColors> {
  // -- core palette --------------------------------------------------------
  final Color canvas;
  final Color surface;
  final Color surfaceMuted;
  final Color surfaceTint;
  final Color border;
  final Color borderStrong;
  final Color dotGrid;

  // -- text ----------------------------------------------------------------
  final Color textPrimary;
  final Color textSecondary;
  final Color textTertiary;

  // -- accent --------------------------------------------------------------
  final Color accent;
  final Color accentHover;
  final Color accentTint;
  final Color accentTint2;
  final Color onAccent;

  // -- semantic ------------------------------------------------------------
  final Color danger;
  final Color dangerTint;
  final Color success;
  final Color successTint;
  final Color successBorder;
  final Color warning;

  // -- dark nav / sidebar (fixed, independent of the light theme) ----------
  final Color navBg;
  final Color navBgDeep;
  final Color navText;
  final Color navMuted;

  const AppColors({
    required this.canvas,
    required this.surface,
    required this.surfaceMuted,
    required this.surfaceTint,
    required this.border,
    required this.borderStrong,
    required this.dotGrid,
    required this.textPrimary,
    required this.textSecondary,
    required this.textTertiary,
    required this.accent,
    required this.accentHover,
    required this.accentTint,
    required this.accentTint2,
    required this.onAccent,
    required this.danger,
    required this.dangerTint,
    required this.success,
    required this.successTint,
    required this.successBorder,
    required this.warning,
    required this.navBg,
    required this.navBgDeep,
    required this.navText,
    required this.navMuted,
  });

  /// The one and only (light) instance — all literal tokens.
  static const AppColors light = AppColors(
    canvas: Color(0xFFF4F6F9),
    surface: Color(0xFFFFFFFF),
    surfaceMuted: Color(0xFFFAFBFC),
    surfaceTint: Color(0xFFF6F8FB),
    border: Color(0xFFE9ECF1),
    borderStrong: Color(0xFFDCE0E7),
    dotGrid: Color(0xFFD7DDE7),
    textPrimary: Color(0xFF0F172A),
    textSecondary: Color(0xFF5B677A),
    textTertiary: Color(0xFF97A1B1),
    accent: Color(0xFFEA580C),
    accentHover: Color(0xFFC2410C),
    accentTint: Color(0xFFFFF3EC),
    accentTint2: Color(0xFFFCE7D9),
    onAccent: Color(0xFFFFFFFF),
    danger: Color(0xFFDC2626),
    dangerTint: Color(0xFFFEF2F2),
    success: Color(0xFF15A34A),
    successTint: Color(0xFFEEFBF2),
    successBorder: Color(0xFFCFF0DB),
    warning: Color(0xFFD97706),
    navBg: Color(0xFF0E1522),
    navBgDeep: Color(0xFF0A0F1A),
    navText: Color(0xFFAEB7C6),
    navMuted: Color(0xFF67738A),
  );

  // -- radii ---------------------------------------------------------------
  static const double radiusCard = 7; // product cards
  static const double radiusPanel = 12; // section panels / cart panel
  static const double radiusChip = 11; // chips / order tabs
  static const double radiusButton = 12; // buttons
  static const double radiusInput = 12; // inputs

  // -- shadows (prefer these over Material elevation) ----------------------
  static const List<BoxShadow> shadowSoft = [
    BoxShadow(color: Color(0x0D0F172A), blurRadius: 3, offset: Offset(0, 1)),
    BoxShadow(color: Color(0x0A0F172A), blurRadius: 2, offset: Offset(0, 1)),
  ];
  static const List<BoxShadow> shadowLifted = [
    BoxShadow(
      color: Color(0x290F172A),
      blurRadius: 28,
      spreadRadius: -8,
      offset: Offset(0, 10),
    ),
  ];

  // -- off-grid text styles (not standard TextTheme slots) -----------------
  /// Cart grand total — 24sp / w800 / textPrimary, tabular figures.
  static const TextStyle displayTotal = TextStyle(
    fontSize: 24,
    fontWeight: FontWeight.w800,
    color: Color(0xFF0F172A),
    fontFeatures: [FontFeature.tabularFigures()],
  );

  /// Price text — 14sp / w700 / textPrimary, tabular figures.
  static const TextStyle priceText = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w700,
    color: Color(0xFF0F172A),
    fontFeatures: [FontFeature.tabularFigures()],
  );

  @override
  AppColors copyWith({
    Color? canvas,
    Color? surface,
    Color? surfaceMuted,
    Color? surfaceTint,
    Color? border,
    Color? borderStrong,
    Color? dotGrid,
    Color? textPrimary,
    Color? textSecondary,
    Color? textTertiary,
    Color? accent,
    Color? accentHover,
    Color? accentTint,
    Color? accentTint2,
    Color? onAccent,
    Color? danger,
    Color? dangerTint,
    Color? success,
    Color? successTint,
    Color? successBorder,
    Color? warning,
    Color? navBg,
    Color? navBgDeep,
    Color? navText,
    Color? navMuted,
  }) {
    return AppColors(
      canvas: canvas ?? this.canvas,
      surface: surface ?? this.surface,
      surfaceMuted: surfaceMuted ?? this.surfaceMuted,
      surfaceTint: surfaceTint ?? this.surfaceTint,
      border: border ?? this.border,
      borderStrong: borderStrong ?? this.borderStrong,
      dotGrid: dotGrid ?? this.dotGrid,
      textPrimary: textPrimary ?? this.textPrimary,
      textSecondary: textSecondary ?? this.textSecondary,
      textTertiary: textTertiary ?? this.textTertiary,
      accent: accent ?? this.accent,
      accentHover: accentHover ?? this.accentHover,
      accentTint: accentTint ?? this.accentTint,
      accentTint2: accentTint2 ?? this.accentTint2,
      onAccent: onAccent ?? this.onAccent,
      danger: danger ?? this.danger,
      dangerTint: dangerTint ?? this.dangerTint,
      success: success ?? this.success,
      successTint: successTint ?? this.successTint,
      successBorder: successBorder ?? this.successBorder,
      warning: warning ?? this.warning,
      navBg: navBg ?? this.navBg,
      navBgDeep: navBgDeep ?? this.navBgDeep,
      navText: navText ?? this.navText,
      navMuted: navMuted ?? this.navMuted,
    );
  }

  @override
  AppColors lerp(ThemeExtension<AppColors>? other, double t) {
    if (other is! AppColors) return this;
    return AppColors(
      canvas: Color.lerp(canvas, other.canvas, t)!,
      surface: Color.lerp(surface, other.surface, t)!,
      surfaceMuted: Color.lerp(surfaceMuted, other.surfaceMuted, t)!,
      surfaceTint: Color.lerp(surfaceTint, other.surfaceTint, t)!,
      border: Color.lerp(border, other.border, t)!,
      borderStrong: Color.lerp(borderStrong, other.borderStrong, t)!,
      dotGrid: Color.lerp(dotGrid, other.dotGrid, t)!,
      textPrimary: Color.lerp(textPrimary, other.textPrimary, t)!,
      textSecondary: Color.lerp(textSecondary, other.textSecondary, t)!,
      textTertiary: Color.lerp(textTertiary, other.textTertiary, t)!,
      accent: Color.lerp(accent, other.accent, t)!,
      accentHover: Color.lerp(accentHover, other.accentHover, t)!,
      accentTint: Color.lerp(accentTint, other.accentTint, t)!,
      accentTint2: Color.lerp(accentTint2, other.accentTint2, t)!,
      onAccent: Color.lerp(onAccent, other.onAccent, t)!,
      danger: Color.lerp(danger, other.danger, t)!,
      dangerTint: Color.lerp(dangerTint, other.dangerTint, t)!,
      success: Color.lerp(success, other.success, t)!,
      successTint: Color.lerp(successTint, other.successTint, t)!,
      successBorder: Color.lerp(successBorder, other.successBorder, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      navBg: Color.lerp(navBg, other.navBg, t)!,
      navBgDeep: Color.lerp(navBgDeep, other.navBgDeep, t)!,
      navText: Color.lerp(navText, other.navText, t)!,
      navMuted: Color.lerp(navMuted, other.navMuted, t)!,
    );
  }
}

/// `context.colorScheme` / `context.appColors` sugar.
extension AppColorsContext on BuildContext {
  ColorScheme get colorScheme => Theme.of(this).colorScheme;
  AppColors get appColors => Theme.of(this).extension<AppColors>()!;
}
