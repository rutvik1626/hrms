# hrms_mobile

Flutter app (Android + iOS) for HRMS. Architecture and screens: `docs/HRMS-Requirements.md` → Stage 8.

## First-time setup (once Flutter is installed)
The Dart code was written by hand. The native `android/` and `ios/` folders don't exist yet; generate them in place:

```bash
cd src/Mobile/hrms_mobile
flutter create --org com.divynesystems --project-name hrms_mobile --platforms android,ios .
flutter pub get
flutter analyze && flutter test
```

`flutter create .` keeps existing files (`lib/`, `test/`, `pubspec.yaml`). If it overwrites `lib/main.dart` or `test/widget_test.dart`, delete `test/widget_test.dart` and restore `lib/main.dart` from git.

Application id: `flutter create` generates `com.divynesystems.hrms_mobile`. Prompt 07 changes it to the chosen id `com.divynesystems.hrms` (prod), with `.dev` / `.staging` suffixes, when it configures native flavors.

## Run
| Flavor | Command |
|---|---|
| dev | `flutter run -t lib/main_dev.dart` |
| staging | `flutter run -t lib/main_staging.dart --dart-define=API_BASE_URL=https://...` |
| prod | `flutter run -t lib/main_prod.dart --dart-define=API_BASE_URL=https://...` |

`--flavor <name>` is added to these commands once prompt 07 sets up native flavors.
The dev flavor targets the local gateway at `http://10.0.2.2:5000` (host machine from the Android emulator). Override with `--dart-define=API_BASE_URL=...`, e.g. for a physical device on your LAN.
