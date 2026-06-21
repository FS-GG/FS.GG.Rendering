# Implementation Plan: Per-Feature Data-Table Refactor (Code-Health Refactoring Phase 4)

**Branch**: `181-feature-data-table-refactor` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/181-feature-data-table-refactor/spec.md`

## Summary

Phase 4 of the whole-repo code-health refactoring converts the harness's per-feature **copy-forward
code into data**. The rendering harness (`tools/Rendering.Harness/`) accreted a near-identical family of
report renderers, CLI handlers, and compatibility tests as features 148–161 shipped. Today, adding a
feature means copying an entire family of `renderFeatureNNN…` functions + a constant quintet + a CLI
handler + a `Feature###Compatibility*Tests.fs` file and editing the numbers. The goal is to make a new
per-feature addition a **single descriptor entry** plus only the genuinely-unique logic, with **zero**
observable change to harness output.

The work lands in three independently-shippable stories, in priority order:

1. **US1 / P1 — Descriptor + generic renderer.** Introduce one `FeatureDescriptor` record (the single
   source of truth) and route the *structurally-repeated* report variants in `Compositor.fs` through a
   generic, descriptor-driven renderer. The constant quintets (`feature###ReadinessDirectory` /
   `…ParityDirectory` / `…TimingDirectory` / `…CompatibilityLedgerPath` / `…ValidationSummaryPath`, ×12)
   are derived from the descriptor instead of hand-declared.
2. **US2 / P2 — Descriptor-keyed CLI table.** Replace the `isFeature###` + `if/elif` dispatch chains in
   `Cli.fs` with a command table keyed by the descriptor, and extract the shared
   performance/readiness runner bodies into one reusable body.
3. **US3 / P3 — Data-driven compatibility tests.** Collapse the per-feature
   `Feature###Compatibility*Tests.fs` family in `tests/Package.Tests/` into one data-driven `testList`
   parameterized over the descriptor set (its `requiredHeaders`/token expectations carried as data).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature carries **no defect/root-cause hypothesis** to confirm against a running app: it is a
> pure structural refactor of an internal tool that must not change any observed output. The
> early-live-smoke clause of the plan template is therefore resolved as **N/A**. `/speckit-tasks` MUST
> instead schedule **baseline capture** as the first Foundational task: run the full harness output
> sweep + `dotnet test` *before any edit*, archive every generated readiness artifact, and gate every
> story on a **byte-for-byte diff** of regenerated artifacts plus a full `dotnet test` showing no new
> failures vs. baseline. Never green a build by weakening an assertion; if a collapse forces an output
> change, that collapse is out of scope — leave the family explicit (FR-007) and record why.

> **Carried lesson from Phase 3 (feature 180, SC-005).** Phase 3's generic-config abstractions
> *increased* net line count (~+170 lines) because they replaced bodies that looked alike but diverged
> in detail. The Explore pass for this feature confirms the same hazard here: same-named variants
> across features (e.g. `renderFeature158ValidationSummary` vs `renderFeature159ValidationSummary`)
> share a markdown *skeleton* but diverge heavily in *content* — different evidence-link sets, different
> reviewer-checklist lines, different decision text, different payload types. Therefore **net line
> reduction is a measured, gated outcome, not an assumption** (SC-005): each variant family is collapsed
> only if (a) its output stays byte-identical AND (b) the collapse does not increase net lines for that
> family. Families that fail either test stay explicit and the exclusion is recorded (FR-007).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`Directory.Build.props`: `TargetFramework=net10.0`,
`FSharpLanguageVersion=latest`).

**Primary Dependencies**: SkiaSharp over OpenGL and Silk.NET (the harness links these but this refactor
does not touch the render/host path); `System.IO.Path` for the directory-constant derivation. No new
dependencies introduced.

