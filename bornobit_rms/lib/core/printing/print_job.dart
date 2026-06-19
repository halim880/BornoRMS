/// A frozen ESC/POS print job parked in the offline retry queue. The raw bytes
/// are captured at enqueue time (content is final — a receipt never changes), so
/// a retry just re-sends them to the same printer; nothing is re-rendered.
class PrintJob {
  final String id;
  final String orderId;
  final String orderNumber;
  final bool isKot;
  final String host;
  final int port;

  /// base64-encoded ESC/POS payload (kept opaque so persistence is trivial).
  final String bytesB64;

  /// Epoch millis when first enqueued (for an "age" display).
  final int createdAtMs;
  final int attempts;
  final String? lastError;

  const PrintJob({
    required this.id,
    required this.orderId,
    required this.orderNumber,
    required this.isKot,
    required this.host,
    required this.port,
    required this.bytesB64,
    required this.createdAtMs,
    this.attempts = 0,
    this.lastError,
  });

  String get label => isKot ? 'KOT' : 'Receipt';
  String get target => '$host:$port';

  PrintJob copyWith({int? attempts, String? lastError}) => PrintJob(
        id: id,
        orderId: orderId,
        orderNumber: orderNumber,
        isKot: isKot,
        host: host,
        port: port,
        bytesB64: bytesB64,
        createdAtMs: createdAtMs,
        attempts: attempts ?? this.attempts,
        lastError: lastError ?? this.lastError,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'orderId': orderId,
        'orderNumber': orderNumber,
        'isKot': isKot,
        'host': host,
        'port': port,
        'bytesB64': bytesB64,
        'createdAtMs': createdAtMs,
        'attempts': attempts,
        'lastError': lastError,
      };

  factory PrintJob.fromJson(Map<String, dynamic> j) => PrintJob(
        id: j['id'] as String,
        orderId: (j['orderId'] ?? '') as String,
        orderNumber: (j['orderNumber'] ?? '') as String,
        isKot: (j['isKot'] ?? false) as bool,
        host: (j['host'] ?? '') as String,
        port: (j['port'] as num?)?.toInt() ?? 9100,
        bytesB64: (j['bytesB64'] ?? '') as String,
        createdAtMs: (j['createdAtMs'] as num?)?.toInt() ?? 0,
        attempts: (j['attempts'] as num?)?.toInt() ?? 0,
        lastError: j['lastError'] as String?,
      );
}
