// DTO for the App Settings screen — mirrors the backend BillingSettingsDto
// (BillingSettingsCommands.cs). JSON field names match the C# DTO property names
// (camelCase): vatPercent, serviceChargePercent, currency, tipEnabled,
// highDiscountThresholdPercent, priceIncludesTax.

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();

/// The restaurant-wide billing/settings defaults.
class AppSettings {
  final double vatPercent;
  final double serviceChargePercent;
  final String currency;
  final bool tipEnabled;
  final double highDiscountThresholdPercent;
  final bool priceIncludesTax;

  AppSettings({
    required this.vatPercent,
    required this.serviceChargePercent,
    required this.currency,
    required this.tipEnabled,
    required this.highDiscountThresholdPercent,
    required this.priceIncludesTax,
  });

  factory AppSettings.fromJson(Map<String, dynamic> j) => AppSettings(
        vatPercent: _d(j['vatPercent']),
        serviceChargePercent: _d(j['serviceChargePercent']),
        currency: j['currency'] as String? ?? 'Tk',
        tipEnabled: j['tipEnabled'] as bool? ?? false,
        highDiscountThresholdPercent: _d(j['highDiscountThresholdPercent']),
        priceIncludesTax: j['priceIncludesTax'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'vatPercent': vatPercent,
        'serviceChargePercent': serviceChargePercent,
        'currency': currency,
        'tipEnabled': tipEnabled,
        'highDiscountThresholdPercent': highDiscountThresholdPercent,
        'priceIncludesTax': priceIncludesTax,
      };

  AppSettings copyWith({
    double? vatPercent,
    double? serviceChargePercent,
    String? currency,
    bool? tipEnabled,
    double? highDiscountThresholdPercent,
    bool? priceIncludesTax,
  }) =>
      AppSettings(
        vatPercent: vatPercent ?? this.vatPercent,
        serviceChargePercent: serviceChargePercent ?? this.serviceChargePercent,
        currency: currency ?? this.currency,
        tipEnabled: tipEnabled ?? this.tipEnabled,
        highDiscountThresholdPercent:
            highDiscountThresholdPercent ?? this.highDiscountThresholdPercent,
        priceIncludesTax: priceIncludesTax ?? this.priceIncludesTax,
      );
}
