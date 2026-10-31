import 'package:flutter_test/flutter_test.dart';
import 'package:hrms_mobile/app/app.dart';
import 'package:hrms_mobile/app/app_config.dart';

void main() {
  testWidgets('shows the app name and flavor', (tester) async {
    await tester.pumpWidget(const HrmsApp(config: AppConfig.dev));

    expect(find.text('HRMS Dev'), findsOneWidget);
    expect(find.text('Mobile app skeleton (dev)'), findsOneWidget);
  });
}
