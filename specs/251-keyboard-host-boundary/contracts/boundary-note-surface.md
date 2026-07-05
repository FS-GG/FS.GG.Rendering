# Contract: Keyboard-Only Boundary Note Surface

**Feature**: 251-keyboard-host-boundary | **Date**: 2026-07-05

This feature ships **no code contract** (no new type/function/`.fsi`). Its "contract" is the **required content of the
surfaced note** and the **assertions** that keep it present and accurate. Two surfaces carry the note; both must
satisfy the content contract, and the generated-product test enforces presence + accuracy.

## Surface A — game-starter `Model.fs` input-wiring comment (authoritative, FR-001/FR-002)

**Location**: `template/base/src/Product/Model.fs`, `profile == "game"` branch, at the input-wiring site — adjacent to
`paddleForKey` (the `ViewerKey -> command` mapping) and/or the `ViewerInput of ViewerKey * isDown` handler.

**Required content** (the comment MUST state all of):

1. The default game host (`Viewer.runApp` / `GeneratedAppHost`) delivers **keyboard input only**.
2. `ViewerKey` has **no mouse/pointer case** (input arrives as `DispatchInput of ViewerKey * isDown`).
3. A **mouse-aimed** control scheme requires the **pointer-aware interactive host** path
   (`InteractiveAppHost` / `Controls.Elmish.runInteractiveApp`, `MapPointer`) — a **different, non-default** host
   wiring, not an edit at this site.

**Constraints**:
- Comment only — no change to `paddleForKey`, the `ViewerInput` handler, or any logic.
- Inside the `profile == "game"` branch only — `app`/`governed`/`headless-scene` branches stay byte-identical.

## Surface B — keyboard-input product skill + fragment mirror (FR-003/FR-004)

**Locations**:
- `template/product-skills/fs-gg-keyboard-input/SKILL.md` — a "Capability boundary" note.
- `template/fragments/keyboard-input/README.md` — the same note (fragment source parity).

**Required content** (both MUST state):
- The game family's default persistent host is keyboard-only: `MapKey` / `ViewerKey`, **no** `MapPointer`.
- Mouse-aimed input requires the pointer-aware interactive host (`InteractiveAppHost` / `runInteractiveApp`,
  `MapPointer`) rather than the default `runApp` path.

**Constraint**: The two texts convey the same boundary (parity), so materialized skill and fragment source do not
drift.

## Assertions (generated-product test — `tests/Product.Tests/BehaviorTests.fs`)

| # | Assertion | Guards |
|---|---|---|
| A1 | The game-starter input-wiring site carries the boundary note (keyboard-only default host + pointer-aware alternative named). | US1 / FR-001 / FR-002 — **present** |
| A2 | The emitted `ViewerKey` exposes **no** mouse/pointer case (only the keyboard cases). | FR-006 — **accurate** |
| A3 | The emitted default host (`GeneratedAppHost`) exposes `MapKey` but **no** `MapPointer`; `InteractiveAppHost` exposes `MapPointer`. | FR-006 — **accurate** (signpost is real) |
| A4 | A fresh game scaffold builds clean and behavior/governance tests pass with no author edits. | FR-005 — **behavior unchanged** |
| A5 | No durable/governance-scanned host file changed; non-game profiles byte-identical. | FR-005 / FR-007 — **non-regressive** |

**Failure semantics**: A2/A3 fail if a future change alters the host contract without updating the note, forcing the
surfaced text back into accuracy (the note cannot rot silently). A1 fails if the comment is removed from the shipped
starter. A4/A5 fail if the surfacing accidentally touches behavior or durable wiring.

## Out of scope (explicitly not this contract)

- Adding a mouse/pointer seam to the default host.
- Rewiring the game starter onto `InteractiveAppHost`.
- Any change to `Program.fs` or governance-scanned durable files.
