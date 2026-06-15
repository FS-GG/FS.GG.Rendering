# Implementation Plan: Wire Retained Identity State onto the Live Path (Feature 092)

**Branch**: `092-wire-retained-identity-state` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/092-wire-retained-identity-state/spec.md`

## Summary

Feature 091 wired the parked keyed reconciler onto the render path and made each frame confer a stable
`RetainedId` on every matched node, carrying a per-identity state map (`StateByIdentity`) frame to
frame — **but 091 only *carried* that map; the live Elmish host ignored it**, so focus and in-progress
text still resolved through the old `ControlId` `hitTest`, which collapses unkeyed same-kind siblings
and cannot survive a positional shift on the real path.

Feature 092 **wires the retained identity state into the live host**: the Elmish adapter
(`ControlsElmish`) now **reads and writes** `StateByIdentity` through the real
`resolveFocus` → `retainedHitTest` → `routeFocusedText` → `RetainedRender.step` seam, so focus and an
in-progress text edit are keyed to the node's stable `RetainedId` and **survive** an unrelated
re-render that shifts the control's position. Alongside that wiring, 092 lands four supporting
guarantees the imported source already carries: **theme is part of the fragment reuse key** (a theme
change invalidates cached fragments and repaints faithfully); the **first frame paints exactly once**
(`init` returns the painted `Render` the adapter reuses, surfacing frame-0 diagnostics 091 reported a
frame late); the work-reduction accounting **splits honestly** changed-vs-shifted nodes under a layout
shift; and it fixes two carried defects (a pre-filled field wiped on its first keystroke, and a
text-area hard-coded to single-line).

**This is a backfill plan.** The implementation (`src/Controls/RetainedRender.fs` + `.fsi`, the
`Controls.Elmish` adapter `resolveFocus`/`routeFocusedText` seam), the accreted `.fsi` surface
(`init`, `step`, `RetainedInit<'msg>`, `RetainedRender<'msg>` with `StateByIdentity`/`Theme`,
`retainedHitTest`), and the authoritative Expecto/FsCheck suites
(`tests/Controls.Tests/Feature092RetainedRenderTests.fs`,
`tests/Elmish.Tests/Feature092LiveSurvivalTests.fs`) **already exist** in the imported, rebranded
source, with captured readiness evidence under `readiness/`. The plan's job is to bring this work
under the canonical `Spec → .fsi → semantic tests → implementation` contract: document the design
decisions the code already embodies, confirm the constitution gates the existing artifacts satisfy,
and record the honest deviations created by importing code ahead of its spec. No new product behavior
is designed here; `/speckit-tasks` and `/speckit-implement` reduce to a **conformance pass** (confirm
the suites are green, the readiness evidence regenerates, and the public-surface delta is zero), not a
build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto + FsCheck (property/semantic tests); the `Controls` package's
`Scene`/`Layout`/`Control.renderTree` measure-paint path; the `Controls.Elmish` adapter (`Elmish`
package integration). No new runtime or package dependency — 092 is an internal wiring of code already
present.

**Storage**: N/A. `StateByIdentity` and the retained structure live in the host loop's existing
mutable-ref state (the interpreter edge); nothing is persisted.

**Testing**: Default-tier "Local inner loop" across **two** in-assembly suites reaching the internal
surface via `InternalsVisibleTo`:
- `tests/Controls.Tests/Feature092RetainedRenderTests.fs` — theme-in-reuse-key, hit-test identity
  distinctness, work-reduction split, first-frame paint/diagnostics, multi-frame parity (US2–US5).
- `tests/Elmish.Tests/Feature092LiveSurvivalTests.fs` — the **headline** live-survival proof driven
  through the real `resolveFocus` + `routeFocusedText` + `RetainedRender.step` seam with no
  hand-seeded state, plus the rebuild-every-frame baseline that loses it (US1).
Deterministic/offscreen — no GL context required.

**Target Platform**: Linux/dev. 092's proofs are deterministic and headless (structural scene
equality, draft-string continuity, work-count invariants), independent of the GPU.

**Project Type**: F# UI framework — an internal module inside the `Controls` runtime library plus the
`Controls.Elmish` adapter internals, exercised by their in-assembly suites.

**Performance Goals**: No wall-clock target. The measurable goals are correctness/work-count
invariants: live state survives a shift (SC-001), distinct identities per field (SC-002), an honest
work split `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount < BaselineNodeCount`
(SC-003), per-frame byte-identical parity (SC-004), single first-frame paint (SC-005), theme-keyed
reuse (SC-006), identity continuity (SC-007).

**Constraints**:
- Surface stays **assembly-internal** — zero public-surface-baseline delta (FR-012). The
  surface-drift check must pass unchanged for both `FS.GG.UI.Controls` and `FS.GG.UI.Controls.Elmish`.
- The wired `step`/`routeFocusedText` path MUST be **total** (never throws) and **deterministic** (no
  wall-clock, no randomness) on the live path (FR-011).
- Reuse decisions use **structural** equality plus the **theme** as part of the reuse key (FR-008).
- A `Replace` (kind change at same key) **drops** the prior identity's state; a removed control's
  state is **filtered out** (FR-007) — no orphaned/false-carry state.
- Frame-0 malformed input (duplicate sibling keys) surfaces a `KeyCollision` diagnostic on frame 0
  while `init` stays total (FR-010, Principle VI).
- Output parity is judged by structural scene equality + bounds + node count, and survival by draft
  continuity (`hix` → `hixy`) — pixel/desktop-visibility proofs are explicitly out of scope (the
  readiness evidence discloses this).

**Scale/Scope**: One internal wiring across two assemblies. **092-in-scope surface**: the live read/
write of `StateByIdentity` through the adapter seam (`resolveFocus`, `routeFocusedText`); the
`RetainedRender<'msg>` `Theme` field as the reuse key; `RetainedInit<'msg>`
(`Retained`/`Render`/`Diagnostics`) and the single-paint `init`; `retainedHitTest` per-node identity
distinctness; the `WorkReductionRecord` changed-vs-shifted split; and the pre-filled-append /
MultiLine / multi-handler fixes. The same `RetainedRender.fsi` carries 091's base surface and
**later-feature accretions** (097 layout cache, 099 live clock, 103 cross-fade, 108/110 perf driver,
113 memo, 114 virtualization, 116/117 caches, 120 fingerprint) — those are **out of scope for 092**
and owned by their own features. Per-control **animation-clock** survival is owned by feature 099;
092 only carries the clock through the same map.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it alters observable behavior (focus and
in-progress text now survive a position-shifting re-render on the *real* host path; pre-filled fields
are no longer wiped; theme participates in reuse). The public API surface delta is **intentionally
zero** (FR-012): the wired surface is `internal` in both `Controls` and `Controls.Elmish`,
deliberately omitted from the capability `contracts:` lists, so the surface-drift baselines are
unchanged *and that zero-delta is itself an asserted requirement*. Per the vertical-slice rule, the
in-assembly Expecto/FsCheck tests are the user-reachable surface for these internal stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: the adapter wiring + the accreted `.fsi` + both suites arrived together at migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` (`RetainedRender.fsi`, `ControlsElmish.fsi`) and the FSI-reachable semantic tests already exist and exercise the **real wired seam** (no hand-seeded state). Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ⚠️ Pass with noted drift | `RetainedRender.fsi` and `ControlsElmish.fsi` are the sole declarations of the surface; the 092 seam (`resolveFocus`, `routeFocusedText`, `retainedHitTest`, `init`/`step`, `RetainedInit`) is `internal` (zero baseline delta). The imported `.fs` files carry redundant `internal`/`private` access modifiers on top-level bindings, which the constitution discourages when an `.fsi` is present. Pre-existing import condition; scoped as a bounded Tier-2 follow-up (tasks.md **DF-1**), not a blocker. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions + tree recursion (legitimate branching structure). Mutation appears only on the render/measure hot path, disclosed at the use site. No SRTP/reflection/type-providers/custom operators requiring justification. |
| IV. Elmish/MVU boundary | ✅ Pass | This is squarely an MVU-boundary feature: `StateByIdentity` is durable Model state; `step`/`routeFocusedText` are **pure** transitions returning the next `RetainedRender` + messages; the adapter interprets at the edge (mutable-ref host loop). Focus/text I/O is represented as data, not performed inside the transition. Both pure-transition tests and the live-seam survival test are present. |
| V. Test evidence mandatory | ✅ Pass | `Feature092LiveSurvivalTests` proves US1 through the real seam and fails on the rebuild-every-frame baseline (fail-first evidence is structural: the baseline branch loses the draft). `Feature092RetainedRenderTests` pins US2–US5. Readiness artifacts captured (`live-survival`, `focus-resolution`, `theme-reuse`, `work-reduction`, `multi-frame`). Any `Synthetic` malformed-input fixture (duplicate-key frame-0) carries the token and discloses its literal. The readiness evidence honestly declares it does **not** prove pixels/desktop visibility. |
| VI. Observability & safe failure | ✅ Pass | A first-frame duplicate-key `KeyCollision` surfaces as a `ControlDiagnostic` on **frame 0** (closing 091's one-frame lag); `init`/`step`/`routeFocusedText` are total for any input. No silent failure or swallowed exception on the wired path. |

**Gate result**: PASS (two deviations justified and recorded; neither is a public-contract or
evidence violation). Re-checked post-Phase-1 design below — unchanged: the design artifacts add no
public surface, no dependency, and no new behavior beyond what the existing suites pin.

## Project Structure

### Documentation (this feature)

```text
specs/092-wire-retained-identity-state/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions recovered from the imported wiring
├── data-model.md        # Phase 1 — the 092-in-scope live-state entities
├── quickstart.md        # Phase 1 — how to run + read the 092 validation (both suites)
├── contracts/
│   └── live-identity-state.md   # Phase 1 — the internal seam contract the suites pin
├── checklists/          # Pre-existing authoring checklists
├── readiness/           # Pre-existing captured evidence (gitignored): live-survival, focus-resolution, theme-reuse, work-reduction, multi-frame
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── RetainedRender.fsi / RetainedRender.fs   # 091 base + 092 accretions: init/step signatures, RetainedInit, Theme reuse key, retainedHitTest
├── Reconcile.fsi / Reconcile.fs             # Feature 067 — keyed VDOM diff wired by 091/092
├── Control.fsi / Control.fs                 # renderTree measure/paint — the full-rebuild parity oracle
└── Types.fsi / Types.fs                     # ControlDiagnostic / KeyCollision / Severity / Theme

src/Controls.Elmish/
└── ControlsElmish.fsi / ControlsElmish.fs   # The live adapter seam 092 wires: resolveFocus, routeFocusedText (read/write StateByIdentity)

tests/Controls.Tests/
└── Feature092RetainedRenderTests.fs         # US2–US5: hit-test distinctness, theme reuse, work split, first-frame, multi-frame parity

tests/Elmish.Tests/
└── Feature092LiveSurvivalTests.fs           # US1: live focus+draft survival through the real seam; rebuild-every-frame baseline fails
```

**Structure Decision**: Single F# solution (`FS.GG.Rendering.slnx`). 092 adds no project and no public
surface; it wires the existing internal `RetainedRender` module to the existing `Controls.Elmish`
adapter and pins the behavior with tests in the existing `Controls.Tests` and `Elmish.Tests`
assemblies. The live-survival proof lives in `Elmish.Tests` because the headline guarantee is a
property of the **adapter seam**, not of `RetainedRender` in isolation. The Replace-drops /
removal-filters proofs (US1 acceptance 3–4) live there too — although they are `StateByIdentity` map
mechanics, they are asserted as observed through the same live `step` seam (focus clears on the wired
path), so they belong with the seam's suite rather than `Feature092RetainedRenderTests`. Surface baselines under
`tests/surface-baselines/` (`FS.GG.UI.Controls.txt`, `FS.GG.UI.Controls.Elmish.txt`) must remain
byte-unchanged.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The adapter wiring, the accreted `.fsi`, and both suites were imported wholesale at migration; this spec/plan is authored afterward. | Re-deriving the wiring from a fresh spec would discard working, evidence-backed code and its history. The backfill restores the chain at lower cost and risk. |
| Redundant `internal`/`private` access modifiers in `RetainedRender.fs` and `ControlsElmish.fs` | Inherited verbatim from the imported source. | Stripping them is a behavior-neutral Tier-2 cleanup; bundling it into this backfill would mix a documentation pass with a code edit. Scoped as a bounded follow-up (tasks.md **DF-1**), not done here. |
| One `RetainedRender.fsi` / `ControlsElmish.fsi` documents many features' fields together | The single imported `.fsi` files accreted later features in place; they cannot be physically split without breaking those features. | 092's plan scopes its surface explicitly (Scale/Scope) and defers the rest to the owning features (097/099/103/108/110/113/114/116/117/120), rather than forking the files. |
