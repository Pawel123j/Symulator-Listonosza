# Android build

Projekt ma gotowy skrypt buildu APK:

- `Listonosz.EditorTools.MobileBuild.BuildAndroid`
- wynik: `Builds/Listonosz.apk`

## Jednorazowy krok: licencje Android SDK

Wbudowany Android SDK Unity lezy w `C:\Program Files`, wiec akceptacja licencji wymaga administratora.

Uruchom PowerShell jako administrator w katalogu projektu (`<PROJEKT>` = katalog z tym repozytorium) i wykonaj:

```powershell
powershell -ExecutionPolicy Bypass -File "<PROJEKT>\tools\Accept-UnityAndroidLicenses.ps1"
```

Przy pytaniach `sdkmanager` wpisuj `y`.

## Build APK z batchmode

Po zaakceptowaniu licencji (ścieżka do `Unity.exe` to domyślna lokalizacja Unity Hub — dostosuj ją):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -quit -accept-apiupdate -buildTarget Android -projectPath "<PROJEKT>" -executeMethod Listonosz.EditorTools.MobileBuild.BuildAndroid -logFile "<PROJEKT>\unity_apk.log"
```
