# 0002 — Ruch swobodny + grid jako warstwa wyłącznie logiczna

[English](0002-ruch-swobodny-grid-logiczny.en.md) · **Polski**

* Status: zaakceptowane
* Data: 2026-06-18
* Decydenci: zespół Symulator Listonosza

## Kontekst i problem

Gra wymaga „adresów” i lokalizacji dostaw w przestrzeni miasta. Naturalną reprezentacją
logiczną jest siatka (grid) komórek — adres = komórka, sprawdzanie zajętości (occupancy),
mapowanie celów dostaw. Jednocześnie chcemy, by sterowanie listonoszem było **płynne i
satysfakcjonujące**, a nie skokowe „o jedno pole”. Trzeba rozstrzygnąć, czy ruch ma być
przypięty do gridu (tile-based), czy swobodny, a grid pełni jedynie rolę logiczną.

## Czynniki decyzyjne

- **Feel sterowania** — swobodny ruch w 3D/2.5D daje lepsze odczucie niż snapowanie.
- **Prostota logiki dostaw/adresów** — grid upraszcza adresowanie i zapytania przestrzenne.
- **Rozszerzalność** — w przyszłości pojazd, ruch uliczny, fizyka kolizji.
- **Rozdzielenie warstw** — prezentacja (ruch w świecie) niezależna od logiki (komórki).

## Rozważane opcje

1. **Ruch swobodny + grid logiczny** — gracz porusza się płynnie (Rigidbody/CharacterController),
   a `GridSystem` mapuje pozycję świata na komórkę (`WorldToCell`/`CellToWorld`) tylko na
   potrzeby logiki (adresy, cele, occupancy). Ruch nie jest snapowany.
2. **Tile-based (snapowanie do komórek)** — gracz przesuwa się o całe komórki; pozycja zawsze
   równa środkowi pola.
3. **Brak gridu** — pełna swoboda, adresy jako czyste pozycje świata / collidery.

## Decyzja

Wybieramy **opcję 1 — ruch swobodny + grid jako warstwa wyłącznie logiczna**.

- Gracz porusza się płynnie w przestrzeni świata; **grid nie wpływa na ruch**.
- `GridSystem` (Etap 1) udostępnia konwersje `WorldToCell(Vector3) -> Vector2Int` oraz
  `CellToWorld(Vector2Int) -> Vector3`, konfigurowane przez `GridConfig` (ScriptableObject).
- Logika dostaw (adres docelowy, „czy jestem we właściwej komórce/punkcie”, occupancy)
  operuje na komórkach; prezentacja (transform gracza) pozostaje ciągła.

### Konsekwencje

**Pozytywne**
- Dobry feel sterowania od początku, niezależny od rozdzielczości logicznej siatki.
- Logika adresowania prosta i deterministyczna (komórka = klucz).
- Czysty rozdział: można zmienić rozmiar komórki/origin bez dotykania kodu ruchu.
- Grid można wizualizować gizmosami do debugowania bez kosztu w buildzie.

**Negatywne / ryzyka**
- Konieczna jednoznaczna konwencja mapowania świat↔komórka (origin, oś Y) — definiowana
  centralnie w `GridConfig`, by uniknąć rozjazdu między systemami.
- „Bycie w komórce celu” wymaga tolerancji/promienia interakcji, nie tylko równości komórek
  (rozstrzygane w Etapie 4 — strefa interakcji wokół `DeliveryTarget`).

## Plusy i minusy opcji

- **Tile-based:** najprostsza logika i brak niejednoznaczności pozycji, ale skokowy,
  „planszowy” feel — niepasujący do symulatora poruszania się po mieście; trudniejsze
  późniejsze dodanie pojazdu i płynnej fizyki.
- **Brak gridu:** maksymalna swoboda, lecz adresowanie i zapytania przestrzenne stają się
  trudniejsze i mniej deterministyczne (więcej kodu na zapytania o najbliższe cele,
  brak naturalnego klucza adresu).
