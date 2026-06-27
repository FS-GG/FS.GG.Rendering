# Implementation Plan: Fix the generated build.fsx governance-engine resolution

**Branch**: `202-fix-build-fsx-engine` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/202-fix-build-fsx-engine/spec.md`

## Summary

The generated `build.fsx` Verify gate binds an in-process governance engine — package
`FS.GG.UI.Build`, type `FS.GG.UI.Build.Evidence.GeneratedRunner`, static `run(target, dir): int`
invoked by reflection — to run the EvidenceGraph and EvidenceAudit gates. Today that path is broken
two ways: (1) the runtime probe targets the **pre-rebrand** cache folder `fs.skia.ui.build`
(`build.fsx:126`) instead of `fs.gg.ui.build`; and (2) **no producer for `FS.GG.UI.Build` exists** in
any repo, feed, or cache, so the runtime restore can never succeed and Verify aborts on the evidence
gates for every profile that includes them.

**Approach (decided here — see research.md):** **(Re)establish an in-repo, in-process `FS.GG.UI.Build`
producer**, authored fresh, that the existing package harness packs into the *coherent* local feed at
the single `$(Version)` — automatically lock-step with `$(FsSkiaUiVersion)` and requiring zero new
pack wiring. The new engine exposes the exact reflected contract (`GeneratedRunner.run`) the generated
`build.fsx` already calls, senses the generated product's `readiness/` evidence surface, and emits
`readiness/evidence-graph.md` + `readiness/evidence-audit.md` honoring the contract already documented
in `template/base/docs/evidence-formats.md` (which was generated *from the original engine's*
`EvidenceFormatSchema`, so the contract is recoverable even though the binary is not). The stale
`fs.skia.ui.build` cache probe is corrected to `fs.gg.ui.build`, removing the last pre-rebrand
identifier from the generated script.

**Why not re-point to `FS.GG.Governance`** (the spec's open alternative): the sibling
`FS.GG.Governance.Cli` (v0.1.1, in the feed) is a separately-versioned `PackAsTool` invoked as a
subprocess. Binding to it would (a) introduce a **second version literal** — violating FR-004's
single-pin lock-step; (b) add an **external process beyond `dotnet test`** — violating FR-006's
in-process mandate; and (c) make every generated product **depend on an external governance platform**
— which the constitution's opening sentence explicitly forbids. The engine choice is therefore not a
preference call; the FRs and constitution jointly force the in-repo, in-process producer.

> **Standing assumption — root-cause hypotheses are unverified until the gate is run.**
> The "no producer + stale path" root cause is grounded in source/feed/cache inspection (research.md),
> but the *engine's runtime behavior* — whether EvidenceGraph has real `readiness/` artifacts to graph
> at the point Verify invokes it, and whether the re-authored engine exits 0 on a healthy product — is
> a hypothesis until a freshly generated product runs the gate. `/speckit-tasks` MUST schedule an
> **early live smoke run** in the Foundational phase (generate → restore against a freshly packed
> coherent feed → `dotnet fsi build.fsx target EvidenceGraph`) right after the new producer compiles
> and before building out the audit logic, to confirm the resolve/load/invoke path and the available
> evidence surface. Do not build the audit rules on the unverified assumption that the artifacts exist.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`). The engine is a packable F# library (`OutputType=Library`,
`IsPackable=true`); the consumer is the generated `build.fsx` FSI script (no FAKE).

**Primary Dependencies**: The new `FS.GG.UI.Build` engine is **dependency-minimal** by constitutional
mandate (no external governance platform): standard library + at most the repo's own dependency-light
packages already in the coherent feed if a evidence/scene vocabulary is genuinely reused. The
generated `build.fsx` consumes the engine purely by reflection (no typed `open`), and resolves its
transitive closure from the NuGet global-packages cache via the existing `AssemblyResolve` handler.

**Storage**: Filesystem only — reads the generated product's `readiness/**` artifacts; writes
`readiness/evidence-graph.md` and `readiness/evidence-audit.md`.

