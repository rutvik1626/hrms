/// Build flavor. Each flavor has its own bundle id, app name and backend.
enum Flavor { dev, staging, prod }

/// Per-flavor configuration, chosen by the `main_<flavor>.dart` entrypoint.
///
/// The API base URL can be overridden at build time:
/// `flutter run --flavor dev -t lib/main_dev.dart --dart-define=API_BASE_URL=http://192.168.1.10:5000`
class AppConfig {
  const AppConfig({
    required this.flavor,
    required this.appName,
    required this.apiBaseUrl,
  });

  final Flavor flavor;
  final String appName;
  final String apiBaseUrl;

  bool get isProduction => flavor == Flavor.prod;

  /// Local gateway; 10.0.2.2 is the host machine as seen from the Android emulator.
  static const dev = AppConfig(
    flavor: Flavor.dev,
    appName: 'HRMS Dev',
    apiBaseUrl: String.fromEnvironment('API_BASE_URL', defaultValue: 'http://10.0.2.2:5000'),
  );

  // Staging/prod URLs are set when the domain is decided (prompt 19); pass API_BASE_URL until then.
  static const staging = AppConfig(
    flavor: Flavor.staging,
    appName: 'HRMS Staging',
    apiBaseUrl: String.fromEnvironment('API_BASE_URL'),
  );

  static const prod = AppConfig(
    flavor: Flavor.prod,
    appName: 'HRMS',
    apiBaseUrl: String.fromEnvironment('API_BASE_URL'),
  );
}
