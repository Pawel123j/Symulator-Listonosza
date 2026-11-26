# 0004 — Mobile-first: touch controls from Touchscreen + follow camera

**English** · [Polski](0004-mobile-input-touch-i-follow-camera.md)

* Status: accepted
* Date: 2026-06-19
* Deciders: Symulator Listonosza team

## Context and problem

The target platform changed to **mobile devices** (Android/iOS). It also turned out that the
environment is **Unity 6 (`6000.3.14f1`)**, not the previously assumed 2022.3 LTS — the project is
retargeted to Unity 6 (URP 17.3.0, Input System). We need touch controls and a camera with a good
feel, without breaking the existing architecture and without the keyboard as the only input.

## Decision drivers

- **Reliability of the implementation** — the code is written without iterative testing in the
  editor, so we prefer solutions with a small error surface.
- **No hard dependencies** — controls should not tie components together rigidly.
- **Testability on desktop** — we want to check the logic without a device.
- **Good feel** — camera-relative movement, smooth camera follow.

## Considered options (touch input)

1. **Raw `Touchscreen` reading + a thin `MobileInput` bridge** — left half of the screen = floating
   joystick, right half = interaction tap; the result goes into a static `MobileInput`, read by the
   player/service/UI. No EventSystem, no On-Screen Controls.
2. **Input System On-Screen Controls** (`OnScreenStick`/`OnScreenButton`) — needs a Canvas with an
   EventSystem + `InputSystemUIInputModule`, and setting `controlPath` from code can be fragile.
3. **Custom joystick on UI events** (`IPointerHandler`) — also needs an EventSystem and raycasters.

## Decision

We choose **option 1**. The `TouchControls` component reads `Touchscreen.current` and writes the
state to `MobileInput` (movement vector, interaction request, visualisation data). `PlayerController`
and `DeliveryService` **add** this input to the existing keyboard/gamepad input, so both paths work
in parallel (desktop = tests, phone = game). The joystick visualisation (`TouchJoystickVisual`) and
the interaction prompt (`InteractionPromptHud`) are done by the UI reading that state — without an EventSystem.

**Camera:** `FollowCamera` (smooth lerp + offset, looks at the player). `PlayerController` gains a
**camera-relative movement** mode (input converted to the XZ plane according to the camera direction).

**Day screens:** the "advance" action in `GameLoopManager` gets a `<Pointer>/press` binding, so
starting and restarting the day works with a tap (mobile) and click/Enter (desktop).

### Consequences

**Positive**
- No EventSystem/On-Screen Controls = small error surface, easier to get right "blind".
- `MobileInput` is a thin boundary — no hard references between controls and logic.
- Fully testable on desktop (inputs are added together).
- Camera-relative movement + follow camera noticeably improve the feel.

**Negative / risks**
- `MobileInput` is static state (mitigation: `RuntimeInitializeOnLoadMethod` resets it at start).
- `InteractionPromptHud`/`TouchJoystickVisual` introduce a **UI → Gameplay** dependency (acyclic).
  A conscious trade-off: domain delivery events still travel through SO channels (the counters HUD
  stays channel-only) — direct reading only applies to real-time widgets.
- Floating joystick without a "fixed zone" — to be refined after device tests.

## Pros and cons of the options

- **On-Screen Controls:** idiomatic for the Input System, but need an EventSystem and setting
  `controlPath` from code is fragile — risky without iterating in the editor.
- **Joystick on UI events:** flexible, but depends on the EventSystem/raycasters and a correct
  Canvas hierarchy — a larger error surface when writing without tests.
