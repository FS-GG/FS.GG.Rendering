# Implementation Plan: Visual-State Style Layer (Feature 093)

**Branch**: `093-visual-state-style-layer` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/093-visual-state-style-layer/spec.md`

## Summary

Feature 093 introduces **one pure, total, deterministic resolver** — `Style.resolve`
(`src/Controls/Style.fsi`/`.fs`) — that folds the active **theme**, a per-kind **base style**, an
ordered list of attached **style classes** (typed `StyleVariant` or free-form `Custom`), and the
control's current **visual state** into a single flat `ResolvedStyle`, under one fixed precedence:
`baseStyle < each class in attach order < current visual state`, last-writer-wins per field. It
replaces the procedural, per-kind, inline-token styling that every migrated control previously
computed at its render site. The styling vocabulary (`VisualState`, `StyleVariant`, `StyleClass`,
`ResolvedStyle`, `ValidationState`) lives on `Types.fsi`; the styling intent rides a control through
the public attributes `Attr.styleClasses` and `Attr.visualState`.

**This is a backfill plan** — the same conformance-backfill pattern feature 091 established. The
implementation, the public `.fsi` surface, the committed surface-area baseline entries, the captured
readiness evidence (`readiness/parity/`), and four executable Expecto/FsCheck test suites
(`Feature093StyleResolverTests`, `Feature093StylePropertyTests`, `Feature093ParityTests`,
`Feature093RetainedStateTests`) **already exist** in the imported, rebranded source. No Spec Kit
spec/plan/tasks ever described them. This plan's job is to bring the work under the canonical
`Spec → .fsi → semantic tests → implementation` contract: it documents the design decisions already
embodied in the code, confirms the constitution gates the existing artifacts satisfy, and records
the honest import-before-spec deviation against Principle I. No new product behavior is designed
here; `/speckit-tasks` and `/speckit-implement` reduce to a **conformance pass** (confirm the suites
are green, the parity oracle matches, and the surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`, from `Directory.Build.props`), `LangVersion=latest`.

**Primary Dependencies**: Expecto (all four suites) + FsCheck (`Feature093StylePropertyTests` only,
≥1000 cases per property). The resolver itself depends only on the `Controls` package's own `Theme` /
`DesignTokens` (DTCG-generated token palette) and `Scene.Color`. No new runtime or package
dependency — 093 is pure folding over types already present.

