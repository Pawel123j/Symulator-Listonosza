# Pokaz techniczny (3 minuty) i jego granice bezpieczeństwa

[English](demo.md) · **Polski**

Zakres: logika pętli dnia i scena wejścia buildu, potwierdzone testami bez Unity w
`tools/LogicTests`. Pokaz **nie** obejmuje Play Mode, buildu APK ani urządzenia — patrz granica niżej.

## Granice zaufania

| Element | Źródło danych | Zapis | Widoczność |
|---|---|---|---|
| Fikstury testów (przesyłki P1/P2, adresy „ul. Kwiatowa 1/12”) | syntetyczne, tworzone w `Program.cs` | tylko pamięć | konsola prezentującego |
| `PlayerPrefs` w testach | atrapa w pamięci (`UnityStubs.cs`) | tylko pamięć | brak |
| Raport testów | `dotnet run --project tools/LogicTests` | stdout | ekran; tylko syntetyczne etykiety i ścieżki względne repo |
| Assety gry (`Assets/_Project/Settings/Generated/*.asset`) | repo (fikcyjne nazwy ulic) | — | publiczne repo |
| `Listonosz.BestScore` (`GameLoopManager`) | rozgrywka | prawdziwe `PlayerPrefs` na komputerze/urządzeniu gracza | tylko lokalnie |
| Linie `Debug.Log` (`GameManager`, `DeliveryService`, `GameLoopManager`) | rozgrywka | konsola edytora / logcat Androida | lokalnie; tylko fikcyjne dane |
| Manifest buildu `ProjectSettings/EditorBuildSettings.asset`, `MobileBuild.ScenePath` | repo | czytane jako tekst przez testy | publiczne repo |
| APK `Builds/Listonosz.apk` | `MobileBuild.BuildAndroid` | `Builds/` (ignorowane przez git) | lokalnie; podpisany kluczem **debug** Unity (bez własnego keystore) — nie do dystrybucji |
| Log batchmode `unity_apk.log` | Unity `-logFile` | katalog główny projektu (ignorowany przez git) | lokalnie; zawiera ścieżki komputera — nigdy nie załączaj surowego |

Sprawdzone ustawienia: usługi Unity / Analytics / Crash Reporting wyłączone (`UnityConnectSettings.asset`),
`ForceInternetPermission: 0`, brak własnego manifestu Androida i keystore, brak kodu sieciowego w `Assets/`.
Minimalne uprawnienia do pokazu: odczyt klona repo i lokalny .NET SDK 8; bez poświadczeń,
bez sieci po instalacji SDK, bez licencji Unity.

## Scenariusz (próba: ~11 s pracy maszyny na świeżym klonie)

Wymagania: zainstalowany .NET SDK 8, sklonowane repozytorium.

1. **Problem (30 s).** Gracz, który kończy dzień i zaczyna od nowa, musi dostać ten sam czysty
   start; wejściem jest `GameManager.Bootstrap` (`[RuntimeInitializeOnLoadMethod]`) w
   `Assets/_Project/Scripts/Core/GameManager.cs`.
2. **Wynik (45 s).**
   ```bash
   dotnet run --project tools/LogicTests
   ```
   Oczekiwane: `== A1 …` z trzema liniami `[ok]`, ostatnia linia `PASSED: <n> checks`, kod wyjścia 0.
3. **Test łapie regresję (45 s).** Usuń strażnik duplikatu i uruchom ponownie:
   ```bash
   sed -i 's/if (Instance != null \&\& Instance != this)/if (false)/' Assets/_Project/Scripts/Core/GameManager.cs
   dotnet run --project tools/LogicTests
   git checkout -- Assets/_Project/Scripts/Core/GameManager.cs
   ```
   Oczekiwane: `FAILED: 3 of <n> checks` (`live GameManager objects: expected=1 observed=2`), potem czyste drzewo.
4. **Granica: scena wejścia buildu (45 s).** Ustaw `MobileBuild.ScenePath` na `Bootstrap.unity`:
   ```bash
   sed -i 's|Scenes/Playground.unity";|Scenes/Bootstrap.unity";|' Assets/_Project/Scripts/Editor/MobileBuild.cs
   dotnet run --project tools/LogicTests
   git checkout -- .
   ```
   Oczekiwane: `FAILED: 2 of <n> checks` z `entry mismatch: manifest=…Playground.unity MobileBuild=…Bootstrap.unity`.
5. **Pytanie do widowni (15 s).** *„Czego te testy nie dowodzą?”* — Odpowiedź: że APK się buduje
   i że dotyk, fizyka i renderowanie działają na urządzeniu; to wymaga edytora Unity, modułu Android
   i telefonu, a nie zostało uruchomione.
