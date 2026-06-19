import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/printing/print_service.dart';
import '../../core/providers/providers.dart';
import '../../core/realtime/live_connection.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_toast.dart';
import '../dashboard/widgets.dart';

// Status tabs (mock order). "All" is rendered separately.
const _statuses = ['Placed', 'Confirmed', 'Preparing', 'Ready', 'Served', 'Completed', 'Cancelled'];

// ---- Local palette: matched to the supplied HTML mock for pixel fidelity.
// These intentionally differ from the global Bo.* tokens; do not refactor onto
// the theme — the mock has its own grey ramp and orange gradient.
const _bg = Color(0xFFF4F5F8);
const _ink = Color(0xFF1C2333);
const _muted = Color(0xFF9AA1B2);
const _body = Color(0xFF41485A);
const _subhead = Color(0xFF5A6B85);
const _cardBorder = Color(0xFFEBEDF2);
const _rowBorder = Color(0xFFF4F5F7);
const _rowAlt = Color(0xFFFCFCFD);
const _toolbarBg = Color(0xFFFBFBFC);
const _fieldBg = Color(0xFFF4F5F8);
const _fieldBorder = Color(0xFFE8EAEF);
const _inputBorder = Color(0xFFE3E6EC);
const _gradient = LinearGradient(
  colors: [Color(0xFFF15A23), Color(0xFFFB7E2D)],
  begin: Alignment.topLeft,
  end: Alignment.bottomRight,
);

// Grid template from the mock (px column widths) + 10px gaps + 22px side padding.
const _cols = <double>[240, 122, 66, 116, 132, 60, 130, 104, 132, 40];
const _gap = 10.0;
const _tableWidth = 1276.0; // sum(_cols) + 9*_gap + 2*22

({Color fg, Color bg}) _statusTone(String s) => switch (s) {
      'Confirmed' => (fg: Color(0xFF2563EB), bg: Color(0xFFE8F0FE)),
      'Preparing' => (fg: Color(0xFFB9740F), bg: Color(0xFFFEF3E2)),
      'Ready' => (fg: Color(0xFF0F9D6B), bg: Color(0xFFE3F6EF)),
      'Served' => (fg: Color(0xFF7C3AED), bg: Color(0xFFF1EBFD)),
      'Completed' => (fg: Color(0xFF1F9D52), bg: Color(0xFFE7F6ED)),
      'Cancelled' => (fg: Color(0xFFDC2626), bg: Color(0xFFFDECEC)),
      _ => (fg: Color(0xFF5B6577), bg: Color(0xFFEEF1F5)), // Placed / unknown
    };

