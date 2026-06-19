// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Bengali Bangla (`bn`).
class AppLocalizationsBn extends AppLocalizations {
  AppLocalizationsBn([String locale = 'bn']) : super(locale);

  @override
  String get appTitle => 'বর্ণবিট আরএমএস';

  @override
  String get brandName => 'বর্ণবিট রেস্টুরেন্ট';

  @override
  String get actionCancel => 'বাতিল';

  @override
  String get actionSave => 'সংরক্ষণ';

  @override
  String get actionClose => 'বন্ধ';

  @override
  String get actionClear => 'মুছুন';

  @override
  String get actionAdd => 'যোগ';

  @override
  String get actionEdit => 'সম্পাদনা';

  @override
  String get actionDelete => 'মুছে ফেলুন';

  @override
  String get actionRefresh => 'রিফ্রেশ';

  @override
  String get actionRetry => 'আবার চেষ্টা';

  @override
  String get actionSignIn => 'সাইন ইন';

  @override
  String get actionSignOut => 'সাইন আউট';

  @override
  String get language => 'ভাষা';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageBengali => 'বাংলা';

  @override
  String get shellToggleMenu => 'মেনু টগল';

  @override
  String get shellModuleNotBuilt => 'এই মডিউলটি এখনো তৈরি হয়নি।';

  @override
  String get settingsLanguageSubtitle =>
      'অ্যাপের ভাষা নির্বাচন করুন। এটি শুধু এই ডিভাইসে সংরক্ষিত হয়।';

  @override
  String get nothingToShow => 'দেখানোর কিছু নেই';

  @override
  String pageOf(int page, int total) {
    return 'পৃষ্ঠা $page / $total';
  }

  @override
  String get loginStaffConsole => 'স্টাফ কনসোল';

  @override
  String get loginEmailOrUsername => 'ইমেইল বা ইউজারনেম';

  @override
  String get loginPassword => 'পাসওয়ার্ড';

  @override
  String posViewCart(int count) {
    return 'কার্ট দেখুন · $countটি আইটেম';
  }

  @override
  String get posNoActiveOrder => 'কোনো সক্রিয় অর্ডার নেই';

  @override
  String get posNoOrderSelected => 'কোনো অর্ডার নির্বাচিত নয়';

  @override
  String get posPickOrderHint =>
      'উপরে একটি অর্ডার বাছুন, অথবা নতুন শুরু করতে + চাপুন।';

  @override
  String get posNoItemsYet => 'এখনো কোনো আইটেম নেই';

  @override
  String get posTapProductHint => 'এই অর্ডার শুরু করতে একটি পণ্যে চাপুন।';

  @override
  String get posWalkIn => 'ওয়াক-ইন';

  @override
  String get posReceipt => 'রসিদ';

  @override
  String get posSendToKitchen => 'রান্নাঘরে পাঠান';

  @override
  String get posCharge => 'চার্জ';

  @override
  String get posStartOrderFirst => 'আগে একটি অর্ডার শুরু করুন (+ চাপুন)।';

  @override
  String posAddedItem(String name) {
    return '$name যোগ হয়েছে';
  }

  @override
  String get posSoldOut => 'শেষ';

  @override
  String get posLow => 'কম';

  @override
  String get posFrom => 'থেকে ';

  @override
  String get posOrderSettled => 'অর্ডার সম্পন্ন হয়েছে।';

  @override
  String get billSubtotal => 'সাবটোটাল';

  @override
  String get billDiscount => 'ছাড়';

  @override
  String get billVat => 'ভ্যাট (5%)';

  @override
  String get billRounding => 'রাউন্ডিং';

  @override
  String get billTotalPayable => 'মোট প্রদেয়';

  @override
  String get billGrandTotal => 'সর্বমোট';

  @override
  String get billAlreadyPaid => 'ইতিমধ্যে পরিশোধিত';

  @override
  String get billBalanceDue => 'বকেয়া';

  @override
  String get billPaid => 'পরিশোধিত';

  @override
  String get billMethod => 'মাধ্যম';

  @override
  String get payCheckout => 'চেকআউট';

  @override
  String get payAmountDue => 'প্রদেয় পরিমাণ';

  @override
  String get payReadyToSettle => 'পরিশোধের জন্য প্রস্তুত';

  @override
  String get payAdjustments => 'সমন্বয়';

  @override
  String get payPayments => 'পেমেন্ট';

  @override
  String get payRoundingFloor => 'নিচে';

  @override
  String get payRoundingCeil => 'উপরে';

  @override
  String get payRoundingNone => 'নেই';