**Testing**:
- `template/base/tests/Product.Tests/GovernanceTests.fs` — text/structure scans over the generated
  `build.fsx` (must stay green: asserts `runGeneratedEvidence "EvidenceGraph"/"EvidenceAudit"`,
  `GeneratedRunner`, `Assembly.LoadFrom`, `FsSkiaUiVersion`, no `#r "nuget: FS.Skia.UI.Build,"`, not
  completion-only logs, no decommissioned scripts, clean text logs).
- New engine: `.fsi`-surfaced module(s) with a surface-area baseline + semantic tests exercising
  `GeneratedRunner.run` against a fixture `readiness/` tree (pass and honest-fail cases).
- End-to-end: per-profile generate → restore (coherent feed) → `build.fsx target Verify`.

**Target Platform**: Linux desktop (SkiaSharp/OpenGL) for the library packages; the engine itself is
headless/pure (no GL).

**Project Type**: Maintenance of a product-scaffolding template inside the FS.GG.Rendering library
repo, **plus a new packable library** (`src/Build/`) added to the repo's own surface.

**Performance Goals**: N/A — correctness/governance task.

**Constraints**:
- **In-process only (FR-006):** the only external process the generated build may spawn remains
  `dotnet test`. The engine runs inside the FSI process via `Assembly.LoadFrom` + reflection.
- **Single version literal (FR-004):** no second version value. The engine is an `FS.GG.UI.*`
  package packed at `$(Version)`; the harness discovers it automatically (`PackageId` prefix +
  `IsPackable`), so a coherent `-p:Version=<V>` pack moves libraries **and** engine together.
- **No pre-rebrand identifier (FR-002):** correct the `fs.skia.ui.build` cache probe to
  `fs.gg.ui.build`; no `FS.Skia.UI` package name or cache path may remain in the generated script.
- **Contract is fixed by the consumer:** the engine MUST expose `FS.GG.UI.Build.Evidence.GeneratedRunner`
  with a static `run : string -> string -> int` (target, working dir). Changing the contract would
  require editing `build.fsx` and the governance tests in lock-step.
- **`template/base` is not directly compilable:** validation goes through `dotnet new` generation,
  then restore/build of the generated product (memory: `template-feed-version-model`).
- **Coherent feed required:** pack with `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=<V>
  -o ~/.local/share/nuget-local` at a FRESH `V` above all per-project versions, set
  `FsSkiaUiVersion=V` (memory: `template-feed-version-model`).

**Scale/Scope**: 1 new producer project (`src/Build/`, a handful of `.fs`/`.fsi` files + baseline);
1 one-line cache-path fix + supporting edits in `template/base/build.fsx`; solution registration in
`FS.GG.Rendering.slnx`; possibly one strengthened governance test (assert no pre-rebrand identifier).
4 profiles validated; 2 of them (`governed`, `headless-scene`) include the evidence gates.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass | The engine's public surface is sketched as `GeneratedRunner.run` (and supporting evidence types) in `.fsi` first; semantic tests exercise it through the packed/loaded assembly against a fixture `readiness/` tree before the `.fs` body is finalized. |
| II. Visibility lives in `.fsi` | ✅ Pass (gated) | The new `src/Build` engine is a public package → **every public module needs a curated `.fsi`** and a surface-area baseline. No `private/internal/public` modifiers in `.fs`. This is the headline Tier-1 obligation. |
| III. Idiomatic simplicity | ✅ Pass | Plain F#: read files, fold an evidence record, render markdown, return an int. Reflection lives only in the *consumer* `build.fsx` (already justified there by the `#r`-literal constraint, Feature 064 R1); the engine itself uses none. |
| IV. Elmish/MVU boundary | ✅ Pass (N/A) | The engine is a pure sense→report function (read artifacts, emit reports, return exit code). No multi-step stateful workflow, retries, or interactive I/O that warrants an MVU boundary; the I/O is a single read/derive/write pass. |
| V. Test evidence is mandatory | ✅ Pass | Real evidence: the engine is tested against real fixture files and validated end-to-end by a real per-profile generate→restore→Verify run. No synthetic substitute for the engine itself. Any fixture used is an explicit, disclosed `readiness/` tree. |
| VI. Observability and safe failure | ✅ Pass | FR-005: engine-unresolvable and engine-internal failures fail loudly with a message naming the engine identity and the feed/path searched; never a silent pass. The engine distinguishes "framework/feed condition" from "defect in the generated product." |

