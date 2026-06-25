# Phase 0 Research — Symbology Live Board Sample (M6)

All NEEDS CLARIFICATION items from Technical Context resolved below. Each decision is grounded against the tree on 2026-06-25 (CanvasDemo precedent, the `FS.GG.UI.Symbology`/`Canvas`/`Scene` public surfaces).

## D1 — Sample shape and project wiring

- **Decision**: A new `samples/SymbologyBoard/` executable (`OutputType=Exe`, `IsPackable=false`), three source files (`Roster.fs`, `Board.fs`, `Program.fs`), in-tree `ProjectReference`s, registered in `FS.GG.Rendering.slnx` under the `/samples/` folder. It is a byte-for-byte structural twin of `samples/CanvasDemo`.
- **Rationale**: CanvasDemo is the shipped, accepted precedent for "pure fixed-timestep sim + volatile canvas + default headless evidence subcommand + graceful interactive fallback" (spec 191). Reusing its shape minimizes risk and satisfies US3 (parity with the existing sample set). `IsPackable=false` keeps the sample off the package surface (FR-012/SC-005).
- **Alternatives considered**: (a) Fold the board into CanvasDemo — rejected: conflates two milestones and two rosters in one sample. (b) A new library + sample pair — rejected: M6 demonstrates an existing grammar; no new reusable surface is in scope (that is M7). (c) An `.fsx` script instead of a project — rejected: US3 requires a *registered, building* solution project with discoverable subcommands.

## D2 — Reusing the approved M5 mapping

- **Decision**: Bring the approved mapping from `specs/192-agent-unit-symbology/readiness/dry-run/FinalSymbolSet.fsx` in-tree as a compiled `Roster.fs` — the same `UnitStats` record and `factionOf`/`klassOf`/`sigilOf`/`mapUnit` logic, plus a fixed roster literal (the 6–10 unit set). It calls `Symbology.defaultToken`/channel DUs through the existing public surface; the grammar is not re-opened.
- **Rationale**: The `.fsx` is a script (`#r` DLL references) and cannot be compiled into a project as-is. Porting the *logic* verbatim preserves the approved channel assignment (FR-001) while making it a first-class, buildable artifact. The mapping is data/pure — no behavior change to the grammar.
- **Alternatives considered**: (a) `#load` the `.fsx` from the project — rejected: `.fsx` is not compilable into an Exe and would couple the sample to a spec-192 readiness path. (b) Re-derive a fresh mapping — rejected: the spec forbids re-opening the M5 convergence (Assumptions). (c) Read the roster from a data file at runtime — rejected: adds IO to the deterministic path and a parsing surface for no benefit; a literal is simplest and deterministic.

## D3 — Motion source and per-symbol motion channel

- **Decision**: Each unit's motion is the existing `Symbology.animate motion token phase` overlay. `phase` is the simulation's accumulated world time `t` (sum of fixed `dt` steps), never a wall clock. A pure `motionOf : UnitStats -> Token -> Motion` picks the overlay from the already-approved channels — documented mapping: `Suspected` state → `Blink`; high `Threat` → `Pulse`; otherwise `Moving` (mobile/scout) / `Spin` (heavy). The "moving board" aspect is a deterministic per-unit position drift advanced by the same fixed step.
- **Rationale**: FR-002/FR-003 require continuous motion derived solely from accumulated fixed-timestep steps. `animate` already takes a caller-owned `phase:float` with no wall-clock (spec 192 FR-007/FR-009), so feeding it `t` is exact and replayable. Deriving `Motion` from existing channels (state/threat/role) keeps the grammar and the approved mapping unchanged.
- **Alternatives considered**: (a) Wall-clock / `DateTime.Now` phase — rejected outright (breaks determinism, the hard constraint). (b) Render-time randomness for "liveliness" — rejected (non-reproducible). (c) Add a new motion channel to the grammar — rejected (M7 scope; FR-012 forbids surface change).

## D4 — Board layout and boundary policy

- **Decision**: Lay the roster out on a fixed-size board (e.g. 480×320). Each unit has a deterministic position and velocity seeded from `seed` + its roster index; positions advance by `velocity * dt` each fixed step and **bounce** (negate the component) when a unit's symbol radius reaches a board edge, keeping every symbol fully on-board. Rendering interpolates `Previous`→`Current` positions with `Loop.alpha`, as CanvasDemo does.
- **Rationale**: FR-011/SC-003 require every symbol to stay legible and on-board across the run. Bounce is the simplest policy that never clips a unit, is trivially deterministic, and reads as "live motion". The spec's edge-case list explicitly allows "bounce, wrap, or clamp — chosen and documented"; bounce is chosen.
- **Alternatives considered**: (a) Wrap-around — rejected: a symbol straddling an edge looks clipped/teleporting and hurts legibility. (b) Clamp (stop at edge) — rejected: units pile on edges and stop moving, undercutting the "moving board" demonstration. (c) Static `Symbology.gallery` grid with only in-place `animate` — rejected: US1 explicitly wants the board *moving*, not a still grid; gallery is the M5 still-board path.

## D5 — Evidence fingerprint and reproducibility check

