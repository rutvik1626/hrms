import 'package:flutter/material.dart';

import 'app_config.dart';

/// Root widget. Placeholder until prompt 07 adds routing, theming, auth and app lock.
class HrmsApp extends StatelessWidget {
  const HrmsApp({super.key, required this.config});

  final AppConfig config;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: config.appName,
      debugShowCheckedModeBanner: !config.isProduction,
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
      darkTheme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: Colors.indigo,
        brightness: Brightness.dark,
      ),
      home: Scaffold(
        appBar: AppBar(title: Text(config.appName)),
        body: Center(
          child: Text('Mobile app skeleton (${config.flavor.name})'),
        ),
      ),
    );
  }
}
