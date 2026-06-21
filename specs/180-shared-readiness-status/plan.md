# Implementation Plan: Shared ReadinessStatus (Code-Health Refactoring Phase 3)

**Branch**: `180-shared-readiness-status` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/180-shared-readiness-status/spec.md`

## Summary

Phase 3 of the whole-repo code-health refactoring collapses the largest remaining duplication, all
concentrated in the readiness/evidence reporting code:

1. **One shared readiness-status vocabulary** (`ReadinessStatus`) in the lowest shared project, with a
   single authoritative display-text projection and a single canonical blocks-acceptance default,
   replacing ~9–10 parallel per-domain status DUs each carrying its own identical `statusText` table.
2. **One parameterized readiness validator** replacing the three near-identical `Feature159Readiness`,
   `Feature160ThroughputReadiness`, and `Feature161HostLaneReadiness` modules (~106 / ~118 / ~133 lines).
3. **One shared Markdown/JSON formatting helper** (`esc`, `q`, `jsonStringArray`, `jsonCounts`,
   `countsText`) replacing the three byte-identical copies in `Testing.fs`.

This changes **no runtime behavior of the shipped product**. The binding constraint is that every
serialized readiness/evidence artifact (JSON + Markdown reports, golden outputs) and every public
surface baseline stays **byte-identical** to a baseline captured immediately before the change, except
for *additive* public surface in `FS.GG.UI.Diagnostics` (the new shared type). The "real evidence" is
the existing regression machinery: a clean `dotnet build` plus the full `dotnet test` run, captured as
a baseline and diffed after each story.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature carries **no defect/root-cause hypothesis** to confirm against a running app: it
> consolidates internal readiness-classification, validation, and formatting code without altering
> shipped behavior. The early-live-smoke clause from the plan template is therefore resolved as **N/A**
> for this feature; `/speckit-tasks` MUST instead schedule **baseline capture** as the first
> Foundational task, and gate every story on a byte-for-byte diff of serialized artifacts + a full
> `dotnet test` showing no new failures relative to baseline. Do not green a build by weakening any
> assertion; if output must change, the consolidation that forces it is out of scope (preserve the
> existing string via a thin per-domain projection instead).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`Directory.Build.props`: `TargetFramework=net10.0`,
`FSharpLanguageVersion=latest`).

**Primary Dependencies**: SkiaSharp over OpenGL (unaffected here); `System.Text.Json` (already used by
`Diagnostics.fs` for JSON emission). No new dependencies introduced.

**Storage**: No persisted runtime state. The durable artifacts that must stay byte-stable are:
`readiness/surface-baselines/*.txt` (public API surface records, esp. `FS.GG.UI.Testing.txt` and
`FS.GG.UI.Diagnostics.txt`), plus the JSON/Markdown readiness reports emitted by the readiness modules
and asserted by the test suites (golden outputs).

**Testing**: `dotnet test FS.GG.Rendering.slnx -c Release` (full suite), driven under `DISPLAY=:1`.
Comprehensive baseline capture via `dotnet fsi scripts/baseline-tests.fsx --out <path>` (globs every
`*.Tests.fsproj`, including release-only / sample lanes outside the solution). Surface-drift via
`dotnet fsi scripts/refresh-surface-baselines.fsx`.

**Target Platform**: Linux desktop (SkiaSharp/GL); CI runs Debug build + tests.

**Project Type**: F# UI framework / library set (multi-project, `FS.GG.UI.*`), built from
`FS.GG.Rendering.slnx`.

**Performance Goals**: N/A — refactor is not on any measured hot path; no perf regression expected.

**Constraints**:
- **Byte-stability is binding.** Every serialized readiness/evidence artifact MUST be byte-identical to
  baseline. When line-reduction and byte-stable output conflict, byte-stable output wins.
- **Public surface**: only *additive* changes allowed (new `ReadinessStatus` surface in
  `FS.GG.UI.Diagnostics`). Existing public DUs in `FS.GG.UI.Testing` MUST remain source-compatible for
  current call sites (preserve via the existing DU names/cases or aliases; relocation only if confirmed
  unused). Surface baselines are regenerated only for the additive Diagnostics surface; `Testing.txt`
  stays unchanged.
- **No `private`/`internal`/`public` modifiers on top-level `.fs` bindings** (Constitution II);
  visibility lives in `.fsi`.

**Scale/Scope**: Two files carry essentially all the change — `src/Testing/Testing.fs` (~4500+ lines;
~9 status DUs, 3 feature validators, 3 formatting-helper copies) and `src/Diagnostics/Diagnostics.fs`
(`ReadinessDiagnosticStatus`, `System.Text.Json`-based formatting). The new shared type/helpers land in
`src/Diagnostics/`. Net source-line reduction expected across the touched reporting code.

### Verified facts from Phase 0 research (see [research.md](./research.md))

- **Dependency layering confirms the shared home.** `Diagnostics` is a leaf (zero outbound
  `ProjectReference`s) and is already referenced by `Testing` and `SkiaViewer`; placing the shared type
  there introduces **no cycle**. Decision: shared `ReadinessStatus` + formatting helpers live in
  `FS.GG.UI.Diagnostics`. (`Scene` is also a leaf but is not the semantic home for readiness taxonomy.)
- **`statusText` tokens are identical across domains** ("accepted", "environment-limited", "blocked",
  "rejected", "fallback-only", "incomplete", "missing-evidence", …) — fully unifiable into one shared
  token table.
- **`blocksAcceptance` rules diverge** for the same conceptual case: `EnvironmentLimited` is
  *non-blocking* in `LayoutReadiness`/`CompositorDamage` but *blocking* in `Feature159Readiness`. The
  shared type therefore exposes **one canonical default rule**, and the (few) domains that diverge keep
  a thin, clearly-commented per-domain override. This is the spec's allowed "per-domain projection."
- **The three formatting copies in `Testing.fs` are byte-identical**; the `Diagnostics.fs` "copies" are
  a **different implementation** (`System.Text.Json`-based `json`/`jsonStringArray`/`jsonCounts` with
  different signatures and no comma-spacing). Reconciliation of the Diagnostics variant is **only**
  pursued where it does not change emitted bytes; otherwise it is left intact and documented.
- **No `InternalsVisibleTo`** wires these symbols; formatting helpers are private (absent from `.fsi`).
  The status DUs *are* in `Testing.fsi` (public surface) → governs the source-compat constraint above.

## Constitution Check

*GATE: evaluated before Phase 0 research; re-checked after Phase 1 design. Result: **PASS**.*

| Principle | Assessment |
|-----------|------------|
| **I. Spec → FSI → Semantic Tests → Implementation** | The new shared `ReadinessStatus` surface is drafted in `Diagnostics.fsi` first; existing semantic tests (readiness golden outputs) are the oracle and run unchanged. No new behavior to test beyond byte-stability, which the existing suites already assert. **Pass.** |
| **II. Visibility in `.fsi`, not `.fs`** | New shared symbols are declared in `Diagnostics.fsi`; no access modifiers added to `.fs`. The collapsed validator and helpers stay private unless already surfaced. **Pass.** |
| **III. Idiomatic Simplicity** | Net simplification: one token table + one validator + one helper module replace many copies. The parameterized validator uses a plain configuration record + functions (no SRTP/reflection/active patterns). **Pass.** |
| **IV. Elmish/MVU boundary** | No stateful/I-O workflow introduced; these are pure classification/formatting functions. **N/A.** |
| **V. Test Evidence** | Behavior is preserved; the existing fail-before/pass-after suites plus byte-diff of golden artifacts are the evidence. No assertion weakened; no synthetic evidence introduced. **Pass.** |
| **VI. Observability & Safe Failure** | Diagnostic emission paths are preserved verbatim (byte-stable). **Pass.** |

**Change Classification**: **Tier 1** for Story 1 (additive public surface in `FS.GG.UI.Diagnostics`
→ requires `.fsi` update + `FS.GG.UI.Diagnostics.txt` surface-baseline refresh + compatibility note);
**Tier 2** for Stories 2 and 3 (internal validator/helper consolidation, no public surface change).
A Tier-1 change that fails to update `.fsi`/baselines is a defect — captured as explicit tasks.

**No constitution violations → Complexity Tracking table intentionally omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/180-shared-readiness-status/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — dependency/home decision, byte-stability nuances
├── data-model.md        # Phase 1 output — ReadinessStatus, ValidatorConfig, FormattingHelper
├── quickstart.md        # Phase 1 output — baseline-capture + byte-diff validation guide
├── contracts/           # Phase 1 output
│   ├── readiness-status-surface.md     # new shared public surface (Diagnostics.fsi additions)
│   ├── validator-config.md             # parameterized-validator contract + per-feature config rows
│   └── formatting-helpers.md           # shared helper signatures + byte-stability reconciliation rules
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Diagnostics/                 # LEAF project — shared home (FS.GG.UI.Diagnostics)
│   ├── Diagnostics.fs           # + shared ReadinessStatus vocabulary; + shared formatting helpers;
│   │                            #   ReadinessDiagnosticStatus migrated to reuse shared vocabulary
│   └── Diagnostics.fsi          # + additive public surface (Tier 1): ReadinessStatus, statusToken,
│                                #   blocksAcceptance default, tryParse; shared helpers stay private
├── Testing/                     # FS.GG.UI.Testing — references Diagnostics + Scene
│   ├── Testing.fs               # per-domain status DUs delegate to shared token/rule (mapper bodies
│   │                            #   collapse); 3 Feature*Readiness modules → 1 parameterized validator
│   │                            #   + 3 config records; 3 formatting-helper copies → shared module use
│   └── Testing.fsi              # UNCHANGED public surface (DUs preserved for source-compat)
└── (Scene, SkiaViewer, Layout, Controls, … — untouched)

readiness/
└── surface-baselines/
    ├── FS.GG.UI.Diagnostics.txt # REGENERATED (additive shared surface)
    └── FS.GG.UI.Testing.txt     # UNCHANGED (DUs/cases preserved)

specs/180-shared-readiness-status/readiness/
├── baseline.md                  # captured before any edit (full *.Tests.fsproj sweep)
└── post-change.md               # captured after, diffed for "no new failures vs baseline"
```

**Structure Decision**: Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). The shared
vocabulary and formatting helpers are added to the existing **leaf** project `src/Diagnostics/`
(reachable cycle-free by all consumers); all consumer-side migration happens in `src/Testing/`. No new
project is created. This mirrors the Phase-1 (178) pattern of adding a small shared surface to an
existing low-tier project rather than introducing structure.

## Sequencing & Independence

Stories map to spec priorities and are each independently shippable (FR-009), but are sequenced for a
single-baseline diff model (mirrors Phase 2 / feature 179):

1. **Setup** — create `specs/180-.../readiness/`, capture `baseline.md` over **every** `*.Tests.fsproj`
   (not just the solution), snapshot serialized readiness/evidence artifacts.
2. **Foundational (GATE)** — record the allowed pre-existing non-green set (e.g. `tests/Package.Tests`,
   `samples/ControlsGallery` stale-feed reds) as baseline-not-regression; resolve the early-live-smoke
   clause as N/A; lock the byte-stability evidence contract. Everything below depends on this gate.
3. **US1 / P1 — shared `ReadinessStatus`** (Tier 1): add shared type + `statusToken` + `blocksAcceptance`
   default to Diagnostics (`.fsi` + `.fs`), migrate `ReadinessDiagnosticStatus` and at least the
   representative `Testing.fs` DUs to delegate; delete the duplicated generic mappers; refresh
   `FS.GG.UI.Diagnostics.txt`; build + test diff + byte-diff.
4. **US2 / P2 — parameterized validator** (Tier 2): introduce one validator driven by a config record,
   express features 159/160/161 as three config entries, delete the three original modules; build +
   test diff + byte-diff of the 159/160/161 readiness outputs.
5. **US3 / P3 — shared formatting helper** (Tier 2): extract the three byte-identical `Testing.fs`
   copies into one shared module; point all call sites at it; reconcile the `Diagnostics.fs` variant
   only where bytes are unchanged; build + test diff + byte-diff.
6. **Polish** — full `dotnet build` + `dotnet test`, capture `post-change.md`, verify SC-001…SC-006,
   confirm net source-line reduction and zero new failures.

US3 is independent of US1/US2 and may land in any order; US2 depends on US1's shared vocabulary.

## Complexity Tracking

> No Constitution Check violations — table omitted.

## Implementation Outcome (2026-06-21 — Phase 3 complete)

All 30 tasks complete. Implemented across two commits on `180-shared-readiness-status`:
`180: shared ReadinessStatus + formatting helpers … (US1, US3)` and
`180: parameterized readiness validator … (US2)`.

**What shipped**

- **US1 (Tier 1, additive surface).** Canonical `[<RequireQualifiedAccess>] ReadinessStatus` (12 cases)
  + `ReadinessStatus` module (`statusToken` / `blocksAcceptance` / `tryParse`) added to the leaf
  `FS.GG.UI.Diagnostics`. `ReadinessDiagnosticStatus` token/parse routed through it (`review-required`
  preserved as a domain projection). Representative Testing DUs migrated onto a private `toShared`
  projection: `VisualReadiness`, `CompositorReadiness`, `CompositorDamage`, and `LayoutReadiness`
  (whose duplicate `blocksAcceptance` was deleted in favour of the shared rule). `CompositorDamage`
  keeps a documented `EnvironmentLimited`-blocks override. **`RetainedInspectionStatus` /
  `VisualInspectionStatus` were found to live in `FS.GG.UI.Scene` (a pure leaf with no Diagnostics
  reference), not in `Testing.fs` as the tasks assumed — migrating them would require a new
  Scene→Diagnostics project reference, so they were left out of scope** (the US1 "≥1 representative
  DU" bar is met by the four Testing-resident DUs above).
- **US3 (Tier 2, internal).** The three byte-identical `esc`/`q`/`jsonStringArray`/`jsonCounts`/
  `countsText` copies collapsed into one internal `ReadinessFormatting` module **inside `Testing.fs`**
  (the contracts named `Diagnostics.fs` as the home, but `Diagnostics` has a signature file, so a
  shared helper there could not be both reachable from `Testing` *and* absent from the public
  surface — a Testing-internal module satisfies SC-003 with zero surface change). The Diagnostics
  `System.Text.Json` variant is left intact with a documenting note (C-FH-3).
- **US2 (Tier 2, internal).** One generic `ReadinessValidatorConfig` + `ReadinessValidator.validateReadiness`
  replaces the three inline validator bodies; features 159/160/161 are three config entries with
  thin source-compatible `validate` wrappers.

**Evidence.** Baseline captured before any edit over **every** `*.Tests.fsproj` (Release,
`DISPLAY=:1`): 14 green, 2 pre-existing reds (`tests/Package.Tests`, `samples/ControlsGallery`). The
post-change sweep (`readiness/post-change.md`) is **identical** pass/fail/counts — no new failures;
every readiness/evidence golden assertion (the byte oracle) stays green. Surface drift is additive
only (`FS.GG.UI.Diagnostics.txt` gains `ReadinessStatus`; `FS.GG.UI.Testing.txt` unchanged).

**Success criteria.** SC-001 (one `statusToken` + one `blocksAcceptance`) ✅; SC-002 (one validator +
3 config entries) ✅; SC-003 (one definition per formatting helper) ✅; SC-004 (byte-identical
serialized artifacts) ✅; SC-006 (same-shaped feature = one config entry) ✅. **SC-005 (net
source-line reduction) ❌ not met** — the single-source-of-truth abstractions (the additive shared
vocabulary, and especially US2's generic config records) cost more lines than the duplication they
remove: the touched files net **+~170 lines** vs. baseline. Duplication and single-source ownership
improved as intended, but raw size did not shrink; US2 is the driver and is isolated in its own
commit so the trade can be reviewed/reverted independently.
