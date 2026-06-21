# Contract: US6 — Tame the `runInteractiveAppWithLauncher` god-function

**Target**: `src/Controls.Elmish/ControlsElmish.fs` (2,227 lines; `runInteractiveAppWithLauncher`
@1186 ~500 lines, ~20 `ref` cells of ad-hoc frame state, ~15 nested closures). **Package**:
`FS.GG.UI.Controls.Elmish`. Inherits all of [surface-invariance.md](./surface-invariance.md).
`FrameLoopState` shape: see [../data-model.md](../data-model.md).

## C-FS-1 — `FrameLoopState` record + module functions (FR-007)

The ~20 `ref` cells (`pointerState`, `focused`, `retained`, `lastRender`, `lastView`,
`lastRuntimeModel`, `scrollOffsets`, `surfacedDiagnostics`, `pendingMove`, `pointerSampleCount`,
`lastWorkReduction`, `lastPresentTiming`, …) are promoted to a `FrameLoopState` record with
module-level functions, so the frame loop reads as typed state transitions instead of an untyped
mutable object. `FrameLoopState` is **internal** — absent from `ControlsElmish.fsi` and from
`FS.GG.UI.Controls.Elmish.txt`.

## C-FS-2 — Elmish boundary preserved (Constitution IV)

`FrameLoopState` is **interpreter-edge** frame state, NOT the Elmish `Model`. `update` stays pure, I/O
stays at the edge, and `Model`/`Msg`/`Cmd` contracts are unchanged. The promotion strengthens the
boundary; it does not cross it.

## C-FS-3 — Mutation retained per-frame (Constitution III)

Mutation MAY be retained where it is simpler/faster on the per-frame path, disclosed with a one-line
`// mutable: hot path / per frame` comment (matching the existing convention at the `ref` sites).

## C-FS-4 — Byte-stable frame loop (FR-003)

Frame-loop transitions, emitted commands, and render-lag traces match baseline for every covered
scenario.

## Acceptance (maps to spec US6)

1. `FrameLoopState` record: frame-loop transitions, emitted commands, render-lag traces match baseline
   (C-FS-1, C-FS-4).
2. Built package: `.fsi` + surface baseline byte-identical (C-SI-1/2).

## Validation

Build `FS.GG.UI.Controls.Elmish`; run its tests + any frame-loop / render-lag trace suite; byte-diff
traces vs baseline; `scripts/refresh-surface-baselines.fsx` → empty `FS.GG.UI.Controls.Elmish.txt` diff
(quickstart Step 1, row US6).

## Implementation Outcome (2026-06-21) — DONE (byte-stable, validated)

The 12 `ref` cells in `runInteractiveAppWithLauncher` (`ControlsElmish.fs:1192`–1246) were promoted to a
single internal `type private FrameLoopState<'model, 'msg>` record (per-field docs carried onto the
type), with all **69** accesses converted `<ref>.Value` → `loopState.<Field>`. A `ref` cell and a
`mutable` record field are the **same heap-mutable-cell semantics**, so the conversion is byte-identical
**by construction** (C-FS-1/3/4); `update` stays pure, I/O at the edge (C-FS-2). `FrameLoopState` is
`type private` — absent from `ControlsElmish.fsi` and the public surface.

- Oracle 1 ✓ `FS.GG.UI.Controls.Elmish.txt` byte-identical.
- Oracle 2 ✓ `Elmish.Tests` **Release** 209/209 = baseline parity.
- Oracle 3 ✓ `controls-elmish-prelude` output byte-clean vs baseline.
