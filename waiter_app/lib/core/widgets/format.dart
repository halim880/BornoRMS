import 'package:intl/intl.dart';

final _money = NumberFormat('#,##0.##');
final _money2 = NumberFormat('#,##0.00');

/// Formats a server-computed amount for display. Never used to re-sum money.
String money(num value, {String currency = '', bool twoDp = false}) {
  final n = (twoDp ? _money2 : _money).format(value);
  return currency.isEmpty ? n : '$currency $n';
}
