# Quickstart: Confirm the Boundary, Verify the Surfacing

**Feature**: 251-keyboard-host-boundary | **Date**: 2026-07-05

Two runnable scenarios. **Scenario 1** confirms the boundary is real on the current template (the standing-assumption
check — inspect the actual emitted host contract). **Scenario 2** verifies the surfacing is present, accurate, and
non-regressive after the edits land.

## Prerequisites

- .NET `net10.0` SDK; repo built from `template/base` per the standard template dev loop.
- The local template feed refreshed (`scripts/refresh-local-feed-and-samples.fsx`) when exercising a real scaffold, as
  in sibling features 246–250.

## Scenario 1 — Confirm the keyboard-only boundary is real (Foundational, before finalizing wording)

Goal: verify, against the shipped surface, that the default game host is keyboard-only and the pointer path is the
interactive host — so the surfaced text is accurate (FR-006), not asserted from memory.

1. Read the emitted keyboard surface — confirm `ViewerKey` has **no** mouse/pointer case:
   - `template/base/docs/api-surface/KeyboardInput/KeyboardInput.fsi` → `type ViewerKey` (keyboard cases only).
2. Read the emitted viewer surface — confirm the two hosts differ:
   - `template/base/docs/api-surface/SkiaViewer/SkiaViewer.fsi` →
     - `DispatchInput of ViewerKey * isDown` (keyboard-shaped host input),
     - `GeneratedAppHost … MapKey: ViewerKey -> bool -> 'msg option` with **no** `MapPointer`,
     - `InteractiveAppHost … MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list`,
     - `val runApp …` (the default game host entry).
3. Confirm the game starter launches the keyboard path — `template/base/src/Product/Program.fs` uses `Viewer.runApp`.

**Expected**: `ViewerKey` has no mouse case; `GeneratedAppHost` has `MapKey` and no `MapPointer`; `InteractiveAppHost`
has `MapPointer`; the game host is `runApp`. If any of these is now false, **stop and update the note wording** before
proceeding — the boundary changed.

## Scenario 2 — Verify the surfacing (after edits)

Goal: the note is present at the authoring site, accurate to the contract, and changes no behavior.

1. **Present at the edit site** — `template/base/src/Product/Model.fs` (`profile == "game"` branch) carries a comment
   at the `paddleForKey` / `ViewerInput` site stating: default host keyboard-only, `ViewerKey` has no mouse case,
   mouse-aim needs `InteractiveAppHost`/`runInteractiveApp` (`MapPointer`).
2. **Present in the guidance** — `template/product-skills/fs-gg-keyboard-input/SKILL.md` and
   `template/fragments/keyboard-input/README.md` both carry the capability-boundary note (parity).
3. **Accurate + non-regressive** — run the generated-product test asserting A1–A5 (see
   [contracts/boundary-note-surface.md](./contracts/boundary-note-surface.md)):

   ```sh
   # scaffold a game product from the local feed, then:
   dotnet build            # clean build, no author edits (A4)
   dotnet test             # BehaviorTests A1–A3 present+accurate; GovernanceTests unchanged (A5)
   ```

4. **Non-game unchanged** — confirm `app`/`governed`/`headless-scene` scaffolds are byte-identical (the edit is inside
   the `profile == "game"` branch only) and no durable/governance file changed.

**Expected**: game scaffold builds clean; A1–A5 pass; non-game profiles byte-identical; durable spine, evidence
tokens, and a starter swap all still pass.

## Done when

- Scenario 1 confirms the boundary on the current template (or the wording is corrected to match a changed contract).
- Scenario 2 shows the note present + accurate on both surfaces, a clean game build, passing behavior/governance
  tests, and no non-game/durable regression.
