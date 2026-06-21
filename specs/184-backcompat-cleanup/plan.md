# Implementation Plan: Backward-Compatibility Shim Removal

**Branch**: `184-backcompat-cleanup` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/184-backcompat-cleanup/spec.md`

## Summary

Remove four pieces of code that exist **only** to preserve backward compatibility with earlier
internal authoring patterns — no present consumer needs them. Each removal is independently shippable
and is verified against the premise "no in-tree consumer depends on this" (`src/` + 4 samples +
template) **before** deletion (FR-009).

| Story | Identity | Real classification (post-research) | Bump | Production path? |
|---|---|---|---|---|
| US1/P1 | `ScrollViewport.MaxOffset` alias | **Tier 1** — public `.fsi` field on a public type | `FS.GG.UI.Controls` | No (read-only field; 3 test readers) |
| US2/P2 | `Composition` legacy node-form layer | **Tier 2** — `module internal`; **not** on any public baseline | **none** | Yes (1 overlay caller → byte-stable) |
| US3/P3 | `ControlEvent.Payload` string field | **Tier 1** — public `.fsi` field on a public type | `FS.GG.UI.Controls` | Yes (~7 src readers + dual-set writers) |
| US4/P4 | Untyped flat-chart fallback | **Tier 2** — internal `chartValues` branch; observable | **none** | Yes (typed path byte-stable) |

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature carries **no defect/root-cause hypothesis**: it is a deletion/migration of dead
> compatibility surface that must not change any observed behavior on the retained paths. The
> early-live-smoke clause of the plan template is therefore resolved as **N/A**. `/speckit-tasks`
> MUST instead schedule **baseline capture** as the first Foundational task (snapshot the 12 surface
> baselines, capture the full Release `*.Tests.fsproj` sweep, and record the overlay-modifier chain
> bytes/fingerprint + chart-`chartValues` output for the typed path *before any edit*). Every story
> is then gated on (a) **byte-stable behavior** for retained production paths (FR-005), (b) an
> **intentional, exact** surface diff (the `.fsi` diff shows only the planned removal; the
> coarse baseline `.txt` is type-granular and will usually be unchanged — see research D2), and (c)
> the full sweep at the **same** red/green set as baseline (FR-011).

> **⚠ Classification refined from the spec — needs maintainer awareness, not a blocker.**
> The spec's "Change Classification" labels all four items **Tier 1 with `.fsi`/baseline updates +
> package bumps**. Phase-0 research (research.md, D1) found that premise holds only for **US1 and
> US3** (public record fields removed from public types). **US2's `Composition` module is
> `module internal`** (`Composition.fsi:9`) — its legacy identities never appear on the public
> surface, so removing them is a **Tier 2 internal cleanup with no public-surface change, no bump,
> and no CompatibilityLedger entry required** by the constitution (Tier 1 = *public* API change).
> **US4** removes an **internal** `chartValues` branch — also Tier 2 (observable-behavior, byte-stable
> retained path; no public surface, no bump). FR-006/FR-007 (baseline update + bump + ledger) bind
> **only the items that actually change public surface (US1, US3)**. This plan proceeds on that
> evidence-based reading; if the maintainer prefers a conservative bump for US2/US4, that is a
> one-line `.fsproj` edit added in Polish.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`Directory.Build.props`: `TargetFramework=net10.0`,
`FSharpLanguageVersion=latest`). `FS0025` (incomplete match) and `FS0078` (visibility modifier on a
top-level `.fs` binding) are **escalated to errors** — so deleting a `LegacyForm`/`NavPayload` case
that some `match` still handles, or leaving an access modifier on a `.fs` binding, fails the build.

**Primary Dependencies**: SkiaSharp over OpenGL + Silk.NET (viewer/host), Yoga (layout). **No new
dependency, project, or inter-project reference** (FR-011) — this feature only deletes/migrates.

**Storage**: No persisted/wire format is touched (the `SceneCodec` wire format is untouched — US2
edits only the in-memory `Composition` modifier IR). The durable artifacts that must stay stable:
the 12 surface baselines (expected **unchanged** — see research D2), the overlay-path modifier chain
(`Source`/`Effect` entries + `Composition.fingerprint`), and chart `chartValues` output for the typed
front door.

**Testing**: `dotnet test FS.GG.Rendering.slnx -c Release` under `DISPLAY=:1` (GL needs a display);
full sweep via `dotnet fsi scripts/baseline-tests.fsx --config Release --out <path>` (globs every
`*.Tests.fsproj` incl. Release-only `Package.Tests` and the sample lanes). Surface oracle:
`dotnet fsi scripts/refresh-surface-baselines.fsx` → `git diff readiness/surface-baselines/`
(type-granular; the binding surface evidence for field removals is the **`.fsi` diff**). Per-package
gate: `tests/*/PublicSurfaceTests.fs`, `tests/Package.Tests/SurfaceAreaTests.fs`. Feed/sample
alignment: `dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase`.

**Target Platform**: Linux desktop (SkiaSharp/GL under `DISPLAY=:1`); CI runs Debug build + tests.

**Project Type**: F# UI framework / library set built from `FS.GG.Rendering.slnx`. All four items live
in **one package, `FS.GG.UI.Controls`** (`src/Controls/`, currently `0.1.45-preview.1`). Consumers that
recompile/re-pin: `FS.GG.UI.Controls.Elmish` (`0.1.46-preview.1`, dual-set writers in US3), the 4
samples, and the template.

**Performance Goals**: N/A. No hot-path shape change; US2 replaces one helper call with a literal
record on a non-per-frame assembly path; US4 deletes two `match` arms.

**Constraints**:
- **Byte-stable behavior is binding (FR-005).** The overlay-path modifier chain (US2) and the typed
  chart `chartValues` output (US4) MUST be byte-identical to a baseline captured immediately before
  the edit. US2's migrated overlay entry MUST keep `Source = Composition.LegacyOverlaySource` and
  `Effect = LayerHint "overlay"` (the exact values `legacyLower LegacyOverlay` produced) so
  `Composition.fingerprint` is unchanged.
- **Intentional, exact surface change (FR-006).** US1/US3 remove exactly one public record field each
  from `Control.fsi`/`Types.fsi`; the `.fsi` diff MUST show only that. No other public type, value, or
  case may move. The coarse baseline `.txt` is expected unchanged (type-granular; research D2).
- **No assertion weakened (FR-008).** Tests asserting *removed* behavior (the `MaxOffset==MaxVerticalOffset`
  alias checks, the Feature-140 legacy-form lowering tests, the flat-list fallback test, the dual-set
  `Payload` assertions) are **deleted**, not loosened. Retained behavior keeps equivalent coverage on
  the typed/modern path.
- **FR-010 retentions.** Items merely *named* legacy/compat that are **live** are kept: the widget
  `*.create` builders, the SkiaViewer `LegacyHostMsg` pump, the `-v1`/`-v2` identity tags, the
  `ModifierSource.LegacyOverlaySource` provenance case (required for US2 byte-stability), and — by
  the same reasoning — the remaining `ModifierSource.Legacy*Source` cases unless a separate prune is
  scoped (research D3).
- **No new project/dependency/reference** (FR-011); dependency graph stays acyclic and unchanged.

**Scale/Scope** (verified at HEAD by the Phase-0 Explore passes — see [research.md](./research.md)):
- **US1**: `ScrollViewport.MaxOffset` at `Control.fsi:283` / `Control.fs:3083`, assigned
  `= extent.MaxVerticalOffset` at `Control.fs:3326` (literal duplicate). **3 test-only readers**
  (`Feature150ScrollViewerExtentTests.fs:16`, `Feature151ScrollViewerCorpusTests.fs:36`,
  `Feature137ClippingTests.fs:162`). No src/sample/template reader.
- **US2**: `Composition.fsi:125-139` / `Composition.fs:367-399` define `LegacyForm`,
  `LegacyCompatibilityStatus`, `legacyLower`, `compatibilityEvidence` — all in `module internal
  Composition`. **One production caller**: `Control.fs:2398-2402` (`compositionEntriesForControl`,
  overlay path). Feature-140 legacy test family
  (`Feature140LegacyCompatibilityTests.fs` + related) exercises the rest.
- **US3**: `ControlEvent.Payload : string option` at `Types.fsi:312-322` / `Types.fs:252-257`; typed
  replacement is `NavPayload` (`Types.fs:247-250`: `SteppedValue`/`MovedSelection`/`MovedCell`). **~7
  production-src readers** (`Interactive2.fs:6`, `Navigation2.fs:6`, `DataEntry2.fs:6`,
  `Widgets/WidgetLowering.fs:21/26`, `Control.fs:3408/3412/3415/3503`, `Widgets/DataGridWidget.fs:40`,
  `Widgets/Containers.fs:59`); **dual-set writers** in `Controls.Elmish/ControlsElmish.fs`
  (`dispatchBindings`@412/426-427, `dispatchNav`@941, plus `:558/863/954`) and `OverlayState.fs:537`;
  **6 test-only readers** (`TypedMigrationTests.fs:337/357`, `Feature100NavigationTests.fs:113/177/197`,
  `Feature144ProductOwnedVisibilityTests.fs:23`).
- **US4**: fallback branch at `Control.fs:482-483` (`float list`/`float array` → `indexed`), typed
  front door at `Control.fs:479-481` (`ChartSeries list`/`ChartPoint list`). **Zero in-tree authors of
  flat lists** (samples + template all use `LineChart.series`/`BarChart.series`/`PieChart.values` with
  typed lists) → **US4 is in-scope for removal** (FR-004 condition met). One fallback test:
  `Feature080ExtractionTests.fs:62-71`.

## Constitution Check

*GATE: evaluated before Phase 0 research; re-checked after Phase 1 design. Result: **PASS**.*

| Principle | Assessment |
|-----------|------------|
| **I. Spec → FSI → Semantic Tests → Implementation** | US1/US3 change the public `.fsi` (remove one field each) — the surface change is expressed in the `.fsi` first, exercised through the existing per-package `PublicSurfaceTests` + the migrated typed-payload tests, then implemented. US2/US4 change no public surface (internal). **Pass.** |
| **II. Visibility in `.fsi`, not `.fs`** | Removals delete from the `.fsi` (US1/US3 public; US2 internal-module signature). No access modifiers added to any `.fs` binding (FS0078=error). **Pass.** |
| **III. Idiomatic Simplicity** | The feature *is* this principle: collapse dual-path "old way or new way" ambiguity to one typed path. US2 replaces a helper indirection with a literal record; US3 routes every reader through the typed `NavPayload`; US4 deletes an untyped fallback. No SRTP/reflection/type-providers/new operators introduced. **Pass.** |
| **IV. Elmish/MVU boundary** | US3 edits dual-set sites inside the existing `Controls.Elmish` `update`/dispatch path; the `Model`/`Msg`/`Cmd` contract is unchanged (only the emitted `ControlEvent` loses a redundant field). No new stateful/I-O workflow. **Pass.** |
| **V. Test Evidence** | Each removal is gated on the full sweep staying at baseline red/green; US2/US4 add/keep a byte-stability check on the retained path. Tests for removed behavior are **deleted, not weakened** (FR-008); pre-existing reds recorded as not-regression. **Pass.** |
| **VI. Observability & Safe Failure** | No diagnostic/evidence/damage emission path changes value (US2 overlay chain byte-stable; US4 typed output byte-stable). No silent failure introduced. **Pass.** |

**Change Classification**: **Mixed, per item** (see the refined table in Summary). US1 + US3 are
**Tier 1** (public `.fsi` field removal → bump `FS.GG.UI.Controls` + CompatibilityLedger entry).
US2 + US4 are **Tier 2** (internal; no public surface change → no bump/ledger required). This refines
the spec's blanket Tier-1 framing on the evidence that `Composition` is `module internal` and the
chart fallback is an internal branch. **No constitution violations → Complexity Tracking table
omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/184-backcompat-cleanup/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — the 4 decisions (per-item tier, baseline granularity, US2
│                        #   byte-stable migration + ModifierSource retention, US4 descope verdict)
├── data-model.md        # Phase 1 — the 4 deprecated identities (before/after), consumer ledger
├── quickstart.md        # Phase 1 — baseline-capture + per-story removal + verify + bump/feed guide
├── contracts/           # Phase 1 — the binding invariants
│   ├── removal-invariance.md    # byte-stable retained path + exact-.fsi-diff + same-red/green oracle (FR-005/006/011)
│   ├── us1-maxoffset.md          # US1 — field removal, 3 test retargets, bump (FR-001)
│   ├── us2-composition-legacy.md # US2 — internal layer removal, overlay migration byte-stability (FR-002)
│   ├── us3-controlevent-payload.md # US3 — typed-payload migration of every reader, field removal, bump (FR-003)
│   └── us4-flatchart-fallback.md # US4 — descope verdict + branch removal, typed byte-stability (FR-004)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/                            # ALL four items — FS.GG.UI.Controls (bump for US1/US3 only)
├── Control.fsi / Control.fs             # EDIT — remove ScrollViewport.MaxOffset (US1, public field);
│                                         #   migrate compositionEntriesForControl off legacyLower (US2);
│                                         #   remove chart flat-list fallback arms @482-483 (US4)
├── Composition.fsi / Composition.fs     # EDIT — delete LegacyForm/LegacyCompatibilityStatus/
│                                         #   legacyLower/compatibilityEvidence (US2, internal module).
│                                         #   RETAIN ModifierSource.LegacyOverlaySource (byte-stability)
├── Types.fsi / Types.fs                 # EDIT — remove ControlEvent.Payload field (US3, public field)
├── Interactive2.fs / Navigation2.fs /   # EDIT — onPayload helpers → typed NavPayload accessor (US3)
│   DataEntry2.fs
├── Widgets/WidgetLowering.fs /          # EDIT — onString/onStringList + selection/float adapters →
│   Widgets/DataGridWidget.fs /          #   typed NavPayload (US3)
│   Widgets/Containers.fs
├── OverlayState.fs                      # EDIT — drop Payload from event construction (US3 writer)
└── Controls.fsproj                      # EDIT — <Version> bump 0.1.45→0.1.46 (US1/US3 land)

src/Controls.Elmish/                     # US3 writers — FS.GG.UI.Controls.Elmish
├── ControlsElmish.fs                    # EDIT — dispatchBindings/dispatchNav stop dual-setting Payload;
│                                         #   emit only Nav (typed). Public .fsi unchanged (writers internal)
└── Controls.Elmish.fsproj               # EDIT — re-pin to bumped Controls; bump only if its own surface moves

tests/Controls.Tests/                    # EDIT/DELETE
├── Feature150ScrollViewerExtentTests.fs # EDIT — retarget MaxOffset → MaxVerticalOffset (US1)
├── Feature151ScrollViewerCorpusTests.fs # EDIT — retarget MaxOffset → MaxVerticalOffset (US1)
├── Feature137ClippingTests.fs           # EDIT — retarget MaxOffset → MaxVerticalOffset (US1)
├── Feature140LegacyCompatibilityTests.fs# DELETE — asserts removed legacy-form lowering (US2, FR-008)
├── TypedMigrationTests.fs               # EDIT — read NavPayload instead of Payload (US3)
└── Feature080ExtractionTests.fs:62-71   # DELETE that one test — flat-list fallback removed (US4, FR-008)

tests/Elmish.Tests/                      # EDIT
├── Feature100NavigationTests.fs         # EDIT — assert via NavPayload (US3)
└── Feature144ProductOwnedVisibilityTests.fs # EDIT — assert via NavPayload (US3)

readiness/surface-baselines/
└── FS.GG.UI.Controls.txt                # expected UNCHANGED (type-granular; field removals not captured).
                                          #   The .fsi diff is the surface evidence. Regenerate to confirm.

specs/184-backcompat-cleanup/readiness/  # local (.gitignore'd), per repo convention
├── baseline/                            # pre-edit: 12 baselines + full sweep + overlay-chain + chartValues
├── compatibility-ledger.md             # US1 + US3 ledger entries (removed field + migration)
└── post-change/                         # post-edit: same, diffed (behavior byte-identical; .fsi = planned)
```

**Structure Decision**: Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). All edits are
in `src/Controls/` (+ `src/Controls.Elmish/` for US3 writers) — no new file, project, package
dependency, or inter-project reference (FR-011). The one cross-package consumer of a removed identity
is `Controls.Elmish` (US3 writers), which re-pins to the bumped `Controls`.

## Sequencing & Independence

Four stories map to spec priorities (lowest surface risk / highest confidence first). Each is
**independently shippable** and shares **one** baseline captured up front (mirrors 179–183):

1. **Setup** — create `specs/184-…/readiness/`; snapshot all 12 `surface-baselines/*.txt`; run the
   full Release `*.Tests.fsproj` sweep into `baseline/`; record the overlay-path modifier chain
   (`Source`/`Effect` + `Composition.fingerprint`) and the typed-chart `chartValues` output.
2. **Foundational (GATE)** — record the allowed pre-existing reds (`Package.Tests` 8-fail,
   `ControlsGallery` 2-fail — stale-feed, per `specs/182/183 known-reds.md`) as baseline-not-regression;
   resolve early-live-smoke as N/A; lock the removal-invariance contract
   ([contracts/removal-invariance.md](./contracts/removal-invariance.md)); confirm the US4 descope
   verdict (FR-004 — zero flat-list authors found) and the per-item tier (research D1). No code edits.
3. **US1 / P1 — remove `MaxOffset`** (Tier 1): delete the field from `Control.fsi`/`Control.fs`
   (incl. the `= extent.MaxVerticalOffset` assignment @3326); retarget the 3 test readers to
   `MaxVerticalOffset`. Build + full sweep + `.fsi` diff (only `MaxOffset` line gone) + baseline
   regen (unchanged). MVP — validates the whole approach end-to-end. SC-001/002/004.
4. **US2 / P2 — retire the `Composition` legacy layer** (Tier 2): migrate `compositionEntriesForControl`
   off `legacyLower` to the literal entry `[{ Source = LegacyOverlaySource; Effect = LayerHint
   "overlay" }]`; delete `LegacyForm`/`LegacyCompatibilityStatus`/`legacyLower`/`compatibilityEvidence`
   from `Composition.fsi`/`.fs`; delete `Feature140LegacyCompatibilityTests.fs`. Build + full sweep +
   **overlay modifier-chain byte-diff = identical** (same `Source`/`Effect`/fingerprint). No bump.
   SC-001/003/004/007.
5. **US3 / P3 — retire `ControlEvent.Payload`** (Tier 1): migrate every reader to `NavPayload` (or a
   typed accessor); stop dual-setting in `Controls.Elmish`/`OverlayState`; delete the field from
   `Types.fsi`/`Types.fs`; migrate the 6 test readers. Build + full sweep + `.fsi` diff (only `Payload`
   gone) + bump `FS.GG.UI.Controls`. SC-001/002/004/007.
6. **US4 / P4 — remove the flat-chart fallback** (Tier 2): delete arms `Control.fs:482-483`; delete the
   one fallback test. Build + full sweep + **typed-path `chartValues` byte-diff = identical**. No bump.
   SC-001/003/004.
7. **Polish** — full `dotnet build` + `dotnet test`; write the CompatibilityLedger (US1 + US3 entries);
   bump `Controls` once (covers US1+US3) and align the feed + actively-maintained sample
   (`dev-repack.fsx --sample samples/SecondAntShowcase`) + template pins; capture `post-change/`;
   verify SC-001…SC-007; record every FR-010 retention with rationale; confirm only the `.fsi`s for
   US1/US3 changed publicly and the dependency graph is unchanged (FR-011).

Stories may land in any order, but US1 first (cleanest, validates the pipeline) and US3 last among the
Controls-public edits keep each `Control.fsi`/`Types.fsi` diff a single clean line. US1, US3, US4 all
touch `Control.fs` — serialize them so each produces one reviewable diff. A single `Controls` bump in
Polish covers both US1 and US3.

## Done When

- [x] Plan workflow executed; design artifacts generated (research, data-model, contracts, quickstart).
- [x] Each story has a contract pinning its behavior oracle + (for US1/US3) its exact `.fsi` diff + bump.
- [x] Per-item tier refined from evidence (US2/US4 internal) and flagged for maintainer awareness.
- [x] CLAUDE.md SpecKit marker points at this plan.

## Complexity Tracking

> No Constitution Check violations — table omitted.
