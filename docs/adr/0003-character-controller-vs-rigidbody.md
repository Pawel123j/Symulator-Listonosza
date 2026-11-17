# 0003 — Sterowanie ruchem gracza: CharacterController zamiast Rigidbody

* Status: zaakceptowane
* Data: 2026-06-18
* Decydenci: zespół Symulator Listonosza

## Kontekst i problem

Etap 2 wprowadza swobodny ruch listonosza (pieszo) po płaszczyźnie XZ. Ruch ma być płynny,
responsywny i przewidywalny, z obsługą kolizji ze statycznym otoczeniem (budynki, przeszkody)
oraz schodków/zboczy. Musimy zdecydować, na czym oprzeć ruch postaci.

## Czynniki decyzyjne

- **Responsywność i przewidywalność** — sterowanie 1:1 z wejściem, bez „ślizgania" i bezwładności.
- **Kolizje + chodzenie po podłożu** — kolizja ze statyczną geometrią, kroki, zbocza, grawitacja.
- **Prostota** — minimum tuningu (masa, drag, materiały fizyczne) w prototypie.
- **Determinizm** — brak jittera zależnego od kroku fizyki.
- **Rozszerzalność** — w przyszłości pojazd / ruch uliczny (Etap 6 — propozycje rozszerzeń).

## Rozważane opcje

1. **CharacterController** — wbudowana obsługa kolizji kapsuły, kroków i zboczy; ruch przez
   `Move()`, grawitacja sterowana ręcznie, brak reakcji na siły fizyki.
2. **Rigidbody (dynamiczny) + collider** — pełna fizyka: siły, pęd, reakcja na zderzenia,
   ruch w `FixedUpdate`.
3. **Transform raw (bez kolizji)** — bezpośrednia modyfikacja pozycji.

## Decyzja

Wybieramy **opcję 1 — CharacterController** dla gracza pieszego.

- Ruch realizowany w `Update` przez `CharacterController.Move()`, grawitacja dosuwana ręcznie.
- Obrót płynny w kierunku ruchu (`RotateTowards`).
- CharacterController daje od ręki kolizje, kroki i zbocza bez strojenia fizyki.

Przyszły **pojazd** (jeśli powstanie) dostanie **osobny** kontroler — tam Rigidbody i fizyka
są uzasadnione (pęd, poślizg). Decyzja dotyczy wyłącznie ruchu pieszego i nie blokuje tej drogi.

### Konsekwencje

**Pozytywne**
- Sterowanie 1:1 z wejściem, brak bezwładności i jittera — dobry feel od początku.
- Mniej parametrów do strojenia niż Rigidbody (brak masy, dragu, materiałów fizycznych).
- Wbudowana obsługa kroków/zboczy/`isGrounded`.
- Logika ruchu w `Update` — prostsza niż rozbicie na `FixedUpdate`/interpolację.

**Negatywne / ryzyka**
- CharacterController nie reaguje na siły fizyki (np. odrzut, popchnięcie) bez ręcznego kodu —
  na potrzeby prototypu nieistotne.
- Interakcje fizyczne z dynamicznymi obiektami wymagałyby dopisania logiki (np. `OnControllerColliderHit`).
- Potrzebne podłoże (kolider) pod graczem, by grawitacja go zatrzymała.

## Plusy i minusy opcji

- **Rigidbody:** realistyczna fizyka i reakcja na zderzenia, ale więcej strojenia, ryzyko
  „ślizgania"/jittera, ruch w `FixedUpdate` + interpolacja, nadmiarowe dla pieszego listonosza.
- **Transform raw:** najprostsze, lecz brak kolizji — gracz przenikałby budynki; nieakceptowalne.
