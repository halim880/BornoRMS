import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/models/enums.dart';

class CartLine {
  final String productId;
  final String? variantId;
  final String name;
  final double price;
  final String currency;
  int qty;

  CartLine({
    required this.productId,
    required this.variantId,
    required this.name,
    required this.price,
    required this.currency,
    this.qty = 1,
  });

  String get key => '$productId:${variantId ?? ''}';

  Map<String, dynamic> toLineJson() =>
      {'menuItemId': productId, 'quantity': qty, 'variantId': variantId, 'notes': null};
}

class CartState {
  final List<CartLine> lines;
  final OrderType type;
  final String? tableId;
  final String? tableNumber;
  final String? sessionId;
  final int? guestCount;
  final String? editingOrderId;
  final String? editingOrderNumber;
  final String notes;
  final String phone;
  final String name;

  const CartState({
    this.lines = const [],
    this.type = OrderType.dineIn,
    this.tableId,
    this.tableNumber,
    this.sessionId,
    this.guestCount,
    this.editingOrderId,
    this.editingOrderNumber,
    this.notes = '',
    this.phone = '',
    this.name = '',
  });

  bool get isEdit => editingOrderId != null;
  int get itemCount => lines.fold(0, (a, l) => a + l.qty);
  // Client-side preview only; the server returns the authoritative total.
  double get total => lines.fold(0.0, (a, l) => a + l.price * l.qty);
  String get currency => lines.isEmpty ? 'Tk' : lines.first.currency;

  bool get canPlace {
    if (lines.isEmpty) return false;
    if (isEdit) return true;
    if (type == OrderType.dineIn && tableId == null) return false;
    return true;
  }

  CartState copyWith({
    List<CartLine>? lines,
    OrderType? type,
    String? tableId,
    String? tableNumber,
    String? sessionId,
    int? guestCount,
    Object? editingOrderId = _sentinel,
    Object? editingOrderNumber = _sentinel,
    String? notes,
    String? phone,
    String? name,
  }) {
    return CartState(
      lines: lines ?? this.lines,
      type: type ?? this.type,
      tableId: tableId ?? this.tableId,
      tableNumber: tableNumber ?? this.tableNumber,
      sessionId: sessionId ?? this.sessionId,
      guestCount: guestCount ?? this.guestCount,
      editingOrderId:
          editingOrderId == _sentinel ? this.editingOrderId : editingOrderId as String?,
      editingOrderNumber:
          editingOrderNumber == _sentinel ? this.editingOrderNumber : editingOrderNumber as String?,
      notes: notes ?? this.notes,
      phone: phone ?? this.phone,
      name: name ?? this.name,
    );
  }
}

const _sentinel = Object();

final cartProvider = NotifierProvider<CartController, CartState>(CartController.new);

class CartController extends Notifier<CartState> {
  @override
  CartState build() => const CartState();

  List<CartLine> _clone() => state.lines
      .map((l) => CartLine(
          productId: l.productId,
          variantId: l.variantId,
          name: l.name,
          price: l.price,
          currency: l.currency,
          qty: l.qty))
      .toList();

  void add(Product p, {ProductVariant? variant}) {
    final lines = _clone();
    final variantId = variant?.id;
    final price = variant?.price ?? p.price;
    final name = variant == null ? p.name : '${p.name} · ${variant.name}';
    final existing = lines.where((l) => l.productId == p.id && l.variantId == variantId).firstOrNull;
    if (existing != null) {
      existing.qty++;
    } else {
      lines.add(CartLine(
          productId: p.id, variantId: variantId, name: name, price: price, currency: p.currency));
    }
    state = state.copyWith(lines: lines);
  }

  void increment(CartLine line) {
    final lines = _clone();
    lines.firstWhere((l) => l.key == line.key).qty++;
    state = state.copyWith(lines: lines);
  }

  void decrement(CartLine line) {
    final lines = _clone();
    final l = lines.firstWhere((x) => x.key == line.key);
    if (l.qty <= 1) {
      lines.removeWhere((x) => x.key == line.key);
    } else {
      l.qty--;
    }
    state = state.copyWith(lines: lines);
  }

  void remove(CartLine line) {
    final lines = _clone()..removeWhere((l) => l.key == line.key);
    state = state.copyWith(lines: lines);
  }

  void setType(OrderType type) => state = state.copyWith(type: type);

  void setTable(RestaurantTable table) =>
      state = state.copyWith(tableId: table.id, tableNumber: table.tableNumber, sessionId: null);

  void setNotes(String v) => state = state.copyWith(notes: v);
  void setPhone(String v) => state = state.copyWith(phone: v);
  void setName(String v) => state = state.copyWith(name: v);

  /// Apply a floor "Take order" target (table + open session).
  void applyTarget(String tableId, String tableNumber, String? sessionId, int guests) {
    state = const CartState().copyWith(
      type: OrderType.dineIn,
      tableId: tableId,
      tableNumber: tableNumber,
      sessionId: sessionId,
      guestCount: guests,
    );
  }

  /// Load an existing order into the cart for editing its lines.
  void loadForEdit(OrderDetail order) {
    final lines = order.lines
        .map((l) => CartLine(
            productId: l.menuItemId,
            variantId: l.variantId,
            name: l.name,
            price: l.unitPrice,
            currency: order.currency,
            qty: l.quantity))
        .toList();
    state = CartState(
      lines: lines,
      type: order.orderType,
      tableNumber: order.tableNumber,
      sessionId: order.diningSessionId,
      editingOrderId: order.id,
      editingOrderNumber: order.orderNumber,
    );
  }

  void clear() => state = state.copyWith(lines: []);

  void reset() => state = const CartState();
}
