import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';

export 'app_colors.dart';

/// Legacy token shim. Older widgets read `Bo.*`; the names stay but the values
/// now point at the orange POS palette (mirrors [AppColors.light]). These must
/// be literal `const Color`s because call sites use them in const contexts.
/// Prefer reading `context.appColors` / `context.colorScheme` in new code.
class Bo {
  // Brand → accent
  static const primary = Color(0xFFEA580C); // accent
  static const primaryStrong = Color(0xFFC2410C); // accentHover
  static const primarySoft = Color(0xFFFCE7D9); // accentTint2
  static const primaryTint = Color(0xFFFFF3EC); // accentTint
  static const primaryEmphasis = Color(0xFFC2410C); // accentHover

  static const accent = Color(0xFFD97706); // warning (kept distinct in charts)

  // Neutrals (slate scale → canvas/surface/border/text ramp)
  static const slate50 = Color(0xFFFAFBFC); // surfaceMuted
  static const slate100 = Color(0xFFF4F6F9); // canvas
  static const slate200 = Color(0xFFE9ECF1); // border
  static const slate300 = Color(0xFFDCE0E7); // borderStrong
  static const slate400 = Color(0xFF97A1B1); // textTertiary
  static const slate500 = Color(0xFF5B677A); // textSecondary
  static const slate600 = Color(0xFF5B677A);
  static const slate700 = Color(0xFF5B677A);
  static const slate800 = Color(0xFF0F172A); // textPrimary
  static const slate900 = Color(0xFF0F172A);

  static const text = Color(0xFF0F172A); // textPrimary
  static const textMuted = Color(0xFF5B677A); // textSecondary
  static const textSubtle = Color(0xFF97A1B1); // textTertiary
  static const border = Color(0xFFE9ECF1);
  static const borderStrong = Color(0xFFDCE0E7);
  static const bgSoft = Color(0xFFFAFBFC); // surfaceMuted

  // Semantic
  static const success = Color(0xFF15A34A);
  static const successSoft = Color(0xFFEEFBF2); // successTint
  static const danger = Color(0xFFDC2626);
  static const dangerSoft = Color(0xFFFEF2F2); // dangerTint
  static const warning = Color(0xFFD97706);
  static const warningSoft = Color(0xFFFEF3C7); // no token; legacy chips
  static const info = Color(0xFF2563EB); // no token; legacy charts
  static const infoSoft = Color(0xFFDBEAFE);

  // Radii (legacy scale; new code uses AppColors.radius*)
  static const radiusSm = 6.0;
  static const radiusMd = 8.0;
  static const radiusLg = 12.0;

  static const surface = Color(0xFFFFFFFF);
  static const scaffold = Color(0xFFF4F6F9); // canvas
}

