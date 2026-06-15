# Implementation Plan: Lookless Slot Composition (Feature 095)

**Branch**: `095-lookless-slot-composition` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/095-lookless-slot-composition/spec.md`

## Summary

Feature 095 introduces **one pure, total, deterministic lowering** — `ControlInternals.lowerSlots`
(`src/Controls/Control.fsi`/`.fs`) — that injects consumer-authored sub-trees into a control's
**named regions** (a `Button`'s `Leading`/`Trailing`, a `Panel`'s `Header`/`Footer`) by folding the
control's per-kind declared regions and its attached **slot fills** into the control's ordinary
`Children`, ordered by region position: `[leading regions; intrinsic children; trailing regions]`.
The slot fills ride the existing `Attr` mechanism under `AttrCategory.Slot` as the
`SlotFillsValue of (string * Control<'msg>) list` carrier; the internal seam is
`slotFill` (builder) / `slotFillsOf` / `slotFor` (readers) / `lowerSlots` (the consumption edge). The
**only sanctioned authoring path** is the typed front-door props — `ButtonProps.Leading`/`Trailing`
(`Widgets/Primitives`) and `PanelProps.Header`/`Footer` (`Widgets/Containers`) — which build the
`Slot`-category carrier internally; there is no public free-form slot builder or slot-name string.
Because fills become normal children, slotted content inherits flat per-`ControlId` dispatch (E1),
retained identity (E2), the feature-093 visual-state resolver (E3), and focus/tab routing (E4) **by
construction**, with no slot-specific special-casing. With no slot attribute present, `lowerSlots`
returns the control **verbatim** (the fast path), so an unfilled slotted control is byte-identical to
its pre-slot self.

**This is a backfill plan** — the same conformance-backfill pattern features 091 and 093
established. The implementation, the public `.fsi` surface, the committed surface-area baseline
entry (`SlotFillsValue` on the already-public `AttrValue<'msg>`), the captured readiness evidence
(`readiness/parity/`), and the executable Expecto/FsCheck suite (`Feature095SlotCompositionTests`)
**already exist** in the imported, rebranded source. No Spec Kit spec/plan/tasks ever described them.
This plan's job is to bring the work under the canonical `Spec → .fsi → semantic tests →
implementation` contract: it documents the design decisions already embodied in the code, confirms
the constitution gates the existing artifacts satisfy, and records the honest import-before-spec
deviation against Principle I. No new product behavior is designed here; `/speckit-tasks` and
`/speckit-implement` reduce to a **conformance pass** (confirm the suite is green, the parity oracle
matches, and the surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`, from `Directory.Build.props`), `LangVersion=latest`.

**Primary Dependencies**: Expecto (the `Feature095SlotCompositionTests` suite) + FsCheck (the
`loweringProperties` list, ≥1000 cases per property via the `Gen095` generator module). The lowering
itself depends only on the `Controls` package's own `Control<'msg>` / `Attr<'msg>` types and the
feature-092 keyed reconciler / feature-093 resolver it composes *with* (consumes, does not duplicate).
No new runtime or package dependency — 095 is pure folding over types already present.