  @override
  String get payMethod => 'পেমেন্ট মাধ্যম';

  @override
  String get payMethodCash => 'নগদ';

  @override
  String get payMethodCard => 'কার্ড';

  @override
  String get payMethodMobile => 'মোবাইল';

  @override
  String get payProvider => 'প্রোভাইডার';

  @override
  String get payCardType => 'কার্ডের ধরন';

  @override
  String get payAmount => 'পরিমাণ';

  @override
  String get payAmountReceived => 'গৃহীত পরিমাণ';

  @override
  String get payReferenceNumber => 'রেফারেন্স নম্বর';

  @override
  String get payTransactionId => 'ট্রানজেকশন আইডি';

  @override
  String get payExact => 'সঠিক';

  @override
  String get payChange => 'ফেরত';

  @override
  String get payAddAnother => 'আরেকটি পেমেন্ট যোগ করুন';

  @override
  String get payComplete => 'পেমেন্ট সম্পন্ন করুন';

  @override
  String get payProcessing => 'প্রসেস হচ্ছে…';

  @override
  String get payAddAtLeastOne => 'অন্তত একটি পেমেন্ট যোগ করুন';

  @override
  String payPartialBalance(String amount) {
    return 'আংশিক পেমেন্ট রেকর্ড হয়েছে — বকেয়া $amount';
  }

  @override
  String get commonAllTypes => 'সব ধরন';

  @override
  String commonTable(String number) {
    return 'টেবিল $number';
  }

  @override
  String commonNote(String text) {
    return 'নোট: $text';
  }

  @override
  String get statusPlaced => 'প্লেসড';

  @override
  String get statusConfirmed => 'নিশ্চিত';

  @override
  String get statusPreparing => 'তৈরি হচ্ছে';

  @override
  String get statusReady => 'প্রস্তুত';

  @override
  String get statusServed => 'পরিবেশিত';

  @override
  String get statusCompleted => 'সম্পন্ন';

  @override
  String get statusCancelled => 'বাতিল';

  @override
  String get typeDineIn => 'ডাইন-ইন';

  @override
  String get typeTakeaway => 'টেকঅ্যাওয়ে';

  @override
  String get typeDelivery => 'ডেলিভারি';

  @override
  String get typeCollection => 'কালেকশন';

  @override
  String get typeWaiting => 'অপেক্ষমাণ';

  @override
  String get kdsTitle => 'কিচেন ডিসপ্লে';

  @override
  String get kdsSubtitle =>
      'লাইভ টিকেট · গ্রহণ করুন, রান্না শুরু করুন, তারপর প্রস্তুতে পাঠান';

  @override
  String get kdsKpiPending => 'অপেক্ষমাণ';

  @override
  String get kdsKpiPreparing => 'তৈরি হচ্ছে';

  @override
  String get kdsKpiReady => 'প্রস্তুত';

  @override
  String get kdsKpiAvgPrep => 'গড় প্রস্তুতি';

  @override
  String get kdsKpiLongestWait => 'দীর্ঘতম অপেক্ষা';

  @override
  String get kdsKpiDoneToday => 'আজ সম্পন্ন';

  @override
  String get kdsAllStations => 'সব স্টেশন';

  @override
  String get kdsFilterType => 'ধরন';

  @override
  String get kdsFilterTable => 'টেবিল #';

  @override
  String get kdsFilterSearch => 'অর্ডার # খুঁজুন';

  @override
  String get kdsColPending => 'অপেক্ষমাণ';

  @override
  String get kdsColPreparing => 'তৈরি হচ্ছে';

  @override
  String get kdsColReady => 'প্রস্তুত';

  @override
  String get kdsNoOrders => 'কোনো অর্ডার নেই';

  @override
  String get kdsAccept => 'গ্রহণ';

  @override
  String get kdsStartCooking => 'রান্না শুরু';

  @override
  String get kdsMarkReady => 'প্রস্তুত চিহ্নিত';

  @override
  String get kdsServe => 'পরিবেশন';

  @override
  String get kdsAdvance => 'এগিয়ে নিন';

  @override
  String get kdsClearPriority => 'অগ্রাধিকার মুছুন';

  @override
  String get kdsMarkPriority => 'অগ্রাধিকার দিন';

  @override
  String get kdsKitchenNotes => 'কিচেন নোট';

  @override
  String kdsNotesTitle(String order) {
    return 'কিচেন নোট · $order';
  }

  @override
  String get kdsNotesInternal => 'শুধু অভ্যন্তরীণ — গ্রাহককে দেখানো হয় না।';

