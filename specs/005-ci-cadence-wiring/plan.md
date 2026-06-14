# Implementation Plan: Wire Validation into CI at Chosen Cadences (Migration Stage R6)

**Branch**: `005-ci-cadence-wiring` *(git-extension before_specify hook)* | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-ci-cadence-wiring/spec.md`

## Summary

Turn the already-decided validation-set cadence labels into real automation. Add a small set of
in-repo CI workflows that run each check on the trigger matching its declared frequency — a fast
**deterministic gate** on every push/PR (build + default local tier + surface-baselines + docs +
harness T0), **release-only** checks on a packaging/release trigger, and **capability-dependent**
tiers (live X11, perf, input) as opt-in scheduled/manual runs that never gate a merge. The whole
design is governed by one rule carried from Stage R5 and Constitution Principle VI: **CI must never
overclaim** — on a headless runner the display/GL/input checks degrade-and-disclose (skip with
written rationale, never report as passing), and each run surfaces what it proved and what it could
not. R6 reuses the R5 harness CLI as the evidence producer and the R3 cadence partition as the
source of truth; it builds neither.

## Technical Context

**Language/Version**: CI workflow definitions (declarative YAML) plus the existing .NET `net10.0`
toolchain the jobs invoke (`dotnet build`/`dotnet test`). Any glue is a thin shell or F# script
(`.fsx`) — **no new compiled project** unless a summary helper proves unavoidable.

**Primary Dependencies**: GitHub-hosted Actions runners (`ubuntu-latest`); standard actions
(`actions/checkout`, `actions/setup-dotnet` if the pinned SDK isn't preinstalled,
`actions/upload-artifact`). Repo-side: the R5 harness CLI (`tests/Rendering.Harness`, subcommands
`probe`/`offscreen`/`perf`/`live-x11`/`input`) as the evidence producer; the validation-set test
projects; `scripts/refresh-surface-baselines.fsx` + `tests/surface-baselines/`; the `fsdocs` docs
build. **No new NuGet.**

**Storage**: Ephemeral CI run artifacts — the harness `run.json`/`metrics.csv`/`summary.md` (and the
job summary) uploaded per run; nothing persisted server-side. The cadence→trigger mapping is
documented in-repo (`docs/ci/cadence-map.md`) as the auditable source.

**Testing**: The wiring is validated three ways: (1) the deterministic gate runs green on a clean
PR and red on a deliberately broken one (merge-block proof); (2) a headless run shows every
display/GL/input check skipped-with-rationale, never passing; (3) the cadence→trigger mapping is
audited against `docs/validation/validation-set.md` (each member in exactly one cadence). Reuses the
harness exit-code contract: `0` = tier ran and passed **or** cleanly skipped (`status:"skipped"`),
`1` = assertion failed, `2` = bad usage. Any thin aggregation script's pure logic gets a minimal
test in `Rendering.Harness.Tests` only if such a script is added.

**Target Platform**: GitHub-hosted Linux runners for the gate (headless: **no X11, no hardware GL,
no `/dev/uinput`** — the design's expected default). An optional display/GL/uinput-capable runner
hosts the scheduled capability tiers once one exists; **provisioning it is out of scope** (the
wiring degrades cleanly until then).

**Project Type**: CI / infrastructure — workflow config + a documented cadence map (+ optional thin
evidence-summary glue). Not product API.

**Performance Goals**: The deterministic gate completes in **under 10 minutes** on a standard hosted
runner (SC-002), preserving the fast-inner-loop intent. Release-only and capability work add **zero**
time to a routine push (SC-008).

**Constraints**: Constitution v1.0.0 — **observability & safe failure are central** (Principle VI):
capability-absent ⇒ skip-with-machine-readable-rationale (harness exit 0 / `status:"skipped"`),
never green-as-proof; misconfiguration ⇒ fail-fast with probe facts (distinct from absence). Never
mark a skipped/blocked check as passing (Principle V). Release-only checks never appear in the push
gate; capability tiers never block merge; fork PRs run without privileged secrets. No governance
dependency; checks stay narrow and self-justifying (Development Workflow section).

**Scale/Scope**: 3 cadences/triggers; ~3 workflow files (gate, release, capability); 11 local-tier
projects + 2 CI checks (surface-baselines, docs) + harness T0 on the gate; 2 release-only checks
(`Package.Tests`, template `Product.Tests`); 3 capability tiers (T2/T3/T-uinput) scheduled/manual;
1 cadence-map doc; 1 proof-scope run-summary contract.

### Inputs this stage wires (verified present)

| Input | Location | Wired into |
|---|---|---|
| Default local tier (11 named projects) | `tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,Controls,Testing,SkiaViewer,Smoke,Lib}.Tests` | Gate (deterministic; GL-needing ones degrade-disclose) |
| Surface-drift check | `tests/surface-baselines/` + `scripts/refresh-surface-baselines.fsx` | Gate (CI cadence) |
| Docs build | `fsdocs` configuration | Gate (CI cadence) |
| Harness T0/T1 | `tests/Rendering.Harness` `offscreen` (exit 0/1, `status`) | Gate (T0 always; T1 offscreen degrades if no GL) |
| Harness T2/T3/T-uinput | `tests/Rendering.Harness` `live-x11`/`perf`/`input` | Capability workflow (scheduled/manual) |
| Release-only checks | `tests/Package.Tests`, template `Product.Tests` | Release workflow |
| Cadence partition (source of truth) | `docs/validation/validation-set.md` | Audited by `docs/ci/cadence-map.md` |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This is **infrastructure** (CI config + docs + optional thin glue), not product F# code. Code-centric
principles apply only to any F# added; the observability/evidence principles apply fully.

| Principle | Assessment |
|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | No new public F# surface. If a thin evidence-summary `.fsx` is added, its pure logic is sketched and tested in `Rendering.Harness.Tests`. **PASS (mostly N/A)** |
| II. Visibility in `.fsi` | No new public module ⇒ no `.fsi`/baseline obligation. A helper script exposes no package surface. **PASS (N/A)** |
| III. Idiomatic Simplicity | Plain declarative workflows; shell to the installed toolchain and the R5 harness CLI rather than reimplementing tiering; one workflow file per cadence. No clever CI meta-framework. **PASS** |
| IV. Elmish/MVU boundary | No stateful F# workflow added; the harness already owns the pure-plan→edge-interpreter split. CI is a thin trigger→invoke→collect shell. **PASS (N/A)** |
| V. Test Evidence Mandatory | Central. CI never marks a skipped/blocked check as passing; skips carry written rationale; the gate is proven by a real red/green PR pair. Synthetic substitution is not used. **PASS** |
| VI. Observability & Safe Failure | **Central.** Capability-absent ⇒ disclosed skip (exit 0/`status:skipped`), misconfig ⇒ fail-fast with probe facts; every run states proved/not-proved (proof scope). Mirrors the harness no-overclaim rule. **PASS** |
| Engineering Constraints | `net10.0` toolchain; no new NuGet; no governance dependency; checks stay narrow and self-justifying; package identity untouched (`FS.Skia.UI.*`); release-only checks stay off the push gate. **PASS** |

**Change Classification**: **Infrastructure / Tier 2** — adds CI tooling and docs; **no** product
public-API change, no new product dependency, no observable product-behavior change. (`.fsi`/baseline
rules are vacuous here; they still bind any F# the stage happens to add.)

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/005-ci-cadence-wiring/
├── plan.md, spec.md, research.md, data-model.md, quickstart.md
├── contracts/
│   ├── cadence-matrix.md        # cadence → trigger → checks; one row per validation-set member
│   ├── gate-contract.md         # required-checks contract: what blocks merge, what never does
│   └── run-summary.schema.md    # per-run proof-scope disclosure (proved / not-proved-here)
└── checklists/requirements.md
```

