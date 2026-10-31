import 'package:flutter/widgets.dart';

import 'app/app.dart';
import 'app/app_config.dart';

/// Shared startup for every flavor entrypoint. Crash reporting, DI and
/// secure storage initialisation are added here in prompt 07.
void bootstrap(AppConfig config) {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(HrmsApp(config: config));
}