**Change classification: Tier 1 (contracted change).** It adds a new packable package (`FS.GG.UI.Build`)
with a public reflected surface and a new producer project, and makes the previously-broken evidence
gate observably pass. Requires the full chain: spec, plan, `.fsi`, surface-area baseline, test
evidence, docs (UPGRADING/PROVENANCE note that the engine is now produced in-repo). No gate violations
→ Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/202-fix-build-fsx-engine/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 — engine-choice decision + root-cause map
├── data-model.md        # Phase 1 — entities (engine, contract, evidence node, verdict, pin)
├── quickstart.md        # Phase 1 — generate→restore→Verify validation per profile
├── contracts/           # Phase 1
│   ├── engine-invocation-contract.md   # the reflected GeneratedRunner.run surface build.fsx calls
│   └── evidence-output-contract.md      # evidence-graph.md / evidence-audit.md shapes (verdict token)
├── checklists/
│   └── requirements.md  # Spec quality checklist (already created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
# NEW producer — the engine the generated build.fsx binds in-process
src/Build/
├── FS.GG.UI.Build.fsproj          # PackageId=FS.GG.UI.Build, IsPackable=true, OutputType=Library
├── Evidence.fsi                   # public surface: GeneratedRunner.run + evidence types (curated)
├── Evidence.fs                    # sense readiness/** → graph/audit → evidence-*.md → exit code
└── (supporting .fsi/.fs as the surface splits warrant)

# NEW engine semantic tests (repo convention: tests/<Name>.Tests)
tests/Build.Tests/
└── Build.Tests.fsproj             # references src/Build/FS.GG.UI.Build.fsproj; pass + honest-fail cases

readiness/surface-baselines/FS.GG.UI.Build.txt   # NEW surface-area baseline for the engine (Principle II)

# Template under fix
template/base/
├── build.fsx                      # FIX: fs.skia.ui.build → fs.gg.ui.build (line ~126); keep contract strings
├── Directory.Packages.props       # FS.GG.UI.Build pinned to $(FsSkiaUiVersion) — already present, unchanged
├── docs/evidence-formats.md       # authoritative contract the re-authored engine honors (read-only target)
└── tests/Product.Tests/GovernanceTests.fs  # keep green; OPTIONALLY add a "no pre-rebrand identifier" scan

# Packing / feed tooling (used as-is — auto-discovers the new package, no edits)
tools/Rendering.Harness/PackageFeed.fs          # discoverPackablePackages: FS.GG.UI.* + IsPackable → packed
scripts/dev-repack.fsx / refresh-local-feed-and-samples.fsx  # same filter; no hardcoded list to update

# Solution
FS.GG.Rendering.slnx               # register src/Build/FS.GG.UI.Build.fsproj + tests/Build.Tests/Build.Tests.fsproj
```

**Structure Decision**: One new producer project at `src/Build/` (so the existing
`discoverPackablePackages` harness packs it automatically — it scans `src/**` for `FS.GG.UI.*` +
`IsPackable`), a one-line-plus cache-path correction in `template/base/build.fsx`, a surface-area
baseline, and solution registration. The pack tooling, the template's `Directory.Packages.props` pin,
and `build.fsx`'s reflection/version-resolution machinery are reused as-is — the only `build.fsx`
behavioral change is the corrected cache identifier; everything else about the engine is new code in
`src/Build/` that satisfies the contract the script already expects.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty. (The single advanced technique,
> reflection-based engine loading, lives in the *consumer* `build.fsx` and is already justified there
> by the F# `#r`-literal constraint; this feature does not add it — it makes the engine it targets real.)
