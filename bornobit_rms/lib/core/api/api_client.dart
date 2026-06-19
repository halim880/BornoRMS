import 'package:dio/dio.dart';
import '../auth/token_store.dart';
import '../config/app_config.dart';
import 'api_exception.dart';

/// Builds the shared Dio instance: base URL, JWT bearer injection, and error
/// normalization. On a 401 it first tries a single silent token refresh (via
/// [refreshSession]) and replays the request; only if that fails does it clear
/// the token and invoke [onUnauthorized] to kick the user back to login.
class ApiClient {
  final Dio dio;
  final TokenStore tokenStore;
  final void Function() onUnauthorized;

  /// Refreshes the access token using the stored refresh token. Returns true on success
  /// (token store updated). Optional so tests / non-auth contexts can omit it.
  final Future<bool> Function()? refreshSession;

  // Single-flight guard: many requests can 401 at once when a token expires; they all
  // await the same refresh instead of stampeding the refresh endpoint.
  Future<bool>? _refreshInFlight;

  ApiClient({required this.tokenStore, required this.onUnauthorized, this.refreshSession})
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
        final req = e.requestOptions;
        final isAuthCall = req.path.contains('/staff/auth/');
        final alreadyRetried = req.extra['__auth_retried'] == true;

        if (e.response?.statusCode == 401 &&
            refreshSession != null &&
            !isAuthCall &&
            !alreadyRetried) {
          final refreshed = await _refreshOnce();
          if (refreshed) {
            // Replay the original request once with the new token. Safe even for mutations:
            // a 401 means the server rejected it unprocessed, so there's nothing to double-apply.
            final token = await tokenStore.readToken();
            if (token != null) req.headers['Authorization'] = 'Bearer $token';
            req.extra['__auth_retried'] = true;
            try {
              return handler.resolve(await dio.fetch(req));
            } catch (_) {
              // fall through to the unauthorized path
            }
          }
          await tokenStore.clear();
          onUnauthorized();
          return handler.next(e);
        }

        if (e.response?.statusCode == 401) {
          await tokenStore.clear();
          onUnauthorized();
        }
        handler.next(e);
      },
    ));

    // Retry transient failures on idempotent GETs (a brief network blip on flaky
    // restaurant Wi-Fi) with a short exponential backoff. Mutations (POST/PUT/PATCH)
    // are never auto-retried here — replaying them is the write-queue's job.
    dio.interceptors.add(InterceptorsWrapper(
      onError: (e, handler) async {
        final req = e.requestOptions;
        final attempt = (req.extra['retry_attempt'] as int?) ?? 0;
        const maxRetries = 2;
        final isGet = req.method.toUpperCase() == 'GET';
        final isTransient = e.type == DioExceptionType.connectionTimeout ||
            e.type == DioExceptionType.receiveTimeout ||
            e.type == DioExceptionType.connectionError;

        if (isGet && isTransient && attempt < maxRetries) {
          await Future.delayed(Duration(milliseconds: 300 * (1 << attempt)));
          req.extra['retry_attempt'] = attempt + 1;
          try {
            return handler.resolve(await dio.fetch(req));
          } catch (_) {
            // Fall through — surface the original error below.
          }
        }
        handler.next(e);
      },
    ));
  }

  /// Runs [refreshSession] at most once concurrently; parallel 401s share the result.
  Future<bool> _refreshOnce() {
    return _refreshInFlight ??= () async {
      try {
        return await refreshSession!();
      } finally {
        _refreshInFlight = null;
      }
    }();
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
