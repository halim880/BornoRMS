import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class PrinterSettings {
  final String host; // empty = no thermal printer configured
  final int port;
  final String headerName;
  const PrinterSettings({this.host = '', this.port = 9100, this.headerName = 'BornoBit Restaurant'});

  bool get configured => host.trim().isNotEmpty;

  PrinterSettings copyWith({String? host, int? port, String? headerName}) =>
      PrinterSettings(host: host ?? this.host, port: port ?? this.port, headerName: headerName ?? this.headerName);
}

final printerSettingsProvider =
    AsyncNotifierProvider<PrinterSettingsNotifier, PrinterSettings>(PrinterSettingsNotifier.new);

class PrinterSettingsNotifier extends AsyncNotifier<PrinterSettings> {
  static const _kHost = 'printer_host';
  static const _kPort = 'printer_port';
  static const _kHeader = 'printer_header';
  final _storage = const FlutterSecureStorage();

  @override
  Future<PrinterSettings> build() async {
    final host = await _storage.read(key: _kHost) ?? '';
    final port = int.tryParse(await _storage.read(key: _kPort) ?? '') ?? 9100;
    final header = await _storage.read(key: _kHeader);
    return PrinterSettings(
      host: host,
      port: port,
      headerName: (header == null || header.isEmpty) ? 'BornoBit Restaurant' : header,
    );
  }

  Future<void> save({required String host, required int port, required String headerName}) async {
    await _storage.write(key: _kHost, value: host.trim());
    await _storage.write(key: _kPort, value: port.toString());
    await _storage.write(key: _kHeader, value: headerName.trim());
    state = AsyncData(PrinterSettings(host: host.trim(), port: port, headerName: headerName.trim()));
  }
}