String _ddmmyyyy(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
String _hm(DateTime d) => '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

class OrdersScreen extends ConsumerStatefulWidget {
  const OrdersScreen({super.key});

  @override
  ConsumerState<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends ConsumerState<OrdersScreen> {
  late final TextEditingController _searchCtrl;
  late final TextEditingController _orderNoCtrl;
  DateTime? _pendingFrom;
  DateTime? _pendingTo;

  @override
  void initState() {
    super.initState();
    final f = ref.read(ordersFilterProvider);
    _searchCtrl = TextEditingController(text: f.search ?? '');
    _orderNoCtrl = TextEditingController(text: f.orderNumber ?? '');
    _pendingFrom = f.fromDate;
    _pendingTo = f.toDate;
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _orderNoCtrl.dispose();
    super.dispose();
  }

  void _apply(OrdersFilter f) => ref.read(ordersFilterProvider.notifier).state = f;

  Future<void> _pickDate(bool isFrom) async {
    final initial = (isFrom ? _pendingFrom : _pendingTo) ?? DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );
    if (picked == null) return;
    setState(() {
      if (isFrom) {
        _pendingFrom = picked;
      } else {
        _pendingTo = picked;
      }
    });
  }

  void _openDetail(String id) =>
      showDialog(context: context, builder: (_) => _OrderDetailDialog(orderId: id));

  @override
  Widget build(BuildContext context) {
    // Refresh the list + KPIs the instant the server signals a relevant change.
    ref.listen(liveTickProvider, (_, next) {
      final scope = next.valueOrNull;
      if (scope == null) return;
      if (scope == LiveScope.all ||
          scope == LiveScope.orders ||
          scope == LiveScope.payments ||
          scope == LiveScope.kitchen) {
        ref.invalidate(ordersProvider);
        ref.invalidate(ordersSummaryProvider);
      }
    });

    final filter = ref.watch(ordersFilterProvider);
    final ordersAsync = ref.watch(ordersProvider);
    final summary = ref.watch(ordersSummaryProvider).valueOrNull;
    final isLive = ref.watch(liveControllerProvider) == LiveStatus.connected;

    return Container(
      color: _bg,
      child: SingleChildScrollView(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 1280),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(26, 26, 26, 40),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _header(isLive),
                  const SizedBox(height: 22),
                  _kpiTiles(summary),
                  const SizedBox(height: 22),
                  _tableCard(filter, ordersAsync, summary),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  // -------------------------------------------------------------- header

  Widget _header(bool isLive) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Text('Orders',
                      style: TextStyle(
                          fontSize: 24, fontWeight: FontWeight.w800, color: _ink, letterSpacing: -.5)),
                  const SizedBox(width: 11),
                  _livePill(isLive),
                ],
              ),
              const SizedBox(height: 4),
              const Text('Track and manage every order across the floor and counter.',
                  style: TextStyle(fontSize: 13.5, color: _muted)),
            ],
          ),
        ),
        const SizedBox(width: 20),
        _softButton(
          icon: Icons.file_download_outlined,
          label: 'Export',
          onTap: () => AppToast.show(context, 'Export isn’t available yet.', type: ToastType.info),
        ),
        const SizedBox(width: 10),
        _gradientButton(
          icon: Icons.add,
          label: 'New Order',
          onTap: () =>
              ref.read(selectedNavProvider.notifier).state = const NavSelection(posRoute, 'POS'),
        ),
      ],
    );
  }

  Widget _livePill(bool live) {
    final fg = live ? const Color(0xFF1F9D52) : _muted;
    final bg = live ? const Color(0xFFE7F6ED) : const Color(0xFFEEF1F5);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(8)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(width: 7, height: 7, decoration: BoxDecoration(color: fg, shape: BoxShape.circle)),
          const SizedBox(width: 6),
          Text(live ? 'Live' : 'Offline',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: fg)),
        ],
      ),
    );
  }

  Widget _softButton({required IconData icon, required String label, required VoidCallback onTap}) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(11),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          decoration: BoxDecoration(
            color: Colors.white,
            border: Border.all(color: _inputBorder),
            borderRadius: BorderRadius.circular(11),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 16, color: _subhead),
              const SizedBox(width: 8),
              Text(label,
                  style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _subhead)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _gradientButton({required IconData icon, required String label, required VoidCallback onTap}) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(11),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          decoration: BoxDecoration(
            gradient: _gradient,
            borderRadius: BorderRadius.circular(11),
            boxShadow: const [BoxShadow(color: Color(0x66F15A23), blurRadius: 18, offset: Offset(0, 8))],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 17, color: Colors.white),
              const SizedBox(width: 8),
              Text(label,
                  style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: Colors.white)),
            ],
          ),
        ),
      ),
    );
  }

  // ------------------------------------------------------------- KPI tiles

  Widget _kpiTiles(OrdersSummary? s) {
    final cur = s?.currency ?? 'Tk';
    final tiles = <Widget>[
      _kpiTile(
        icon: const Text('#',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Color(0xFFF15A23))),
        iconBg: const Color(0xFFFFF3EC),
        label: 'Total Orders',
        value: s == null ? '—' : count(s.totalOrders),
      ),
      _kpiTile(
        icon: const Icon(Icons.schedule, size: 20, color: Color(0xFF2563EB)),
        iconBg: const Color(0xFFE8F0FE),
        label: 'Active Now',
        value: s == null ? '—' : count(s.activeOrders),
      ),
      _kpiTile(
        icon: const Text('৳',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Color(0xFF1F9D52))),
        iconBg: const Color(0xFFE7F6ED),
        label: 'Paid Revenue',
        value: s == null ? '—' : money(s.paidRevenue, cur),
      ),
      _kpiTile(
        icon: const Text('!',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Color(0xFFC47D12))),
        iconBg: const Color(0xFFFEF3E2),
        label: 'Outstanding',
        value: s == null ? '—' : money(s.outstanding, cur),
      ),
    ];

    return LayoutBuilder(
      builder: (_, c) {
        final cols = c.maxWidth >= 880 ? 4 : (c.maxWidth >= 460 ? 2 : 1);
        final w = (c.maxWidth - (cols - 1) * 14) / cols;
        return Wrap(
          spacing: 14,
          runSpacing: 14,
          children: [for (final t in tiles) SizedBox(width: w, child: t)],
        );
      },
    );
  }

  Widget _kpiTile(
      {required Widget icon, required Color iconBg, required String label, required String value}) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: _cardBorder),
        borderRadius: BorderRadius.circular(15),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
      child: Row(
        children: [
          Container(
              width: 42,
              height: 42,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: iconBg, borderRadius: BorderRadius.circular(12)),
              child: icon),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: _muted)),
                const SizedBox(height: 2),
                Text(value,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                        fontSize: 21, fontWeight: FontWeight.w800, color: _ink, letterSpacing: -.2)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ----------------------------------------------------------- table card

  Widget _tableCard(
      OrdersFilter filter, AsyncValue<Paged<OrderListItem>> ordersAsync, OrdersSummary? summary) {
    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: _cardBorder),
        borderRadius: BorderRadius.circular(18),
        boxShadow: const [BoxShadow(color: Color(0x08141E3C), blurRadius: 2, offset: Offset(0, 1))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _toolbar(filter, summary),
          ordersAsync.when(
            loading: () =>
                const SizedBox(height: 260, child: Center(child: CircularProgressIndicator())),
            error: (e, _) => SizedBox(
              height: 260,
              child: Center(
                child: Column(mainAxisSize: MainAxisSize.min, children: [
                  const Icon(Icons.cloud_off, color: _muted, size: 36),
                  const SizedBox(height: 8),
                  Text(e.toString(), style: const TextStyle(color: _subhead)),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: () => ref.invalidate(ordersProvider),
                    icon: const Icon(Icons.refresh),
                    label: const Text('Retry'),
                  ),
                ]),
              ),
            ),
            data: (paged) => _tableBody(filter, paged),
          ),
        ],
      ),
    );
  }

  Widget _toolbar(OrdersFilter filter, OrdersSummary? summary) {
    final orderNoApplied = (filter.orderNumber ?? '').isNotEmpty;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Color(0xFFF0F1F4)))),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Status tabs with per-status counts.
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              _tab('All', summary?.totalOrders ?? 0, filter.status == null,
                  () => _apply(filter.copyWith(page: 1, clearStatus: true))),
              for (final s in _statuses)
                _tab(s, summary?.countFor(s) ?? 0, filter.status == s,
                    () => _apply(filter.copyWith(status: s, page: 1))),
            ],
          ),
          const SizedBox(height: 12),
          // Search + order-# lookup + date range.
          Wrap(
            spacing: 10,
            runSpacing: 10,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              _searchBox(filter),
              _orderLookup(filter, orderNoApplied),
              Opacity(opacity: orderNoApplied ? 0.45 : 1, child: _dateRange(filter)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _tab(String label, int n, bool selected, VoidCallback onTap) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 7),
          decoration: BoxDecoration(
            gradient: selected ? _gradient : null,
            color: selected ? null : Colors.white,
            border: Border.all(color: selected ? Colors.transparent : _fieldBorder),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(label,
                  style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: selected ? Colors.white : _subhead)),
              const SizedBox(width: 7),
              Container(
                constraints: const BoxConstraints(minWidth: 20),
                padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
                decoration: BoxDecoration(
                  color: selected ? const Color(0x40FFFFFF) : const Color(0xFFEEF1F6),
                  borderRadius: BorderRadius.circular(7),
                ),
                child: Text('$n',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: selected ? Colors.white : const Color(0xFF8B93A7))),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _searchBox(OrdersFilter filter) {
    return Container(
      width: 230,
      decoration: BoxDecoration(
          color: _fieldBg, border: Border.all(color: _fieldBorder), borderRadius: BorderRadius.circular(11)),
      padding: const EdgeInsets.symmetric(horizontal: 13),
      child: Row(
        children: [
          const Icon(Icons.search, size: 16, color: _muted),
          const SizedBox(width: 9),
          Expanded(
            child: TextField(
              controller: _searchCtrl,
              style: const TextStyle(fontSize: 13, color: _body),
              textInputAction: TextInputAction.search,
              onSubmitted: (v) => _apply(filter.copyWith(search: v.trim(), page: 1)),
              decoration: const InputDecoration(
                isDense: true,
                filled: false,
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                contentPadding: EdgeInsets.symmetric(vertical: 10),
                hintText: 'Search order or table…',
                hintStyle: TextStyle(color: _muted, fontSize: 13),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _orderLookup(OrdersFilter filter, bool applied) {
    return Container(
      decoration: BoxDecoration(
          color: _fieldBg, border: Border.all(color: _fieldBorder), borderRadius: BorderRadius.circular(11)),
      padding: const EdgeInsets.fromLTRB(12, 6, 8, 6),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.format_list_numbered, size: 15, color: _muted),
          const SizedBox(width: 8),
          SizedBox(
            width: 130,
            child: TextField(
              controller: _orderNoCtrl,
              style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: _body),
              textInputAction: TextInputAction.search,
              onSubmitted: (v) => _apply(filter.copyWith(orderNumber: v.trim(), page: 1)),
              decoration: InputDecoration(
                isDense: true,
                filled: true,
                fillColor: Colors.white,
                contentPadding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                hintText: 'Order #',
                hintStyle: const TextStyle(color: _muted, fontSize: 12.5, fontWeight: FontWeight.w500),
                border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8), borderSide: const BorderSide(color: _inputBorder)),
                enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8), borderSide: const BorderSide(color: _inputBorder)),
                focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                    borderSide: const BorderSide(color: Color(0xFFF7813A))),
              ),
            ),
          ),
          const SizedBox(width: 8),
          _miniButton(
            icon: Icons.search,
            label: 'Find',
            onTap: () => _apply(filter.copyWith(orderNumber: _orderNoCtrl.text.trim(), page: 1)),
          ),
          if (applied)
            TextButton(
              onPressed: () {
                _orderNoCtrl.clear();
                _apply(filter.copyWith(clearOrderNumber: true, page: 1));
              },
              style: TextButton.styleFrom(
                  minimumSize: const Size(0, 0),
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap),
              child: const Text('Clear',
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: _muted,
                      decoration: TextDecoration.underline)),
            ),
        ],
      ),
    );
  }

  Widget _dateRange(OrdersFilter filter) {
    return Container(
      decoration: BoxDecoration(
          color: _fieldBg, border: Border.all(color: _fieldBorder), borderRadius: BorderRadius.circular(11)),
      padding: const EdgeInsets.fromLTRB(12, 6, 8, 6),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.calendar_today_outlined, size: 15, color: _muted),
          const SizedBox(width: 8),
          _dateField(_pendingFrom, 'From', () => _pickDate(true)),
          const SizedBox(width: 8),
          const Text('to', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: _muted)),
          const SizedBox(width: 8),
          _dateField(_pendingTo, 'To', () => _pickDate(false)),
          const SizedBox(width: 8),
          _gradientMini(
            icon: Icons.sync,
            label: 'Load',
            onTap: () => _apply(filter.copyWith(
                  page: 1,
                  fromDate: _pendingFrom,
                  toDate: _pendingTo,
                  clearDates: _pendingFrom == null && _pendingTo == null,
                )),
          ),
        ],
      ),
    );
  }

  Widget _dateField(DateTime? value, String placeholder, VoidCallback onTap) {
    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: BoxDecoration(
            color: Colors.white, border: Border.all(color: _inputBorder), borderRadius: BorderRadius.circular(8)),
        child: Text(
          value == null ? placeholder : _ddmmyyyy(value),
          style: TextStyle(
              fontSize: 12.5, fontWeight: FontWeight.w600, color: value == null ? _muted : _body),
        ),
      ),
    );
  }

  Widget _miniButton({required IconData icon, required String label, required VoidCallback onTap}) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 7),
          decoration: BoxDecoration(
              color: Colors.white, border: Border.all(color: _inputBorder), borderRadius: BorderRadius.circular(8)),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 15, color: _subhead),
              const SizedBox(width: 6),
              Text(label, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: _subhead)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _gradientMini({required IconData icon, required String label, required VoidCallback onTap}) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 8),
          decoration: BoxDecoration(
            gradient: _gradient,
            borderRadius: BorderRadius.circular(8),
            boxShadow: const [BoxShadow(color: Color(0x66F15A23), blurRadius: 14, offset: Offset(0, 6))],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 15, color: Colors.white),
              const SizedBox(width: 6),
              Text(label, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: Colors.white)),
            ],
          ),
        ),
      ),
    );
  }

  // --------------------------------------------------------- table body

  Widget _tableBody(OrdersFilter filter, Paged<OrderListItem> paged) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: SizedBox(
        width: _tableWidth,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _columnHeader(),
            if (paged.items.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 60, horizontal: 20),
                child: Column(children: [
                  Text('No orders found',
                      style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: _subhead)),
                  SizedBox(height: 4),
                  Text('Try a different filter or search term.',
                      style: TextStyle(fontSize: 13, color: _muted)),
                ]),
              )
            else
              for (var i = 0; i < paged.items.length; i++) _orderRow(paged.items[i], i),
            _footer(filter, paged),
          ],
        ),
      ),
    );
  }

  Widget _columnHeader() {
    Widget th(String label, {Alignment align = Alignment.centerLeft}) => Align(
          alignment: align,
          child: Text(label.toUpperCase(),
              style: const TextStyle(
                  fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: .6, color: _muted)),
        );
    return _gridRow(
      [
        th('Order'),
        th('Customer'),
        th('Table'),
        th('Type'),
        th('Time'),
        th('Items', align: Alignment.center),
        th('Total', align: Alignment.centerRight),
        th('Paid'),
        th('Status'),
        const SizedBox.shrink(),
      ],
      padding: const EdgeInsets.fromLTRB(22, 12, 22, 12),
      color: _toolbarBg,
      border: const Border(bottom: BorderSide(color: Color(0xFFF0F1F4))),
    );
  }

  Widget _orderRow(OrderListItem o, int index) {
    final tone = _statusTone(o.status);
    final dine = o.orderType.toLowerCase().contains('dine');
    final zero = o.itemCount == 0;
    final name = (o.customerName?.isNotEmpty == true) ? o.customerName! : o.customerPhone;
    final initial = name.isNotEmpty ? name.characters.first.toUpperCase() : '?';

    return Material(
      color: index.isOdd ? _rowAlt : Colors.white,
      child: InkWell(
        onTap: () => _openDetail(o.id),
        hoverColor: const Color(0xFFFAFBFD),
        child: _gridRow(
          [
            // Order # + status dot
            Row(children: [
              Container(width: 9, height: 9, decoration: BoxDecoration(color: tone.fg, shape: BoxShape.circle)),
              const SizedBox(width: 11),
              Expanded(
                child: Text(o.orderNumber,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                        fontSize: 13.5, fontWeight: FontWeight.w700, color: _ink, letterSpacing: -.1)),
              ),
            ]),
            // Customer
            Row(children: [
              Container(
                width: 28,
                height: 28,
                alignment: Alignment.center,
                decoration:
                    BoxDecoration(color: const Color(0xFFEEF1F6), borderRadius: BorderRadius.circular(9)),
                child: Text(initial,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: Color(0xFF6B7488))),
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Text(name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _body)),
              ),
            ]),
            // Table
            Text(o.tableNumber ?? '—',
                style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: o.tableNumber == null ? const Color(0xFFC4C9D4) : _body)),
            // Type
            Row(children: [
              Icon(dine ? Icons.restaurant : Icons.takeout_dining,
                  size: 15, color: dine ? const Color(0xFF7C3AED) : const Color(0xFF0F9D6B)),
              const SizedBox(width: 7),
              Flexible(
                child: Text(o.orderType,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: _subhead)),
              ),
            ]),
            // Time (date + clock)
            Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(_ddmmyyyy(o.orderedAtUtc),
                    style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _body)),
                Text(_hm(o.orderedAtUtc), style: const TextStyle(fontSize: 11.5, color: _muted)),
              ],
            ),
            // Items
            Align(
              alignment: Alignment.center,
              child: Text('${o.itemCount}',
                  style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w700,
                      color: zero ? const Color(0xFFC4C9D4) : _body)),
            ),
            // Total
            Align(
              alignment: Alignment.centerRight,
              child: Text(money(o.total, o.currency),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w700,
                      color: o.total == 0 ? const Color(0xFFC4C9D4) : _ink)),
            ),
            // Paid
            _pill(
              o.isPaid ? 'Paid' : 'Unpaid',
              fg: o.isPaid ? const Color(0xFF1F9D52) : const Color(0xFFC47D12),
              bg: o.isPaid ? const Color(0xFFE7F6ED) : const Color(0xFFFEF3E2),
              dot: true,
            ),
            // Status
            Align(alignment: Alignment.centerLeft, child: _pill(o.status, fg: tone.fg, bg: tone.bg)),
            // Overflow menu
            Align(
              alignment: Alignment.center,
              child: IconButton(
                icon: const Icon(Icons.more_vert, size: 18, color: Color(0xFFB4BAC8)),
                visualDensity: VisualDensity.compact,
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(minWidth: 30, minHeight: 30),
                onPressed: () => _openDetail(o.id),
              ),
            ),
          ],
          padding: const EdgeInsets.fromLTRB(22, 14, 22, 14),
          border: const Border(bottom: BorderSide(color: _rowBorder)),
        ),
      ),
    );
  }

  Widget _pill(String text, {required Color fg, required Color bg, bool dot = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(8)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (dot) ...[
            Container(width: 6, height: 6, decoration: BoxDecoration(color: fg, shape: BoxShape.circle)),
            const SizedBox(width: 6),
          ],
          Text(text, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: fg)),
        ],
      ),
    );
  }

  Widget _footer(OrdersFilter filter, Paged<OrderListItem> paged) {
    final total = paged.totalPages == 0 ? 1 : paged.totalPages;
    final noun = paged.totalCount == 1 ? 'order' : 'orders';
    final label = (filter.orderNumber ?? '').isNotEmpty
        ? 'Order “${filter.orderNumber}”'
        : (filter.status ?? 'All statuses');
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 14),
      color: _toolbarBg,
      child: Row(
        children: [
          Expanded(
            child: Text.rich(
              TextSpan(children: [
                TextSpan(
                    text: '${paged.totalCount}',
                    style: const TextStyle(color: _body, fontWeight: FontWeight.w700, fontSize: 13)),
                TextSpan(text: ' $noun · $label', style: const TextStyle(color: _muted, fontSize: 13)),
              ]),
            ),
          ),
          _pageBtn(Icons.chevron_left,
              paged.page > 1 ? () => _apply(filter.copyWith(page: paged.page - 1)) : null),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: Text('Page ${paged.page} of $total',
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _subhead)),
          ),
          _pageBtn(Icons.chevron_right,
              paged.page < paged.totalPages ? () => _apply(filter.copyWith(page: paged.page + 1)) : null),
        ],
      ),
    );
  }

  Widget _pageBtn(IconData icon, VoidCallback? onTap) {
    final enabled = onTap != null;
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(9),
        onTap: onTap,
        child: Container(
          width: 34,
          height: 34,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: Colors.white,
            border: Border.all(color: _fieldBorder),
            borderRadius: BorderRadius.circular(9),
          ),
          child: Icon(icon, size: 17, color: enabled ? _subhead : const Color(0xFFB4BAC8)),
        ),
      ),
    );
  }

  Widget _gridRow(List<Widget> cells, {required EdgeInsets padding, Color? color, Border? border}) {
    final children = <Widget>[];
    for (var i = 0; i < cells.length; i++) {
      children.add(SizedBox(width: _cols[i], child: cells[i]));
      if (i < cells.length - 1) children.add(const SizedBox(width: _gap));
    }
    return Container(
      padding: padding,
      decoration: BoxDecoration(color: color, border: border),
      child: Row(children: children),
    );
  }
}