  @override
  String get kdsNotesHint => 'যেমন: উপকরণের অপেক্ষায়, রান্না শুরু হয়েছে…';

  @override
  String get kdsToastAccepted => 'অর্ডার গৃহীত — কিচেন টিকেট পাঠানো হয়েছে।';

  @override
  String get kdsToastCooking => 'রান্না শুরু হয়েছে।';

  @override
  String get kdsToastReady =>
      'অর্ডার প্রস্তুত — ফ্রন্ট অফ হাউসকে জানানো হয়েছে।';

  @override
  String get kdsToastServed => 'অর্ডার পরিবেশিত।';

  @override
  String get kdsToastAdvanced => 'অর্ডার এগিয়ে নেওয়া হয়েছে।';

  @override
  String get kdsToastPriorityCleared => 'অগ্রাধিকার মুছে ফেলা হয়েছে।';

  @override
  String get kdsToastPriorityMarked => 'অগ্রাধিকার চিহ্নিত।';

  @override
  String get kdsToastNotesSaved => 'কিচেন নোট সংরক্ষিত।';

  @override
  String get tableStatusAvailable => 'খালি';

  @override
  String get tableStatusOccupied => 'ব্যবহৃত';

  @override
  String get tableStatusReserved => 'সংরক্ষিত';

  @override
  String get tableStatusWaitingPayment => 'পেমেন্টের অপেক্ষায়';

  @override
  String get requestCallWaiter => 'ওয়েটার ডাকুন';

  @override
  String get requestBill => 'বিল চাই';

  @override
  String get requestNeedWater => 'পানি দরকার';

  @override
  String get requestNeedTissue => 'টিস্যু দরকার';

  @override
  String get billTax => 'ট্যাক্স';

  @override
  String get billServiceCharge => 'সার্ভিস চার্জ';

  @override
  String get billSubtotalLabel => 'সাবটোটাল';

  @override
  String wtTabFloor(int count) {
    return 'ফ্লোর ($count)';
  }

  @override
  String wtTabSessions(int count) {
    return 'আমার সেশন ($count)';
  }

  @override
  String wtTabReady(int count) {
    return 'প্রস্তুত ($count)';
  }

  @override
  String wtTabRequests(int count) {
    return 'অনুরোধ ($count)';
  }

  @override
  String get wtKpiMyTables => 'আমার টেবিল';

  @override
  String get wtKpiAvailable => 'খালি';

  @override
  String get wtKpiReadyToServe => 'পরিবেশনের জন্য প্রস্তুত';

  @override
  String get wtKpiPendingRequests => 'অমীমাংসিত অনুরোধ';

  @override
  String get wtKpiMyRevenue => 'আমার আয় (আজ)';

  @override
  String get wtStatActive => 'সক্রিয়';

  @override
  String get wtStatOccupied => 'ব্যবহৃত';

  @override
  String get wtStatBills => 'বিল';

  @override
  String get wtNoTables => 'কোনো টেবিল কনফিগার করা নেই';

  @override
  String get wtNoSessions => 'কোনো খোলা সেশন নেই';

  @override
  String get wtNothingReady => 'পরিবেশনের জন্য কিছু প্রস্তুত নেই';

  @override
  String get wtNoRequests => 'কোনো অমীমাংসিত অনুরোধ নেই';

  @override
  String wtSeats(int capacity) {
    return 'আসন $capacity';
  }

  @override
  String wtGuests(int count) {
    return '$count জন অতিথি';
  }

  @override
  String wtOrders(int count) {
    return '$countটি অর্ডার';
  }

  @override
  String get wtFree => 'খালি';

  @override
  String get wtBill => 'বিল';

  @override
  String get wtPay => 'পেমেন্ট';

  @override
  String get wtClose => 'বন্ধ';

  @override
  String get wtServe => 'পরিবেশন';

  @override
  String get wtResolve => 'সমাধান';

  @override
  String get wtToastCashierNotified => 'ক্যাশিয়ারকে জানানো হয়েছে';

  @override
  String get wtToastSessionClosed => 'সেশন বন্ধ হয়েছে';

  @override
  String get wtToastMarkedServed => 'পরিবেশিত চিহ্নিত';

  @override
  String get wtToastRequestResolved => 'অনুরোধ সমাধান হয়েছে';

  @override
  String get rangeToday => 'আজ';

  @override
  String get rangeYesterday => 'গতকাল';

  @override
  String get rangeLast7Days => 'গত ৭ দিন';

  @override
  String get rangeThisMonth => 'এই মাস';

  @override
  String get dashOverview => 'সারসংক্ষেপ';

  @override
  String get dashTodaysSales => 'আজকের বিক্রি';

