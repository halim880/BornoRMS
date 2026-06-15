import 'package:flutter/material.dart';

void showError(BuildContext context, String message) =>
    _show(context, message, isError: true);

void showInfo(BuildContext context, String message) =>
    _show(context, message, isError: false);

void _show(BuildContext context, String message, {required bool isError}) {
  final messenger = ScaffoldMessenger.maybeOf(context);
  if (messenger == null) return;
  messenger
    ..clearSnackBars()
    ..showSnackBar(SnackBar(
      content: Text(message),
      backgroundColor: isError ? Colors.red.shade700 : null,
      behavior: SnackBarBehavior.floating,
    ));
}