class _OrderDetailDialog extends ConsumerWidget {
  final String orderId;
  const _OrderDetailDialog({required this.orderId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(orderDetailProvider(orderId));
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 640),
        child: async.when(
          loading: () => const SizedBox(height: 240, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => SizedBox(
            height: 240,
            child: Center(child: Text(e.toString(), style: const TextStyle(color: Bo.textMuted))),
          ),
          data: (o) => _DetailBody(o: o),
        ),
      ),
    );
  }
}

class _DetailBody extends ConsumerWidget {
  final OrderDetail o;
  const _DetailBody({required this.o});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    Widget totalRow(String label, double v, {bool bold = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 2),
          child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Text(label, style: TextStyle(fontSize: 13, color: bold ? Bo.text : Bo.textMuted, fontWeight: bold ? FontWeight.w700 : FontWeight.w400)),
            Text(money(v, o.currency), style: TextStyle(fontSize: 13, fontWeight: bold ? FontWeight.w800 : FontWeight.w600)),
          ]),
        );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Header
        Container(
          padding: const EdgeInsets.all(16),
          decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Bo.border))),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(o.orderNumber, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    const SizedBox(height: 2),
                    Text('${o.orderType} · ${shortDateTime(o.orderedAtUtc)}',
                        style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                ),
              ),
              ToneChip(o.status, orderStatusTone(o.status)),
              const SizedBox(width: 8),
              IconButton(onPressed: () => Navigator.of(context).pop(), icon: const Icon(Icons.close)),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _kv('Customer', o.customerName?.isNotEmpty == true ? '${o.customerName} (${o.customerPhone})' : o.customerPhone),
              if (o.tableNumber != null) _kv('Table', o.tableNumber!),
              if (o.waiterName != null) _kv('Waiter', o.waiterName!),
              if (o.notes != null && o.notes!.isNotEmpty) _kv('Notes', o.notes!),
              const SizedBox(height: 12),
              const Text('Items', style: TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 6),
              for (final l in o.lines)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: Row(
                    children: [
                      Text('${l.quantity}×', style: const TextStyle(fontWeight: FontWeight.w700, color: Bo.textMuted)),
                      const SizedBox(width: 8),
                      Expanded(child: Text(l.name, overflow: TextOverflow.ellipsis)),
                      Text(money(l.lineTotal, o.currency), style: const TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                ),
              const Divider(),
              totalRow('Subtotal', o.subtotal),
              if (o.discountAmount != 0) totalRow('Discount', -o.discountAmount),
              if (o.taxAmount != 0) totalRow('Tax', o.taxAmount),
              if (o.serviceChargeAmount != 0) totalRow('Service charge', o.serviceChargeAmount),
              if (o.deliveryChargeAmount != 0) totalRow('Delivery charge', o.deliveryChargeAmount),
              if (o.tipAmount != 0) totalRow('Tip', o.tipAmount),
              totalRow('Grand total', o.grandTotal, bold: true),
              const SizedBox(height: 8),
              Row(children: [
                o.isPaid ? const ToneChip('Paid', 'success') : const ToneChip('Unpaid', 'warning'),
                const SizedBox(width: 8),
                if (o.paymentMethod != null) Text(o.paymentMethod!, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                const Spacer(),
                if (o.balanceDue > 0) Text('Balance due ${money(o.balanceDue, o.currency)}', style: const TextStyle(color: Bo.warning, fontWeight: FontWeight.w600, fontSize: 12)),
              ]),
              if (o.payments.isNotEmpty) ...[
                const SizedBox(height: 16),
                const Text('Payments', style: TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 6),
                for (final p in o.payments) _PaymentRow(order: o, payment: p),
              ],
            ],
          ),
        ),
        _PrintActions(o: o),
      ],
    );
  }

  Widget _kv(String k, String v) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 90, child: Text(k, style: const TextStyle(color: Bo.textSubtle, fontSize: 13))),
          Expanded(child: Text(v, style: const TextStyle(fontSize: 13))),
        ]),
      );
}