**Storage**: No persisted runtime state. The durable artifacts that MUST stay byte-stable are the
generated readiness artifacts under `specs/###-*/readiness/**` (Markdown + JSON reports emitted by the
harness commands and asserted by `tests/Package.Tests/`), plus harness CLI stdout/stderr/exit codes. No
shipped public surface changes (the harness is an internal `tools/` executable, FR-008).

**Testing**: `dotnet test FS.GG.Rendering.slnx -c Release` (full suite), driven under `DISPLAY=:1`.
Comprehensive baseline capture via `dotnet fsi scripts/baseline-tests.fsx --out <path>` (globs every
`*.Tests.fsproj`, including release-only / sample lanes outside the solution). The byte oracle is a
regenerate-and-diff of `specs/###-*/readiness/**` plus stdout/stderr/exit-code capture of each
per-feature harness command (see [quickstart.md](./quickstart.md)).

**Target Platform**: Linux desktop (SkiaSharp/GL); CI runs Debug build + tests.

**Project Type**: F# UI framework / library set built from `FS.GG.Rendering.slnx`. The unit of change is
the internal CLI tool `tools/Rendering.Harness/Rendering.Harness.fsproj` (`OutputType=Exe`, references 6
`src/` projects, itself tested by `tests/Rendering.Harness.Tests/`) plus its test mirror in
`tests/Package.Tests/`.

**Performance Goals**: N/A — refactor is not on any measured hot path; no perf regression expected. The
harness's *measured* timing/readiness outputs must stay byte-identical, but the harness's own runtime is
not a goal.

**Constraints**:
- **Byte-stability is binding.** Every regenerated readiness artifact + every per-feature command's
  stdout/stderr/exit code MUST be byte-identical to a baseline captured immediately before the change.
  When line-reduction and byte-stable output conflict, **byte-stable output wins** (FR-003, SC-002).
- **No shipped public surface change** (FR-008). All signature changes stay inside
  `tools/Rendering.Harness/`. `.fsi` surface baselines of `FS.GG.UI.*` packages MUST remain unchanged.
- **No `private`/`internal`/`public` modifiers on top-level `.fs` bindings** (Constitution II); the
  harness's internal `.fsi` files declare visibility. New descriptor types/functions are added to the
  relevant harness `.fsi` (e.g. `Compositor.fsi`, a new `FeatureCatalog.fsi`) — internal-tool surface
  only, no package contract.
- **Net line reduction is gated, not assumed** (SC-005); divergent bodies are retained explicitly with
  recorded rationale (FR-007).

**Scale/Scope** (verified at HEAD by the Phase-0 Explore pass — see [research.md](./research.md)):
- `tools/Rendering.Harness/Compositor.fs` ≈ 5,667 lines, **97 `renderFeature…` functions**, **262
  per-feature constants**, payload type defs per feature.
- `tools/Rendering.Harness/Cli.fs` ≈ 4,004 lines, **12 `isFeature###` checks**, `if/elif` dispatch
  chains, per-feature command handlers up to ~400 lines.
- `tests/Package.Tests/` — per-feature `Feature###CompatibilityLedgerTests.fs` (146–154) +
  `Feature###CompatibilityTests.fs` (155–161), structurally identical.
- **Feature set is 148, 149, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161** (12 features,
  **non-contiguous** — 150/151 absent). Report-variant coverage is **non-uniform**: `ValidationSummary`
  and `CompatibilityLedger` are universal; `PackageValidation`/`RegressionValidation` only 153–161;
  `LiveProof`/`Parity`/`Reuse`/`Snapshot`/`ProofSet`/`Timing` and feature-unique variants appear only on
  subsets. The descriptor MUST express *which* variants each feature supports (FR-001, Edge Cases).

## Constitution Check

*GATE: evaluated before Phase 0 research; re-checked after Phase 1 design. Result: **PASS**.*

