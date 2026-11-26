# Symulator Listonosza

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
    Scenes/        // Bootstrap.unity
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
symulowane akcje wejścia). Wymaga .NET SDK 8:

```
dotnet run --project tools/LogicTests
```

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
  (gracz na urządzeniu nie dostaje komunikatu).

Ograniczenie: to nie zastępuje Play Mode ani testu na urządzeniu (brak renderowania, fizyki, dotyku).

## Decyzje architektoniczne

Patrz [docs/adr](docs/adr/README.md).