/// Footer toolbar: reprint the receipt/KOT (re-runs the print path — prints if the
/// printer is up, otherwise the job lands in the offline retry queue) or open the
/// server-rendered PDF as a preview.
class _PrintActions extends ConsumerStatefulWidget {
  final OrderDetail o;
  const _PrintActions({required this.o});

  @override
  ConsumerState<_PrintActions> createState() => _PrintActionsState();
}

class _PrintActionsState extends ConsumerState<_PrintActions> {
  bool _busy = false;

  Future<void> _run(Future<PrintOutcome> Function(PrintService) action) async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final outcome = await action(ref.read(printServiceProvider));
      if (mounted) {
        AppToast.show(context, outcome.message,
            type: outcome.result == PrintResult.failed ? ToastType.error : ToastType.success);
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final o = widget.o;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
      decoration: const BoxDecoration(border: Border(top: BorderSide(color: Bo.border))),
      child: Row(
        children: [
          if (_busy)
            const Padding(
              padding: EdgeInsets.only(right: 12),
              child: SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
            ),
          Expanded(
            child: OutlinedButton.icon(
              onPressed: _busy ? null : () => _run((s) => s.printReceipt(o)),
              icon: const Icon(Icons.receipt_long_outlined, size: 18),
              label: const Text('Reprint'),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: OutlinedButton.icon(
              onPressed: _busy ? null : () => _run((s) => s.printKot(o)),
              icon: const Icon(Icons.soup_kitchen_outlined, size: 18),
              label: const Text('KOT'),
            ),
          ),
          const SizedBox(width: 8),
          IconButton(
            tooltip: 'Preview PDF',
            onPressed: _busy ? null : () => _run((s) => s.previewPdf(o, isKot: false)),
            icon: const Icon(Icons.picture_as_pdf_outlined, color: Bo.textMuted),
          ),
        ],
      ),
    );
  }
}

