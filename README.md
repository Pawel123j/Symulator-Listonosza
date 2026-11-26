# Postman Simulator (Symulator Listonosza)

[![Logic tests](https://github.com/Pawel123j/Symulator-Listonosza/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/Pawel123j/Symulator-Listonosza/actions/workflows/tests.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**English** · [Polski](README.pl.md)

A mobile game prototype in Unity 6: you play a postman who picks up parcels at the post office and
delivers them across town before the working day ends. Free movement in the world, with a grid used
only as a logical layer (world position ↔ cell).

## Features

- **Workday loop** as a state machine: briefing → work → summary (`DayStart → Working → DayEnd`),
  with a timer, score by parcel priority, speed bonus, combo and a persistent best score.
- **Deliveries**: pick-up point, delivery targets bound to address assets, a bag with an optional
  capacity limit, failed parcels at the end of the day.
- **Mobile controls**: touch joystick on the left half of the screen, tap to interact on the right
  half, follow camera; keyboard and gamepad work in parallel.
- **Feedback**: target beacon, confetti and a procedural sound on delivery, HUD with active parcels
  and the direction to the nearest target.

## Tech stack

- **Unity 6 `6000.3.14f1`**, URP 17.3.0, Input System 1.14.1, C#
- Target platform: **Android / iOS**
- Architecture: ScriptableObject-driven data + **event channels** (no hard references between
  systems), manager pattern, assembly definitions per layer
- No paid assets: primitives and simple meshes as placeholders

## Architecture

```
Assets/_Project/
  Scripts/
    Core/      GameManager, event channels                     -> Listonosz.Core
    Data/      ScriptableObjects: parcels, addresses, day DB   -> Listonosz.Data
    Grid/      logical grid (world <-> cell)                   -> Listonosz.Grid
    Gameplay/  player, deliveries, day loop, interactions      -> Listonosz.Gameplay
    UI/        HUD, day screens, interaction prompt            -> Listonosz.UI
    Editor/    scene builder, Android build
  Scenes/      Playground.unity (game, build entry), Bootstrap.unity (stage 0)
docs/adr/      Architecture Decision Records
tools/LogicTests/  headless regression tests (.NET 8)
```

Assembly dependencies enforce the direction of the architecture:
`Data → Core`, `Grid → Core, Data`, `Gameplay → Core, Data, Grid`, `UI → Core, Data`.
Design decisions are recorded in [docs/adr](docs/adr/README.md) (in Polish).

## Tests without Unity

`tools/LogicTests` compiles the **real** gameplay scripts against a minimal stand-in of the Unity
API (lifecycle, in-memory `PlayerPrefs`, simulated input, record-only audio/particles) and checks
the day loop, deliveries, event-channel subscriptions and the build entry scene — 131 checks in
about 5 seconds. It runs in CI on every push and pull request.

Requirements: .NET SDK 8 (`apt install dotnet-sdk-8.0` or `winget install Microsoft.DotNet.SDK.8`).

```bash
dotnet run --project tools/LogicTests
```

Expected: the last line is `PASSED: <n> checks` and the exit code is `0`. The full list of covered
behaviour is in [README.pl.md](README.pl.md#testy-logiki-pętli-dnia-bez-unity); a 3-minute demo
script is in [docs/demo.md](docs/demo.md).

These tests do not replace Play Mode or a device test: there is no rendering, physics or real touch
input.

## Running the game

1. **Unity Hub → Open** this folder; it opens in `6000.3.14f1` and resolves the packages.
2. Accept enabling the **new Input System backend** when asked (the editor restarts).
3. Open `Assets/_Project/Scenes/Playground.unity` and press **Play**. To regenerate the scene and
   its assets use the menu **Listonosz → Zbuduj scenę testową**.
4. Without a phone: **Game view → Simulator** emulates touch with the mouse.

Android build: menu **Listonosz → Build APK (Android)** or batch mode, see
[docs/android-build.md](docs/android-build.md). The APK is signed with the Unity debug key.

## License

[MIT](LICENSE)
