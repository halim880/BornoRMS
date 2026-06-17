import 'package:dio/dio.dart';
import '../auth/token_store.dart';
import '../config/app_config.dart';
import 'api_exception.dart';

/// Builds the shared Dio instance: base URL, JWT bearer injection, and error
/// normalization. A 401 clears the token and invokes [onUnauthorized] so the UI
/// can kick the user back to the login screen.
class ApiClient {
  final Dio dio;
  final TokenStore tokenStore;
  final void Function() onUnauthorized;

  ApiClient({required this.tokenStore, required this.onUnauthorized})
      : dio = Dio(BaseOptions(
          baseUrl: AppConfig.baseUrl,
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 20),
        )) {
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await tokenStore.readToken();
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (e, handler) async {
        if (e.response?.statusCode == 401) {
          await tokenStore.clear();
          onUnauthorized();
        }
        handler.next(e);
      },
    ));
  }

  /// Wraps a Dio call, converting DioException → ApiException for the UI layer.
  Future<T> guard<T>(Future<T> Function() call) async {
    try {
      return await call();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
