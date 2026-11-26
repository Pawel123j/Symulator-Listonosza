# Symulator Listonosza

[English](README.md) · **Polski**

Prototyp gry, w której gracz wciela się w listonosza dostarczającego przesyłki po mieście.
Ruch swobodny (free movement), grid wyłącznie jako warstwa logiczna (mapowanie świata → komórki).

## Stack (techniczny)

- **Unity 6 — `6000.3.14f1`**, URP 17.3.0 (wersja wykryta na maszynie; pierwotne założenie 2022.3 LTS
  okazało się nietrafione i zostało zmienione — patrz [ADR-0004](docs/adr/0004-mobile-input-touch-i-follow-camera.md))
- **Platforma docelowa: mobile (Android/iOS)** — sterowanie dotykowe, kamera podążająca
- C#, Input System (new), legacy `Text` w HUD (do migracji na TMP)
- Architektura: ScriptableObject-driven + manager pattern + event channels (SO) — bez sztywnych
  referencji między systemami
- Bez płatnych assetów — placeholdery (prymitywy / proste meshe)

## Struktura projektu

```
Assets/
  _Project/
    Scripts/
      Core/        // GameManager, event channels, serwisy        -> asmdef Listonosz.Core
      Gameplay/    // ruch gracza, dostawy, interakcje             -> asmdef Listonosz.Gameplay
      Data/        // ScriptableObjecty (przesyłki, adresy, configi)-> asmdef Listonosz.Data
      Grid/        // logiczna warstwa gridu (world<->cell)         -> asmdef Listonosz.Grid
      UI/          // HUD, lista zadań                              -> asmdef Listonosz.UI
    Settings/      // URP assety, Input Actions
    Prefabs/
    Scenes/        // Bootstrap.unity (Etap 0), Playground.unity (scena gry, wejście buildu)
docs/
  adr/             // Architecture Decision Records (MADR)
```

Namespacing: `Listonosz.<Warstwa>` (np. `Listonosz.Core`, `Listonosz.Gameplay`).

### Zależności assembly (asmdef)

```
Listonosz.Core      (bazowa, brak zależności wewnętrznych)
Listonosz.Data      -> Core
Listonosz.Grid      -> Core, Data
Listonosz.Gameplay  -> Core, Data, Grid
Listonosz.UI        -> Core, Data
```

Podział na asmdef wymusza kierunek zależności (czysta architektura) i skraca czas
rekompilacji.

## Konfiguracja środowiska (jednorazowo)

Repozytorium zawiera kod, ADR-y, scenę oraz `ProjectSettings/ProjectVersion.txt` i
`Packages/manifest.json` (pod Unity 6 `6000.3.14f1`). Pozostałe `ProjectSettings` i `Library`
wygeneruje edytor przy pierwszym otwarciu.

1. **Unity Hub → Open** → wskaż ten folder. Hub otworzy go w `6000.3.14f1`; edytor pobierze pakiety
   (URP, Input System) i wygeneruje `ProjectSettings`.
2. Gdy edytor zapyta o **włączenie nowego backendu Input System** — potwierdź (restart edytora).
   Bez tego sterowanie nie działa (choć kod i tak się kompiluje).
3. *(URP/render)* jeśli scena renderuje się jako Built-in: utwórz *URP Asset (with Renderer)* i
   przypisz w *Project Settings → Graphics* oraz w *Quality*.

### Build na mobile (skrót)
- *File → Build Profiles/Settings* → przełącz platformę na **Android** (lub iOS).
- *Project Settings → Player*: orientacja (np. Landscape), Company/Product Name, Min API.
- Sterowanie dotykowe działa po włączeniu nowego backendu Input System (krok 2).
- Test bez urządzenia: okno **Simulator** (Game view → Simulator) + dotyk myszą; równolegle
  działa klawiatura/pad, bo wejścia się sumują.

## Jak przetestować Etap 0

1. Otwórz scenę `Assets/_Project/Scenes/Bootstrap.unity`.
2. Naciśnij **Play**.
3. W *Console* powinien pojawić się log:
   `[Listonosz] GameManager zainicjalizowany (Etap 0 — szkielet).`

`GameManager` tworzy się automatycznie (`RuntimeInitializeOnLoadMethod`), więc log pojawi
się niezależnie od zawartości sceny. Scena Bootstrap jest celowo „pusta” — jej zadaniem
jest inicjalizacja systemów (kamera/rozgrywka dojdą w kolejnych etapach).

