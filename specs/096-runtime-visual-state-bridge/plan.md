# Implementation Plan: Runtime Visual-State Bridge (Feature 096)

**Branch**: `096-runtime-visual-state-bridge` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/096-runtime-visual-state-bridge/spec.md`

## Summary

Feature 096 introduces the **runtime visual-state bridge** — the seam that turns *live interaction*
(which control is focused, hovered, pressed, or selected) into the per-control `VisualState` that
feature 093's resolver paints, **with zero consumer code**. It is two functions on `ControlRuntime`
(`src/Controls/ControlRuntime.fsi`/`.fs`): the public, pure projection
`deriveVisualState : ControlRuntimeModel -> ControlId -> VisualState` (`.fsi:96`, impl `.fs:208`)
that selects the highest-ranked state under the **closed runtime precedence**
`Pressed > Selected > Focused > Hover > Normal`; and the internal host bridge
`applyRuntimeVisualState : ControlRuntimeModel -> Control<'msg> -> Control<'msg>` (`.fsi:103`) that
walks the lowered `Control<'msg>` tree pre-reconcile and stamps each control's derived state onto the
single `visualState` attribute carrier (the same carrier feature 093's `visualStateOf` reads,
`Control.fsi:100`). A consumer-set non-`Normal` state always wins; only a consumer-`Normal`/absent slot
is filled by the derived runtime state, and a derived `Normal` emits **nothing** — so a control nobody
is interacting with is returned byte-identical to its un-bridged self. The later feature-112
targeted-stamp variants (`applyRuntimeVisualStateTargeted` / `runtimeStampFor` / `RuntimeStampResult`,
`.fsi:116`/`:129`/`:76`) are `internal` and ride along on the same carrier.

