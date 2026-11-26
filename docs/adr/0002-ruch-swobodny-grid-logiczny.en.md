# 0002 — Free movement + grid as a purely logical layer

**English** · [Polski](0002-ruch-swobodny-grid-logiczny.md)

* Status: accepted
* Date: 2026-06-18
* Deciders: Symulator Listonosza team

## Context and problem

The game needs "addresses" and delivery locations in the city space. The natural logical
representation is a grid of cells — address = cell, occupancy checks, mapping of delivery targets.
At the same time we want controlling the postman to feel **smooth and satisfying**, not jumping
"one tile at a time". We have to decide whether movement is snapped to the grid (tile-based) or
free, with the grid playing a purely logical role.

## Decision drivers

- **Control feel** — free movement in 3D/2.5D feels better than snapping.
- **Simple delivery/address logic** — a grid simplifies addressing and spatial queries.
- **Extensibility** — a vehicle, traffic and collision physics in the future.
- **Layer separation** — presentation (movement in the world) independent of logic (cells).

## Considered options

1. **Free movement + logical grid** — the player moves smoothly (Rigidbody/CharacterController)
   and `GridSystem` maps a world position to a cell (`WorldToCell`/`CellToWorld`) only for the
   logic (addresses, targets, occupancy). Movement is not snapped.
2. **Tile-based (snapping to cells)** — the player moves by whole cells; the position always
   equals the centre of a tile.
3. **No grid** — full freedom, addresses as plain world positions / colliders.

## Decision

We choose **option 1 — free movement + grid as a purely logical layer**.

- The player moves smoothly in world space; **the grid does not affect movement**.
- `GridSystem` (stage 1) provides the conversions `WorldToCell(Vector3) -> Vector2Int` and
  `CellToWorld(Vector2Int) -> Vector3`, configured by `GridConfig` (ScriptableObject).
- Delivery logic (target address, "am I in the right cell/point", occupancy) works on cells;
  the presentation (player transform) stays continuous.

### Consequences

**Positive**
- Good control feel from the start, independent of the logical grid resolution.
- Simple, deterministic addressing logic (cell = key).
- Clean separation: cell size/origin can change without touching movement code.
- The grid can be visualised with gizmos for debugging at no cost in the build.

**Negative / risks**
- An unambiguous world↔cell mapping convention is required (origin, Y axis) — defined
  centrally in `GridConfig` to avoid drift between systems.
- "Being in the target cell" needs a tolerance/interaction radius, not just equal cells
  (settled in stage 4 — an interaction zone around `DeliveryTarget`).

## Pros and cons of the options

- **Tile-based:** simplest logic and no ambiguity of position, but a jumpy, "board game" feel —
  not fitting a simulator of moving around a city; harder to add a vehicle and smooth physics later.
- **No grid:** maximum freedom, but addressing and spatial queries become harder and less
  deterministic (more code for nearest-target queries, no natural address key).
