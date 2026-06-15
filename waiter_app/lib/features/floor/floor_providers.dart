import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/models/enums.dart';

/// null = "All" statuses.
final floorStatusFilterProvider = StateProvider<DerivedTableStatus?>((ref) => null);

/// "My tables only" toggle.
final floorMineOnlyProvider = StateProvider<bool>((ref) => false);

/// Selected table id for the expanded-width side action panel (null = none).
final selectedFloorTableProvider = StateProvider<String?>((ref) => null);
