import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/dtos.dart';
import '../../core/providers/providers.dart';

/// Selected category filter on the Take order tab (null = All).
final selectedCategoryProvider = StateProvider<String?>((ref) => null);

/// productId -> availability, for out-of-stock / low-stock badges.
final availabilityMapProvider = Provider<Map<String, ProductAvailability>>((ref) {
  final list = ref.watch(availabilityProvider).valueOrNull ?? const [];
  return {for (final a in list) a.productId: a};
});

/// Only active products (mirrors the Blazor console runtime filter).
final activeProductsProvider = Provider<List<Product>>((ref) {
  final list = ref.watch(productsProvider).valueOrNull ?? const [];
  return list.where((p) => p.isActive).toList()
    ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
});