  @override
  String get dashStatOrders => 'অর্ডার';

  @override
  String get dashStatAvg => 'গড়';

  @override
  String get dashTables => 'টেবিল';

  @override
  String dashOccupiedValue(int count) {
    return '$count ব্যবহৃত';
  }

  @override
  String get dashStatFree => 'খালি';

  @override
  String get dashStatReserved => 'সংরক্ষিত';

  @override
  String get dashStatToPay => 'পরিশোধযোগ্য';

  @override
  String get dashKitchen => 'রান্নাঘর';

  @override
  String dashPendingValue(int count) {
    return '$count অপেক্ষমাণ';
  }

  @override
  String get dashStatPreparing => 'তৈরি হচ্ছে';

  @override
  String get dashStatReady => 'প্রস্তুত';

  @override
  String get dashCustomerActivity => 'গ্রাহক কার্যকলাপ';

  @override
  String dashSessionsValue(int count) {
    return '$count সেশন';
  }

  @override
  String get dashStatQr => 'QR';

  @override
  String get dashStatStaff => 'স্টাফ';

  @override
  String get dashLiveFloor => 'লাইভ ফ্লোর';

  @override
  String get dashNoActiveTables => 'কোনো সক্রিয় টেবিল নেই';

  @override
  String get dashSalesByHour => 'ঘণ্টা অনুযায়ী বিক্রি';

  @override
  String get dashSalesByCategory => 'ক্যাটাগরি অনুযায়ী বিক্রি';

  @override
  String get dashTopItems => 'সর্বাধিক বিক্রিত আইটেম';

  @override
  String get dashLiveOrders => 'লাইভ অর্ডার';

  @override
  String dashTotalSuffix(int count) {
    return 'মোট $count';
  }

  @override
  String get dashNoOrdersYet => 'এখনো কোনো অর্ডার নেই';

  @override
  String get dashColOrder => 'অর্ডার';

  @override
  String get dashColTable => 'টেবিল';

  @override
  String get dashColSource => 'উৎস';

  @override
  String get dashColTime => 'সময়';

  @override
  String get dashColAmount => 'পরিমাণ';

  @override
  String get dashColStatus => 'অবস্থা';

  @override
  String get dashKitchenPerf => 'রান্নাঘরের পারফরম্যান্স';

  @override
  String get dashStatAvgPrep => 'গড় প্রস্তুতি (মিনিট)';

  @override
  String get dashStatCompleted => 'সম্পন্ন';

  @override
  String get dashStatWaitingOver10 => '১০ মিনিটের বেশি অপেক্ষা';

  @override
  String dashLongestWaiting(String order, int minutes) {
    return 'দীর্ঘতম অপেক্ষা: $order ($minutes মিনিট)';
  }

  @override
  String get dashKitchenLoad => 'রান্নাঘরের লোড';

  @override
  String get dashStatPending => 'অপেক্ষমাণ';

  @override
  String get dashCustomerRequests => 'গ্রাহক অনুরোধ';

  @override
  String get dashNoPendingRequests => 'কোনো অমীমাংসিত অনুরোধ নেই';

  @override
  String get dashInventoryAlerts => 'ইনভেন্টরি সতর্কতা';

  @override
  String get dashLowStock => 'কম স্টক';

  @override
  String get dashOutOfStock => 'স্টক শেষ';

  @override
  String get dashTodaysConsumption => 'আজকের ব্যবহার';

  @override
  String get dashNone => 'নেই';

  @override
  String dashMore(int count) {
    return 'আরও $countটি';
  }

  @override
  String get dashStaffLeaderboard => 'স্টাফ লিডারবোর্ড';

  @override
  String get dashNoStaffActivity => 'আজ কোনো স্টাফ কার্যকলাপ নেই';

  @override
  String get dashStatOrd => 'অর্ডার';

  @override
  String get dashStatTbl => 'টেবিল';

  @override
  String get dashRevenueBreakdown => 'আয়ের বিবরণ';

  @override
  String get dashRevDineIn => 'ডাইন-ইন';

  @override
  String get dashRevTakeaway => 'টেকঅ্যাওয়ে';

  @override
  String get dashRevDelivery => 'ডেলিভারি';

  @override
  String get dashRevQrOrdering => 'QR অর্ডারিং';

  @override
  String get dashRevDiscount => 'ছাড়';

  @override
  String get dashRevTaxCollected => 'সংগৃহীত ট্যাক্স';

  @override
  String get dashRevServiceCharge => 'সার্ভিস চার্জ';

  @override
  String get dashRevGrandTotal => 'সর্বমোট';
}
