# Implementation Plan: Type-Safety Hardening (Code-Health Refactoring Phase 6)

**Branch**: `183-type-safety-hardening` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/183-type-safety-hardening/spec.md`

## Summary

Phase 6 — the **final** code-health item — removes the three remaining places where correctness is
maintained by hand instead of by the compiler, so that adding a control kind or a `SceneNode` case
becomes a single-site, compiler-checked change:

| Story | Sub-goal | Tier | Bump | Seam |
|---|---|---|---|---|
| US1/P1 | **Control `Kind` registry** | Tier 2 (internal) | none | Collapse the ~13 parallel `Kind`-keyed dispatch sites across `Control.fs`/`Inspection.fs`/`Accessibility.fs`/`Catalog.fs`/`ControlRuntime.fs`/`RetainedRender.fs` into one **internal** registry table keyed by the existing `string` kind. `Control.Kind: string` is unchanged → no surface change, no bump. |
| US2/P2 | **`SceneNode` codec symmetry** | Tier 1 (mild) | Scene | Per-case write/read codec **table** so a missing case is a build/test failure, not silent drift, over the **frozen** wire format (tags 0–24). Normalize the 19 bare-tuple `SceneNode` cases to **named fields preserving exact arity and types** (source-compatible). |
| US3/P3 | **Named flag records** | Tier 1 | Scene, SkiaViewer | Replace positional `bool`/positional tails on `validateDamage`, `classifyWindowObservation`, `damageRegion`, `promotionDecision`, `damageRegionSet`, `popoverGeom` with small named flag/parameter records. |

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature carries **no defect/root-cause hypothesis**: it is a representation/typing refactor that
> must not change any observed behavior. The early-live-smoke clause of the plan template is therefore
> resolved as **N/A**. `/speckit-tasks` MUST instead schedule **baseline capture** as the first
> Foundational task: snapshot the 12 surface baselines, capture the full Release `*.Tests.fsproj` sweep
> (`scripts/baseline-tests.fsx`), and record codec round-trip bytes + scene hashes/fingerprints + damage
> regions for the touched subsystems *before any edit*. Every story is then gated on (a) **behavior**
> byte-stability (codec bytes, rendered output, evidence verdicts, hashes, damage regions, metrics
> byte-identical), (b) an **intentional, exact** surface diff (the regenerated baseline + `.fsi` diff
> shows only the planned change), and (c) the full sweep at the **same** red/green set as baseline.

> **Carried lesson from Phases 3–5 (180/181/182).** An abstraction is only worth it when it removes a
> genuine hazard. Here the justification is **restored compiler enforcement** (exhaustiveness for kinds,
> symmetry for the codec, named-at-call-site for the flags), **not** line reduction (net lines may rise
> from the registry table and the flag-record types — acceptable, SC is not a line metric). Each sub-goal
> is unified only if behavior stays byte-stable; anything that would change behavior, the wire format, or
> require a back-edge/new dependency is retained explicit per FR-010.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`Directory.Build.props`: `TargetFramework=net10.0`,
`FSharpLanguageVersion=latest`). `FS0025` (incomplete match) and `FS0078` (visibility modifier on a
top-level `.fs` binding) are **escalated to errors** — so an unhandled DU case on the **write** path
already fails to compile; the **read** path's `| tag -> failwithf` wildcard is the symmetry gap US2
closes.

**Primary Dependencies**: SkiaSharp over OpenGL + Silk.NET (viewer/host), Yoga (layout). No new
dependency, project, or inter-project reference (FR-011).

**Storage**: The `SceneNode` binary **wire format is frozen** (`SceneCodec.fs`, tags 0–24, sequential
DU order) — replay/persisted caches depend on byte-identical serialization. Durable artifacts that must
stay stable: codec wire bytes + round-trip values, the 12 surface baselines (intentionally changing for
Scene/SkiaViewer only), regenerated readiness/evidence artifacts, scene hashes/fingerprints, damage
regions.

**Testing**: `dotnet test FS.GG.Rendering.slnx -c Release` under `DISPLAY=:1` (GL needs a display); full
sweep via `dotnet fsi scripts/baseline-tests.fsx --config Release --out <path>` (globs every
`*.Tests.fsproj` incl. Release-only `Package.Tests` and the sample lanes). Surface oracle:
`dotnet fsi scripts/refresh-surface-baselines.fsx` → `git diff readiness/surface-baselines/`. Per-package
gate: `tests/*/PublicSurfaceTests.fs`, `tests/Package.Tests/SurfaceAreaTests.fs`.

**Target Platform**: Linux desktop (SkiaSharp/GL under `DISPLAY=:1`); CI runs Debug build + tests.

**Project Type**: F# UI framework / library set built from `FS.GG.Rendering.slnx`. Units of change:
`src/Controls/` (US1 + the Controls-side of US2/US3), `src/Scene/` (US2 + US3), `src/SkiaViewer/` (US3).

**Performance Goals**: N/A as a target. The registry lookup replaces ~13 `match`/`Set.contains` sites;
it MUST NOT add per-frame allocation on the hot paths (`RetainedRender.countVirtual`, `paintLeaf`) — the
registry is built once (module-level, eager) and read by `Map`/dictionary lookup, not rebuilt per node.

**Constraints**:
- **Behavior byte-stability is binding.** Codec wire bytes + round-trips, rendered output,
  evidence/readiness verdicts, scene hashes/fingerprints, damage regions, and metrics MUST be
  byte-identical to a baseline captured immediately before the change. When type-safety and byte-stable
  behavior conflict, **byte-stable behavior wins** (FR-005); the offending part is retained per FR-010.
- **Frozen wire format.** Named-field normalization and the codec table MUST NOT change the serialized
  bytes: tag values (0–24), field order, and primitive encodings are fixed. A format change is out of
  scope (Out of Scope, FR-010).
- **Source-compatible DU normalization.** Named fields are added **preserving exact arity and field
  types** (e.g. `Rectangle of (float*float*float*float)*Color` → `Rectangle of bounds:(float*float*float*float) * fill:Color`,
  **not** flattened to 5 fields, **not** retyped to `Rect`). F# positional construction and positional
  matching stay valid, so consumers, samples, the template, and generated products recompile unchanged.
- **Intentional, exact surface change.** Only `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer` change public
  surface (US2 DU field names + US3 `damageRegion`/`validateDamage`/`classifyWindowObservation`). The
  regenerated baseline diff + `.fsi` diff MUST show **only** the planned changes (FR-006). US1 and the
  internal flag functions (`promotionDecision`/`damageRegionSet`/`popoverGeom`) leave the public baseline
  unchanged.
- **No new project/dependency/reference** (FR-011); dependency graph stays acyclic and unchanged.

**Scale/Scope** (verified at HEAD by the Phase-0 Explore passes — see [research.md](./research.md)):
- **US1**: ~13 `Kind`-keyed dispatch sites (`Control.fs` @502/606/613/1930/2050/2157/2351/2356/2413,
  `Inspection.fs` @48/68/89/161, `Accessibility.fs` @28, `Catalog.fs` @501, `ControlRuntime.fs` @373,
  `RetainedRender.fs` @1732); ~98 kinds (catalog SSOT in `Catalog.fs`); two family sets `richFamilies`
  (~51) / `chartFamilies` (~19). **All dispatch helpers are private/internal — none in any `.fsi`.**
- **US2**: `SceneNode` @`Scene.fs:391` — 25 cases (19 bare-tuple to name, 6 already named), public in
  `Scene.fsi`. `writeSceneNode`@761 / `readSceneNode`@877 are **private** (not in `SceneCodec.fsi`), tags
  0–24. ~95 `writeX`/`readX` pairs; 3 `writeXOption` near-clones over generic `writeOption`.
- **US3**: `validateDamage` (public, `OpenGl.fsi:299`, 5 bools, 1 internal call site), `classifyWindowObservation`
  (public, `SkiaViewer.fsi:118`, 2 bool + 2 bool-option, test-only call sites), `damageRegion` (public,
  `Scene.fsi:1276`, 10 positional, **cross-package** call at `Controls/Inspection.fs:460` + tests),
  `promotionDecision`/`damageRegionSet` (`internal`, `RetainedRender.fsi`, internal + test call sites),
  `popoverGeom` (private, `Control.fs:1755`, 3 internal call sites).

## Constitution Check

*GATE: evaluated before Phase 0 research; re-checked after Phase 1 design. Result: **PASS**.*

| Principle | Assessment |
|-----------|------------|
| **I. Spec → FSI → Semantic Tests → Implementation** | US2/US3 *do* change public surface (Scene/SkiaViewer `.fsi`), so the FSI-first order applies for real this time: the new named-field DU and the flag-record signatures are drafted in the `.fsi`, exercised through the existing per-package `PublicSurfaceTests`/round-trip suites, then implemented. US1 introduces no public surface (internal registry). **Pass.** |
| **II. Visibility in `.fsi`, not `.fs`** | The registry, the codec table, and the internal flag functions declare visibility via `module internal`/`val internal` or omission from the `.fsi`, never via access modifiers on `.fs` bindings (FS0078=error enforces this). Public changes are expressed by editing the `.fsi` (DU field names, public flag-record signatures) and regenerating the baseline. **Pass.** |
| **III. Idiomatic Simplicity** | The feature *is* this principle: a registry record, a codec table, and small flag records replace stringly-typed switches, hand-symmetry, and boolean traps. The DU normalization takes the **minimal** source-compatible form (name, don't flatten/retype) — the simplest change that achieves "named fields throughout." No SRTP/reflection/type-providers introduced; the registry is a plain `Map`. **Pass.** |
| **IV. Elmish/MVU boundary** | No `Model`/`Msg`/`Cmd` contract touched; no new stateful/I-O workflow. **Pass.** |
| **V. Test Evidence** | Behavior is preserved; the existing round-trip / rendering / inspection / damage suites plus the codec-byte and surface-baseline diffs are the evidence. US2 adds an **every-case codec round-trip test** as the read-side symmetry guard. No assertion weakened; pre-existing reds recorded as not-regression. **Pass.** |
| **VI. Observability & Safe Failure** | All diagnostic/evidence/damage emission paths preserved verbatim (byte-stable, FR-005). The flag records make the *call sites* safer (no silent flag transposition) without changing emitted values. **Pass.** |

**Change Classification**: **Tier 1** overall (US2/US3 intentionally change `FS.GG.UI.Scene` /
`FS.GG.UI.SkiaViewer` public surface and bump those packages); **US1 is Tier 2** (internal only, no
bump). This is the deliberate, maintainer-confirmed difference from Phases 0–5. Per the Constitution,
Tier 1 requires the surface change be expressed in the `.fsi`, captured in the regenerated baseline, and
the affected packages bumped with the feed/samples aligned (FR-007). **No constitution violations →
Complexity Tracking table omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/183-type-safety-hardening/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — the 5 design decisions (registry shape, DU-normalization form,
│                        #   codec-table mechanism, flag-record shapes, bump/cascade set)
├── data-model.md        # Phase 1 — ControlKindRegistry record, SceneNodeCodec table, flag records,
│                        #   normalized SceneNode DU (per-case before/after)
├── quickstart.md        # Phase 1 — baseline-capture + behavior-byte-diff + surface-diff + bump/feed guide
├── contracts/           # Phase 1 — per-story contracts (the binding invariants)
│   ├── behavior-invariance.md   # the byte-stable-behavior + intentional-exact-surface oracle (FR-005/006)
│   ├── kind-registry.md         # US1 — registry record, the ~13 dispatch sites, internal-only (FR-001)
│   ├── scenenode-codec.md       # US2 — frozen wire format, per-case table, source-compatible naming (FR-002/003)
│   └── flag-records.md          # US3 — the 6 functions, public/internal split, bump set (FR-004)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/                            # US1 (+ Controls side of US2/US3) — FS.GG.UI.Controls
├── ControlKindRegistry.fs (+ internal)  # NEW (US1) — internal registry table; inserted BEFORE Control.fs
│                                         #   in Controls.fsproj <Compile Include> order
├── Control.fs / Inspection.fs /         # EDIT — ~13 Kind-dispatch sites → registry lookups (US1);
│   Accessibility.fs / Catalog.fs /      #   popoverGeom flag record (US3)
│   ControlRuntime.fs / RetainedRender.fs#   promotionDecision/damageRegionSet flag records (US3, internal)
├── *.fsi                                 # UNCHANGED public surface (US1 internal; internal val sigs may
│                                         #   change text but not the public baseline)
└── Inspection.fs:460                     # EDIT — updated call to Scene.damageRegion's new signature (US3)

src/Scene/                               # US2 + US3 — FS.GG.UI.Scene (.fsi CHANGES → bump)
├── Scene.fs                              # EDIT — SceneNode DU named fields (US2); damageRegion param record (US3)
├── Scene.fsi                             # EDIT — DU field names + damageRegion signature (reviewed surface diff)
├── SceneCodec.fs                         # EDIT — per-case write/read table; every-case symmetry (US2, internal)
└── Scene.fsproj                          # EDIT — <Version> bump

src/SkiaViewer/                          # US3 — FS.GG.UI.SkiaViewer (.fsi CHANGES → bump)
├── Host/OpenGl.fs / OpenGl.fsi           # EDIT — validateDamage flag record (public)
├── SkiaViewer.fs / SkiaViewer.fsi        # EDIT — classifyWindowObservation flag record (public)
└── SkiaViewer.fsproj                     # EDIT — <Version> bump

readiness/surface-baselines/
├── FS.GG.UI.Scene.txt                    # regenerated — reviewed intentional diff (US2/US3)
├── FS.GG.UI.SkiaViewer.txt               # regenerated — reviewed intentional diff (US3)
└── (other 10)                            # UNCHANGED

specs/183-type-safety-hardening/readiness/
├── baseline/                            # pre-edit: 12 baselines + full sweep + codec bytes/hashes/damage
└── post-change/                         # post-edit: same, diffed (behavior byte-identical; surface = planned)
```

**Structure Decision**: Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). The one new
file (`ControlKindRegistry.fs`) is added **inside `src/Controls/`**, inserted into the `.fsproj`
`<Compile Include>` order *before* `Control.fs`/`Inspection.fs`/etc. so they reference the registry with
no back-edge (F# file-order rule). US2/US3 edit existing files in place. No new project, package
dependency, or inter-project reference (FR-011).

## Sequencing & Independence

Three stories map to spec priorities (highest day-to-day hazard / lowest surface risk first). Each is
**independently shippable** — none depends on another — and shares **one** baseline captured up front
(mirrors features 179–182):

1. **Setup** — create `specs/183-…/readiness/`; capture the pre-edit baseline: snapshot all 12
   `surface-baselines/*.txt`; run the full Release `*.Tests.fsproj` sweep into `baseline/`; record codec
   round-trip bytes for representative scenes, scene hashes/fingerprints, and damage regions for the
   touched subsystems.
2. **Foundational (GATE)** — record the allowed pre-existing reds (`Package.Tests` 8-fail,
   `ControlsGallery` 2-fail — stale-feed, per `specs/182-…/readiness/baseline/known-reds.md`) as
   baseline-not-regression; resolve early-live-smoke as N/A; lock the behavior-invariance + exact-surface
   contract ([contracts/behavior-invariance.md](./contracts/behavior-invariance.md)). No code edits.
3. **US1 / P1 — Control `Kind` registry** (Tier 2): add `ControlKindRegistry.fs`; replace the ~13
   dispatch sites with registry lookups preserving every default/fallthrough. Build + full test +
   surface-diff (`FS.GG.UI.Controls.txt` **unchanged**) + control scene-hash/fingerprint/inspection/a11y
   byte-diff. **MVP — independently shippable, no bump** (SC-001/004/005).
4. **US2 / P2 — `SceneNode` codec symmetry** (Tier 1, mild): name the 19 bare-tuple cases (arity/types
   preserved); convert `writeSceneNode`/`readSceneNode` to a per-case table with an every-case round-trip
   symmetry guard. Build + full test + **codec wire-byte diff = identical** + `Scene.fsi`/baseline diff =
   only field names + bump `FS.GG.UI.Scene`. SC-002/004/007.
5. **US3 / P3 — named flag records** (Tier 1): convert the 6 functions; public ones
   (`validateDamage`/`classifyWindowObservation` → SkiaViewer; `damageRegion` → Scene, update the
   cross-package call at `Controls/Inspection.fs:460`) bump their packages; internal/private ones change
   no public baseline. Build + full test + diagnostic/damage/promotion byte-diff + reviewed surface diffs
   + bump `FS.GG.UI.SkiaViewer` (and `FS.GG.UI.Scene` if not already bumped by US2). SC-003/004/005/007.
6. **Polish** — full `dotnet build` + `dotnet test`; align the feed + actively-maintained sample
   (`dev-repack.fsx --sample samples/SecondAntShowcase`) + template pins for the bumped packages; capture
   `post-change/`; verify SC-001…SC-008; record every FR-010 retention with rationale; confirm only
   Scene/SkiaViewer baselines changed and the dependency graph is unchanged (FR-011).

Stories may land in any order; US1 is sequenced first (zero surface risk, highest payoff). US2 and US3
both touch `src/Scene/` — serialize them so each produces one clean, reviewable `FS.GG.UI.Scene.txt`
surface diff (US2 = field names, US3 = `damageRegion` signature).

## Done When

- [x] Plan workflow executed; design artifacts generated (research, data-model, contracts, quickstart).
- [x] Each sub-goal has a contract pinning its behavior oracle + (for US2/US3) its exact surface diff + bump.
- [x] CLAUDE.md SpecKit marker points at this plan.

## Implementation Progress (2026-06-21)

All three stories implemented, verified byte-stable, and committed on `183-type-safety-hardening`:

| Story | Status | Evidence |
|---|---|---|
| **US1** Control `Kind` registry (Tier 2, no bump) | ✅ done | `src/Controls/ControlKindRegistry.fs` SSOT (functions + 96-entry Map); ~9 dispatch sites migrated (`Control.fs`/`Inspection.fs`/`Accessibility.fs`/`ControlRuntime.fs`/`RetainedRender.fs`). Controls.Tests **933 pass** incl. SC-001 completeness guard. `Controls.txt` + Controls `.fsi` **unchanged**. |
| **US2** `SceneNode` codec symmetry + DU naming (Tier 1, bump Scene → 0.1.37) | ✅ done | Read driven by per-case `readerByTag` table (25 rows, tags 0–24); `writeSceneNode` stays exhaustive (FS0025 gate), byte-unchanged. 19 bare-tuple cases named (source-compatible). Scene.Tests **75 pass** incl. SC-002 (frozen-SHA byte gate + all-25 round-trip). Surface diff = only the 19 `Scene.fsi` field names. |
| **US3** Named flag records (Tier 1, bump SkiaViewer → 0.1.47) | ✅ done | `DamageValidationFlags`, `WindowObservationInputs`, `DamageNodeCounts` (public); `PromotionInputs`/`DamageSetInputs` (internal); `PopoverKind` (private). All ~45 call sites updated. Surface diff = exactly the 3 planned public types; Controls + other 9 baselines unchanged. |

**Gates met:** behavior byte-stable (FR-005) — codec wire bytes (SHA), scene hashes, damage/diagnostic
outputs, and the test red/green set all unchanged; surface change intentional/minimal/exact (FR-006) —
only `FS.GG.UI.Scene` + `FS.GG.UI.SkiaViewer` moved, as planned; no new project/dependency/reference
(FR-011). FR-010 retentions recorded in `readiness/post-change/retentions.md` (painter dispatch,
required-attr validation, `clipStatusOf`, the `transform` substring probe, the optional option-codec
fold). 

**Polish (T033–T038) complete.** Full Release sweep reproduces the baseline red/green **exactly**
(14 green; `Package.Tests` 8-fail, `ControlsGallery` 2-fail — SC-006). Feed refreshed and
`samples/SecondAntShowcase` re-pinned + restored at `0.1.47-preview.1`; its **171 tests pass** against
the freshly-packed bumped packages (FR-007/SC-007). Behavior corpus byte-identity is asserted by the
green suite + the codec SHA gate (SC-004). **SC-001…SC-008 all hold:** SC-001 catalog↔registry
completeness (Controls.Tests), SC-002 every-case codec round-trip (Scene.Tests), SC-003 named flag
records, SC-004 behavior byte-stable, SC-005 surface intentional/minimal/exact, SC-006 same red/green,
SC-007 feed/sample aligned, SC-008 dependency graph acyclic & unchanged.

> Readiness evidence (`specs/183-…/readiness/baseline|post-change/`: test baselines, known-reds,
> retentions ledger) is kept local per the repo convention (`.gitignore: specs/*/readiness/`, as in
> features 179–182) — it is not part of the merge.

## Complexity Tracking

> No Constitution Check violations — table omitted.
