import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// A standard modal create/edit dialog: titled header, scrollable body, and a
/// Cancel / Save footer with a busy state. Used by the CRUD module screens.
///
/// [onSave] returns true to close the dialog (success). Throwing surfaces the
/// error via [errorText] and keeps the dialog open.
class AppFormDialog extends StatefulWidget {
  final String title;
  final IconData? icon;
  final Widget child;
  final Future<bool> Function() onSave;
  final String saveLabel;
  final double maxWidth;
  const AppFormDialog({
    super.key,
    required this.title,
    required this.child,
    required this.onSave,
    this.icon,
    this.saveLabel = 'Save',
    this.maxWidth = 520,
  });

  @override
  State<AppFormDialog> createState() => _AppFormDialogState();
}

class _AppFormDialogState extends State<AppFormDialog> {
  bool _busy = false;
  String? _error;

  Future<void> _save() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final ok = await widget.onSave();
      if (ok && mounted) {
        Navigator.of(context).pop(true);
        return;
      }
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    }
    if (mounted) setState(() => _busy = false);
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: widget.maxWidth, maxHeight: 680),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // header
            Container(
              padding: const EdgeInsets.fromLTRB(16, 14, 8, 14),
              decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Bo.border))),
              child: Row(
                children: [
                  if (widget.icon != null) ...[
                    Icon(widget.icon, size: 20, color: Bo.textMuted),
                    const SizedBox(width: 8),
                  ],
                  Expanded(
                    child: Text(widget.title,
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: Bo.text)),
                  ),
                  IconButton(
                    onPressed: _busy ? null : () => Navigator.of(context).pop(false),
                    icon: const Icon(Icons.close),
                  ),
                ],
              ),
            ),
            // body
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: widget.child,
              ),
            ),
            // error
            if (_error != null)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                color: Bo.dangerSoft,
                child: Text(_error!, style: const TextStyle(color: Bo.danger, fontSize: 13)),
              ),
            // footer
            Container(
              padding: const EdgeInsets.all(12),
              decoration: const BoxDecoration(border: Border(top: BorderSide(color: Bo.border))),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: _busy ? null : () => Navigator.of(context).pop(false),
                    child: const Text('Cancel'),
                  ),
                  const SizedBox(width: 8),
                  FilledButton(
                    onPressed: _busy ? null : _save,
                    child: _busy
                        ? const SizedBox(
                            width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : Text(widget.saveLabel),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// A labelled field wrapper for use inside [AppFormDialog] bodies.
class FormField2 extends StatelessWidget {
  final String label;
  final Widget child;
  const FormField2({super.key, required this.label, required this.child});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Bo.textMuted)),
          const SizedBox(height: 6),
          child,
        ],
      ),
    );
  }
}