| Principle | Assessment |
|-----------|------------|
| **I. Spec → FSI → Semantic Tests → Implementation** | The new `FeatureDescriptor` + generic-renderer surface is internal to the harness; it is drafted in the harness `.fsi` first and exercised by the existing `Rendering.Harness.Tests` / `Package.Tests` suites, whose golden outputs are the oracle. No new product behavior. **Pass.** |
| **II. Visibility in `.fsi`, not `.fs`** | Descriptor types/functions are declared in the harness's own `.fsi` files; no access modifiers added to `.fs`. Internal-tool surface only — no `FS.GG.UI.*` package `.fsi` changes (FR-008). **Pass.** |
| **III. Idiomatic Simplicity** | The abstraction is a plain record + a list of records + ordinary functions over them (no SRTP/reflection/type-providers/non-trivial CEs). Where a generic body would be *less* legible or *longer* than the explicit one, the explicit body is kept (FR-007) — honoring "idiomatic simplicity," not dogmatic deduplication. **Pass.** |
| **IV. Elmish/MVU boundary** | No new stateful/I-O workflow; renderers are pure `descriptor → string`, the CLI table is a pure dispatch map, the existing I/O (file writes) is unchanged. **N/A.** |
| **V. Test Evidence** | Behavior is preserved; the existing fail-before/pass-after suites plus the byte-diff of regenerated readiness artifacts and command stdout are the evidence. No assertion weakened; no synthetic evidence introduced. **Pass.** |
| **VI. Observability & Safe Failure** | Diagnostic/evidence emission paths are preserved verbatim (byte-stable). **Pass.** |

**Change Classification**: **Tier 2 (internal change)** for all three stories. The refactor adds,
removes, and modifies *internal* harness signatures only; it introduces no public API surface, no new
dependency, and no observable-behavior change covered by a product spec. Per Constitution Change
Classification, Tier 2 requires spec + tests and leaves `.fsi`/baselines of shipped packages untouched —
which is exactly FR-008. (If any step is found to require touching a shipped `FS.GG.UI.*` `.fsi`, that
step is out of scope by FR-008 and must be re-planned.)

**No constitution violations → Complexity Tracking table intentionally omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/181-feature-data-table-refactor/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — descriptor shape, divergence map, byte-oracle decision
├── data-model.md        # Phase 1 — FeatureDescriptor, ReportVariant, descriptor catalog
├── quickstart.md        # Phase 1 — baseline-capture + byte-diff validation guide
├── contracts/           # Phase 1 — internal harness contracts
│   ├── feature-descriptor.md     # FeatureDescriptor record + ReportVariant set + catalog
│   ├── generic-renderer.md       # generic renderer signatures + per-variant collapse/exclude rules
│   └── command-table.md          # descriptor-keyed CLI dispatch + shared runner contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
tools/Rendering.Harness/                 # internal CLI tool (FR-008: no package surface here)
├── Rendering.Harness.fsproj             # + FeatureCatalog.fs(i) added to include order (before Compositor)
├── FeatureCatalog.fsi / .fs             # NEW — FeatureDescriptor record, ReportVariant DU, the
│                                        #       descriptor catalog (12 entries), derived directory paths
├── Compositor.fsi / .fs                 # constant quintets derived from catalog; structurally-repeated
│                                        #   render variants routed through generic renderer; genuinely
│                                        #   divergent bodies retained explicitly (FR-007)
├── Cli.fsi / .fs                        # isFeature###/if-elif chains → descriptor-keyed command table;
│                                        #   shared performance/readiness runner extracted once
└── (Domain.fs, Evidence.fs, … untouched except where they consume the catalog)