### Source Code (repository root)

```text
.github/workflows/
├── gate.yml                     # push + pull_request: build → default local tier → surface-baselines
│                                #   → docs → harness T0 (T1 degrades if no GL); blocks merge on failure
├── release.yml                  # release/tag (+ workflow_dispatch): Package.Tests + template Product.Tests
└── capability.yml               # schedule + workflow_dispatch: harness live-x11/perf/input on a capable
                                 #   runner; degrade-and-disclose by default; never a required check
docs/ci/cadence-map.md           # FR-012: documented, auditable cadence→trigger mapping vs validation-set.md
scripts/ci/                      # thin glue ONLY if needed:
└── summarize-evidence.*         #   fold harness run.json artifacts into one proof-scope run summary (FR-006)
```

**Structure Decision**: One workflow file per cadence (`gate`/`release`/`capability`) keeps the
required-vs-advisory split legible and lets branch protection require exactly the gate. The harness
CLI is the evidence engine — workflows *invoke* `probe`/`offscreen`/`perf`/`live-x11` and read their
exit code + `run.json`, rather than reimplementing any tiering in YAML (Principle III). The
cadence→trigger mapping lives in `docs/ci/cadence-map.md` so FR-009 (each member in exactly one
cadence) is verifiable by inspection against `docs/validation/validation-set.md`. A
`summarize-evidence` script is added **only if** the harness's own `summary.md` plus a job-summary
step can't satisfy FR-006 on their own — default is to avoid new code.

## Complexity Tracking

No constitution violations — section intentionally empty.
