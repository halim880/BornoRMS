import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../dashboard/widgets.dart';

const userManualRoute = '/system/user-manual';

/// Static, informational help page summarising how each module of the app works.
/// Mirrors the Blazor UserManual page (no backend). Uses [SectionCard] so it
/// matches the rest of the console.
class UserManualScreen extends StatelessWidget {
  const UserManualScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const PageHeader(
          title: 'User Manual',
          subtitle:
              'A quick guide to each module — how the day-to-day flow works and where to find things.',
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            children: const [
              _ManualSection(
                title: 'Getting started',
                icon: Icons.login,
                intro:
                    'Sign in with your staff email and password. The menus you see depend on your role — '
                    'anything you are not permitted to use is hidden from the navigation.',
                points: [
                  'The bottom/side navigation switches between modules (POS, Waiter, Kitchen, Orders, etc.).',
                  'Your role controls which modules and screens appear.',
                  'Use Pull-to-refresh or the Retry button if a screen fails to load.',
                ],
              ),
              _ManualSection(
                title: 'POS — counter sales',
                icon: Icons.point_of_sale,
                intro:
                    'The Point-of-Sale terminal for quick counter and takeaway orders: build a cart, '
                    'apply discounts, take payment and print.',
                points: [
                  'Tap products to add them to the cart; adjust quantity, variants and add-ons per line.',
                  'Apply a percentage or fixed discount, then choose a rounding mode if needed.',
                  'Record one or more payments (cash, card, mobile wallet) to settle the bill.',
                  'Print the kitchen ticket (KOT) and the customer receipt as PDF.',
                ],
              ),
              _ManualSection(
                title: 'Waiter — table orders',
                icon: Icons.restaurant_menu,
                intro:
                    'For dine-in service. Pick a table, take the order and send it to the kitchen.',
                points: [
                  'Select a table, then add items with their modifiers and combos.',
                  'Save/send the order — it appears on the Kitchen Display.',
                  'Add to an existing order as more items are requested.',
                ],
              ),
              _ManualSection(
                title: 'Kitchen — preparation',
                icon: Icons.soup_kitchen,
                intro:
                    'The Kitchen Display (KDS) shows incoming orders so the kitchen can cook and track progress.',
                points: [
                  'Orders are listed with their items, quantities and notes.',
                  'Mark items or whole tickets as prepared/ready when done.',
                  'Stations and priority help the kitchen sequence work.',
                ],
              ),
              _ManualSection(
                title: 'Orders',
                icon: Icons.receipt_long,
                intro:
                    'The full list of orders with their status and totals.',
                points: [
                  'Filter by status (Placed, Preparing, Completed, Cancelled, …).',
                  'Open any order to view its lines, payments and customer info.',
                  'Download the receipt / invoice PDF for any order.',
                ],
              ),
              _ManualSection(
                title: 'Stock & inventory',
                icon: Icons.inventory_2,
                intro:
                    'Track ingredients and supplies tied to what you sell.',
                points: [
                  'See stock levels and low-stock alerts at a glance.',
                  'Recipes (BOM) automatically deduct ingredients as orders are confirmed/paid.',
                  'Record purchase orders, goods receipts, wastage and stock history.',
                ],
              ),
              _ManualSection(
                title: 'Accounts',
                icon: Icons.account_balance_wallet,
                intro:
                    'Income, expenses, payables and financial reports.',
                points: [
                  'Record income/expense transactions against cash, bank or wallet accounts.',
                  'Settle supplier payables and review account balances.',
                  'Run reports — Profit & Loss, VAT, Cash Book and Day-End Close.',
                ],
              ),
              _ManualSection(
                title: 'Admin',
                icon: Icons.admin_panel_settings,
                intro:
                    'Manage who can do what across the system.',
                points: [
                  'Create users and assign roles (Admin, Manager, Waiter, Chef, Cashier).',
                  'Control which roles see which menus and modules.',
                  'Configure document numbering and catalog setup (products, categories, tables).',
                ],
              ),
              _ManualSection(
                title: 'App Settings',
                icon: Icons.settings,
                intro:
                    'Restaurant-wide billing defaults. Saving requires Admin permission.',
                points: [
                  'Set the currency symbol/code used across the app.',
                  'Configure VAT %, service charge % and whether prices include tax.',
                  'Enable tips and set the high-discount threshold for flagging large discounts.',
                ],
              ),
              _RolesCard(),
              SizedBox(height: 8),
              _TipsCard(),
            ],
          ),
        ),
      ],
    );
  }
}

class _ManualSection extends StatelessWidget {
  final String title;
  final IconData icon;
  final String intro;
  final List<String> points;
  const _ManualSection({
    required this.title,
    required this.icon,
    required this.intro,
    required this.points,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: SectionCard(
        title: title,
        icon: icon,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(intro,
                style: const TextStyle(color: Bo.textMuted, fontSize: 13, height: 1.5)),
            const SizedBox(height: 10),
            for (final p in points) _Bullet(p),
          ],
        ),
      ),
    );
  }
}

class _Bullet extends StatelessWidget {
  final String text;
  const _Bullet(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(top: 6, right: 8),
            child: Icon(Icons.circle, size: 6, color: Bo.primary),
          ),
          Expanded(
            child: Text(text,
                style: const TextStyle(color: Bo.text, fontSize: 13, height: 1.5)),
          ),
        ],
      ),
    );
  }
}

class _RolesCard extends StatelessWidget {
  const _RolesCard();

  static const _roles = <(String, String)>[
    ('SuperAdmin', 'Full access to everything, including system-level setup.'),
    ('Admin', 'Catalog, inventory, accounts and administration.'),
    ('Manager', 'Dashboards, reports and stock oversight.'),
    ('Waiter', 'Take table orders.'),
    ('Chef', 'Run the Kitchen Display.'),
    ('Cashier', 'Billing and payments at the counter.'),
  ];

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: SectionCard(
        title: 'Roles & access',
        icon: Icons.groups,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            for (final r in _roles)
              Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: RichText(
                  text: TextSpan(
                    style: const TextStyle(color: Bo.text, fontSize: 13, height: 1.5),
                    children: [
                      TextSpan(
                          text: '${r.$1} — ',
                          style: const TextStyle(fontWeight: FontWeight.w700)),
                      TextSpan(text: r.$2, style: const TextStyle(color: Bo.textMuted)),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _TipsCard extends StatelessWidget {
  const _TipsCard();

  static const _tips = <String>[
    "Menu missing? Your role may not have permission — ask an Admin to check Menu/Module permissions.",
    "Stock not decreasing? Make sure the product's recipe (BOM) is set correctly.",
    'Need a receipt? Open the order in Orders and download the PDF.',
    'Close the day from Accounts → Reporting → Day-End Close.',
  ];

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Tips & FAQ',
      icon: Icons.lightbulb_outline,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [for (final t in _tips) _Bullet(t)],
      ),
    );
  }
}
