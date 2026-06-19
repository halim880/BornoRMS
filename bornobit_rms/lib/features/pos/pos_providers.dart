import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';
import '../../core/realtime/live_connection.dart';
import 'pos_models.dart';

// ---------- catalog (fetched once; refreshable) ----------
class PosCatalog {
  final List<PosProduct> products;
  final List<PosCategory> categories;
  final Map<String, PosAvailability> availability;
  PosCatalog({required this.products, required this.categories, required this.availability});
}

final posCatalogProvider = FutureProvider<PosCatalog>((ref) async {
  final api = ref.read(staffApiProvider);
  final results = await Future.wait([api.posProducts(), api.posCategories(), api.posAvailability()]);
  final products = (results[0] as List<PosProduct>).where((p) => p.isActive).toList();
  final categories = (results[1] as List<PosCategory>).where((c) => c.isActive).toList()
    ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  final avail = {for (final a in results[2] as List<PosAvailability>) a.productId: a};
  return PosCatalog(products: products, categories: categories, availability: avail);
});

/// Per-product option groups, cached by product id.
final posOptionGroupsProvider =
    FutureProvider.family<List<PosOptionGroup>, String>((ref, productId) =>
        ref.read(staffApiProvider).posOptionGroups(productId));

final posTablesProvider = FutureProvider<List<PosTable>>((ref) => ref.read(staffApiProvider).posTables());

// ---------- active orders (polled) ----------
final posActiveOrdersProvider =
    AsyncNotifierProvider<PosActiveOrdersNotifier, List<ActiveOrder>>(PosActiveOrdersNotifier.new);

class PosActiveOrdersNotifier extends PollingNotifier<List<ActiveOrder>> {
  @override
  List<String> get liveScopes =>
      const [LiveScope.orders, LiveScope.payments, LiveScope.kitchen];

  @override
  Future<List<ActiveOrder>> fetch() => ref.read(staffApiProvider).posActiveOrders();
}

// ---------- cash drawer / shift (polled; reacts to payment ticks) ----------
final posDrawerProvider =
    AsyncNotifierProvider<PosDrawerNotifier, CashDrawer?>(PosDrawerNotifier.new);

class PosDrawerNotifier extends PollingNotifier<CashDrawer?> {
  @override
  List<String> get liveScopes => const [LiveScope.payments];

  @override
  Future<CashDrawer?> fetch() => ref.read(staffApiProvider).drawerCurrent();

  Future<CashDrawer> open({required double openingBalance, String? notes}) async {
    final drawer = await ref.read(staffApiProvider).drawerOpen(openingBalance: openingBalance, notes: notes);
    state = AsyncData(drawer);
    return drawer;
  }

  Future<DrawerCloseResult> close(String id, {required double countedBalance, String? notes}) async {
    final result = await ref.read(staffApiProvider).drawerClose(id, countedBalance: countedBalance, notes: notes);
    state = const AsyncData(null);
    return result;
  }
}

/// Drawer takings broken down by payment method, for the close screen.
final posDrawerSummaryProvider =
    FutureProvider.family<DrawerSummary, String>((ref, drawerId) =>
        ref.read(staffApiProvider).drawerSummary(drawerId));

// ---------- filters ----------
final posCategoryProvider = StateProvider<String?>((ref) => null);
final posSearchProvider = StateProvider<String>((ref) => '');

// ---------- the cashier controller ----------
class PosState {
  final String? orderId;
  final OrderDetail? detail;
  final bool busy;
  final String? error;
  const PosState({this.orderId, this.detail, this.busy = false, this.error});

  PosState copyWith({String? orderId, OrderDetail? detail, bool? busy, String? error, bool clearOrder = false}) =>
      PosState(
        orderId: clearOrder ? null : (orderId ?? this.orderId),
        detail: clearOrder ? null : (detail ?? this.detail),
        busy: busy ?? this.busy,
        error: error,
      );
}

final posControllerProvider = NotifierProvider<PosController, PosState>(PosController.new);

class PosController extends Notifier<PosState> {
  @override
  PosState build() => const PosState();

  dynamic get _api => ref.read(staffApiProvider);

  Future<void> _reloadDetail() async {
    final id = state.orderId;
    if (id == null) {
      state = state.copyWith(detail: null);
      return;
    }
    final detail = await _api.order(id);
    state = PosState(orderId: id, detail: detail, busy: false);
  }

  Future<void> _run(Future<void> Function() body) async {
    state = state.copyWith(busy: true, error: null);
    try {
      await body();
    } on ApiException catch (e) {
      state = state.copyWith(busy: false, error: e.message);
      rethrow;
    } catch (e) {
      state = state.copyWith(busy: false, error: e.toString());
      rethrow;
    }
  }

  void _refreshQueue() => ref.read(posActiveOrdersProvider.notifier).refresh();

  Future<void> selectOrder(String id) => _run(() async {
        state = PosState(orderId: id, busy: true);
        await _reloadDetail();
      });

  void clearSelection() => state = const PosState();

