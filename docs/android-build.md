# Android build

Projekt ma gotowy skrypt buildu APK:

- `Listonosz.EditorTools.MobileBuild.BuildAndroid`
- wynik: `Builds/Listonosz.apk`

## Jednorazowy krok: licencje Android SDK

Wbudowany Android SDK Unity lezy w `C:\Program Files`, wiec akceptacja licencji wymaga administratora.

Uruchom PowerShell jako administrator i wykonaj:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\Pawel\Desktop\Symulator Listonosza\tools\Accept-UnityAndroidLicenses.ps1"
```

Przy pytaniach `sdkmanager` wpisuj `y`.

## Build APK z batchmode

Po zaakceptowaniu licencji:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -quit -accept-apiupdate -buildTarget Android -projectPath "C:\Users\Pawel\Desktop\Symulator Listonosza" -executeMethod Listonosz.EditorTools.MobileBuild.BuildAndroid -logFile "C:\Users\Pawel\Desktop\Symulator Listonosza\unity_apk.log"
```

Albo popros Codexa: "zbuduj APK".
