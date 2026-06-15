import 'package:dio/dio.dart';

/// A user-presentable API error. The backend returns `{ message, errors[] }` on
/// 400/404/409 (FluentValidation / NotFound / Conflict), which we surface as a
/// snackbar — the mobile equivalent of the Blazor console's error toasts.
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
    if (code == 401) {
      return ApiException('Session expired. Please sign in again.',
          statusCode: 401, isUnauthorized: true);
    }
    String msg = 'Something went wrong.';
    final data = res?.data;
    if (data is Map && data['message'] is String) {
      msg = data['message'] as String;
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.connectionError) {
      msg = 'Cannot reach the server. Check the API address and that you are on the same network.';
    } else if (e.type == DioExceptionType.receiveTimeout) {
      msg = 'The server took too long to respond.';
    }
    return ApiException(msg, statusCode: code);
  }
}
