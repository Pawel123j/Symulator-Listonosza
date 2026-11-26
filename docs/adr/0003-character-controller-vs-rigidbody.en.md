# 0003 — Player movement: CharacterController instead of Rigidbody

**English** · [Polski](0003-character-controller-vs-rigidbody.md)

* Status: accepted
* Date: 2026-06-18
* Deciders: Symulator Listonosza team

## Context and problem

Stage 2 introduces free movement of the postman (on foot) on the XZ plane. Movement must be
smooth, responsive and predictable, with collisions against static surroundings (buildings,
obstacles) and support for steps/slopes. We have to decide what the character's movement is built on.

## Decision drivers

- **Responsiveness and predictability** — 1:1 control from input, no sliding or inertia.
- **Collisions + walking on ground** — collision with static geometry, steps, slopes, gravity.
- **Simplicity** — minimal tuning (mass, drag, physics materials) in a prototype.
- **Determinism** — no jitter depending on the physics step.
- **Extensibility** — a vehicle / traffic in the future (stage 6 — extension proposals).

## Considered options

1. **CharacterController** — built-in capsule collision, steps and slopes; movement through
   `Move()`, gravity handled manually, no reaction to physics forces.
2. **Rigidbody (dynamic) + collider** — full physics: forces, momentum, collision response,
   movement in `FixedUpdate`.
3. **Raw transform (no collisions)** — modifying the position directly.

## Decision

We choose **option 1 — CharacterController** for the player on foot.

- Movement runs in `Update` through `CharacterController.Move()`, with gravity added manually.
- Smooth rotation towards the movement direction (`RotateTowards`).
- CharacterController provides collisions, steps and slopes out of the box without physics tuning.

A future **vehicle** (if it is built) will get a **separate** controller — there Rigidbody and
physics are justified (momentum, skidding). This decision only covers walking and does not block that path.

### Consequences

**Positive**
- 1:1 control from input, no inertia or jitter — good feel from the start.
- Fewer parameters to tune than Rigidbody (no mass, drag, physics materials).
- Built-in steps/slopes/`isGrounded`.
- Movement logic in `Update` — simpler than splitting into `FixedUpdate`/interpolation.

**Negative / risks**
- CharacterController does not react to physics forces (e.g. knockback, pushing) without manual
  code — irrelevant for the prototype.
- Physical interactions with dynamic objects would need extra logic (e.g. `OnControllerColliderHit`).
- Ground (a collider) is needed under the player so that gravity stops it.

## Pros and cons of the options

- **Rigidbody:** realistic physics and collision response, but more tuning, risk of sliding/jitter,
  movement in `FixedUpdate` + interpolation; more than a walking postman needs.
- **Raw transform:** simplest, but no collisions — the player would walk through buildings; unacceptable.