/// One captured/voided/refunded payment row. A captured charge can be voided (mistaken tender) or
/// refunded (part/all); both are manager-gated server-side via [_PaymentActionDialog].
class _PaymentRow extends ConsumerWidget {
  final OrderDetail order;
  final OrderPayment payment;
  const _PaymentRow({required this.order, required this.payment});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final label = payment.isCharge ? payment.method : 'Refund · ${payment.method}';
    final tone = switch (payment.status) {
      'Voided' => 'neutral',
      'Refunded' => 'warning',
      _ => payment.isCharge ? 'success' : 'info',
    };
    final canAct = payment.isCaptured && payment.isCharge;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(children: [
                  Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
                  if (payment.provider != null) ...[
                    const SizedBox(width: 6),
                    Text(payment.provider!, style: const TextStyle(color: Bo.textSubtle, fontSize: 12)),
                  ],
                  const SizedBox(width: 8),
                  ToneChip(payment.status, tone),
                ]),
                if (payment.cashierName != null)
                  Text(payment.cashierName!, style: const TextStyle(color: Bo.textSubtle, fontSize: 11)),
              ],
            ),
          ),
          Text(money(payment.amount, order.currency), style: const TextStyle(fontWeight: FontWeight.w700)),
          if (canAct) ...[
            const SizedBox(width: 4),
            IconButton(
              tooltip: 'Refund',
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.undo, size: 18, color: Bo.warning),
              onPressed: () => _showAction(context, ref, isRefund: true),
            ),
            IconButton(
              tooltip: 'Void',
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.block, size: 18, color: Bo.danger),
              onPressed: () => _showAction(context, ref, isRefund: false),
            ),
          ],
        ],
      ),
    );
  }

  void _showAction(BuildContext context, WidgetRef ref, {required bool isRefund}) {
    showDialog(
      context: context,
      builder: (_) => _PaymentActionDialog(order: order, payment: payment, isRefund: isRefund),
    );
  }
}