tests/Package.Tests/
├── Package.Tests.fsproj                 # per-feature Feature###Compatibility*Tests.fs removed from
│                                        #   include order; one data-driven module added
├── CompatibilityLedgerTests.fs          # NEW — one testList over the descriptor set (replaces the
│                                        #   Feature146–161 copy-forward files)
└── (Feature###Compatibility*Tests.fs … deleted once the data-driven list covers them)

tests/Rendering.Harness.Tests/           # exercises the harness; unchanged behavior, may gain a
                                         #   descriptor-coverage assertion (every catalog entry renders)

specs/181-feature-data-table-refactor/readiness/
├── baseline/                            # regenerated readiness artifacts + command stdout, pre-edit
└── post-change/                         # same, post-edit — diffed byte-for-byte (the acceptance gate)
```

**Structure Decision**: Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). The descriptor
catalog is added as a **new low-level module inside the harness** (`FeatureCatalog`, included before
`Compositor.fs` so both `Compositor` and `Cli` can consume it), not as a new shipped project — the
harness is an internal tool and FR-008 forbids touching shipped package surfaces. This mirrors the
Phase-3 (180) pattern of adding a small shared surface to an existing low layer rather than introducing
new structure, but scoped to `tools/` because the duplication is harness-local.

## Sequencing & Independence

Stories map to spec priorities (renderer → CLI → tests) and are each independently shippable (each ends
green on `dotnet build` + `dotnet test`, per the project's per-phase convention), sequenced for a
single-baseline byte-diff model (mirrors features 179/180):

1. **Setup** — create `specs/181-…/readiness/`, capture the pre-edit baseline: run every per-feature
   harness command into a clean tree, snapshot all generated `readiness/**` artifacts + each command's
   stdout/stderr/exit code, and run the full `*.Tests.fsproj` sweep into `baseline/`.
2. **Foundational (GATE)** — record the allowed pre-existing non-green set (e.g. `tests/Package.Tests`,
   `samples/ControlsGallery` stale-feed reds, per feature 180's evidence) as baseline-not-regression;
   resolve the early-live-smoke clause as N/A; lock the byte-stability evidence contract; author the
   `FeatureDescriptor` + `ReportVariant` types and the 12-entry catalog (the data the next three stories
   all read). Everything below depends on this gate.
3. **US1 / P1 — descriptor + generic renderer** (Tier 2): derive the constant quintets from the catalog;
   route each *structurally-repeated, byte-stable, line-reducing* variant family through a generic
   renderer; retain divergent families explicitly with recorded rationale (FR-007). Build + full test +
   byte-diff of all regenerated readiness artifacts. **MVP — independently shippable here** (SC-001/003).
4. **US2 / P2 — descriptor-keyed CLI table** (Tier 2): replace `isFeature###`/`if-elif` dispatch with a
   table keyed by descriptor; extract the shared performance/readiness runner once. Build + full test +
   byte-diff of every per-feature command's stdout/stderr/exit code (SC-002) and its emitted artifacts.
5. **US3 / P3 — data-driven compatibility tests** (Tier 2): collapse the `Feature###Compatibility*Tests.fs`
   family into one `testList` over the descriptor set; delete the per-feature files; confirm equivalent
   coverage (every previously-covered feature still asserted). Build + full test (SC-004).
6. **Polish** — full `dotnet build` + `dotnet test`; capture `post-change/`; verify SC-001…SC-006;
   **measure and record net line delta per family** and confirm no family was collapsed at a net line
   cost (SC-005); list every FR-007 retention with rationale.

US3 is independent of US1/US2 and may land in any order; US2 reads the catalog from US1's Foundational
gate. The descriptor catalog (Foundational) is the shared prerequisite for all three.

## Implementation Outcome

**Status**: implemented on `181-feature-data-table-refactor`. Byte-stability held throughout; the full
`*.Tests.fsproj` sweep matches the baseline red/green set (14 green / 2 pre-existing reds:
`tests/Package.Tests` Feature128+Feature163, `samples/ControlsGallery` stale-feed — **unchanged**).

### Foundational — `FeatureCatalog` (single source of truth)
`tools/Rendering.Harness/FeatureCatalog.(fsi/fs)` adds the 12-entry descriptor catalog, `ReportVariant`
DU, `FeatureConfig`, and path/alias helpers, wired before `Compositor` in the include order. Derived path
helpers reproduce the hand-declared `feature###…` constants **byte-for-byte**, locked by a direct equality
test (C-FD-2). No shipped `FS.GG.UI.*` `.fsi` changed (FR-008).

### US1 — renderer collapse (per-family COLLAPSE/RETAIN, measured)
| Family | Count | Decision | Why |
|---|---|---|---|
| PackageValidation | 9 | **COLLAPSED** | 156-161 → `renderPackageValidation` generic over a shared `renderValidationDoc` skeleton; 153-155 thin wrappers. Byte-identical. |
| RegressionValidation | 9 | **COLLAPSED** | 156-161 → `renderRegressionValidation`; same skeleton. Byte-identical. |
| UnsupportedHostReport | 6 | **RETAINED (FR-007)** | Measured collapse = **+20 lines** (per-feature wrappers verbose; 160 needs dynamic interpolation). SC-005 fail. |
| CompatibilityLedger | 12 | **RETAINED (FR-007)** | 2–5 sections, 8+ distinct headings, ~20–40 lines of feature-specific prose each — collapse relocates prose to data with no net win + readability loss (the feature-180 trap). |
| ValidationSummary | 12 | **RETAINED (FR-007)** | Each takes a distinct per-feature payload type (`Feature156TimingSummary`, …); no shared signature. |
| LiveProof/Parity/Timing/ProofSet/Reuse/Snapshot + feature-unique | — | **RETAINED (FR-007)** | Per-feature payloads / single-instance bodies; collapse saves nothing or risks drift. |

- **SC-003**: `let renderFeature` count **97 → 85** (12 collapsed). Measured against the `renderFeature`
  subset (resolves I1); the broader `let render…` family is unchanged by design. The "great majority
  collapsed" aspiration is bounded by the measured divergence above — the RETAIN set is the faithful
  outcome of the maximal-collapse attempt, carrying the phase-3/180 SC-005 lesson.
- **SC-005**: whole-feature net delta **−251 lines** (716 ins / 967 del) **including** the new ~198-line
  catalog; `Compositor.fs` −155, `Cli.fs` −76. No collapsed family increased net lines.

### US2 — catalog-driven CLI selection
The 12 `isFeature###` predicates in `Cli.fs` now delegate to one `selectFeature` +
`FeatureDescriptor.tryByAlias` (C-CT-3/C-FD-4). The shared ~400-line performance/readiness runner bodies
(T020) were **RETAINED** explicit: their probe→profile→classify→write steps diverge per feature (lane id,
policy id, summary shape) and an extracted runner measured net-neutral with higher drift risk (FR-007).

### US3 — data-driven compatibility tests
The 12 catalog `Feature###Compatibility*Tests.fs` collapsed into one `CompatibilityLedgerTests.fs`
(per-feature `(path, tokens)` data table + catalog-coverage lock). Equivalent coverage (SC-004): the two
in-memory Feature157 damage tests ported verbatim, the 161 fsi checks kept discrete. Non-catalog
146/147/150/151/170 untouched. `Package.Tests` 8 reds unchanged; passing 98 → 101.

### SC checklist (quickstart "Done when")
- [x] SC-001 — one `FeatureDescriptor` catalog drives renderer/CLI/tests; appending an entry needs no new function.
- [x] SC-002 — readiness/** + command output byte-identical (golden suites green; CLI smoke confirmed).
- [x] SC-003 — surviving `renderFeature` = retained-divergent set (97 → 85), measured & recorded.
- [x] SC-004 — per-feature catalog compatibility test files = 0; coverage equivalent.
- [x] SC-005 — net lines **reduced** (−251); every collapse byte-stable AND non-increasing; FR-007 retentions recorded.
- [x] SC-006 — full sweep red/green identical to baseline.
- [x] FR-008 — no `FS.GG.UI.*` surface baseline changed (all new surface is harness-internal `.fsi`).

## Complexity Tracking

> No Constitution Check violations — table omitted.
