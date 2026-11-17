# 0001 — Architektura: ScriptableObject-driven + event channels

* Status: zaakceptowane
* Data: 2026-06-18
* Decydenci: zespół Symulator Listonosza

## Kontekst i problem

Budujemy prototyp gry rozwijany etapowo (ruch → dane → logika dostaw → UI → pętla dnia).
Systemy będą dochodzić przyrostowo i muszą się komunikować (np. UI reaguje na dostarczenie
przesyłki), ale nie chcemy, by każdy nowy system wymagał ręcznego „podpinania” referencji
do pozostałych. Potrzebujemy architektury, która:

- pozwala dodawać/wymieniać systemy bez modyfikacji istniejących (niski coupling),
- umożliwia balansowanie danych (przesyłki, adresy, configi) bez rekompilacji,
- jest czytelna dla małego zespołu i nie wprowadza ciężkiego frameworka DI.

## Czynniki decyzyjne

- **Luźne powiązania** — systemy nie powinny trzymać twardych referencji do siebie.
- **Iteracja na danych** — projektant ma móc tworzyć/edytować zawartość w edytorze.
- **Testowalność i podmienialność** — łatwo zamockować/zastąpić nadawcę lub odbiorcę zdarzenia.
- **Niski narzut** — bez zewnętrznych zależności i skomplikowanego setupu.
- **Idiomatyczność dla Unity** — rozwiązanie zgodne z dobrymi praktykami silnika.

## Rozważane opcje

1. **ScriptableObject-driven + event channels (SO)** — dane jako ScriptableObjecty,
   komunikacja przez SO pełniące rolę kanałów zdarzeń (raise/subscribe).
2. **Klasyczne C# eventy + statyczny EventBus** — singletonowy bus z `event`/`Action`.
3. **Zewnętrzny framework DI / mediator** (Zenject, MessagePipe itp.).
4. **Bezpośrednie referencje + singletony managerów** — systemy wołają się wprost.

## Decyzja

Wybieramy **opcję 1 — ScriptableObject-driven + event channels**.

- **Dane** (przesyłki, adresy, configi gridu, baza zadań na dzień) modelujemy jako
  `ScriptableObject` — projektant tworzy je jako assety i edytuje w Inspectorze.
- **Komunikacja między systemami** odbywa się przez **event channel SO**: asset będący
  kanałem (`OnDeliveryPickedUp`, `OnDeliveryCompleted`, `OnDeliveryFailed`), który nadawca
  „podnosi” (`Raise`), a odbiorcy subskrybują. Nadawca i odbiorca znają tylko kanał (asset),
  nie siebie nawzajem.
- Managery (`GameManager`, `DeliveryService`, `GameLoopManager`) pozostają cienkimi
  koordynatorami orkiestrującymi serwisy; logika rozłożona, nie w jednym „bogu-obiekcie”.

Inspiracja: wzorzec prezentowany przez Unite (Ryan Hipple, „Game Architecture with
ScriptableObjects”).

### Konsekwencje

**Pozytywne**
- UI i logika rozwijane niezależnie; UI subskrybuje kanały, nic nie wie o `DeliveryService`.
- Treść (zadania, configi) edytowalna bez kodu i rekompilacji.
- Łatwe dodawanie nowych reakcji na zdarzenie (kolejny subskrybent kanału).
- Brak zewnętrznych zależności.

**Negatywne / ryzyka**
- Więcej assetów do zarządzania (kanały, definicje) — wymaga dyscypliny w `Settings/Data`.
- Przepływ zdarzeń bywa mniej „widoczny” niż wywołanie metody — mitygacja: konwencje nazw
  (`On...`), logowanie w trybie debug, dokumentacja kanałów.
- Trzeba pamiętać o odsubskrybowaniu (`OnDisable`), by uniknąć wycieków/`MissingReference`.

## Plusy i minusy opcji

- **EventBus/statyczne eventy:** prosty, ale globalny stan statyczny, gorzej testowalny,
  brak edytowalności danych w Inspectorze.
- **Framework DI (Zenject itp.):** potężny, lecz duży narzut i krzywa uczenia dla prototypu;
  płatność/zależność zewnętrzna i nadmiarowość.
- **Bezpośrednie referencje + singletony:** najszybsze na start, ale silny coupling i
  „spaghetti” referencji rosnące z każdym etapem — sprzeczne z celami projektu.