/// Reason + (optional) manager-credential override for voiding/refunding a payment. A manager/admin
/// on the till can leave the credential fields blank; a cashier supplies a manager login to authorize.
class _PaymentActionDialog extends ConsumerStatefulWidget {
  final OrderDetail order;
  final OrderPayment payment;
  final bool isRefund;
  const _PaymentActionDialog({required this.order, required this.payment, required this.isRefund});

  @override
  ConsumerState<_PaymentActionDialog> createState() => _PaymentActionDialogState();
}

class _PaymentActionDialogState extends ConsumerState<_PaymentActionDialog> {
  late final TextEditingController _amount =
      TextEditingController(text: widget.payment.amount.toStringAsFixed(2));
  final _reason = TextEditingController();
  final _mgrUser = TextEditingController();
  final _mgrPass = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _amount.dispose();
    _reason.dispose();
    _mgrUser.dispose();
    _mgrPass.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final reason = _reason.text.trim();
    if (reason.isEmpty) {
      setState(() => _error = 'A reason is required.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      final api = ref.read(staffApiProvider);
      final mgrUser = _mgrUser.text.trim().isEmpty ? null : _mgrUser.text.trim();
      final mgrPass = _mgrPass.text.trim().isEmpty ? null : _mgrPass.text.trim();
      if (widget.isRefund) {
        final amount = double.tryParse(_amount.text.trim());
        if (amount == null || amount <= 0) {
          setState(() { _busy = false; _error = 'Enter a valid refund amount.'; });
          return;
        }
        await api.posRefundPayment(widget.order.id, widget.payment.id,
            amount: amount, reason: reason, managerUserName: mgrUser, managerPassword: mgrPass);
      } else {
        await api.posVoidPayment(widget.order.id, widget.payment.id,
            reason: reason, managerUserName: mgrUser, managerPassword: mgrPass);
      }
      ref.invalidate(orderDetailProvider(widget.order.id));
      ref.invalidate(ordersProvider);
      ref.invalidate(ordersSummaryProvider);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      setState(() { _busy = false; _error = e.toString(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = widget.isRefund ? 'Refund payment' : 'Void payment';
    return AlertDialog(
      title: Text('$title · ${widget.payment.method}'),
      content: SizedBox(
        width: 400,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              widget.isRefund
                  ? 'Refunds money back to the customer and restores stock once fully refunded.'
                  : 'Reverses a mistaken tender. Requires manager authorization.',
              style: const TextStyle(color: Bo.textSubtle, fontSize: 12),
            ),
            const SizedBox(height: 12),
            if (widget.isRefund)
              TextField(
                controller: _amount,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(labelText: 'Refund amount', prefixText: '${widget.order.currency} '),
              ),
            if (widget.isRefund) const SizedBox(height: 8),
            TextField(controller: _reason, decoration: const InputDecoration(labelText: 'Reason')),
            const SizedBox(height: 12),
            const Text('Manager authorization (leave blank if a manager is signed in)',
                style: TextStyle(color: Bo.textSubtle, fontSize: 11)),
            const SizedBox(height: 4),
            TextField(controller: _mgrUser, decoration: const InputDecoration(labelText: 'Manager username')),
            const SizedBox(height: 8),
            TextField(controller: _mgrPass, obscureText: true, decoration: const InputDecoration(labelText: 'Manager password')),
            if (_error != null) ...[
              const SizedBox(height: 10),
              Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 12)),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.of(context).pop(), child: const Text('Cancel')),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: widget.isRefund ? Bo.warning : Bo.danger),
          onPressed: _busy ? null : _submit,
          child: _busy
              ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(widget.isRefund ? 'Refund' : 'Void'),
        ),
      ],
    );
  }
}
