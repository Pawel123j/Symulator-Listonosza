# 0004 — Mobile-first: sterowanie dotykowe z Touchscreen + kamera podążająca

* Status: zaakceptowane
* Data: 2026-06-19
* Decydenci: zespół Symulator Listonosza

## Kontekst i problem

Cel platformowy zmienił się na **urządzenia mobilne** (Android/iOS). Dodatkowo ujawniono, że
środowisko to **Unity 6 (`6000.3.14f1`)**, a nie zakładane wcześniej 2022.3 LTS — projekt
retargetujemy na Unity 6 (URP 17.3.0, Input System). Potrzebujemy sterowania dotykowego oraz
kamery dającej dobry „feel", bez psucia dotychczasowej architektury i bez sterowania
klawiaturą jako jedynego wejścia.

## Czynniki decyzyjne

- **Niezawodność implementacji** — kod powstaje bez możliwości iteracyjnego testu w edytorze,
  więc preferujemy rozwiązania o małej powierzchni błędu.
- **Brak sztywnych zależności** — sterowanie nie powinno wiązać komponentów na sztywno.
- **Testowalność na desktopie** — chcemy móc sprawdzać logikę bez urządzenia.
- **Dobry feel** — ruch względem kamery, płynne podążanie kamery.

## Rozważane opcje (wejście dotykowe)

1. **Surowy odczyt `Touchscreen` + cienki pomost `MobileInput`** — lewa połowa ekranu = pływający
   joystick, prawa = tap interakcji; wynik trafia do statycznego `MobileInput`, czytanego przez
   gracza/serwis/UI. Bez EventSystem, bez On-Screen Controls.
2. **Input System On-Screen Controls** (`OnScreenStick`/`OnScreenButton`) — wymaga Canvas z
   EventSystem + `InputSystemUIInputModule`, a programowe ustawianie `controlPath` bywa kruche.
3. **Własny joystick na zdarzeniach UI** (`IPointerHandler`) — też wymaga EventSystem i raycasterów.

## Decyzja

Wybieramy **opcję 1**. Komponent `TouchControls` czyta `Touchscreen.current` i zapisuje stan do
`MobileInput` (wektor ruchu, żądanie interakcji, dane do wizualizacji). `PlayerController` i
`DeliveryService` **sumują** to wejście z dotychczasowym klawiatura/pad, więc obie ścieżki działają
równolegle (desktop = testy, telefon = gra). Wizualizację joysticka (`TouchJoystickVisual`) i
podpowiedź interakcji (`InteractionPromptHud`) realizuje UI, czytając stan — bez EventSystem.

**Kamera:** `FollowCamera` (płynny lerp + offset, patrzy na gracza). `PlayerController` zyskuje
tryb **ruchu względem kamery** (wejście przeliczane na płaszczyznę XZ wg kierunku kamery).

**Ekrany dnia:** akcja „dalej" w `GameLoopManager` dostaje binding `<Pointer>/press`, więc start
i restart dnia działają na tap (mobile) i klik/Enter (desktop).

### Konsekwencje

**Pozytywne**
- Brak EventSystem/On-Screen Controls = mała powierzchnia błędu, łatwiej trafić „na ślepo".
- `MobileInput` to cienka granica — zero sztywnych referencji między sterowaniem a logiką.
- Pełna testowalność na desktopie (wejścia się sumują).
- Ruch względem kamery + follow-camera znacząco poprawiają feel.

**Negatywne / ryzyka**
- `MobileInput` to stan statyczny (mitygacja: `RuntimeInitializeOnLoadMethod` zeruje przy starcie).
- `InteractionPromptHud`/`TouchJoystickVisual` wprowadzają zależność **UI → Gameplay** (acykliczną).
  Świadomy kompromis: domenowe zdarzenia dostaw nadal lecą kanałami SO (HUD liczników pozostaje
  channel-only) — bezpośredni odczyt dotyczy tylko widżetów czasu rzeczywistego.
- Pływający joystick bez „strefy stałej" — do dopracowania po testach na urządzeniu.

## Plusy i minusy opcji

- **On-Screen Controls:** idiomatyczne dla Input System, ale wymagają EventSystem i programowe
  ustawienie `controlPath` jest kruche — ryzykowne bez iteracji w edytorze.
- **Joystick na zdarzeniach UI:** elastyczny, lecz zależny od EventSystem/raycasterów i poprawnej
  hierarchii Canvas — większa powierzchnia błędu przy pisaniu bez testów.