**This is a backfill plan** — the same conformance-backfill pattern features 091, 093, and 095
established. The implementation, the public `.fsi` surface, the committed surface-area baseline
(`ControlRuntime` module + `ControlRuntimeModel`/`Msg`/`Effect` in
`tests/surface-baselines/FS.GG.UI.Controls.txt:61-89`), the captured readiness evidence
(`readiness/focus-survives-reshuffle.md`, `readiness/responds-proof.md`), and the executable
Expecto/FsCheck suites (`Feature096RuntimeBridgeTests` + `Feature096BridgePropertyTests` in
`Controls.Tests`, and `Feature096LiveBridgeTests` in `Elmish.Tests`) **already exist** in the
imported, rebranded source. No Spec Kit spec/plan/tasks ever described them. This plan's job is to
bring the work under the canonical `Spec → .fsi → semantic tests → implementation` contract: it
documents the design decisions already embodied in the code, confirms the constitution gates the
existing artifacts satisfy, and records the honest import-before-spec deviation against Principle I.
No new product behavior is designed here; `/speckit-tasks` and `/speckit-implement` reduce to a
**conformance pass** (confirm the suites are green, the readiness evidence regenerates, and the
surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`, from `Directory.Build.props`), `LangVersion=latest`.

**Primary Dependencies**: Expecto (the `Feature096RuntimeBridgeTests` / `Feature096BridgePropertyTests`
/ `Feature096LiveBridgeTests` suites) + FsCheck (the property tests at ≥1000 cases via the `Gen096`
generator module). The bridge depends only on the `Controls` package's own `Control<'msg>` /
`Attr<'msg>` / `VisualState` types, the `ControlRuntimeModel` the `ControlRuntime` MVU maintains, the
feature-093 resolver (E3) it composes *with* (consumes, does not duplicate), and the feature-092 keyed
reconciler / live `RetainedRender` path (E2) it rides. No new runtime or package dependency — 096 is a
pure projection plus a pure tree walk over types already present.

**Storage**: N/A. `deriveVisualState` is a pure value projection and `applyRuntimeVisualState` is a
pure value-to-value tree walk; nothing is persisted. (US4's retained-identity survival rides feature
092's live `RetainedRender` path, which 096 only consumes.)

**Testing**: Default-tier "Local inner loop" — `Controls.Tests`, reaching the internal bridge
(`applyRuntimeVisualState` and the feature-112 targeted variants) via `[<assembly:
InternalsVisibleTo("Controls.Tests")>]`, plus `Elmish.Tests` for the live retained-path stories
(US4 / responds-proof). The suites drive the **public projection** (`deriveVisualState`) and the host
bridge seam, and assert on the **observable resolved paint** (`faithfulContent` / `renderTree.Scene`)
and the lowered control's visible `VisualState` — not on private helpers. The property tests run
purity/determinism/totality and the consumer-vs-derived arbitration at ≥1000 generated
`(model, id, consumer-state)` cases via `Gen096`. Deterministic and offscreen — no GL context required
(record comparison + structural scene equality + the in-process live retained step).

**Target Platform**: Linux/dev. 096's proofs are deterministic and headless: `VisualState`/record
comparison for the projection and the stamp, structural `Scene` equality against the un-bridged build
for the at-rest-inert and responds proofs, and the in-process live `RetainedRender.step` for the
retained-identity and bounded-repaint proofs. None require the GPU.

**Project Type**: F# UI framework — the bridge lives in the `ControlRuntime` module on
`ControlRuntime.fsi`/`.fs` inside the `Controls` runtime library, consuming the `VisualState` carrier
on `Types.fsi`/`Attributes.fs` and the `visualStateOf` reader on `Control.fsi`. The suites are
`Feature096RuntimeBridgeTests` + `Feature096BridgePropertyTests` in the existing `Controls.Tests`
assembly and `Feature096LiveBridgeTests` in the existing `Elmish.Tests` assembly.

**Performance Goals**: No wall-clock target. `deriveVisualState` is a bounded ordered cascade (four
membership checks, no per-kind branching); `applyRuntimeVisualState` is a single bounded tree walk.
The measurable goals are **behavioral**: purity/determinism over ≥1000 inputs (SC-004), totality —
never throws for any `(model, id)` (SC-001/SC-004), structural `Scene`-equal at-rest parity to the
un-bridged build with **zero** recomputed nodes (SC-003), a **bounded** repaint
(`RecomputedNodeCount < BaselineNodeCount`) for a single localized interaction (SC-005), and
retained-identity survival of the derived look across a position-shifting re-render (SC-007).

**Constraints**:
- **Closed runtime precedence** — `deriveVisualState` selects the highest-ranked state under
  `Pressed > Selected > Focused > Hover > Normal`, the runtime-derivable *tail* of the full order
  `Disabled > Validation > Loading > Pressed > Selected > Focused > Hover > Normal`. The three head
  states (`Disabled`/`Validation`/`Loading`) are **author intent only** and are **never** produced by
  the projection (FR-002, SC-001).
- **No per-kind branching** — the projection is a plain ordered cascade over the `ControlRuntimeModel`;
  it does not inspect control kind (FR-001).
- **Author intent out-ranks derived** — a consumer-set non-`Normal` `visualState` is preserved 100%;
  only a consumer-`Normal`/absent slot is filled by the derived state (FR-003, SC-004).
- **One carrier, replace-or-append** — arbitration flows through the lone `visualState` attribute
  feature 093's resolver reads; the bridge replace-or-appends (last writer) so the channel never
  accumulates stale state (FR-003).
- **Byte-identical at rest** — a derived `Normal` emits nothing; a `Normal`-and-unset control/tree is
  returned verbatim, renders a `Scene` byte-identical to the un-bridged build, and recomputes **zero**
  nodes on the live retained path (FR-005, SC-003).
- **Pre-reconcile tree walk in the `ControlId` domain** — `applyRuntimeVisualState` walks the lowered
  tree with id = `Key` if present else `Kind`, applied before reconcile, so the look rides E2 retained
  identity through the keyed diff (FR-004, FR-007).
- **E2/E3 by construction, no duplication** — the derived state rides the control's attributes through
  feature 092's keyed reconciler (E2) and is painted by feature 093's resolver (E3); 096 duplicates
  neither the resolver nor 095's slot composition (FR-004, FR-007).
- **Additive and scoped** — the visible restyle is realized only for the kinds 096 widened
  (`button`/`slider`/`text-box`/`radio-group`/`switch`); an unmigrated kind
  (`progress-bar`/`numeric-input`) is stamped but shows **no render delta**; a `Normal` attribute is
  byte-identical to the unset render for every kind (FR-006, SC-006).
- **Pure / total / deterministic** — no clock, randomness, or side effects; identical inputs always
  yield identical results; never throws for any model/id (FR-001, SC-004).
- **Zero public-surface-baseline delta** — `deriveVisualState` is the lone public entry; the
  `ControlRuntime` module (with `ControlRuntimeModel`/`Msg`/`Effect`) was already committed at import,
  and the bridge functions (`applyRuntimeVisualState`, the feature-112 variants) are `internal`. The
  surface-drift check must pass unchanged (FR-008, SC-008).
- Render-output equivalence and the "responds" proof are judged by **structural `Scene` equality** (an
  inert build paints identical frames regardless of interaction; the bridged build differs only because
  interaction restyles) — pixel-level / desktop-visibility proofs are explicitly out of scope, as the
  readiness evidence discloses.

**Scale/Scope**: The bridge — `deriveVisualState` (public) + `applyRuntimeVisualState` (internal) on
`ControlRuntime.fsi`/`.fs`, plus the feature-112 internal targeted-stamp variants
(`applyRuntimeVisualStateTargeted` / `runtimeStampFor` / `RuntimeStampResult`) — over the `VisualState`
DU (`Types.fsi:256`, eight cases), the `visualState` attribute carrier (`Attributes.fs:72`), and the
`visualStateOf` reader (`Control.fsi:100`). Two suites in `Controls.Tests`
(`Feature096RuntimeBridgeTests`, sixteen tests across the precedence/stamp/at-rest/focus/arbitration/
bounded-repaint/scoped-kinds clauses; `Feature096BridgePropertyTests`, three ≥1000-case properties via
`Gen096`) and one in `Elmish.Tests` (`Feature096LiveBridgeTests`, the two live-retained-path stories
that also regenerate the readiness markdown). **Scope footprint:** the projection's `Selected` branch
is forward-looking on the real render path — the live host (`ControlsElmish`) populates focus/hover/
press but not the text-range `Selection`, so `Selected`-derivation is exercised by tests today and
wired for a future host without a code change (DF-2). Widening the visible restyle to further kinds
beyond the five opted in is a bounded follow-up (DF-3), not part of this contract.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — its single public-surface entry is
`ControlRuntime.deriveVisualState` (and it alters observable behavior: an interacted control now
restyles via the derived state stamped onto its `visualState` carrier). The public-surface-baseline
**delta is zero** because the `ControlRuntime` module — with `ControlRuntimeModel`/`ControlRuntimeMsg`/
`ControlRuntimeEffect` — was already committed at import; that zero-delta is itself an asserted
requirement (FR-008/SC-008), validated by the surface-drift check. The bridge functions
(`applyRuntimeVisualState` and the feature-112 targeted variants) are `internal`; the authoring path
consumers actually use is feature 093's typed visual-state attribute. Following the **typed-front-door
vertical slice** the 091/093/095 backfills established — and consistent with Principle I.3 (tests
assert observable behavior, not internals) — the in-assembly Expecto/FsCheck tests reach the internal
bridge only via `InternalsVisibleTo("Controls.Tests")` to drive the host seam and assert on the
resulting **observable resolved paint** (`faithfulContent` / `renderTree.Scene`) and the lowered
control's visible `VisualState`.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: code + `.fsi` (`ControlRuntime.fsi` projection + bridge), tests (`Controls.Tests`/`Elmish.Tests`), surface baseline, and readiness arrived together in the rebranded-source migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` and the FSI-reachable semantic suites already exist and exercise the real projection/bridge. Recorded in Complexity Tracking. **I.3 (tests through the public surface, not internals):** honored via the typed-front-door vertical slice — the suites drive the public `deriveVisualState` and feature 093's typed visual-state attribute and assert on the observable resolved paint / lowered `VisualState`; they reach the `internal` bridge only through `InternalsVisibleTo`. |
| II. Visibility lives in `.fsi` | ✅ Pass | `ControlRuntime.fsi` is the sole declaration of the bridge surface: `deriveVisualState` is public (`:96`); `applyRuntimeVisualState` (`:103`), `applyRuntimeVisualStateTargeted` (`:116`), `runtimeStampFor` (`:129`), and `RuntimeStampResult<'msg>` (`:76`) carry an explicit `internal` qualifier in the `.fsi`. See Complexity Tracking DF-1 on the `internal`-in-`.fsi` qualifier (this is `.fsi`, the sanctioned home for visibility, not an `.fs` access modifier — Principle II forbids the latter, not the former). No top-level `private`/`internal`/`public` modifiers appear in `ControlRuntime.fs`. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions: `deriveVisualState` is a five-arm `if/elif` cascade over the `ControlRuntimeModel` (`.fs:208-222`) with no SRTP, reflection, type providers, custom operators, or non-trivial computation expressions; `applyRuntimeVisualState` is a plain recursive tree walk (`let rec` for genuine tree structure — exactly Principle III's sanctioned use). No mutation in the projection. |
| IV. Elmish/MVU boundary | ✅ Pass (consumed) | The stateful interaction tracking is the `ControlRuntime` MVU (`ControlRuntimeModel`/`ControlRuntimeMsg`/`ControlRuntimeEffect`, `init`/`update`/effect interpreter), which exposes its `Model`/`Msg`/`Effect` on `ControlRuntime.fsi` per Principle IV. Feature 096 adds only a **pure projection** *from* that model and a **pure stamp**, introducing no new stateful workflow or I/O of its own — so it sits on the read side of an MVU boundary that already satisfies the principle. The live retained-path stories are exercised through `Elmish.Tests`. |
| V. Test evidence mandatory | ✅ Pass | `Feature096RuntimeBridgeTests` (sixteen tests: precedence/peel + unknown-id ⇒ `Normal` + determinism for SC-001; no-attribute restyle + sibling-unchanged for SC-002/US1; at-rest unchanged + Scene-identity + zero-recompute for SC-003; focus-indicator + focus-move for SC-002; consumer-vs-derived arbitration for SC-004/US3; bounded single-hover repaint for SC-005; per-widened-kind restyle + unmigrated-kind no-delta for SC-006), `Feature096BridgePropertyTests` (three ≥1000-case `Gen096` properties: total+deterministic projection, closed-order arbitration, deterministic stamp — SC-004), and `Feature096LiveBridgeTests` (live retained path: focus survives a sibling shift on a stable retained id for SC-007; focus/hover input restyles on the live path while an un-bridged build is inert for the responds-proof — and regenerates `readiness/focus-survives-reshuffle.md` + `readiness/responds-proof.md`). No suite weakens an assertion to green a build. |
| VI. Observability & safe failure | ✅ Pass | `deriveVisualState` is total — the `else` arm returns `Normal`, an unknown/unreferenced id resolves to `Normal`, and there is no failure path to swallow (a pure ordered cascade cannot silently fail). `applyRuntimeVisualState` is a total walk — a derived `Normal` emits nothing rather than erroring. Totality is pinned by the ≥1000-case property list. The `ControlRuntime` MVU's effect interpreter owns the operationally-significant diagnostics (`ControlDiagnostic`/`ControlRuntimeEffect`); 096's pure read side has nothing to fail. |

**Gate result**: PASS. One deviation remains (import-inverted order), justified and recorded; the
`internal`-qualifier-in-`.fsi` note (DF-1) is the sanctioned `.fsi` home for visibility, not an `.fs`
access modifier, so it does not breach Principle II. The forward-looking `Selected` branch (DF-2) and
the scoped restyle footprint (DF-3) are recorded as bounded follow-ups, not gate failures — the
projection is still total and deterministic over every state, and the at-rest/byte-identity proofs
cover every kind. Re-checked post-Phase-1 design below — unchanged: the design artifacts add no public
surface, no dependency, and no new behavior beyond what the existing suites pin.

## Project Structure

### Documentation (this feature)

```text
specs/096-runtime-visual-state-bridge/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — design decisions recovered from the imported implementation
├── data-model.md        # Phase 1 — the runtime-state vocabulary + projection/bridge semantics
├── quickstart.md        # Phase 1 — how to run + read the 096 validation
├── contracts/
│   └── runtime-bridge.md    # Phase 1 — the public projection + internal bridge contract the suites pin
├── checklists/
│   └── requirements.md  # Pre-existing requirements checklist
├── readiness/
│   ├── focus-survives-reshuffle.md   # Captured: E2 retained-id survival across a sibling shift (SC-007)
│   └── responds-proof.md             # Captured: bridged frame differs from inert build for the same input
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── ControlRuntime.fsi / ControlRuntime.fs   # Feature 096 — the bridge:
│                                             #   deriveVisualState (public projection, .fsi:96 / .fs:208,
│                                             #     closed precedence cascade .fs:208-222),
│                                             #   applyRuntimeVisualState (internal pre-reconcile stamp, .fsi:103);
│                                             #   feature-112 variants applyRuntimeVisualStateTargeted (.fsi:116),
│                                             #     runtimeStampFor (.fsi:129), RuntimeStampResult<'msg> (.fsi:76) — internal.
│                                             #   ControlRuntimeModel/Msg/Effect MVU types (.fsi:41/:53/:28)
├── Types.fsi / Types.fs              # VisualState DU (.fsi:256, 8 cases) consumed by the projection
├── Attributes.fs                     # visualState attribute builder (:72) — the single carrier
└── Control.fsi / Control.fs          # visualStateOf reader (.fsi:100 / .fs:96) — feature 093's carrier read

tests/Controls.Tests/
└── Feature096RuntimeBridgeTests.fs   # Feature096RuntimeBridgeTests (16) + Feature096BridgePropertyTests (3)
                                       #   + Gen096 generator (≥1000 cases) — Expecto + FsCheck

tests/Elmish.Tests/
└── Feature096LiveBridgeTests.fs      # Feature096LiveBridgeTests — live RetainedRender path (US4 / responds);
                                       #   regenerates readiness/focus-survives-reshuffle.md + responds-proof.md

tests/surface-baselines/
└── FS.GG.UI.Controls.txt             # Already lists ControlRuntime + ControlRuntimeModel/Msg/Effect (lines 61-89);
                                       #   deriveVisualState is the lone public function entry, zero-delta now
```

**Structure Decision**: Single F# project layout. 096 adds the projection + bridge to the existing
`ControlRuntime.fsi`/`.fs`, consuming the `VisualState` DU on `Types.fsi`, the `visualState` carrier on
`Attributes.fs`, and the `visualStateOf` reader on `Control.fsi` — no new project, no new package, and
(because the `ControlRuntime` module was committed at import) no new baseline delta. The bridge sits in
`ControlRuntime` beside the interaction MVU it projects from; the painting it drives is feature 093's
`Style.resolve` (E3) and the identity it rides is feature 092's keyed reconciler (E2) — both consumed,
neither duplicated.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The projection + bridge, the feature-112 targeted variants, the surface baseline entry, and the readiness evidence were imported wholesale in the rebranded-source migration; this spec/plan is authored afterward. | Re-deriving the bridge from a fresh spec would discard working, evidence-backed code, its committed surface baseline, and its readiness proofs. The backfill restores the chain at lower cost and risk. |
| `internal` qualifier on the bridge in `ControlRuntime.fsi` *(DF-1, disclosed)* | `applyRuntimeVisualState` and the feature-112 variants are declared `internal` in the `.fsi` so they are reachable from `Controls.Tests` via `InternalsVisibleTo` but absent from the public surface baseline. | This is the `.fsi` — the sanctioned single home for visibility under Principle II, which forbids access modifiers in `.fs`, **not** in `.fsi`. Declaring `internal` here (rather than splitting into a separate internal module) is the orthodox F# way to expose a test-only seam; disclosed for visibility, not a violation. |
| The `Selected` derivation branch is forward-looking on the live host *(DF-2, disclosed)* | `deriveVisualState` derives `Selected` from `model.Selection`, but the live host (`ControlsElmish`) populates focus/hover/press and not the text-range `Selection` today, so that branch is exercised by tests rather than the real render path. | Removing the branch would make a future host that *does* track selection require a code change to the projection; keeping it (and proving it via tests) lets selection light up without touching 096. The branch is total and deterministic regardless. Disclosed rather than hidden. |
| Five kinds opt into the visible restyle; further kinds are inert | 096 widened `button`/`slider`/`text-box`/`radio-group`/`switch` to show a runtime-state delta; `progress-bar`/`numeric-input` (and others) are stamped but render no delta. | Widening the visible restyle to further kinds is new styling work (feature 093 territory) outside this backfill's scope. Every kind is still correctly inert at `Normal` and byte-identical to its unset render; the stamp is uniform. Pinning further kinds is scoped as bounded follow-up (tasks.md **DF-3**), disclosed rather than hidden. |
