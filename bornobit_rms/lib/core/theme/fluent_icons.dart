import 'package:flutter/material.dart';

/// Maps the Fluent UI icon names stored on AppMenus (see AppMenuSeeder) to the
/// nearest Material icon. Unknown names fall back to a neutral dot.
IconData fluentIcon(String? name) {
  switch (name) {
    case 'ClipboardTextLtr':
      return Icons.assignment_outlined;
    case 'DataPie':
      return Icons.pie_chart_outline;
    case 'DocumentAdd':
      return Icons.note_add_outlined;
    case 'ReceiptMoney':
      return Icons.receipt_long_outlined;
    case 'CalculatorMultiple':
      return Icons.calculate_outlined;
    case 'DocumentBulletList':
      return Icons.list_alt_outlined;
    case 'ClipboardTaskListLtr':
    case 'ClipboardTask':
      return Icons.checklist_outlined;
    case 'ChartMultiple':
      return Icons.bar_chart_outlined;
    case 'ArrowTrendingLines':
      return Icons.trending_up_outlined;
    case 'Receipt':
      return Icons.receipt_outlined;
    case 'Settings':
      return Icons.settings_outlined;
    case 'PaintBrush':
      return Icons.brush_outlined;
    case 'BookOpenMicroscope':
      return Icons.menu_book_outlined;
    case 'BoxMultiple':
      return Icons.inventory_2_outlined;
    case 'Box':
      return Icons.inventory_outlined;
    case 'FolderList':
      return Icons.folder_outlined;
    case 'Table':
      return Icons.table_restaurant_outlined;
    case 'Alert':
      return Icons.warning_amber_outlined;
    case 'Tag':
      return Icons.sell_outlined;
    case 'Delete':
      return Icons.delete_outline;
    case 'History':
      return Icons.history_outlined;
    case 'PeopleTeam':
      return Icons.groups_outlined;
    case 'Building':
      return Icons.business_outlined;
    case 'Money':
      return Icons.payments_outlined;
    case 'Wallet':
      return Icons.account_balance_wallet_outlined;
    case 'Person':
      return Icons.person_outline;
    case 'DocumentTable':
      return Icons.grid_on_outlined;
    case 'AppsList':
      return Icons.apps_outlined;
    case 'ShieldKeyhole':
      return Icons.security_outlined;
    case 'NumberSymbol':
      return Icons.tag_outlined;
    default:
      return Icons.circle_outlined;
  }
}
