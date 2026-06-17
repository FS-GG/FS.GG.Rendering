# Phase 0 Research — Games + Productivity Sample Apps (G2)

Decisions that resolve the Technical Context unknowns. Each is **Decision / Rationale / Alternatives**.
Grounding facts were read from the shipped G1 sample (`samples/ControlsGallery/*`) and the public surface
baselines; G2 reuses G1's machinery wherever possible and only adds what the game loop and the 6-sample
breadth require.

## R1 — Project layout: one `samples/SampleApps/` tree, 3 projects, package-only

**Decision**: A single standalone tree `samples/SampleApps/` with three projects — `SampleApps.Core`
(pure: shared harness + all six sample cores + coverage/backlog), `SampleApps.App` (thin GL/I-O edge),
`SampleApps.Tests` (Expecto) — consuming the framework **only** as packed `FS.GG.UI.*` packages from
`~/.local/share/nuget-local/` via a local `nuget.config`, and **kept out of `FS.GG.Rendering.slnx`**.

**Rationale**: Byte-for-byte the G1 shape (`ControlsGallery.Core`/`.App`/`.Tests`), which is proven to
build against the local feed and is the SC-006 public-consumer proof. One shared `Core` lets the six
samples share the harness, registry, evidence schema, and PRNG without duplication, and keeps all
deterministic logic GL-free and unit-testable (Principle IV). Staying out of the `.slnx` means the main
solution build never depends on packed output.

**Alternatives**: (a) **Per-sample 3-project trees** → 18 projects, six copies of the harness wiring —
rejected as project explosion. (b) **Two trees, `samples/Games/` + `samples/Productivity/`** → duplicates
the harness/registry/evidence/coverage machinery and splits the 22-spec backlog across two reports —
rejected. (c) **Add to `.slnx` with `ProjectReference` into `src/`** → faster dev loop but violates
FR-010/SC-006 (consumer-path proof) — rejected, same as G1 research R1.

## R2 — Heterogeneous sample registry: closure-erased `SampleEntry`

**Decision**: Each sample exposes its own MVU types (`Model`/`Msg`) and a value
`entry : SampleEntry` whose fields are **non-generic closures** that capture the sample's types:

```fsharp
type SampleEntry =
    { Id: string                                   // "tetris", "todo", …
      Family: string                               // "game" | "productivity"
      Title: string
      Controls: string list                        // catalog control ids the sample renders (coverage)
      Inputs: string list                          // "keyboard" | "pointer" | "timing-step"
      RunEvidence: int -> string -> SampleEvidenceRecord   // seed -> outDir -> record (headless, no GL needed for state)
      Interactive: ThemeMode -> int                // GL-gated; returns process exit code
      Outcome: ExpectedOutcome }                   // authored acceptance outcome (R6)
```

`Registry.all : SampleEntry list` holds all six. The generic `InteractiveAppHost<'M,'Msg>`, the
`FrameInput<'Msg>` script, and the `update` live **inside** the closures, so the registry itself carries no
type parameter.

**Rationale**: F# has no first-class existential type; the idiomatic erasure is to hide the type variables
behind functions whose signatures mention only ground types. The shared `Harness.evidenceFor host script
toStateText outcome` builds each `RunEvidence` closure so every sample reuses one evidence runner. G1 never
hit this because it had a single `GalleryModel`; G2's six independent apps force it.

**Alternatives**: (a) A **single unified `Model`/`Msg` union** spanning all samples — couples the apps into
one ~200-case type, defeats "each sample is an independent MVU app", and makes adding a backlog sample a
core-type edit — rejected. (b) **Boxing to `obj` + downcasts** — unsafe and un-idiomatic — rejected.

## R3 — Deterministic game loop: injected `Tick` deltas → host `Tick` → step message

**Decision**: Game time advances **only** through `FrameInput.Tick(TimeSpan)` entries in the seeded script,
mapped by the host's `Tick: TimeSpan -> Msg option` field to a `Step`/`Gravity`/`Advance` message that the
pure `update` folds into the next state. No wall-clock anywhere in `Core`. The App edge's interactive mode
supplies real `Tick` deltas from the viewer; headless mode supplies fixed deltas from the script via
`ControlsElmish.Perf.runScript` — the identical `update` runs in both.

