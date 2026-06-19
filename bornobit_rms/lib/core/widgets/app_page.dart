import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_theme.dart';

/// Shared scaffolding for the back-office module screens ported from the Blazor
/// staff console. Keeps every page visually consistent (white sections, paginated
/// tables, uniform async/empty/error states) so individual modules stay small.

final _dmy = DateFormat('dd/MM/yyyy'); // project rule: dd/MM/yyyy everywhere
final _dmyTime = DateFormat('dd/MM/yyyy HH:mm');

/// Full date dd/MM/yyyy.
String shortDate(DateTime d) => _dmy.format(d);

/// Full date + time dd/MM/yyyy HH:mm.
String dateTimeDmy(DateTime d) => _dmyTime.format(d);

/// Standard page header: title + optional subtitle + trailing actions, on the
/// white page background used across the console.
class PageHeader extends StatelessWidget {
  final String title;
  final String? subtitle;
  final List<Widget> actions;
  const PageHeader({super.key, required this.title, this.subtitle, this.actions = const []});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title,
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Bo.text)),
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(subtitle!, style: const TextStyle(color: Bo.textSubtle, fontSize: 13)),
                ],
              ],
            ),
          ),
          ...actions,
        ],
      ),
    );
  }
}

/// Renders an [AsyncValue]-like state with uniform loading / error+retry / data.
/// Pass the resolved value handling via [data]; on error a retry button calls
/// [onRetry].
class AsyncStateView<T> extends StatelessWidget {
  final bool isLoading;
  final Object? error;
  final T? value;
  final Widget Function(T value) data;
  final VoidCallback? onRetry;
  const AsyncStateView({
    super.key,
    required this.isLoading,
    required this.error,
    required this.value,
    required this.data,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    if (error != null && value == null) {
      return ErrorRetry(message: error.toString(), onRetry: onRetry);
    }
    if (value == null && isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (value == null) {
      return const SizedBox.shrink();
    }
    return data(value as T);
  }
}

/// Uniform error panel with a retry affordance.
class ErrorRetry extends StatelessWidget {
  final String message;
  final VoidCallback? onRetry;
  const ErrorRetry({super.key, required this.message, this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, color: Bo.textSubtle, size: 36),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Text(message, textAlign: TextAlign.center, style: const TextStyle(color: Bo.textMuted)),
          ),
          if (onRetry != null) ...[
            const SizedBox(height: 12),
            FilledButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
                label: Text(AppLocalizations.of(context).actionRetry)),
          ],
        ],
      ),
    );
  }
}

/// Centered "nothing here" placeholder for empty lists/tables.
class EmptyState extends StatelessWidget {
  final String message;
  final IconData icon;
  const EmptyState({super.key, required this.message, this.icon = Icons.inbox_outlined});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: Bo.slate400, size: 36),
          const SizedBox(height: 8),
          Text(message, style: const TextStyle(color: Bo.textSubtle)),
        ],
      ),
    );
  }
}

/// A white card hosting a horizontally + vertically scrollable [DataTable],
/// with an optional pager pinned to the bottom. The de-facto pattern for every
/// list page (honors the "datatables must have pagination" rule when [pager] is
/// supplied).
class DataTableCard extends StatelessWidget {
  final List<DataColumn> columns;
  final List<DataRow> rows;
  final String emptyMessage;
  final Widget? pager;
  const DataTableCard({
    super.key,
    required this.columns,
    required this.rows,
    this.emptyMessage = 'Nothing to show',
    this.pager,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: rows.isEmpty
              ? EmptyState(message: emptyMessage)
              : SingleChildScrollView(
                  child: SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: DataTable(
                        columnSpacing: 28,
                        showCheckboxColumn: false,
                        columns: columns,
                        rows: rows,
                      ),
                    ),
                  ),
                ),
        ),
        if (pager != null) ...[
          const Divider(height: 1),
          pager!,
        ],
      ],
    );
  }
}

/// Generic prev/next pager driven by callbacks. [label] is the left-aligned
/// total ("123 items").
class Pager extends StatelessWidget {
  final int page;
  final int totalPages;
  final String label;
  final ValueChanged<int> onPage;
  const Pager({
    super.key,
    required this.page,
    required this.totalPages,
    required this.label,
    required this.onPage,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Text(label, style: const TextStyle(color: Bo.textSubtle, fontSize: 13)),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: page > 1 ? () => onPage(page - 1) : null,
          ),
          Text(AppLocalizations.of(context).pageOf(page, totalPages == 0 ? 1 : totalPages),
              style: const TextStyle(fontSize: 13)),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: page < totalPages ? () => onPage(page + 1) : null,
          ),
        ],
      ),
    );
  }
}

/// A responsive grid of KPI / summary cards (wraps to available width).
class KpiGrid extends StatelessWidget {
  final List<Widget> children;
  final double minTileWidth;
  const KpiGrid({super.key, required this.children, this.minTileWidth = 220});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, c) {
        final cols = (c.maxWidth / minTileWidth).floor().clamp(1, 4);
        final width = (c.maxWidth - (cols - 1) * 12) / cols;
        return Wrap(
          spacing: 12,
          runSpacing: 12,
          children: [for (final w in children) SizedBox(width: width, child: w)],
        );
      },
    );
  }
}
