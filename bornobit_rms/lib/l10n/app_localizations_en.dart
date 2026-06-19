// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'BornoBit RMS';

  @override
  String get brandName => 'BornoBit Restaurant';

  @override
  String get actionCancel => 'Cancel';

  @override
  String get actionSave => 'Save';

  @override
  String get actionClose => 'Close';

  @override
  String get actionClear => 'Clear';

  @override
  String get actionAdd => 'Add';

  @override
  String get actionEdit => 'Edit';

  @override
  String get actionDelete => 'Delete';

  @override
  String get actionRefresh => 'Refresh';

  @override
  String get actionRetry => 'Retry';

  @override
  String get actionSignIn => 'Sign in';

  @override
  String get actionSignOut => 'Sign out';

  @override
  String get language => 'Language';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageBengali => 'বাংলা';

  @override
  String get shellToggleMenu => 'Toggle menu';

  @override
  String get shellModuleNotBuilt => 'This module is not built yet.';

  @override
  String get settingsLanguageSubtitle =>
      'Choose the app display language. This is saved on this device only.';

  @override
  String get nothingToShow => 'Nothing to show';

  @override
  String pageOf(int page, int total) {
    return 'Page $page of $total';
  }

  @override
  String get loginStaffConsole => 'Staff console';

  @override
  String get loginEmailOrUsername => 'Email or username';

  @override
  String get loginPassword => 'Password';

  @override
  String posViewCart(int count) {
    return 'View cart · $count item(s)';
  }

  @override
  String get posNoActiveOrder => 'No active order';

  @override
  String get posNoOrderSelected => 'No order selected';

  @override
  String get posPickOrderHint => 'Pick an order above, or tap + to start one.';

  @override
  String get posNoItemsYet => 'No items yet';

  @override
  String get posTapProductHint => 'Tap a product to start this order.';

  @override
  String get posWalkIn => 'Walk-in';

  @override
  String get posReceipt => 'Receipt';

  @override
  String get posSendToKitchen => 'Send to kitchen';

  @override
  String get posCharge => 'Charge';

  @override
  String get posStartOrderFirst => 'Start an order first (tap +).';

  @override
  String posAddedItem(String name) {
    return 'Added $name';
  }

  @override
  String get posSoldOut => 'Sold out';

  @override
  String get posLow => 'Low';

  @override
  String get posFrom => 'from ';

  @override
  String get posOrderSettled => 'Order settled.';

  @override
  String get billSubtotal => 'Subtotal';

  @override
  String get billDiscount => 'Discount';

  @override
  String get billVat => 'VAT (5%)';

  @override
  String get billRounding => 'Rounding';

  @override
  String get billTotalPayable => 'Total payable';

  @override
  String get billGrandTotal => 'Grand total';

  @override
  String get billAlreadyPaid => 'Already paid';

  @override
  String get billBalanceDue => 'Balance due';

  @override
  String get billPaid => 'Paid';

  @override
  String get billMethod => 'Method';

  @override
  String get payCheckout => 'Checkout';

  @override
  String get payAmountDue => 'AMOUNT DUE';

  @override
  String get payReadyToSettle => 'READY TO SETTLE';

  @override
  String get payAdjustments => 'Adjustments';

  @override
  String get payPayments => 'Payments';

  @override
  String get payRoundingFloor => 'Floor';

  @override
  String get payRoundingCeil => 'Ceil';

  @override
  String get payRoundingNone => 'None';

  @override
  String get payMethod => 'Payment method';

  @override
  String get payMethodCash => 'Cash';

  @override
  String get payMethodCard => 'Card';

  @override
  String get payMethodMobile => 'Mobile';

  @override
  String get payProvider => 'Provider';

  @override
  String get payCardType => 'Card type';

  @override
  String get payAmount => 'Amount';

  @override
  String get payAmountReceived => 'Amount received';

  @override
  String get payReferenceNumber => 'Reference number';

  @override
  String get payTransactionId => 'Transaction ID';

  @override
  String get payExact => 'Exact';

  @override
  String get payChange => 'CHANGE';

  @override
  String get payAddAnother => 'Add another payment';

  @override
  String get payComplete => 'Complete payment';

  @override
  String get payProcessing => 'Processing…';

  @override
  String get payAddAtLeastOne => 'Add at least one payment';

  @override
  String payPartialBalance(String amount) {
    return 'Partial payment recorded — balance $amount';
  }

  @override
  String get commonAllTypes => 'All types';

  @override
  String commonTable(String number) {
    return 'Table $number';
  }

  @override
  String commonNote(String text) {
    return 'Note: $text';
  }

  @override
  String get statusPlaced => 'Placed';

  @override
  String get statusConfirmed => 'Confirmed';

  @override
  String get statusPreparing => 'Preparing';

  @override
  String get statusReady => 'Ready';

  @override
  String get statusServed => 'Served';

  @override
  String get statusCompleted => 'Completed';

  @override
  String get statusCancelled => 'Cancelled';

  @override
  String get typeDineIn => 'Dine-in';

  @override
  String get typeTakeaway => 'Takeaway';

  @override
  String get typeDelivery => 'Delivery';

  @override
  String get typeCollection => 'Collection';

  @override
  String get typeWaiting => 'Waiting';

  @override
  String get kdsTitle => 'Kitchen Display';

  @override
  String get kdsSubtitle =>
      'Live tickets · accept, start cooking, then bump to ready';

  @override
  String get kdsKpiPending => 'Pending';

  @override
  String get kdsKpiPreparing => 'Preparing';

  @override
  String get kdsKpiReady => 'Ready';

  @override
  String get kdsKpiAvgPrep => 'Avg prep';

  @override
  String get kdsKpiLongestWait => 'Longest wait';

  @override
  String get kdsKpiDoneToday => 'Done today';

  @override
  String get kdsAllStations => 'All Stations';

  @override
  String get kdsFilterType => 'Type';

  @override
  String get kdsFilterTable => 'Table #';

  @override
  String get kdsFilterSearch => 'Search order #';

  @override
  String get kdsColPending => 'Pending';

  @override
  String get kdsColPreparing => 'Preparing';

  @override
  String get kdsColReady => 'Ready';

  @override
  String get kdsNoOrders => 'No orders';

  @override
  String get kdsAccept => 'Accept';

  @override
  String get kdsStartCooking => 'Start cooking';

  @override
  String get kdsMarkReady => 'Mark ready';

  @override
  String get kdsServe => 'Serve';

  @override
  String get kdsAdvance => 'Advance';

  @override
  String get kdsClearPriority => 'Clear priority';

  @override
  String get kdsMarkPriority => 'Mark priority';

  @override
  String get kdsKitchenNotes => 'Kitchen notes';

  @override
  String kdsNotesTitle(String order) {
    return 'Kitchen notes · $order';
  }

  @override
  String get kdsNotesInternal => 'Internal only — not shown to the customer.';

  @override
  String get kdsNotesHint => 'e.g. Waiting for ingredients, cooking started…';

  @override
  String get kdsToastAccepted => 'Order accepted — kitchen ticket sent.';

  @override
  String get kdsToastCooking => 'Cooking started.';

  @override
  String get kdsToastReady => 'Order is ready — front of house notified.';

  @override
  String get kdsToastServed => 'Order served.';

  @override
  String get kdsToastAdvanced => 'Order advanced.';

  @override
  String get kdsToastPriorityCleared => 'Priority cleared.';

  @override
  String get kdsToastPriorityMarked => 'Marked priority.';

  @override
  String get kdsToastNotesSaved => 'Kitchen notes saved.';

  @override
  String get tableStatusAvailable => 'Available';

  @override
  String get tableStatusOccupied => 'Occupied';

  @override
  String get tableStatusReserved => 'Reserved';

  @override
  String get tableStatusWaitingPayment => 'Awaiting payment';

  @override
  String get requestCallWaiter => 'Call Waiter';

  @override
  String get requestBill => 'Request Bill';

  @override
  String get requestNeedWater => 'Need Water';

  @override
  String get requestNeedTissue => 'Need Tissue';

  @override
  String get billTax => 'Tax';

  @override
  String get billServiceCharge => 'Service charge';

  @override
  String get billSubtotalLabel => 'Subtotal';

  @override
  String wtTabFloor(int count) {
    return 'Floor ($count)';
  }

  @override
  String wtTabSessions(int count) {
    return 'My sessions ($count)';
  }

  @override
  String wtTabReady(int count) {
    return 'Ready ($count)';
  }

  @override
  String wtTabRequests(int count) {
    return 'Requests ($count)';
  }

  @override
  String get wtKpiMyTables => 'My tables';

  @override
  String get wtKpiAvailable => 'Available';

  @override
  String get wtKpiReadyToServe => 'Ready to serve';

  @override
  String get wtKpiPendingRequests => 'Pending requests';

  @override
  String get wtKpiMyRevenue => 'My revenue (today)';

  @override
  String get wtStatActive => 'active';

  @override
  String get wtStatOccupied => 'occupied';

  @override
  String get wtStatBills => 'bills';

  @override
  String get wtNoTables => 'No tables configured';

  @override
  String get wtNoSessions => 'No open sessions';

  @override
  String get wtNothingReady => 'Nothing ready to serve';

  @override
  String get wtNoRequests => 'No pending requests';

  @override
  String wtSeats(int capacity) {
    return 'Seats $capacity';
  }

  @override
  String wtGuests(int count) {
    return '$count guests';
  }

  @override
  String wtOrders(int count) {
    return '$count orders';
  }

  @override
  String get wtFree => 'Free';

  @override
  String get wtBill => 'Bill';

  @override
  String get wtPay => 'Pay';

  @override
  String get wtClose => 'Close';

  @override
  String get wtServe => 'Serve';

  @override
  String get wtResolve => 'Resolve';

  @override
  String get wtToastCashierNotified => 'Cashier notified';

  @override
  String get wtToastSessionClosed => 'Session closed';

  @override
  String get wtToastMarkedServed => 'Marked served';

  @override
  String get wtToastRequestResolved => 'Request resolved';

  @override
  String get rangeToday => 'Today';

  @override
  String get rangeYesterday => 'Yesterday';

  @override
  String get rangeLast7Days => 'Last 7 days';

  @override
  String get rangeThisMonth => 'This month';

  @override
  String get dashOverview => 'Overview';

  @override
  String get dashTodaysSales => 'Today\'s Sales';

  @override
  String get dashStatOrders => 'orders';

  @override
  String get dashStatAvg => 'avg';

  @override
  String get dashTables => 'Tables';

  @override
  String dashOccupiedValue(int count) {
    return '$count occupied';
  }

  @override
  String get dashStatFree => 'free';

  @override
  String get dashStatReserved => 'reserved';

  @override
  String get dashStatToPay => 'to pay';

  @override
  String get dashKitchen => 'Kitchen';

  @override
  String dashPendingValue(int count) {
    return '$count pending';
  }

  @override
  String get dashStatPreparing => 'preparing';

  @override
  String get dashStatReady => 'ready';

  @override
  String get dashCustomerActivity => 'Customer Activity';

  @override
  String dashSessionsValue(int count) {
    return '$count sessions';
  }

  @override
  String get dashStatQr => 'QR';

  @override
  String get dashStatStaff => 'staff';

  @override
  String get dashLiveFloor => 'Live floor';

  @override
  String get dashNoActiveTables => 'No active tables';

  @override
  String get dashSalesByHour => 'Sales by hour';

  @override
  String get dashSalesByCategory => 'Sales by category';

  @override
  String get dashTopItems => 'Top selling items';

  @override
  String get dashLiveOrders => 'Live orders';

  @override
  String dashTotalSuffix(int count) {
    return '$count total';
  }

  @override
  String get dashNoOrdersYet => 'No orders yet';

  @override
  String get dashColOrder => 'Order';

  @override
  String get dashColTable => 'Table';

  @override
  String get dashColSource => 'Source';

  @override
  String get dashColTime => 'Time';

  @override
  String get dashColAmount => 'Amount';

  @override
  String get dashColStatus => 'Status';

  @override
  String get dashKitchenPerf => 'Kitchen performance';

  @override
  String get dashStatAvgPrep => 'avg prep (min)';

  @override
  String get dashStatCompleted => 'completed';

  @override
  String get dashStatWaitingOver10 => 'waiting >10m';

  @override
  String dashLongestWaiting(String order, int minutes) {
    return 'Longest waiting: $order ($minutes min)';
  }

  @override
  String get dashKitchenLoad => 'Kitchen load';

  @override
  String get dashStatPending => 'pending';

  @override
  String get dashCustomerRequests => 'Customer requests';

  @override
  String get dashNoPendingRequests => 'No pending requests';

  @override
  String get dashInventoryAlerts => 'Inventory alerts';

  @override
  String get dashLowStock => 'Low stock';

  @override
  String get dashOutOfStock => 'Out of stock';

  @override
  String get dashTodaysConsumption => 'Today\'s consumption';

  @override
  String get dashNone => 'None';

  @override
  String dashMore(int count) {
    return '+$count more';
  }

  @override
  String get dashStaffLeaderboard => 'Staff leaderboard';

  @override
  String get dashNoStaffActivity => 'No staff activity today';

  @override
  String get dashStatOrd => 'ord';

  @override
  String get dashStatTbl => 'tbl';

  @override
  String get dashRevenueBreakdown => 'Revenue breakdown';

  @override
  String get dashRevDineIn => 'Dine-in';

  @override
  String get dashRevTakeaway => 'Takeaway';

  @override
  String get dashRevDelivery => 'Delivery';

  @override
  String get dashRevQrOrdering => 'QR ordering';

  @override
  String get dashRevDiscount => 'Discount';

  @override
  String get dashRevTaxCollected => 'Tax collected';

  @override
  String get dashRevServiceCharge => 'Service charge';

  @override
  String get dashRevGrandTotal => 'Grand total';
}
