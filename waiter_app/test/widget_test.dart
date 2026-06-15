// Unit tests covering wire parsing + cart math (no platform plugins involved).
import 'package:flutter_test/flutter_test.dart';

import 'package:waiter_app/core/models/dtos.dart';
import 'package:waiter_app/core/models/enums.dart';

void main() {
  test('enums parse from wire names with unknown fallback', () {
    expect(OrderType.fromName('DineIn'), OrderType.dineIn);
    expect(DerivedTableStatus.fromName('WaitingPayment'), DerivedTableStatus.waitingPayment);
    expect(OrderStatus.fromName('Served'), OrderStatus.served);
    expect(OrderType.fromName('Martian'), OrderType.unknown);
  });

  test('TableOverviewRow parses a floor payload', () {
    final row = TableOverviewRow.fromJson({
      'tableId': 't1',
      'tableNumber': '5',
      'capacity': 4,
      'status': 'Occupied',
      'guestCount': 2,
      'sessionMinutes': 18,
      'currentBill': 540.5,
      'orderId': 'o1',
      'orderNumber': 'ORD-1',
      'currency': 'Tk',
      'sessionId': 's1',
      'orderCount': 1,
      'waiterName': 'Ali',
    });
    expect(row.status, DerivedTableStatus.occupied);
    expect(row.currentBill, 540.5);
    expect(row.sessionId, 's1');
  });

  test('Product derives hasVariants / minPrice', () {
    final p = Product.fromJson({
      'id': 'p1',
      'code': 'P1',
      'name': 'Pizza',
      'productCategoryId': 'c1',
      'categoryName': 'Mains',
      'price': 0,
      'currency': 'Tk',
      'displayOrder': 0,
      'isActive': true,
      'variants': [
        {'id': 'v1', 'name': 'Small', 'price': 200, 'displayOrder': 0},
        {'id': 'v2', 'name': 'Large', 'price': 350, 'displayOrder': 1},
      ],
    });
    expect(p.hasVariants, true);
    expect(p.minPrice, 200);
  });
}