  Future<PlaceOrderResult?> createOrder({
    required String type,
    String? tableId,
    String? customerPhone,
    String? customerName,
    String? customerAddress,
    double? deliveryCharge,
  }) async {
    PlaceOrderResult? result;
    await _run(() async {
      result = await _api.posCreateOrder(
        type: type,
        tableId: tableId,
        customerPhone: customerPhone,
        customerName: customerName,
        customerAddress: customerAddress,
        deliveryCharge: deliveryCharge,
      );
      state = PosState(orderId: result!.orderId, busy: true);
      await _reloadDetail();
      _refreshQueue();
    });
    return result;
  }

  Future<void> updateMeta({
    required String type,
    String? tableId,
    String? customerPhone,
    String? customerName,
    String? customerAddress,
  }) =>
      _run(() async {
        final id = state.orderId;
        if (id == null) return;
        await _api.posUpdateOrder(id,
            type: type,
            tableId: tableId,
            customerPhone: customerPhone,
            customerName: customerName,
            customerAddress: customerAddress);
        await _reloadDetail();
        _refreshQueue();
      });

  // ---- line editing ----
  List<Map<String, dynamic>> _currentLineInputs() {
    final lines = state.detail?.lines ?? const [];
    return lines.map((l) {
      final optionIds = l.modifiers.map((m) => m.optionId).whereType<String>().toList();
      return <String, dynamic>{
        'menuItemId': l.menuItemId,
        'quantity': l.quantity,
        if (l.variantId != null) 'variantId': l.variantId,
        if (optionIds.isNotEmpty) 'optionIds': optionIds,
        if (l.notes != null) 'notes': l.notes,
      };
    }).toList();
  }

  String _key(String menuItemId, String? variantId, List<String> optionIds) {
    final opt = (optionIds.toList()..sort()).join(',');
    return '$menuItemId|${variantId ?? ''}|$opt';
  }

  String _keyOfInput(Map<String, dynamic> m) {
    final opt = ((m['optionIds'] as List?)?.map((e) => e.toString()).toList() ?? <String>[]);
    return _key(m['menuItemId'] as String, m['variantId'] as String?, opt);
  }

  Future<void> addItem({
    required String menuItemId,
    String? variantId,
    List<String> optionIds = const [],
  }) =>
      _run(() async {
        final id = state.orderId;
        if (id == null) return;
        final inputs = _currentLineInputs();
        final key = _key(menuItemId, variantId, optionIds);
        final existing = inputs.indexWhere((m) => _keyOfInput(m) == key);
        if (existing >= 0) {
          inputs[existing]['quantity'] = (inputs[existing]['quantity'] as int) + 1;
        } else {
          inputs.add({
            'menuItemId': menuItemId,
            'quantity': 1,
            if (variantId != null) 'variantId': variantId,
            if (optionIds.isNotEmpty) 'optionIds': optionIds,
          });
        }
        await _api.posSetLines(id, inputs);
        await _reloadDetail();
        _refreshQueue();
      });

  Future<void> changeQty(OrderLine line, int delta) => _run(() async {
        final id = state.orderId;
        if (id == null) return;
        final optionIds = line.modifiers.map((m) => m.optionId).whereType<String>().toList();
        final key = _key(line.menuItemId, line.variantId, optionIds);
        final inputs = _currentLineInputs();
        final idx = inputs.indexWhere((m) => _keyOfInput(m) == key);
        if (idx < 0) return;
        final newQty = (inputs[idx]['quantity'] as int) + delta;
        if (newQty <= 0) {
          inputs.removeAt(idx);
        } else {
          inputs[idx]['quantity'] = newQty;
        }
        await _api.posSetLines(id, inputs);
        await _reloadDetail();
        _refreshQueue();
      });

  Future<void> removeLine(OrderLine line) => _run(() async {
        final id = state.orderId;
        if (id == null) return;
        final optionIds = line.modifiers.map((m) => m.optionId).whereType<String>().toList();
        final key = _key(line.menuItemId, line.variantId, optionIds);
        final inputs = _currentLineInputs()..removeWhere((m) => _keyOfInput(m) == key);
        await _api.posSetLines(id, inputs);
        await _reloadDetail();
        _refreshQueue();
      });

  // ---- billing ----
  Future<BillSummary> applyDiscount({double? percent, double? amount, String? reason}) async {
    final id = state.orderId!;
    final summary = await _api.posDiscount(id, percent: percent, amount: amount, reason: reason);
    await _reloadDetail();
    return summary;
  }

  Future<BillSummary> applyRounding(String mode) async {
    final id = state.orderId!;
    final summary = await _api.posRounding(id, mode);
    await _reloadDetail();
    return summary;
  }

  Future<SettlementResult> addPayment(List<Map<String, dynamic>> payments, {String? idempotencyKey}) async {
    final id = state.orderId!;
    final result = await _api.posAddPayment(id, payments, idempotencyKey: idempotencyKey);
    await _reloadDetail();
    _refreshQueue();
    return result;
  }

  Future<void> cancel({String? reason}) => _run(() async {
        final id = state.orderId;
        if (id == null) return;
        await _api.posCancel(id, reason: reason);
        _refreshQueue();
        state = const PosState();
      });
}
