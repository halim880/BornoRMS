import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_bn.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('bn'),
    Locale('en'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'BornoBit RMS'**
  String get appTitle;

  /// No description provided for @brandName.
  ///
  /// In en, this message translates to:
  /// **'BornoBit Restaurant'**
  String get brandName;

  /// No description provided for @actionCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get actionCancel;

  /// No description provided for @actionSave.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get actionSave;

  /// No description provided for @actionClose.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get actionClose;

  /// No description provided for @actionClear.
  ///
  /// In en, this message translates to:
  /// **'Clear'**
  String get actionClear;

  /// No description provided for @actionAdd.
  ///
  /// In en, this message translates to:
  /// **'Add'**
  String get actionAdd;

  /// No description provided for @actionEdit.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get actionEdit;

  /// No description provided for @actionDelete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get actionDelete;

  /// No description provided for @actionRefresh.
  ///
  /// In en, this message translates to:
  /// **'Refresh'**
  String get actionRefresh;

  /// No description provided for @actionRetry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get actionRetry;

  /// No description provided for @actionSignIn.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get actionSignIn;

  /// No description provided for @actionSignOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get actionSignOut;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

  /// No description provided for @languageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get languageEnglish;

  /// No description provided for @languageBengali.
  ///
  /// In en, this message translates to:
  /// **'বাংলা'**
  String get languageBengali;

  /// No description provided for @shellToggleMenu.
  ///
  /// In en, this message translates to:
  /// **'Toggle menu'**
  String get shellToggleMenu;

  /// No description provided for @shellModuleNotBuilt.
  ///
  /// In en, this message translates to:
  /// **'This module is not built yet.'**
  String get shellModuleNotBuilt;

  /// No description provided for @settingsLanguageSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Choose the app display language. This is saved on this device only.'**
  String get settingsLanguageSubtitle;

  /// No description provided for @nothingToShow.
  ///
  /// In en, this message translates to:
  /// **'Nothing to show'**
  String get nothingToShow;

  /// No description provided for @pageOf.
  ///
  /// In en, this message translates to:
  /// **'Page {page} of {total}'**
  String pageOf(int page, int total);

  /// No description provided for @loginStaffConsole.
  ///
  /// In en, this message translates to:
  /// **'Staff console'**
  String get loginStaffConsole;

  /// No description provided for @loginEmailOrUsername.
  ///
  /// In en, this message translates to:
  /// **'Email or username'**
  String get loginEmailOrUsername;

  /// No description provided for @loginPassword.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get loginPassword;

  /// No description provided for @posViewCart.
  ///
  /// In en, this message translates to:
  /// **'View cart · {count} item(s)'**
  String posViewCart(int count);

  /// No description provided for @posNoActiveOrder.
  ///
  /// In en, this message translates to:
  /// **'No active order'**
  String get posNoActiveOrder;

  /// No description provided for @posNoOrderSelected.
  ///
  /// In en, this message translates to:
  /// **'No order selected'**
  String get posNoOrderSelected;

  /// No description provided for @posPickOrderHint.
  ///
  /// In en, this message translates to:
  /// **'Pick an order above, or tap + to start one.'**
  String get posPickOrderHint;

  /// No description provided for @posNoItemsYet.
  ///
  /// In en, this message translates to:
  /// **'No items yet'**
  String get posNoItemsYet;

  /// No description provided for @posTapProductHint.
  ///
  /// In en, this message translates to:
  /// **'Tap a product to start this order.'**
  String get posTapProductHint;

  /// No description provided for @posWalkIn.
  ///
  /// In en, this message translates to:
  /// **'Walk-in'**
  String get posWalkIn;

  /// No description provided for @posReceipt.
  ///
  /// In en, this message translates to:
  /// **'Receipt'**
  String get posReceipt;

  /// No description provided for @posSendToKitchen.
  ///
  /// In en, this message translates to:
  /// **'Send to kitchen'**
  String get posSendToKitchen;

  /// No description provided for @posCharge.
  ///
  /// In en, this message translates to:
  /// **'Charge'**
  String get posCharge;

  /// No description provided for @posStartOrderFirst.
  ///
  /// In en, this message translates to:
  /// **'Start an order first (tap +).'**
  String get posStartOrderFirst;

  /// No description provided for @posAddedItem.
  ///
  /// In en, this message translates to:
  /// **'Added {name}'**
  String posAddedItem(String name);

  /// No description provided for @posSoldOut.
  ///
  /// In en, this message translates to:
  /// **'Sold out'**
  String get posSoldOut;

  /// No description provided for @posLow.
  ///
  /// In en, this message translates to:
  /// **'Low'**
  String get posLow;

  /// No description provided for @posFrom.
  ///
  /// In en, this message translates to:
  /// **'from '**
  String get posFrom;

  /// No description provided for @posOrderSettled.
  ///
  /// In en, this message translates to:
  /// **'Order settled.'**
  String get posOrderSettled;

  /// No description provided for @billSubtotal.
  ///
  /// In en, this message translates to:
  /// **'Subtotal'**
  String get billSubtotal;

  /// No description provided for @billDiscount.
  ///
  /// In en, this message translates to:
  /// **'Discount'**
  String get billDiscount;

  /// No description provided for @billVat.
  ///
  /// In en, this message translates to:
  /// **'VAT (5%)'**
  String get billVat;

  /// No description provided for @billRounding.
  ///
  /// In en, this message translates to:
  /// **'Rounding'**
  String get billRounding;

  /// No description provided for @billTotalPayable.
  ///
  /// In en, this message translates to:
  /// **'Total payable'**
  String get billTotalPayable;

  /// No description provided for @billGrandTotal.
  ///
  /// In en, this message translates to:
  /// **'Grand total'**
  String get billGrandTotal;

  /// No description provided for @billAlreadyPaid.
  ///
  /// In en, this message translates to:
  /// **'Already paid'**
  String get billAlreadyPaid;

  /// No description provided for @billBalanceDue.
  ///
  /// In en, this message translates to:
  /// **'Balance due'**
  String get billBalanceDue;

  /// No description provided for @billPaid.
  ///
  /// In en, this message translates to:
  /// **'Paid'**
  String get billPaid;

  /// No description provided for @billMethod.
  ///
  /// In en, this message translates to:
  /// **'Method'**
  String get billMethod;

  /// No description provided for @payCheckout.
  ///
  /// In en, this message translates to:
  /// **'Checkout'**
  String get payCheckout;

  /// No description provided for @payAmountDue.
  ///
  /// In en, this message translates to:
  /// **'AMOUNT DUE'**
  String get payAmountDue;

  /// No description provided for @payReadyToSettle.
  ///
  /// In en, this message translates to:
  /// **'READY TO SETTLE'**
  String get payReadyToSettle;

  /// No description provided for @payAdjustments.
  ///
  /// In en, this message translates to:
  /// **'Adjustments'**
  String get payAdjustments;

  /// No description provided for @payPayments.
  ///
  /// In en, this message translates to:
  /// **'Payments'**
  String get payPayments;

  /// No description provided for @payRoundingFloor.
  ///
  /// In en, this message translates to:
  /// **'Floor'**
  String get payRoundingFloor;

  /// No description provided for @payRoundingCeil.
  ///
  /// In en, this message translates to:
  /// **'Ceil'**
  String get payRoundingCeil;

  /// No description provided for @payRoundingNone.
  ///
  /// In en, this message translates to:
  /// **'None'**
  String get payRoundingNone;

  /// No description provided for @payMethod.
  ///
  /// In en, this message translates to:
  /// **'Payment method'**
  String get payMethod;

  /// No description provided for @payMethodCash.
  ///
  /// In en, this message translates to:
  /// **'Cash'**
  String get payMethodCash;

  /// No description provided for @payMethodCard.
  ///
  /// In en, this message translates to:
  /// **'Card'**
  String get payMethodCard;

  /// No description provided for @payMethodMobile.
  ///
  /// In en, this message translates to:
  /// **'Mobile'**
  String get payMethodMobile;

  /// No description provided for @payProvider.
  ///
  /// In en, this message translates to:
  /// **'Provider'**
  String get payProvider;

  /// No description provided for @payCardType.
  ///
  /// In en, this message translates to:
  /// **'Card type'**
  String get payCardType;

  /// No description provided for @payAmount.
  ///
  /// In en, this message translates to:
  /// **'Amount'**
  String get payAmount;

  /// No description provided for @payAmountReceived.
  ///
  /// In en, this message translates to:
  /// **'Amount received'**
  String get payAmountReceived;

  /// No description provided for @payReferenceNumber.
  ///
  /// In en, this message translates to:
  /// **'Reference number'**
  String get payReferenceNumber;

  /// No description provided for @payTransactionId.
  ///
  /// In en, this message translates to:
  /// **'Transaction ID'**
  String get payTransactionId;

  /// No description provided for @payExact.
  ///
  /// In en, this message translates to:
  /// **'Exact'**
  String get payExact;

  /// No description provided for @payChange.
  ///
  /// In en, this message translates to:
  /// **'CHANGE'**
  String get payChange;

  /// No description provided for @payAddAnother.
  ///
  /// In en, this message translates to:
  /// **'Add another payment'**
  String get payAddAnother;

  /// No description provided for @payComplete.
  ///
  /// In en, this message translates to:
  /// **'Complete payment'**
  String get payComplete;

  /// No description provided for @payProcessing.
  ///
  /// In en, this message translates to:
  /// **'Processing…'**
  String get payProcessing;

  /// No description provided for @payAddAtLeastOne.
  ///
  /// In en, this message translates to:
  /// **'Add at least one payment'**
  String get payAddAtLeastOne;

  /// No description provided for @payPartialBalance.
  ///
  /// In en, this message translates to:
  /// **'Partial payment recorded — balance {amount}'**
  String payPartialBalance(String amount);

  /// No description provided for @commonAllTypes.
  ///
  /// In en, this message translates to:
  /// **'All types'**
  String get commonAllTypes;

  /// No description provided for @commonTable.
  ///
  /// In en, this message translates to:
  /// **'Table {number}'**
  String commonTable(String number);

  /// No description provided for @commonNote.
  ///
  /// In en, this message translates to:
  /// **'Note: {text}'**
  String commonNote(String text);

  /// No description provided for @statusPlaced.
  ///
  /// In en, this message translates to:
  /// **'Placed'**
  String get statusPlaced;

  /// No description provided for @statusConfirmed.
  ///
  /// In en, this message translates to:
  /// **'Confirmed'**
  String get statusConfirmed;

  /// No description provided for @statusPreparing.
  ///
  /// In en, this message translates to:
  /// **'Preparing'**
  String get statusPreparing;

  /// No description provided for @statusReady.
  ///
  /// In en, this message translates to:
  /// **'Ready'**
  String get statusReady;

  /// No description provided for @statusServed.
  ///
  /// In en, this message translates to:
  /// **'Served'**
  String get statusServed;

  /// No description provided for @statusCompleted.
  ///
  /// In en, this message translates to:
  /// **'Completed'**
  String get statusCompleted;

  /// No description provided for @statusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get statusCancelled;

  /// No description provided for @typeDineIn.
  ///
  /// In en, this message translates to:
  /// **'Dine-in'**
  String get typeDineIn;

  /// No description provided for @typeTakeaway.
  ///
  /// In en, this message translates to:
  /// **'Takeaway'**
  String get typeTakeaway;

  /// No description provided for @typeDelivery.
  ///
  /// In en, this message translates to:
  /// **'Delivery'**
  String get typeDelivery;

  /// No description provided for @typeCollection.
  ///
  /// In en, this message translates to:
  /// **'Collection'**
  String get typeCollection;

  /// No description provided for @typeWaiting.
  ///
  /// In en, this message translates to:
  /// **'Waiting'**
  String get typeWaiting;

  /// No description provided for @kdsTitle.
  ///
  /// In en, this message translates to:
  /// **'Kitchen Display'**
  String get kdsTitle;

  /// No description provided for @kdsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Live tickets · accept, start cooking, then bump to ready'**
  String get kdsSubtitle;

  /// No description provided for @kdsKpiPending.
  ///
  /// In en, this message translates to:
  /// **'Pending'**
  String get kdsKpiPending;

  /// No description provided for @kdsKpiPreparing.
  ///
  /// In en, this message translates to:
  /// **'Preparing'**
  String get kdsKpiPreparing;

  /// No description provided for @kdsKpiReady.
  ///
  /// In en, this message translates to:
  /// **'Ready'**
  String get kdsKpiReady;

  /// No description provided for @kdsKpiAvgPrep.
  ///
  /// In en, this message translates to:
  /// **'Avg prep'**
  String get kdsKpiAvgPrep;

  /// No description provided for @kdsKpiLongestWait.
  ///
  /// In en, this message translates to:
  /// **'Longest wait'**
  String get kdsKpiLongestWait;

  /// No description provided for @kdsKpiDoneToday.
  ///
  /// In en, this message translates to:
  /// **'Done today'**
  String get kdsKpiDoneToday;

  /// No description provided for @kdsAllStations.
  ///
  /// In en, this message translates to:
  /// **'All Stations'**
  String get kdsAllStations;

  /// No description provided for @kdsFilterType.
  ///
  /// In en, this message translates to:
  /// **'Type'**
  String get kdsFilterType;

  /// No description provided for @kdsFilterTable.
  ///
  /// In en, this message translates to:
  /// **'Table #'**
  String get kdsFilterTable;

  /// No description provided for @kdsFilterSearch.
  ///
  /// In en, this message translates to:
  /// **'Search order #'**
  String get kdsFilterSearch;

  /// No description provided for @kdsColPending.
  ///
  /// In en, this message translates to:
  /// **'Pending'**
  String get kdsColPending;

  /// No description provided for @kdsColPreparing.
  ///
  /// In en, this message translates to:
  /// **'Preparing'**
  String get kdsColPreparing;

  /// No description provided for @kdsColReady.
  ///
  /// In en, this message translates to:
  /// **'Ready'**
  String get kdsColReady;

  /// No description provided for @kdsNoOrders.
  ///
  /// In en, this message translates to:
  /// **'No orders'**
  String get kdsNoOrders;

  /// No description provided for @kdsAccept.
  ///
  /// In en, this message translates to:
  /// **'Accept'**
  String get kdsAccept;

  /// No description provided for @kdsStartCooking.
  ///
  /// In en, this message translates to:
  /// **'Start cooking'**
  String get kdsStartCooking;

  /// No description provided for @kdsMarkReady.
  ///
  /// In en, this message translates to:
  /// **'Mark ready'**
  String get kdsMarkReady;

  /// No description provided for @kdsServe.
  ///
  /// In en, this message translates to:
  /// **'Serve'**
  String get kdsServe;

  /// No description provided for @kdsAdvance.
  ///
  /// In en, this message translates to:
  /// **'Advance'**
  String get kdsAdvance;

  /// No description provided for @kdsClearPriority.
  ///
  /// In en, this message translates to:
  /// **'Clear priority'**
  String get kdsClearPriority;

  /// No description provided for @kdsMarkPriority.
  ///
  /// In en, this message translates to:
  /// **'Mark priority'**
  String get kdsMarkPriority;

  /// No description provided for @kdsKitchenNotes.
  ///
  /// In en, this message translates to:
  /// **'Kitchen notes'**
  String get kdsKitchenNotes;

  /// No description provided for @kdsNotesTitle.
  ///
  /// In en, this message translates to:
  /// **'Kitchen notes · {order}'**
  String kdsNotesTitle(String order);

  /// No description provided for @kdsNotesInternal.
  ///
  /// In en, this message translates to:
  /// **'Internal only — not shown to the customer.'**
  String get kdsNotesInternal;

  /// No description provided for @kdsNotesHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. Waiting for ingredients, cooking started…'**
  String get kdsNotesHint;

  /// No description provided for @kdsToastAccepted.
  ///
  /// In en, this message translates to:
  /// **'Order accepted — kitchen ticket sent.'**
  String get kdsToastAccepted;

  /// No description provided for @kdsToastCooking.
  ///
  /// In en, this message translates to:
  /// **'Cooking started.'**
  String get kdsToastCooking;

  /// No description provided for @kdsToastReady.
  ///
  /// In en, this message translates to:
  /// **'Order is ready — front of house notified.'**
  String get kdsToastReady;

  /// No description provided for @kdsToastServed.
  ///
  /// In en, this message translates to:
  /// **'Order served.'**
  String get kdsToastServed;

  /// No description provided for @kdsToastAdvanced.
  ///
  /// In en, this message translates to:
  /// **'Order advanced.'**
  String get kdsToastAdvanced;

  /// No description provided for @kdsToastPriorityCleared.
  ///
  /// In en, this message translates to:
  /// **'Priority cleared.'**
  String get kdsToastPriorityCleared;

  /// No description provided for @kdsToastPriorityMarked.
  ///
  /// In en, this message translates to:
  /// **'Marked priority.'**
  String get kdsToastPriorityMarked;

  /// No description provided for @kdsToastNotesSaved.
  ///
  /// In en, this message translates to:
  /// **'Kitchen notes saved.'**
  String get kdsToastNotesSaved;

  /// No description provided for @tableStatusAvailable.
  ///
  /// In en, this message translates to:
  /// **'Available'**
  String get tableStatusAvailable;

  /// No description provided for @tableStatusOccupied.
  ///
  /// In en, this message translates to:
  /// **'Occupied'**
  String get tableStatusOccupied;

  /// No description provided for @tableStatusReserved.
  ///
  /// In en, this message translates to:
  /// **'Reserved'**
  String get tableStatusReserved;

  /// No description provided for @tableStatusWaitingPayment.
  ///
  /// In en, this message translates to:
  /// **'Awaiting payment'**
  String get tableStatusWaitingPayment;

  /// No description provided for @requestCallWaiter.
  ///
  /// In en, this message translates to:
  /// **'Call Waiter'**
  String get requestCallWaiter;

  /// No description provided for @requestBill.
  ///
  /// In en, this message translates to:
  /// **'Request Bill'**
  String get requestBill;

  /// No description provided for @requestNeedWater.
  ///
  /// In en, this message translates to:
  /// **'Need Water'**
  String get requestNeedWater;

  /// No description provided for @requestNeedTissue.
  ///
  /// In en, this message translates to:
  /// **'Need Tissue'**
  String get requestNeedTissue;

  /// No description provided for @billTax.
  ///
  /// In en, this message translates to:
  /// **'Tax'**
  String get billTax;

  /// No description provided for @billServiceCharge.
  ///
  /// In en, this message translates to:
  /// **'Service charge'**
  String get billServiceCharge;

  /// No description provided for @billSubtotalLabel.
  ///
  /// In en, this message translates to:
  /// **'Subtotal'**
  String get billSubtotalLabel;

  /// No description provided for @wtTabFloor.
  ///
  /// In en, this message translates to:
  /// **'Floor ({count})'**
  String wtTabFloor(int count);

  /// No description provided for @wtTabSessions.
  ///
  /// In en, this message translates to:
  /// **'My sessions ({count})'**
  String wtTabSessions(int count);

  /// No description provided for @wtTabReady.
  ///
  /// In en, this message translates to:
  /// **'Ready ({count})'**
  String wtTabReady(int count);

  /// No description provided for @wtTabRequests.
  ///
  /// In en, this message translates to:
  /// **'Requests ({count})'**
  String wtTabRequests(int count);

  /// No description provided for @wtKpiMyTables.
  ///
  /// In en, this message translates to:
  /// **'My tables'**
  String get wtKpiMyTables;

  /// No description provided for @wtKpiAvailable.
  ///
  /// In en, this message translates to:
  /// **'Available'**
  String get wtKpiAvailable;

  /// No description provided for @wtKpiReadyToServe.
  ///
  /// In en, this message translates to:
  /// **'Ready to serve'**
  String get wtKpiReadyToServe;

  /// No description provided for @wtKpiPendingRequests.
  ///
  /// In en, this message translates to:
  /// **'Pending requests'**
  String get wtKpiPendingRequests;

  /// No description provided for @wtKpiMyRevenue.
  ///
  /// In en, this message translates to:
  /// **'My revenue (today)'**
  String get wtKpiMyRevenue;

  /// No description provided for @wtStatActive.
  ///
  /// In en, this message translates to:
  /// **'active'**
  String get wtStatActive;

  /// No description provided for @wtStatOccupied.
  ///
  /// In en, this message translates to:
  /// **'occupied'**
  String get wtStatOccupied;

  /// No description provided for @wtStatBills.
  ///
  /// In en, this message translates to:
  /// **'bills'**
  String get wtStatBills;

  /// No description provided for @wtNoTables.
  ///
  /// In en, this message translates to:
  /// **'No tables configured'**
  String get wtNoTables;

  /// No description provided for @wtNoSessions.
  ///
  /// In en, this message translates to:
  /// **'No open sessions'**
  String get wtNoSessions;

  /// No description provided for @wtNothingReady.
  ///
  /// In en, this message translates to:
  /// **'Nothing ready to serve'**
  String get wtNothingReady;

  /// No description provided for @wtNoRequests.
  ///
  /// In en, this message translates to:
  /// **'No pending requests'**
  String get wtNoRequests;

  /// No description provided for @wtSeats.
  ///
  /// In en, this message translates to:
  /// **'Seats {capacity}'**
  String wtSeats(int capacity);

  /// No description provided for @wtGuests.
  ///
  /// In en, this message translates to:
  /// **'{count} guests'**
  String wtGuests(int count);

  /// No description provided for @wtOrders.
  ///
  /// In en, this message translates to:
  /// **'{count} orders'**
  String wtOrders(int count);

  /// No description provided for @wtFree.
  ///
  /// In en, this message translates to:
  /// **'Free'**
  String get wtFree;

  /// No description provided for @wtBill.
  ///
  /// In en, this message translates to:
  /// **'Bill'**
  String get wtBill;

  /// No description provided for @wtPay.
  ///
  /// In en, this message translates to:
  /// **'Pay'**
  String get wtPay;

  /// No description provided for @wtClose.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get wtClose;

  /// No description provided for @wtServe.
  ///
  /// In en, this message translates to:
  /// **'Serve'**
  String get wtServe;

  /// No description provided for @wtResolve.
  ///
  /// In en, this message translates to:
  /// **'Resolve'**
  String get wtResolve;

  /// No description provided for @wtToastCashierNotified.
  ///
  /// In en, this message translates to:
  /// **'Cashier notified'**
  String get wtToastCashierNotified;

  /// No description provided for @wtToastSessionClosed.
  ///
  /// In en, this message translates to:
  /// **'Session closed'**
  String get wtToastSessionClosed;

  /// No description provided for @wtToastMarkedServed.
  ///
  /// In en, this message translates to:
  /// **'Marked served'**
  String get wtToastMarkedServed;

  /// No description provided for @wtToastRequestResolved.
  ///
  /// In en, this message translates to:
  /// **'Request resolved'**
  String get wtToastRequestResolved;

  /// No description provided for @rangeToday.
  ///
  /// In en, this message translates to:
  /// **'Today'**
  String get rangeToday;

  /// No description provided for @rangeYesterday.
  ///
  /// In en, this message translates to:
  /// **'Yesterday'**
  String get rangeYesterday;

  /// No description provided for @rangeLast7Days.
  ///
  /// In en, this message translates to:
  /// **'Last 7 days'**
  String get rangeLast7Days;

  /// No description provided for @rangeThisMonth.
  ///
  /// In en, this message translates to:
  /// **'This month'**
  String get rangeThisMonth;

  /// No description provided for @dashOverview.
  ///
  /// In en, this message translates to:
  /// **'Overview'**
  String get dashOverview;

  /// No description provided for @dashTodaysSales.
  ///
  /// In en, this message translates to:
  /// **'Today\'s Sales'**
  String get dashTodaysSales;

  /// No description provided for @dashStatOrders.
  ///
  /// In en, this message translates to:
  /// **'orders'**
  String get dashStatOrders;

  /// No description provided for @dashStatAvg.
  ///
  /// In en, this message translates to:
  /// **'avg'**
  String get dashStatAvg;

  /// No description provided for @dashTables.
  ///
  /// In en, this message translates to:
  /// **'Tables'**
  String get dashTables;

  /// No description provided for @dashOccupiedValue.
  ///
  /// In en, this message translates to:
  /// **'{count} occupied'**
  String dashOccupiedValue(int count);

  /// No description provided for @dashStatFree.
  ///
  /// In en, this message translates to:
  /// **'free'**
  String get dashStatFree;

  /// No description provided for @dashStatReserved.
  ///
  /// In en, this message translates to:
  /// **'reserved'**
  String get dashStatReserved;

  /// No description provided for @dashStatToPay.
  ///
  /// In en, this message translates to:
  /// **'to pay'**
  String get dashStatToPay;

  /// No description provided for @dashKitchen.
  ///
  /// In en, this message translates to:
  /// **'Kitchen'**
  String get dashKitchen;

  /// No description provided for @dashPendingValue.
  ///
  /// In en, this message translates to:
  /// **'{count} pending'**
  String dashPendingValue(int count);

  /// No description provided for @dashStatPreparing.
  ///
  /// In en, this message translates to:
  /// **'preparing'**
  String get dashStatPreparing;

  /// No description provided for @dashStatReady.
  ///
  /// In en, this message translates to:
  /// **'ready'**
  String get dashStatReady;

  /// No description provided for @dashCustomerActivity.
  ///
  /// In en, this message translates to:
  /// **'Customer Activity'**
  String get dashCustomerActivity;

  /// No description provided for @dashSessionsValue.
  ///
  /// In en, this message translates to:
  /// **'{count} sessions'**
  String dashSessionsValue(int count);

  /// No description provided for @dashStatQr.
  ///
  /// In en, this message translates to:
  /// **'QR'**
  String get dashStatQr;

  /// No description provided for @dashStatStaff.
  ///
  /// In en, this message translates to:
  /// **'staff'**
  String get dashStatStaff;

  /// No description provided for @dashLiveFloor.
  ///
  /// In en, this message translates to:
  /// **'Live floor'**
  String get dashLiveFloor;

  /// No description provided for @dashNoActiveTables.
  ///
  /// In en, this message translates to:
  /// **'No active tables'**
  String get dashNoActiveTables;

  /// No description provided for @dashSalesByHour.
  ///
  /// In en, this message translates to:
  /// **'Sales by hour'**
  String get dashSalesByHour;

  /// No description provided for @dashSalesByCategory.
  ///
  /// In en, this message translates to:
  /// **'Sales by category'**
  String get dashSalesByCategory;

  /// No description provided for @dashTopItems.
  ///
  /// In en, this message translates to:
  /// **'Top selling items'**
  String get dashTopItems;

  /// No description provided for @dashLiveOrders.
  ///
  /// In en, this message translates to:
  /// **'Live orders'**
  String get dashLiveOrders;

  /// No description provided for @dashTotalSuffix.
  ///
  /// In en, this message translates to:
  /// **'{count} total'**
  String dashTotalSuffix(int count);

  /// No description provided for @dashNoOrdersYet.
  ///
  /// In en, this message translates to:
  /// **'No orders yet'**
  String get dashNoOrdersYet;

  /// No description provided for @dashColOrder.
  ///
  /// In en, this message translates to:
  /// **'Order'**
  String get dashColOrder;

  /// No description provided for @dashColTable.
  ///
  /// In en, this message translates to:
  /// **'Table'**
  String get dashColTable;

  /// No description provided for @dashColSource.
  ///
  /// In en, this message translates to:
  /// **'Source'**
  String get dashColSource;

  /// No description provided for @dashColTime.
  ///
  /// In en, this message translates to:
  /// **'Time'**
  String get dashColTime;

  /// No description provided for @dashColAmount.
  ///
  /// In en, this message translates to:
  /// **'Amount'**
  String get dashColAmount;

  /// No description provided for @dashColStatus.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get dashColStatus;

  /// No description provided for @dashKitchenPerf.
  ///
  /// In en, this message translates to:
  /// **'Kitchen performance'**
  String get dashKitchenPerf;

  /// No description provided for @dashStatAvgPrep.
  ///
  /// In en, this message translates to:
  /// **'avg prep (min)'**
  String get dashStatAvgPrep;

  /// No description provided for @dashStatCompleted.
  ///
  /// In en, this message translates to:
  /// **'completed'**
  String get dashStatCompleted;

  /// No description provided for @dashStatWaitingOver10.
  ///
  /// In en, this message translates to:
  /// **'waiting >10m'**
  String get dashStatWaitingOver10;

  /// No description provided for @dashLongestWaiting.
  ///
  /// In en, this message translates to:
  /// **'Longest waiting: {order} ({minutes} min)'**
  String dashLongestWaiting(String order, int minutes);

  /// No description provided for @dashKitchenLoad.
  ///
  /// In en, this message translates to:
  /// **'Kitchen load'**
  String get dashKitchenLoad;

  /// No description provided for @dashStatPending.
  ///
  /// In en, this message translates to:
  /// **'pending'**
  String get dashStatPending;

  /// No description provided for @dashCustomerRequests.
  ///
  /// In en, this message translates to:
  /// **'Customer requests'**
  String get dashCustomerRequests;

  /// No description provided for @dashNoPendingRequests.
  ///
  /// In en, this message translates to:
  /// **'No pending requests'**
  String get dashNoPendingRequests;

  /// No description provided for @dashInventoryAlerts.
  ///
  /// In en, this message translates to:
  /// **'Inventory alerts'**
  String get dashInventoryAlerts;

  /// No description provided for @dashLowStock.
  ///
  /// In en, this message translates to:
  /// **'Low stock'**
  String get dashLowStock;

  /// No description provided for @dashOutOfStock.
  ///
  /// In en, this message translates to:
  /// **'Out of stock'**
  String get dashOutOfStock;

  /// No description provided for @dashTodaysConsumption.
  ///
  /// In en, this message translates to:
  /// **'Today\'s consumption'**
  String get dashTodaysConsumption;

  /// No description provided for @dashNone.
  ///
  /// In en, this message translates to:
  /// **'None'**
  String get dashNone;

  /// No description provided for @dashMore.
  ///
  /// In en, this message translates to:
  /// **'+{count} more'**
  String dashMore(int count);

  /// No description provided for @dashStaffLeaderboard.
  ///
  /// In en, this message translates to:
  /// **'Staff leaderboard'**
  String get dashStaffLeaderboard;

  /// No description provided for @dashNoStaffActivity.
  ///
  /// In en, this message translates to:
  /// **'No staff activity today'**
  String get dashNoStaffActivity;

  /// No description provided for @dashStatOrd.
  ///
  /// In en, this message translates to:
  /// **'ord'**
  String get dashStatOrd;

  /// No description provided for @dashStatTbl.
  ///
  /// In en, this message translates to:
  /// **'tbl'**
  String get dashStatTbl;

  /// No description provided for @dashRevenueBreakdown.
  ///
  /// In en, this message translates to:
  /// **'Revenue breakdown'**
  String get dashRevenueBreakdown;

  /// No description provided for @dashRevDineIn.
  ///
  /// In en, this message translates to:
  /// **'Dine-in'**
  String get dashRevDineIn;

  /// No description provided for @dashRevTakeaway.
  ///
  /// In en, this message translates to:
  /// **'Takeaway'**
  String get dashRevTakeaway;

  /// No description provided for @dashRevDelivery.
  ///
  /// In en, this message translates to:
  /// **'Delivery'**
  String get dashRevDelivery;

  /// No description provided for @dashRevQrOrdering.
  ///
  /// In en, this message translates to:
  /// **'QR ordering'**
  String get dashRevQrOrdering;

  /// No description provided for @dashRevDiscount.
  ///
  /// In en, this message translates to:
  /// **'Discount'**
  String get dashRevDiscount;

  /// No description provided for @dashRevTaxCollected.
  ///
  /// In en, this message translates to:
  /// **'Tax collected'**
  String get dashRevTaxCollected;

  /// No description provided for @dashRevServiceCharge.
  ///
  /// In en, this message translates to:
  /// **'Service charge'**
  String get dashRevServiceCharge;

  /// No description provided for @dashRevGrandTotal.
  ///
  /// In en, this message translates to:
  /// **'Grand total'**
  String get dashRevGrandTotal;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['bn', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'bn':
      return AppLocalizationsBn();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