- **Decision**: Reuse the CanvasDemo evidence shape exactly: `evidence seed script` folds the scripted `Msg` sequence from `init seed` and returns `SceneCodec.packageIdentity (SceneCodec.export (renderScene final)).CanonicalBytes`. The default/`evidence` subcommand runs it twice from the same seed, prints the fingerprint, and exits `0` on a byte-identical match, non-zero with a diff-style message on divergence (FR-005). A separate documented invocation from a different seed demonstrates divergence (FR-006/SC-002).
- **Rationale**: `SceneCodec.export(...).CanonicalBytes` is the spec-192 determinism identity (equal `Scene` ⇒ equal canonical bytes), already used by CanvasDemo. It needs no wall clock or GPU and is exact in headless CI.
- **Alternatives considered**: (a) PNG pixel hash via `Symbology.Render.toPng` — rejected: rasterization is heavier, can vary across Skia builds, and the canonical-bytes path is the established, byte-stable identity (see memory: canonical-bytes vs coarse readback hash). (b) A multi-frame filmstrip fingerprint — rejected: a single final-board fingerprint is the spec's "single canonical board fingerprint" (FR-004); multi-frame is unnecessary for the milestone exit (could be a follow-up).

## D6 — Test reachability of the sample's deterministic core

- **Decision**: `tests/SymbologyBoard.Tests/` adds a `ProjectReference` to `samples/SymbologyBoard/SymbologyBoard.fsproj` and calls into its `Board`/`Roster` modules directly (F#/.NET permits referencing an `OutputType=Exe` project's public modules). Tests assert: same-seed reproducibility, different-seed divergence, the on-board/non-zero-area invariant sampled across N steps, and a non-empty board for a single-unit/degenerate roster.
- **Rationale**: Principle I/V require automated fail-before/pass-after evidence for behavior-changing code; the deterministic core is the behavior. Referencing the Exe avoids extracting the core into a separate library (which would add package surface — FR-012). The `[<EntryPoint>]` in `Program.fs` does not interfere with referencing `Board`/`Roster`.
- **Alternatives considered**: (a) No test project, rely only on the sample's self-check — rejected: the self-check is in-process and not fail-before/pass-after evidence; US2 is P1 and explicitly evidence-grade. (b) Extract the core into a shared library — rejected: adds public surface and a new package for a sample, contradicting FR-012 and the M6 "demonstrate, don't extend" scope. **Confirmation task**: the Foundational smoke run builds the test project against the Exe to confirm reachability in this checkout before US work proceeds; fallback if the Exe reference is rejected by the toolchain = an FSI script under readiness/ that `#r`s the sample DLL and runs the same assertions (disclosed as the evidence path).

## D7 — Interactive fallback and host wiring

- **Decision**: Mirror CanvasDemo's `runInteractive`: query `Viewer.runtimeCapability()`, and if `not PersistentWindow`, print `"symbology-board: interactive mode skipped — no live window/GL host."` and return `0`; otherwise launch `ControlsElmish.runInteractiveApp` with a `ViewerOptions` (title, board size, `FrameRateCap = Some 60`) and an `InteractiveAppHost` whose `Tick = fun _ -> Some (Tick dt)`.
- **Rationale**: FR-007/SC-004 require graceful headless degradation (notice + zero exit, never block/crash). `runtimeCapability().PersistentWindow` is the established gate (CanvasDemo). The host MVU boundary satisfies Constitution IV.
- **Alternatives considered**: (a) Try/catch around a blind window launch — rejected: blocks or crashes on headless hosts; the capability probe is the precedent. (b) A custom render loop outside `runInteractiveApp` — rejected: re-implements the viewer host for no benefit and bypasses the accepted MVU edge.

## D8 — Optional raw input (FR-014)

- **Decision**: Out of scope for the default build of this milestone. If added later, any pointer/key influence on the board MUST be reconstructed deterministically (CanvasDemo's `PaddleTarget` held-input pattern) and MUST NOT feed the `evidence` path. The milestone exit does not depend on it.
- **Rationale**: FR-014 marks raw input as optional and forbids it affecting the deterministic evidence. Omitting it keeps the first cut minimal and the evidence path clean; the pattern is already proven in CanvasDemo if a follow-up wants it.
- **Alternatives considered**: Include interactive drag/select now — deferred: pure demonstrative extra, not required for US1/US2/US3 or any SC.

## Resolved unknowns summary

| Unknown (Technical Context) | Resolution |
|---|---|
| Sample shape / wiring | D1 — CanvasDemo twin, `samples/SymbologyBoard/`, in-tree refs, slnx-registered |
| How the approved set is reused | D2 — port `FinalSymbolSet.fsx` logic into compiled `Roster.fs` + roster literal |
| Motion source | D3 — `Symbology.animate` with accumulated-step `phase`; `motionOf` from approved channels |
| Layout + boundary policy | D4 — fixed board, seeded drift, **bounce** at edges, `Loop.alpha` interpolation |
| Evidence fingerprint | D5 — `SceneCodec.packageIdentity` of canonical bytes; two same-seed runs match, diff-seed diverges |
| Test reachability | D6 — test project `ProjectReference`s the Exe; FSI fallback if rejected |
| Interactive fallback | D7 — `Viewer.runtimeCapability().PersistentWindow` gate + `runInteractiveApp` host |
| Optional raw input | D8 — deferred; if added, deterministic reconstruction, never affects evidence |
