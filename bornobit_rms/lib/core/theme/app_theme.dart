import 'package:flutter/material.dart';

/// Borno UI design tokens (mirrors the staff console's `--bo-*` CSS vars in
/// wwwroot/app.css), expressed as Flutter colors. Brand-matched, but the widgets
/// themselves use idiomatic Material 3.
class Bo {
  // Brand (cyan-700)
  static const primary = Color(0xFF0E7490);
  static const primaryStrong = Color(0xFF155E75);
  static const primarySoft = Color(0xFFCFFAFE);
  static const primaryTint = Color(0xFFECFEFF);
  static const primaryEmphasis = Color(0xFF164E63);

  static const accent = Color(0xFFF59E0B);

  // Slate neutrals
  static const slate50 = Color(0xFFF8FAFC);
  static const slate100 = Color(0xFFF1F5F9);
  static const slate200 = Color(0xFFE2E8F0);
  static const slate300 = Color(0xFFCBD5E1);
  static const slate400 = Color(0xFF94A3B8);
  static const slate500 = Color(0xFF64748B);
  static const slate600 = Color(0xFF475569);
  static const slate700 = Color(0xFF334155);
  static const slate800 = Color(0xFF1E293B);
  static const slate900 = Color(0xFF0F172A);

  static const text = slate900;
  static const textMuted = slate700;
  static const textSubtle = slate500;
  static const border = slate200;
  static const borderStrong = slate300;
  static const bgSoft = slate100;

  // Semantic
  static const success = Color(0xFF16A34A);
  static const successSoft = Color(0xFFDCFCE7);
  static const danger = Color(0xFFDC2626);
  static const dangerSoft = Color(0xFFFEE2E2);
  static const warning = Color(0xFFD97706);
  static const warningSoft = Color(0xFFFEF3C7);
  static const info = Color(0xFF2563EB);
  static const infoSoft = Color(0xFFDBEAFE);

  // Radii
  static const radiusSm = 6.0;
  static const radiusMd = 8.0;
  static const radiusLg = 12.0;

  // Page surface — like the staff console, page sections sit on white.
  static const surface = Colors.white;
  static const scaffold = slate100;
}

ThemeData buildBornoTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: Bo.primary,
    primary: Bo.primary,
    brightness: Brightness.light,
  ).copyWith(surface: Bo.surface);

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: Bo.scaffold,
    fontFamily: 'Segoe UI',
    cardTheme: CardThemeData(
      color: Bo.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(Bo.radiusLg),
        side: const BorderSide(color: Bo.border),
      ),
      margin: EdgeInsets.zero,
    ),
    dividerTheme: const DividerThemeData(color: Bo.border, thickness: 1, space: 1),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Bo.surface,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(Bo.radiusMd)),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(Bo.radiusSm)),
      ),
    ),
    dataTableTheme: const DataTableThemeData(
      headingTextStyle: TextStyle(fontWeight: FontWeight.w600, color: Bo.textMuted, fontSize: 13),
      dataTextStyle: TextStyle(color: Bo.text, fontSize: 13),
    ),
  );
}

/// Maps a semantic tone name to (background, foreground) — used for status chips.
({Color bg, Color fg}) toneColors(String tone) => switch (tone) {
      'success' => (bg: Bo.successSoft, fg: Bo.success),
      'danger' => (bg: Bo.dangerSoft, fg: Bo.danger),
      'warning' => (bg: Bo.warningSoft, fg: Bo.warning),
      'info' => (bg: Bo.infoSoft, fg: Bo.info),
      'primary' => (bg: Bo.primarySoft, fg: Bo.primaryEmphasis),
      _ => (bg: Bo.slate100, fg: Bo.slate600),
    };