**Storage**: N/A. `lowerSlots` is a pure value-to-value function; nothing is persisted. (US3's
retained-identity survival rides feature 092's `RetainedRender` structure, which 095 only consumes.)

**Testing**: Default-tier "Local inner loop" — `Controls.Tests`, reaching the internal slot seam
(`slotFill`/`slotFillsOf`/`slotFor`/`lowerSlots`) via `[<assembly:
InternalsVisibleTo("Controls.Tests")>]`. The suite drives the **typed front door**
(`ButtonProps.Leading`/`Trailing`, `PanelProps.Header`/`Footer`) — the user-reachable surface for
these stories. The property list runs its purity/determinism/totality properties at ≥1000 generated
`(kind, fills)` cases via `Gen095`. Deterministic and offscreen — no GL context required (record
comparison + structural scene equality).

**Target Platform**: Linux/dev. 095's proofs are deterministic and headless: record/`Children`
comparison for placement, structural scene equality against a frozen procedural oracle for the
unfilled-parity case, and the in-process keyed reconciler for retained-identity survival. None
require the GPU.

**Project Type**: F# UI framework — the internal slot seam on `Control.fsi`/`.fs`, the
`AttrCategory.Slot` carrier + `SlotFillsValue` case on `Types.fsi`, and the typed slot props on
`Widgets/Primitives` + `Widgets/Containers`, all inside the `Controls` runtime library, with the
`Feature095SlotCompositionTests` suite in the existing `Controls.Tests` assembly.

**Performance Goals**: No wall-clock target. `lowerSlots` is a bounded fold (a per-region `pick` over
the fill list, two list concatenations into `Children`, and one carrier-attribute filter); the
measurable goals are **behavioral**: purity/determinism over ≥1000 inputs (SC-005), totality —
never throws for any `(kind, fills)` (SC-005), structural scene-equal parity to the frozen pre-slot
baseline (SC-002), and exact identity `lowerSlots c = c` when no slot attribute is present
(SC-002/SC-005).

**Constraints**:
- **Closed, typed front door** — authoring goes only through the typed `Props` fields; there is
  **no public free-form slot builder**, no `Attr.slot`, and no consumer slot-name string. Only the
  internal `slotFill` produces a `Slot`-category attribute (FR-001, SC-006).
- **One carrier, last-writer-wins** — a control carries at most one `Slot`-category attribute; an
  ordered association list region-name → fill; **absent ≠ empty** (a region present-but-empty is
  `Some`, absent is `None`) (FR-002).
- **Identity when unfilled** — with no slot attribute present, `lowerSlots` returns the control
  verbatim; the unfilled case is structurally scene-equal to the prior pre-slot output (FR-003,
  SC-002).
- **Ordered injection + carrier consumption** — fills land in `Children` ordered `[leading;
  intrinsic; trailing]`, and the slot carrier attribute is filtered out (consumed) so it leaves no
  residue (FR-004).
- **E1–E4 by construction** — because fills become normal children, they inherit flat per-`ControlId`
  dispatch (E1), retained identity (E2, via the live `RetainedRender` path), the feature-093 resolver
  (E3), and focus routing (E4), with no slot-specific special-casing (FR-004, FR-005).
- **Pure / total / deterministic** — no clock, randomness, or side effects; identical `(kind, fills)`
  inputs always lower to an identical intermediate representation; never throws for any combination
  (FR-006).
- **Additive and scoped** — only the kinds that opt in (`button`, `panel` in `slotRegions`) carry
  regions; a non-slotted kind (`CheckBox`) returns `[], []` and is unaffected (FR-007).
- **Lookless, single-control** — fills are static `Control<'msg>` values; no data-bound templates,
  no selector / specificity / cascade. These are permanent non-goals, not deferrals (FR-008).
- **Zero public-surface-baseline delta** — `SlotFillsValue` is already in the committed
  `tests/surface-baselines/FS.GG.UI.Controls.txt`; the slot seam is `internal`; the typed props are
  the authoring path. The surface-drift check must pass unchanged. (Surface scope is a Tier-1
  *characteristic*, but the delta this backfill adds is zero.)
- Render-output equivalence is judged by **structural scene equality** (the same oracle technique
  features 091/093 use); pixel-level / desktop-visibility proofs are explicitly out of scope, as the
  readiness evidence discloses.

**Scale/Scope**: The internal slot seam — `slotFill`, `slotFillsOf`, `slotFor`, `lowerSlots` on
`Control.fsi`/`.fs`; the `AttrCategory.Slot` discriminant + `SlotFillsValue` case on `Types.fsi`; the
typed slot props (`ButtonProps.Leading`/`Trailing`, `PanelProps.Header`/`Footer`) — and one test
suite with seven test lists. The lowering's body uses two `private` helpers (`slotName`,
`slotRegions`) plus a private `SlotName` DU. **Slot exposure footprint:** `slotRegions` pins regions
to exactly two kinds — `button` (`Leading`/`Trailing`) and `panel` (`Header`/`Footer`) — the only
kinds with a frozen pre-slot oracle under `readiness/parity/`. Only the `button` baselines are
captured today (light + dark `normal`); the `panel` unfilled case is proven by direct
`lowerSlots`-identity rather than a frozen scene. Extending slots to further kinds and capturing
their parity scenes is a bounded follow-up (DF-2), not part of this contract.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — its single public-surface entry is the
`SlotFillsValue` case on the already-public `AttrValue<'msg>` type (and it alters observable behavior:
a slotted control now lowers fills into `Children`). The public-surface-baseline **delta is zero**
because `SlotFillsValue` was already committed at import; that zero-delta is itself an asserted
requirement, validated by the surface-drift check. The slot builders/readers are `internal`; the
authoring path is the typed `Props` fields. Following the **typed-front-door vertical slice** the
091/093 backfills established — and consistent with Principle I.3 (tests assert observable behavior,
not internals) — the in-assembly Expecto/FsCheck tests reach the internal seam only via
`InternalsVisibleTo("Controls.Tests")` to drive the **public typed front door** and assert on the
resulting lowered `Children`; the typed `Props` are the user-reachable surface for these stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: code + `.fsi` (`Control.fsi` seam, `Types.fsi` carrier, the typed `Props`) + tests + surface baseline + readiness arrived together in the rebranded-source migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi`, the typed props, and the FSI-reachable semantic suite already exist and exercise the real lowering. Recorded in Complexity Tracking. **I.3 (tests through the public surface, not internals):** honored via the typed-front-door vertical slice — the suite drives the public typed `Props` (`ButtonProps.Leading`/`Trailing`, `PanelProps.Header`/`Footer`) and asserts on the observable lowered `Children`; it reaches the `internal` seam only through `InternalsVisibleTo`, never asserting on the private helpers (`slotName`/`slotRegions`/`SlotName`). |
| II. Visibility lives in `.fsi` | ✅ Pass | `Control.fsi` is the sole declaration of the internal seam's signatures (`slotFill`/`slotFillsOf`/`slotFor`/`lowerSlots`), `Types.fsi` of `AttrCategory.Slot` + `SlotFillsValue`, and the `Widgets/*.fsi` of the typed props. `SlotName`, `slotName`, and `slotRegions` are private by omission from `Control.fsi` (FS0078-as-error per `Directory.Build.props` keeps them so). `Control.fs` carries the lone `private` on `slotRegions`/`SlotName` — see Complexity Tracking DF-1 on the `.fs`-access-modifier deviation. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions: a single-case `match` on `slotFillsOf`, a `List.choose`/`List.tryFind` `pick` per region, two `@` concatenations into `Children`, and one `List.filter` to consume the carrier. No SRTP, reflection, type providers, custom operators, or non-trivial computation expressions. `SlotName` is a four-case simple-discriminant DU — within Principle III's allowance. No mutation. |
| IV. Elmish/MVU boundary | ✅ N/A (pure) | `lowerSlots` is a pure, total value-to-value function with no state or I/O, so the MVU boundary does not apply to it. The retained-identity story (US3) is delegated to feature 092's `RetainedRender` MVU boundary, which 095 only consumes; 095 adds no stateful workflow of its own. |
| V. Test evidence mandatory | ✅ Pass | `Feature095SlotCompositionTests` (seven lists): `slotPlacement` (SC-001 placement/ordering/`slotFor` absent≠empty), `loweringProperties` (SC-005 purity/determinism/totality at ≥1000 cases via `Gen095` + no-slot identity), `typedClosure` (SC-006 no public free-form slot path), `unfilledParity` (SC-002/SC-007 byte-identity, frozen-oracle scene equality both themes, unfilled panel == legacy, CheckBox unchanged), `compose` (SC-003 E1/E3/E4), `retainedIdentity` (SC-004 E2 via the live retained path), `evidence` (writes/confirms the `readiness/parity/*.scene.txt`). No suite weakens an assertion to green a build. |
| VI. Observability & safe failure | ✅ Pass | `lowerSlots` is total — the `[]` arm returns the control verbatim, every kind resolves a (possibly empty) region pair via `slotRegions`, and a region absent from the fill list simply contributes no child. There is no failure path to swallow: a pure total fold cannot silently fail. Totality is pinned by the property list. |

**Gate result**: PASS. One deviation remains (import-inverted order), justified and recorded; the
`.fs`-access-modifier note (DF-1) is a single `private` on the genuinely-private `slotRegions`/`SlotName`
that have no `.fsi` entry — harmless under FS0078-as-error and called out for visibility. Neither is a
public-contract or test-evidence violation. The slot-exposure footprint (two kinds opted in, only
`button` parity-pinned with a frozen scene) is recorded as a bounded follow-up (DF-2), not a gate
failure — `panel`'s unfilled case is still covered by `lowerSlots`-identity and the totality/purity
proofs cover every kind. Re-checked post-Phase-1 design below — unchanged: the design artifacts add no
public surface, no dependency, and no new behavior beyond what the existing suite pins.

## Project Structure

### Documentation (this feature)

```text
specs/095-lookless-slot-composition/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — design decisions recovered from the imported implementation
├── data-model.md        # Phase 1 — the slot vocabulary + lowering semantics
├── quickstart.md        # Phase 1 — how to run + read the 095 validation
├── contracts/
│   └── slot-composition.md  # Phase 1 — the internal slot seam + typed front-door contract the suite pins
├── readiness/
│   └── parity/          # Pre-existing captured evidence: button.{light,dark}.normal.scene.txt frozen-oracle scenes
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── Control.fsi / Control.fs       # Feature 095 — the slot seam: slotFill / slotFillsOf / slotFor (readers),
│                                   #   lowerSlots (pure/total/deterministic lowering, ~L163), + private
│                                   #   SlotName DU, slotName, slotRegions (per-kind scoped regions, ~L148)
├── Types.fsi / Types.fs           # AttrCategory.Slot discriminant + SlotFillsValue of (string * Control<'msg>) list
└── Widgets/
    ├── Primitives.fsi / .fs       # ButtonProps.Leading / Trailing → ControlInternals.slotFill (~L88)
    └── Containers.fsi / .fs       # PanelProps.Header / Footer → ControlInternals.slotFill (~L126)

tests/Controls.Tests/
└── Feature095SlotCompositionTests.fs   # slotPlacement / loweringProperties / typedClosure /
                                        #   unfilledParity / compose / retainedIdentity / evidence (Expecto + FsCheck Gen095)

tests/surface-baselines/
└── FS.GG.UI.Controls.txt          # Already lists FS.GG.UI.Controls.AttrValue`1+SlotFillsValue (the lone public delta, zero-delta now)
```

**Structure Decision**: Single F# project layout. 095 adds the internal slot seam to the existing
`Control.fsi`/`.fs`, the `Slot` carrier to `Types.fsi`, the typed props to the existing
`Widgets/Primitives` and `Widgets/Containers`, and one suite to the existing `Controls.Tests`
assembly — no new project, no new package, and (because `SlotFillsValue` was committed at import) no
new baseline delta. The slot seam sits in `ControlInternals` beside the keyed reconciler and the
`Style.resolve` (E3) it composes with; the typed `Props` fields are the front door that builds the
carrier so the seam never surfaces to consumers.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The slot seam, the `Slot` carrier + `SlotFillsValue` case, the typed props, the surface baseline entry, and the readiness evidence were imported wholesale in the rebranded-source migration; this spec/plan is authored afterward. | Re-deriving the seam from a fresh spec would discard working, evidence-backed code, its committed surface baseline, and its parity oracle. The backfill restores the chain at lower cost and risk. |
| `private` on `slotRegions` / `SlotName` in `Control.fs` *(DF-1, disclosed)* | These helpers have **no `.fsi` entry**, so they are already private by omission under FS0078-as-error; the explicit `private` on `slotRegions`/`SlotName` is inherited verbatim from the imported source — harmless duplication, not a second source of truth for a *public* symbol. | Stripping it is a behavior-neutral Tier-2 tidy that can ride a later pass; unlike 093's DF-1 (helpers shadowing public-surface concerns), here the symbols are unambiguously internal and the duplication is cosmetic. Disclosed rather than silently kept. |
| Two kinds opt into slots but only `button` has a frozen parity scene | `slotRegions` declares regions for `button` and `panel`, but only `button.{light,dark}.normal.scene.txt` are captured under `readiness/parity/`; the `panel` unfilled case is proven by `lowerSlots`-identity, not a frozen scene. | Capturing a frozen `panel` oracle (and any future opted-in kind) is new verification work outside this backfill's scope. `panel`'s no-slot inertness is still covered by the identity proof, and totality/purity cover every kind; pinning `panel`'s frozen parity is scoped as a bounded follow-up (tasks.md **DF-2**), disclosed rather than hidden. |
