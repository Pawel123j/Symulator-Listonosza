# Technical demo (3 minutes) and its safety boundaries

**English** · [Polski](demo.pl.md)

Scope: the day-loop logic and the build entry scene, proven by the headless harness in
`tools/LogicTests`. It does **not** cover Play Mode, the APK build or a device; see the boundary below.

## Trust boundaries

| Element | Data source | Written to | Visibility |
|---|---|---|---|
| Harness fixtures (parcels P1/P2, addresses "ul. Kwiatowa 1/12") | synthetic, created in `Program.cs` | memory only | console of the presenter |
| Harness `PlayerPrefs` | in-memory adapter (`UnityStubs.cs`) | memory only | none |
| Harness report | `dotnet run --project tools/LogicTests` | stdout | screen; contains only synthetic labels and repo-relative paths |
| Game assets (`Assets/_Project/Settings/Generated/*.asset`) | repo (fictional street names) | — | public repo |
| `Listonosz.BestScore` (`GameLoopManager`) | gameplay | real `PlayerPrefs` on the player's machine/device | local only |
| `Debug.Log` lines (`GameManager`, `DeliveryService`, `GameLoopManager`) | gameplay | Editor console / Android logcat | local; fictional data only |
| Build manifest `ProjectSettings/EditorBuildSettings.asset`, `MobileBuild.ScenePath` | repo | read as text by the harness | public repo |
| APK `Builds/Listonosz.apk` | `MobileBuild.BuildAndroid` | `Builds/` (git-ignored) | local; signed with the Unity **debug** key (no custom keystore) — not for distribution |
| Batchmode log `unity_apk.log` | Unity `-logFile` | project root (git-ignored) | local; contains machine paths — never attach raw |

Settings checked: Unity Services/Analytics/Crash reporting disabled (`UnityConnectSettings.asset`),
`ForceInternetPermission: 0`, no custom Android manifest or keystore, no network code in `Assets/`.
Minimal permissions for the demo: read access to a clone of the repo and a local .NET 8 SDK;
no credentials, no network after the SDK is installed, no Unity licence.

## Script (rehearsed: ~11 s of machine time on a fresh clone)

Prerequisites: .NET SDK 8 installed, repository cloned.

1. **Problem (30 s).** A player who finishes a day and starts again must get the same clean
   start; the entry is `GameManager.Bootstrap` (`[RuntimeInitializeOnLoadMethod]`) in
   `Assets/_Project/Scripts/Core/GameManager.cs`.
2. **Result (45 s).**
   ```bash
   dotnet run --project tools/LogicTests
   ```
   Expected: `== A1 …` with three `[ok]` lines, last line `PASSED: <n> checks`, exit code 0.
3. **The test catches a regression (45 s).** Remove the duplicate guard and run again:
   ```bash
   sed -i 's/if (Instance != null \&\& Instance != this)/if (false)/' Assets/_Project/Scripts/Core/GameManager.cs
   dotnet run --project tools/LogicTests
   git checkout -- Assets/_Project/Scripts/Core/GameManager.cs
   ```
   Expected: `FAILED: 3 of <n> checks` (`live GameManager objects: expected=1 observed=2`), then a clean tree.
4. **Boundary: build entry scene (45 s).** Point `MobileBuild.ScenePath` at `Bootstrap.unity`:
   ```bash
   sed -i 's|Scenes/Playground.unity";|Scenes/Bootstrap.unity";|' Assets/_Project/Scripts/Editor/MobileBuild.cs
   dotnet run --project tools/LogicTests
   git checkout -- .
   ```
   Expected: `FAILED: 2 of <n> checks` with `entry mismatch: manifest=…Playground.unity MobileBuild=…Bootstrap.unity`.
5. **Question for the audience (15 s).** *"What does this harness not prove?"* — Answer: that
   the APK builds and that touch input, physics and rendering behave on a device; those need the
   Unity editor, the Android module and a phone, and were not run.
