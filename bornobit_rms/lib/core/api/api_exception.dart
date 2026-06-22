import 'package:dio/dio.dart';

/// A user-presentable API error. The backend returns `{ message, errors[] }` on
/// 400/404/409 (FluentValidation / NotFound / Conflict), surfaced as a snackbar.
class ApiException implements Exception {
  final int? statusCode;
  final String message;
  final bool isUnauthorized;

  ApiException(this.message, {this.statusCode, this.isUnauthorized = false});

  @override
  String toString() => message;

  factory ApiException.fromDio(DioException e) {
    final res = e.response;
    final code = res?.statusCode;
    final data = res?.data;
    final serverMsg =
        (data is Map && data['message'] is String) ? data['message'] as String : null;
    if (code == 401) {
      // Prefer the server's reason (e.g. "Invalid credentials." on a failed login) so a bad
      // password isn't mislabelled as an expired session. Fall back to the generic message only
      // for a bodyless 401 (a token actually rejected by middleware).
      return ApiException(serverMsg ?? 'Session expired. Please sign in again.',
          statusCode: 401, isUnauthorized: true);
    }
    String msg = 'Something went wrong.';
    if (serverMsg != null) {
      msg = serverMsg;
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.connectionError) {
      msg = 'Cannot reach the server. Check the API address and that it is running.';
    } else if (e.type == DioExceptionType.receiveTimeout) {
      msg = 'The server took too long to respond.';
    }
    return ApiException(msg, statusCode: code);
  }
}