ThemeData buildBornoTheme() {
  const c = AppColors.light;

  final scheme = ColorScheme(
    brightness: Brightness.light,
    primary: c.accent,
    onPrimary: c.onAccent,
    primaryContainer: c.accentTint,
    onPrimaryContainer: c.accentHover,
    secondary: c.success,
    onSecondary: const Color(0xFFFFFFFF),
    secondaryContainer: c.successTint,
    onSecondaryContainer: c.success,
    error: c.danger,
    onError: const Color(0xFFFFFFFF),
    errorContainer: c.dangerTint,
    onErrorContainer: c.danger,
    surface: c.surface,
    onSurface: c.textPrimary,
    onSurfaceVariant: c.textSecondary,
    surfaceContainerLowest: const Color(0xFFFFFFFF),
    surfaceContainerLow: c.surfaceMuted,
    surfaceContainer: c.surfaceTint,
    surfaceContainerHigh: c.canvas,
    outline: c.border,
    outlineVariant: c.borderStrong,
  );

  // Inter mapped onto the typographic scale. The two numeric styles with
  // tabular figures (displayTotal / priceText) live in AppColors.
  final textTheme = GoogleFonts.interTextTheme().copyWith(
    titleLarge: GoogleFonts.inter(
      fontSize: 18,
      fontWeight: FontWeight.w700,
      color: c.textPrimary,
    ),
    titleMedium: GoogleFonts.inter(
      fontSize: 16,
      fontWeight: FontWeight.w700,
      color: c.textPrimary,
    ),
    bodyLarge: GoogleFonts.inter(
      fontSize: 14,
      fontWeight: FontWeight.w600,
      color: c.textPrimary,
    ),
    bodyMedium: GoogleFonts.inter(
      fontSize: 13,
      fontWeight: FontWeight.w500,
      color: c.textSecondary,
    ),
    bodySmall: GoogleFonts.inter(
      fontSize: 12,
      fontWeight: FontWeight.w500,
      color: c.textTertiary,
    ),
    labelLarge: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600),
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: c.canvas,
    textTheme: textTheme,
    extensions: const [AppColors.light],

    // Section / cart panels: surface bg, no border, soft shadow applied by the
    // widgets that wrap a DecoratedBox with AppColors.shadowSoft. Plain Cards
    // stay borderless on white at radius 16.
    cardTheme: CardThemeData(
      color: c.surface,
      elevation: 0,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusPanel),
      ),
      margin: EdgeInsets.zero,
    ),

    // Order line items: flat rows on white split by 1px border dividers.
    dividerTheme: DividerThemeData(color: c.border, thickness: 1, space: 1),

    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: c.surface,
      hintStyle: TextStyle(color: c.textTertiary),
      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusInput),
        borderSide: BorderSide(color: c.border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusInput),
        borderSide: BorderSide(color: c.border),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusInput),
        borderSide: BorderSide(color: c.accent, width: 1.5),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusInput),
        borderSide: BorderSide(color: c.danger),
      ),
    ),

    // Primary CTA: accent bg, white text, radius 12.
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: c.accent,
        foregroundColor: c.onAccent,
        disabledBackgroundColor: c.accentTint,
        disabledForegroundColor: c.onAccent,
        textStyle: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600),
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppColors.radiusButton),
        ),
      ),
    ),

    // Receipt/KOT-style: surface bg, borderStrong outline, textSecondary.
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        backgroundColor: c.surface,
        foregroundColor: c.textSecondary,
        side: BorderSide(color: c.borderStrong),
        textStyle: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppColors.radiusButton),
        ),
      ),
    ),

    // Cancel/destructive: transparent, textTertiary, danger on press.
    textButtonTheme: TextButtonThemeData(
      style: ButtonStyle(
        foregroundColor: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.pressed)) return c.danger;
          return c.textTertiary;
        }),
        textStyle: WidgetStatePropertyAll(
          GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600),
        ),
        shape: WidgetStatePropertyAll(
          RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppColors.radiusButton),
          ),
        ),
      ),
    ),

    // Neutral chip: surfaceMuted bg/border/textSecondary, radius 11.
    chipTheme: ChipThemeData(
      backgroundColor: c.surfaceMuted,
      side: BorderSide(color: c.border),
      labelStyle: TextStyle(
        color: c.textSecondary,
        fontSize: 13,
        fontWeight: FontWeight.w500,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppColors.radiusChip),
      ),
    ),

    dataTableTheme: DataTableThemeData(
      headingTextStyle: TextStyle(
        fontWeight: FontWeight.w600,
        color: c.textSecondary,
        fontSize: 13,
      ),
      dataTextStyle: TextStyle(color: c.textPrimary, fontSize: 13),
    ),
  );
}

/// Maps a semantic tone name to (background, foreground) for status chips.
/// Backed by the new tokens; the success tone pairs with [AppColors.successBorder].
({Color bg, Color fg}) toneColors(String tone) => switch (tone) {
      'success' => (bg: Bo.successSoft, fg: Bo.success),
      'danger' => (bg: Bo.dangerSoft, fg: Bo.danger),
      'warning' => (bg: Bo.warningSoft, fg: Bo.warning),
      'info' => (bg: Bo.infoSoft, fg: Bo.info),
      'primary' => (bg: Bo.primaryTint, fg: Bo.primaryEmphasis),
      _ => (bg: Bo.bgSoft, fg: Bo.textMuted),
    };