## Testy logiki pętli dnia (bez Unity)

`tools/LogicTests` kompiluje prawdziwe skrypty z `Assets/_Project/Scripts` (Core, Data, Grid,
pętla dnia i dostawy) z atrapą API Unity (`UnityStubs.cs`: cykl życia, `PlayerPrefs` w pamięci,
symulowane akcje wejścia).

### Najkrótsza droga od czystego checkoutu do pierwszego dowodu

| Krok | Wymaga | Wejście | Oczekiwany wynik |
|---|---|---|---|
| 1. Podstawowy (bez Unity) | git, **.NET SDK 8.0.x** (np. `apt install dotnet-sdk-8.0`, `winget install Microsoft.DotNet.SDK.8`); żadnych pakietów NuGet, po instalacji SDK działa offline | `dotnet run --project tools/LogicTests` | pierwsza sekcja `== A1 …` z `[ok]`, na końcu `PASSED: <n> checks`, kod wyjścia `0` (~5 s) |
| 2. Edytor | **Unity 6000.3.14f1** (`ProjectSettings/ProjectVersion.txt`), pakiety z `Packages/manifest.json` (m.in. Input System 1.14.1, URP 17.3.0), licencja Unity | scena `Bootstrap.unity` → Play | log `[Listonosz] GameManager zainicjalizowany (Etap 0 — szkielet).` |
| 3. Build/urządzenie | krok 2 + moduł Android (SDK/NDK, licencje), telefon | `docs/android-build.md` | `Builds/Listonosz.apk` |

Krok 1 uruchamia też CI (`.github/workflows/tests.yml`) przy każdym pushu i PR. Kroki 2–3 zależą
od licencji, GUI edytora i sprzętu i nie dają się wykonać w kontenerze bez Unity.

Kod wyjścia `0` = wszystkie asercje przeszły. Kontrakt objęty testami:
- dwa starty tej samej fikstury dają ten sam stan początkowy (`DayStart`, wynik/licznik/combo 0,
  pełny czas, pusta torba, jeden `GameManager`); **rekord (`BestScore`) celowo przetrwa** między startami;
- z `DayEnd` jedno „Dalej” przechodzi do `DayStart` nowego dnia (bez pomijania briefingu, chyba że
  `autoStartWork`), zeruje licznik/wynik/combo/czas i udostępnia ponownie te same przesyłki;
- przesyłka jest powiązana z adresem przez **referencję do assetu `AddressPointSO`**, nie przez tekst:
  podobne lub identyczne `DisplayName`/`AddressId` dwóch adresów nie powodują pomyłki dostawy;
- `DeliveryDatabaseSO.Count` (cel dnia przy `dailyGoal = 0`) liczy unikalne, niepuste wpisy — pusta
  pozycja listy ani powtórzona przesyłka nie blokują ukończenia dnia. Przesyłka bez adresu nadal jest
  liczona, ale nie da się jej dostarczyć (na koniec dnia trafia do nieudanych);
- powrót do punktu odbioru wydaje tylko przesyłki jeszcze nie dostarczone i nie niesione (do limitu
  torby `maxItems`); dostarczona przesyłka nie wraca tego samego dnia;
- poprawny punkt: jedno confetti + jeden dźwięk (`DeliveryFeedback`) nad komórką adresu przesyłki;
  błędny punkt: brak efektów i zdarzeń, torba i wynik bez zmian — jedyną informacją jest log w Console
  (gracz na urządzeniu nie dostaje komunikatu);
- kanały `EventChannelSO` (assety) przeżywają scenę: każdy słuchacz zapisuje się w `OnEnable` i wypisuje
  w `OnDisable`, więc po ponownym wejściu do sceny liczba subskrypcji jest taka sama, a jedno zdarzenie
  daje jedną reakcję;
- scena wejścia buildu: pierwsza włączona scena w `ProjectSettings/EditorBuildSettings.asset` to
  `Playground.unity` i jest to ta sama scena, którą buduje `MobileBuild.BuildAndroid` (stała `ScenePath`);
  GUID w manifeście zgadza się z `.meta` sceny. `Bootstrap.unity` służy tylko do testu Etapu 0 w edytorze.

Ograniczenie: to nie zastępuje Play Mode ani testu na urządzeniu (brak renderowania, fizyki, dotyku).

## Decyzje architektoniczne

Patrz [docs/adr](docs/adr/README.md).

## Licencja

[MIT](LICENSE)
