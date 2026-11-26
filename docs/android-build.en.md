# Android build

**English** · [Polski](android-build.md)

The project has a ready APK build script:

- `Listonosz.EditorTools.MobileBuild.BuildAndroid` (menu **Listonosz → Build APK (Android)**)
- output: `Builds/Listonosz.apk` (signed with the Unity debug key — for testing, not for store release)

## One-time step: Android SDK licences

Unity's bundled Android SDK lives in `C:\Program Files`, so accepting the licences requires administrator rights.

Run PowerShell as administrator in the project folder (`<PROJECT>` = the folder with this repository) and execute:

```powershell
powershell -ExecutionPolicy Bypass -File "<PROJECT>\tools\Accept-UnityAndroidLicenses.ps1"
```

Answer `y` to the `sdkmanager` prompts.

## APK build in batch mode

After accepting the licences (the path to `Unity.exe` is the default Unity Hub location — adjust it):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -quit -accept-apiupdate -buildTarget Android -projectPath "<PROJECT>" -executeMethod Listonosz.EditorTools.MobileBuild.BuildAndroid -logFile "<PROJECT>\unity_apk.log"
```

The log file is git-ignored; do not attach it raw anywhere, it contains local machine paths.
