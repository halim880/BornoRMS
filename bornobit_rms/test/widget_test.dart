import 'package:flutter_test/flutter_test.dart';

import 'package:bornobit_rms/core/providers/providers.dart';

void main() {
  test('dashboard range windows are well-formed', () {
    for (final r in DashboardRange.values) {
      final w = r.window();
      expect(w.to.isBefore(w.from), isFalse, reason: '${r.label} window must be from<=to');
    }
  });
}
