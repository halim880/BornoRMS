import '../../l10n/app_localizations.dart';

/// Localized display labels for the canonical order-status / order-type values
/// the API returns (the raw values stay English for logic/equality). Shared by
/// the kitchen, waiter, and dashboard screens. Unknown values pass through.
String orderStatusLabel(AppLocalizations t, String status) => switch (status) {
      'Placed' => t.statusPlaced,
      'Confirmed' => t.statusConfirmed,
      'Preparing' => t.statusPreparing,
      'Ready' => t.statusReady,
      'Served' => t.statusServed,
      'Completed' => t.statusCompleted,
      'Cancelled' => t.statusCancelled,
      _ => status,
    };

String orderTypeLabel(AppLocalizations t, String type) => switch (type) {
      'DineIn' => t.typeDineIn,
      'Takeaway' => t.typeTakeaway,
      'Delivery' => t.typeDelivery,
      'Collection' => t.typeCollection,
      'Waiting' => t.typeWaiting,
      _ => type,
    };

String tableStatusLabelL10n(AppLocalizations t, String status) => switch (status) {
      'Available' => t.tableStatusAvailable,
      'Occupied' => t.tableStatusOccupied,
      'Reserved' => t.tableStatusReserved,
      'WaitingPayment' => t.tableStatusWaitingPayment,
      _ => status,
    };

String requestTypeLabel(AppLocalizations t, String type) => switch (type) {
      'CallWaiter' => t.requestCallWaiter,
      'RequestBill' => t.requestBill,
      'NeedWater' => t.requestNeedWater,
      'NeedTissue' => t.requestNeedTissue,
      _ => type,
    };