**Storage**: N/A. `Style.resolve` is a pure value-to-value function; nothing is persisted. (US3's
state survival rides feature 092's retained structure, which 093 only consumes.)

**Testing**: Default-tier "Local inner loop" — `Controls.Tests`, reaching internals via
`[<assembly: InternalsVisibleTo("Controls.Tests")>]` where a suite needs the parity oracle or the
retained path. The styling surface under test (`Style.resolve`, the five styling types, the two
attributes) is **public**. The property suite runs its purity/determinism/precedence properties at
≥1000 generated cases via the `Gen093` generator module. Deterministic and offscreen — no GL context
required (structural scene equality, record comparison).

**Target Platform**: Linux/dev. 093's proofs are deterministic and headless: record comparison for
the resolver, structural scene equality against a frozen procedural oracle for parity, and the
in-process keyed reconciler for state survival. None require the GPU.

**Project Type**: F# UI framework — a public module (`Style`) plus public styling types
(`Types.fsi`) and public attributes (`Attributes.fs`) inside the `Controls` runtime library, with
its four semantic suites in the existing `Controls.Tests` assembly.

**Performance Goals**: No wall-clock target. `Style.resolve` is a bounded fold (a `List.fold` over
the attached classes followed by one state application); the measurable goals are **behavioral**:
purity/determinism over ≥1000 inputs (SC-004), structural scene-equal parity to the frozen
procedural baseline (SC-003), and exact identity `resolve theme base [] Normal = base` (SC-003/SC-004).

**Constraints**:
- **Total** over every input — every `StyleVariant`, any `Custom` string (unknown ⇒ identity delta,
  never an exception or a silent field drop), and all eight `VisualState` cases (FR-002, FR-004).
- **Pure/deterministic** — no clock, randomness, or side effects; identical inputs always produce an
  identical `ResolvedStyle` (FR-006).
- **Fixed precedence**, last-writer-wins per field: `baseStyle < classes-in-attach-order < state`
  (FR-003). The state layer is provably outermost.
- **Token-sourced colours only** — every colour the variant/state layers emit originates from the
  active `Theme`'s DTCG-generated tokens; **no inline colour literals** (FR-008).
- **Additive and scoped** — the resolver governs **paint and typography only**; geometry is computed
  as before. Default no-class output is structurally scene-equal to the prior procedural geometry for
  the migrated kinds (FR-007); unmigrated kinds are unaffected by an attached class.
- **Zero public-surface-baseline delta** — the resolver, types, and attributes are already in the
  committed `tests/surface-baselines/FS.GG.UI.Controls.txt`; the surface-drift check must pass
  unchanged. (Surface scope is a Tier-1 *characteristic*, but the delta this backfill adds is zero.)
- Render-output equivalence is judged by **structural scene equality** (the `DesignTokenParityTests`
  oracle technique); pixel-level / desktop-visibility proofs are explicitly out of scope, as the
  readiness evidence discloses.

**Scale/Scope**: One public module (`Style`), five public styling types on `Types.fsi`
(`VisualState`, `ValidationState`, `StyleVariant`, `StyleClass`, `ResolvedStyle`), two public
attributes (`Attr.styleClasses`/`Attr.visualState`) plus their `AttrValue` carriers
(`StyleClassesValue`/`VisualStateValue`), and four test suites. The resolver's body uses eight
`private` helpers (`isDark`, `successColor`, `warningColor`, `applyVariant`, `applyCustom`,
`applyClass`, `applyValidation`, `applyState`) — see Complexity Tracking on the `.fs`-access-modifier
deviation. **Migration footprint vs. spec scope:** the spec/FR-007 pins the parity *contract* to
`Button` and `CheckBox` (the only kinds with a frozen procedural oracle). The shipped code, however,
already calls `Style.resolve` from **six** controls — `Button` (`Control.fs:840`), `CheckBox`
(`:735`), `RadioGroup` (`:625`), `Slider` (`:662`), `Switch` (`:698`), and `TextBox` (`:1009`). The
extra four are exercised by the resolver/property/totality proofs but are **not** pinned by a
frozen-oracle parity test; this is recorded as a deviation (Complexity Tracking) and a bounded
follow-up, not silently absorbed.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it adds public API surface (the `Style`
module, five styling types, two attributes) and alters observable behavior (migrated controls now
paint via the resolver rather than inline tokens). The public-surface-baseline **delta is zero**
because the surface was already committed at import; that zero-delta is itself an asserted
requirement, validated by the surface-drift check. Per the vertical-slice rule, `Style.resolve` and
its public types/attributes — plus the in-assembly suites that reach the parity oracle and retained
path — are the user-reachable surface for these stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: code + `.fsi` + tests + surface baselines + readiness arrived together (the rebranded source migration). This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` (`Style.fsi`, `Types.fsi`), the public attributes, and the four FSI-reachable semantic suites already exist and exercise the real resolver. Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ✅ Pass | `Style.fsi` and `Types.fsi` are the sole declarations of the public surface (resolver, five types, attributes), all present in the committed baseline. The imported `Style.fs` previously carried redundant `private` access modifiers on its eight top-level helpers; **DF-1 is now resolved** — the modifiers have been stripped in this pass, so visibility is `.fsi`-driven alone (FS0078 is promoted to error per `Directory.Build.props`, keeping the helpers private by omission). Confirmed behavior-neutral by a clean build. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions + a single `List.fold` over classes then one state application. No SRTP, reflection, type providers, custom operators, or non-trivial computation expressions. `StyleVariant` is `[<RequireQualifiedAccess>]` over six discriminants — within Principle III's "simple discriminants" allowance. No mutation. |
| IV. Elmish/MVU boundary | ✅ N/A (pure) | `Style.resolve` is a pure, total value-to-value function with no state or I/O, so the MVU boundary does not apply to it. The state-survival story (US3) is delegated to feature 092's `RetainedRender` MVU boundary, which 093 only consumes; 093 adds no stateful workflow of its own. |
| V. Test evidence mandatory | ✅ Pass | Four suites: `Feature093StyleResolverTests` (SC-001 variant distinctness, SC-002 precedence/override), `Feature093StylePropertyTests` (SC-004 purity/determinism/outermost-state at ≥1000 cases via `Gen093`), `Feature093ParityTests` (SC-003 frozen-oracle byte parity for Button/CheckBox both themes, SC-007 unmigrated-kind no-delta; writes the six `readiness/parity/*.scene.txt`), `Feature093RetainedStateTests` (SC-005 survival across a position-shifting re-render via the live retained path). The readiness evidence honestly declares it proves structural scene equality, **not** pixels/desktop visibility. No suite weakens an assertion to green a build. |
| VI. Observability & safe failure | ✅ Pass | `Style.resolve` is total — an unknown `Custom` string yields an identity delta rather than throwing or dropping a field, and every `VisualState` (incl. `Validation _`) is matched. There is no failure path to swallow: a pure total fold cannot silently fail. Totality is pinned by the property suite. |

**Gate result**: PASS. One deviation remains (import-inverted order), justified and recorded; the
second (redundant `private` modifiers in `Style.fs`, formerly DF-1) has been **resolved in this pass**
by stripping the modifiers, confirmed behavior-neutral by a clean build. Neither is a public-contract
or test-evidence violation. The
migration-footprint-vs-spec-scope gap (six migrated controls, two parity-pinned) is recorded as a
bounded follow-up, not a gate failure — the four unpinned kinds are still covered by the
totality/purity proofs. Re-checked post-Phase-1 design below — unchanged: the design artifacts add no
public surface, no dependency, and no new behavior beyond what the existing suites pin.

## Project Structure

### Documentation (this feature)

```text
specs/093-visual-state-style-layer/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — design decisions recovered from the imported implementation
├── data-model.md        # Phase 1 — the styling vocabulary + resolver fold semantics
├── quickstart.md        # Phase 1 — how to run + read the 093 validation
├── contracts/
│   └── style-resolver.md    # Phase 1 — the public resolver contract the suites pin
├── readiness/
│   └── parity/          # Pre-existing captured evidence: six *.scene.txt frozen-oracle scenes
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── Style.fsi / Style.fs        # Feature 093 — the pure/total/deterministic resolver (resolve + 8 private helpers)
├── Types.fsi / Types.fs        # VisualState, ValidationState, StyleVariant, StyleClass, ResolvedStyle
├── Attributes.fs               # Attr.styleClasses / Attr.visualState (public builders) + AttrValue carriers
├── Control.fs                  # Migrated render sites calling Style.resolve: Button(840), CheckBox(735),
│                               #   RadioGroup(625), Slider(662), Switch(698), TextBox(1009)
└── DesignTokens / Theme        # DTCG-generated token palette every resolved colour is sourced from

tests/Controls.Tests/
├── Feature093StyleResolverTests.fs   # SC-001 / SC-002 — variant distinctness, precedence, class override (Expecto)
├── Feature093StylePropertyTests.fs   # SC-004 — purity/determinism/outermost-state ≥1000 cases (Expecto + FsCheck, Gen093)
├── Feature093ParityTests.fs          # SC-003 / SC-007 — frozen-oracle byte parity + unmigrated no-delta (Expecto)
└── Feature093RetainedStateTests.fs   # SC-005 — state survives a position-shifting re-render (Expecto, live retained path)

tests/surface-baselines/
└── FS.GG.UI.Controls.txt       # Already lists Style, the five styling types, and the two AttrValue carriers
```

**Structure Decision**: Single F# project layout. 093 adds the public `Style` module and styling
types/attributes to the existing `Controls` library and four suites to the existing `Controls.Tests`
assembly — no new project, no new package, and (because the surface was committed at import) no new
baseline delta. The resolver sits beside the `Theme`/`DesignTokens` it folds, and `ResolvedStyle` is
declared on `Types.fsi` **before** `Theme` so the overlapping bare field names
(`Foreground`/`FontFamily`/`FontSize`) resolve to `Theme` at the many unannotated `theme.*` render
sites (documented in `Style.fsi`).

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The resolver, types, attributes, surface baselines, and readiness evidence were imported wholesale in the rebranded-source migration; this spec/plan is authored afterward. | Re-deriving the module from a fresh spec would discard working, evidence-backed code, its surface baseline, and its parity oracle. The backfill restores the chain at lower cost and risk. |
| Redundant `private` modifiers on the eight helpers in `Style.fs` *(RESOLVED — DF-1)* | Inherited verbatim from the imported source; FS0078-as-error already makes `.fsi` the visibility authority, so they were harmless duplication. | **Resolved in this pass:** the eight `private` modifiers were stripped — a behavior-neutral Tier-2 edit confirmed by a clean `dotnet build src/Controls/Controls.fsproj` — so `Style.fs` now carries no access modifiers (Principle II clean). No longer an open deviation. |
| Six controls call `Style.resolve` but the spec pins parity to two (`Button`, `CheckBox`) | The import migrated `RadioGroup`/`Slider`/`Switch`/`TextBox` to the resolver too, but only `Button`/`CheckBox` have a frozen procedural oracle in `readiness/parity/`. | Forging frozen oracles for the other four now would be new verification work outside this backfill's scope. The four are still covered by the totality/purity/determinism proofs; pinning their parity is scoped as a bounded follow-up (tasks.md **DF-2**), disclosed rather than hidden. |
