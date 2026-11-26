# 0001 — Architecture: ScriptableObject-driven + event channels

**English** · [Polski](0001-architektura-so-driven-event-channels.md)

* Status: accepted
* Date: 2026-06-18
* Deciders: Symulator Listonosza team

## Context and problem

We are building a game prototype developed in stages (movement → data → delivery logic → UI →
day loop). Systems are added incrementally and have to communicate (e.g. the UI reacts when a
parcel is delivered), but we do not want every new system to require manually wiring references
to the others. We need an architecture that:

- lets us add or replace systems without modifying existing ones (low coupling),
- allows balancing data (parcels, addresses, configs) without recompiling,
- is readable for a small team and does not introduce a heavy DI framework.

## Decision drivers

- **Loose coupling** — systems should not hold hard references to each other.
- **Data iteration** — a designer must be able to create and edit content in the editor.
- **Testability and replaceability** — the sender or receiver of an event is easy to mock or replace.
- **Low overhead** — no external dependencies or complicated setup.
- **Idiomatic for Unity** — a solution in line with the engine's good practices.

## Considered options

1. **ScriptableObject-driven + event channels (SO)** — data as ScriptableObjects, communication
   through SOs acting as event channels (raise/subscribe).
2. **Plain C# events + a static EventBus** — a singleton bus with `event`/`Action`.
3. **External DI framework / mediator** (Zenject, MessagePipe, etc.).
4. **Direct references + manager singletons** — systems call each other directly.

## Decision

We choose **option 1 — ScriptableObject-driven + event channels**.

- **Data** (parcels, addresses, grid configs, the day's task database) is modelled as
  `ScriptableObject` — the designer creates them as assets and edits them in the Inspector.
- **Communication between systems** goes through **event channel SOs**: an asset acting as a
  channel (`OnDeliveryPickedUp`, `OnDeliveryCompleted`, `OnDeliveryFailed`) that the sender
  raises (`Raise`) and receivers subscribe to. Sender and receiver know only the channel (asset),
  not each other.
- Managers (`GameManager`, `DeliveryService`, `GameLoopManager`) stay thin coordinators
  orchestrating services; the logic is distributed rather than placed in one "god object".

Inspiration: the pattern presented at Unite (Ryan Hipple, "Game Architecture with
ScriptableObjects").

### Consequences

**Positive**
- UI and logic evolve independently; the UI subscribes to channels and knows nothing about `DeliveryService`.
- Content (tasks, configs) is editable without code or recompilation.
- New reactions to an event are easy to add (one more channel subscriber).
- No external dependencies.

**Negative / risks**
- More assets to manage (channels, definitions) — requires discipline in `Settings/Data`.
- The flow of events can be less "visible" than a method call — mitigation: naming conventions
  (`On...`), logging in debug mode, documented channels.
- Unsubscribing (`OnDisable`) must not be forgotten, to avoid leaks / `MissingReference`.

## Pros and cons of the options

- **EventBus / static events:** simple, but global static state, harder to test,
  no data editing in the Inspector.
- **DI framework (Zenject, etc.):** powerful, but large overhead and learning curve for a prototype;
  an external dependency and more than needed.
- **Direct references + singletons:** fastest to start with, but strong coupling and a growing
  "spaghetti" of references with every stage — contrary to the project's goals.