**Rationale**: This is exactly G1's `Scripts.fs` + `Perf.runScript` pattern; the **only** change is that
G1 set `Tick = fun _ -> None` (a gallery doesn't tick) whereas a game maps each tick to a step. `FrameInput`
already exposes a `Tick` case and `InteractiveAppHost` already exposes a `Tick` field in the public surface
(both confirmed), so no new framework surface is needed. Fixed deltas → deterministic frame sequence →
byte-identical evidence (SC-002).

**Alternatives**: (a) A background timer / `Async.Sleep` loop — non-deterministic, wall-clock — rejected
(FR-006). (b) Folding gravity into the keyboard `MapKey` — conflates input with the passage of time and
can't reach a terminal state without keystrokes — rejected.

## R4 — Seeded randomness: a pure in-sample LCG, never `System.Random`

**Decision**: A tiny pure linear-congruential generator in `Prng.fs` —
`type Prng = { State: uint64 }` with `next : Prng -> uint32 * Prng` and `seed : int -> Prng` — threaded
through each game's `Model` and seeded from the CLI `--seed`. All in-game randomness (Tetris 7-bag order,
Snake food placement, Pong serve direction) is drawn from it. No `System.Random`, no `Math.random`, no
`Guid`, no wall-clock.

**Rationale**: Determinism (FR-006/SC-002) requires randomness to be a pure function of the seed carried in
the model. An LCG is the plainest such generator (Principle III) and makes the seed the single source of
variation, so two same-seed runs are identical and different seeds give genuinely different play. The PRNG
is part of the model, so `update` stays pure.

**Alternatives**: (a) `System.Random(seed)` — stateful object, not referentially transparent, and its
algorithm is not contractually stable across runtimes — rejected. (b) Hardcoded fixed sequences (no PRNG) —
wouldn't exercise a real game and couldn't vary by seed — rejected.

## R5 — Evidence record + harness: re-implement G1's package-only schema, extend with a sample outcome

**Decision**: Port G1's `ControlsGallery.Core/Evidence.fs` to `SampleApps.Core/Evidence.fs` (the
package-only, hand-rolled, byte-stable `run.json` / `state.txt` / `summary.md` writer with `ProofLevel`,
`AuthoritativeFor`, non-empty `NotAuthoritativeFor`, and the degrade-and-disclose `ScreenshotSummary`).
**Extend** the record with a sample-specific `Outcome` block (e.g. `score`, `clearedRows`, final-occupancy
hash for games; item counts / column contents / validation-rejections for productivity) so the evidence
both *discloses* and *checks* the authored acceptance outcome.

**Rationale**: G1 already solved "deterministic evidence without an internal harness dependency"; reusing
its exact serialization keeps the records byte-stable and the schema familiar. The added `Outcome` block is
what turns "a run happened" into "the run met its source-spec acceptance criteria" (FR-009/SC-001).
`state.txt` keeps G1's count/bool `FrameMetrics` golden (timing excluded) so determinism is provable
field-by-field.

**Alternatives**: (a) Depend on the internal `tests/Rendering.Harness` evidence schema — breaks the
package-only rule (FR-010) — rejected, same as G1 research R3. (b) Serialize via `System.Text.Json` —
key-order/formatting nondeterminism risk and a parser dependency — rejected in favor of the hand-rolled
fixed-order writer G1 already uses.

## R6 — Acceptance outcomes provenance: authored-and-rebranded, pinned + asserted, disclosed

**Decision**: Because the archived game/productivity specs (`docs/testSpecs/Games|Productivity/*`) are **not
present in this repository**, each sample's goal / control scheme / state model / **acceptance outcome** is
**authored here** from the implementation plan §10 description and rebranded `FS.Skia.UI.*` → `FS.GG.UI.*`.
The authored outcome is pinned as the sample's `ExpectedOutcome` value and asserted by the build-outcome +
determinism suites. The authoring + rebrand is disclosed in `samples/SampleApps/PROVENANCE.md`, mirroring
G1's `PROVENANCE.md`, and called out in the spec's Assumptions.

**Rationale**: Identical situation and resolution to G1 (its research R9 / PROVENANCE): adopt the archive's
*intent* where its *text* is unavailable, name the authoritative fallbacks (the plan + the live public
surface), and never fabricate an upstream label. The outcomes are real deterministic assertions (not mocks
or canned responses), so Principle V's `Synthetic` token is not required — but the *provenance* of the
numbers is disclosed.

**Alternatives**: (a) Fetch the archive specs into the repo — out of scope and not available here —
rejected. (b) Skip outcome assertions and check only determinism — would not satisfy "meets its source-spec
acceptance criteria" (FR-009) — rejected.

## R7 — Coverage + backlog: a committed report + a Core honesty check over all 22 specs

**Decision**: `SampleApps.Core/Coverage.fs` exposes (a) `coverageRows : CoverageRow list` mapping each of
the six samples to the catalog control ids and input modalities it exercises, and (b) `backlog :
BacklogEntry list` enumerating **all 22** archived specs (12 games + 10 productivity, names taken from plan
§10) each marked `Adopted` or `Deferred` with a reason. A committed `coverage-backlog.md` renders both. The
`CoverageBacklogTests` suite fails if: any curated sample is missing from `coverageRows`; the union of
`Inputs` does not span `keyboard` + `pointer` + `timing-step`; any of the 22 specs is unaccounted, listed
twice, or lacks a disposition/reason; or a `CoverageRow.Controls` id is absent from `Catalog.supportedControls`.

**Rationale**: Mirrors G1's coverage check (catalog→page 1:1) and the repo's matrix-honesty precedent
(feature 132/133 coverage matrices, feature 131 docs-coverage check). It makes the "representative slice"
and "maximal honest coverage" claims machine-checked rather than narrative, and keeps the backlog honest as
the curated set grows. Control ids are validated against the live `Catalog.supportedControls`, so the
report can't drift from the real surface.

**The 22-spec enumeration** (plan §10, lines 367–368):
- **Games (12)**: Tetris*, Snake*, Pong*, Asteroids, Breakout, Lunar Lander, Sokoban, Space Invaders,
  Tower Defense, Top-down Racer, Bomberman-lite, Platformer.
- **Productivity (10)**: Kanban board*, Todo/task manager*, Calendar scheduler*, Contact manager,
  Expense tracker, File manager, Invoice builder, Markdown notes, Pomodoro timer, Spreadsheet editor.
- `*` = adopted in this slice (6); the other 16 are `Deferred`.

**Alternatives**: (a) A prose-only backlog in README with no test — drifts silently — rejected. (b) Derive
the 22 from a fetched archive index — not available in-repo — rejected; the plan's enumeration is the
authoritative source (disclosed in PROVENANCE).

## R8 — Productivity validation + inline edit: pure reducers, `Result`-typed validation

**Decision**: Forms validate via a pure `validate : Draft -> Result<Item, string list>`; the `update`
reducer commits an item only on `Ok` and records the error list (surfaced in the view) on `Error`, never
mutating the committed data on invalid input. Inline edit is a `BeginEdit id` / `CommitEdit (id, value)`
message pair whose commit updates both the displayed value and the underlying record. An empty data model
renders a defined empty-state view (no crash).

**Rationale**: Keeps validation/inline-edit inside the MVU boundary (Principle IV) and makes "invalid input
is rejected without committing" (FR-004/SC-007) a checkable pure-function property — `ValidationTests` can
assert it without any GL or I/O. `Result` is the idiomatic F# carrier for validate-or-reject.

**Alternatives**: (a) Exceptions for invalid input — un-idiomatic, fails the warnings-as-errors posture,
and harder to assert — rejected. (b) Commit-then-rollback — leaves a window of invalid committed state —
rejected.

## R9 — Themes: Light/Dark + accents over shipped palettes, no F/D dependence

**Decision**: A `SampleTheme.fs` mirroring G1's `GalleryTheme` — `resolve : ThemeMode -> Color -> Theme`
over the existing `FS.GG.UI.Themes.Default` Light/Dark and consumer-owned accent `Color` literals — with no
dependence on the Ant/Fluent/Material themes or any kit.

**Rationale**: FR-015 restricts the samples to controls/themes that exist today; G1's `GalleryTheme` is the
proven, package-only way to do Light/Dark + accent. Reusing the same approach keeps G2 independent of
Workstreams D/F and the Ant restyle (G3).

**Alternatives**: (a) Use the shipped `FS.GG.UI.Themes.AntDesign` — pulls G2 into the Ant arc and violates
the "existing-themes-only, G3 is separate" scope — rejected. (b) A bespoke theme type — needless, the
public `Theme` + `Theming` already cover Light/Dark + accent — rejected.

---

**All Technical Context unknowns resolved.** No `NEEDS CLARIFICATION` remains. The only novelty over the
proven G1 pattern is the `Tick`-driven loop (R3), the seeded PRNG (R4), and the closure-erased registry
(R2) — each justified in Complexity Tracking.
