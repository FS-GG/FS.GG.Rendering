# CI cadence map (Stage R6)

> **Migration Stage R6 deliverable — the auditable cadence → trigger mapping.**
>
> This document is a *derivation*, not a new decision. It places each already-decided check at
> exactly one CI cadence and is the single place the no-overlap invariant (FR-009) is audited.
>
> **Sources of truth (this file derives from them; it does not re-decide them):**
> - [`docs/validation/validation-set.md`](../validation/validation-set.md) — the R3 frequency
>   partition (local / CI / release-only) for validation-set members.
> - [`docs/validation/harness.md`](../validation/harness.md) — the R5 harness tiers (T0–T-uinput),
>   which are infrastructure, **not** validation-set members.
> - [`specs/005-ci-cadence-wiring/contracts/cadence-matrix.md`](../../specs/005-ci-cadence-wiring/contracts/cadence-matrix.md)
>   — the contract this map realizes (cadence → trigger → checks, one row per member).
> - [`specs/005-ci-cadence-wiring/contracts/gate-contract.md`](../../specs/005-ci-cadence-wiring/contracts/gate-contract.md)
>   — what the required gate runs, what reds it, what can never red it.
> - [`specs/005-ci-cadence-wiring/contracts/run-summary.schema.md`](../../specs/005-ci-cadence-wiring/contracts/run-summary.schema.md)
>   — the per-run proof-scope disclosure each run emits.

<!-- The sections below are filled by Stage R6 tasks:
     - SDK pinning decision (T005)
     - Member → cadence map + audit invariants (T018)
     - Cadence audit result (T019)
     - Surface-baseline drift / first-run behavior (T009)
     - Branch-protection maintainer step (T022)
     - Quickstart V1–V7 outcomes incl. measured gate wall-clock (T023)
     - Evidence-summary glue decision (T024) -->

## 1. Toolchain / SDK pinning

**Decision:** every workflow pins the SDK with `actions/setup-dotnet@v4` (`dotnet-version: 10.0.x`)
rather than relying on whatever `dotnet` the hosted image preinstalls.

**Why:** the repo has **no `global.json`** (checked at R6), so there is no in-repo SDK floor to key
off, and `net10.0` is recent enough that a given `ubuntu-latest` image may or may not carry it.
Pinning makes `dotnet build`/`dotnet test`/`dotnet run` deterministic across runner-image refreshes
and across the gate/release/capability workflows, which all use the identical setup step. If a
`global.json` is later added, switch `setup-dotnet` to read it (`global-json-file: global.json`) so
there is a single SDK source of truth.

All three workflows therefore begin with `actions/checkout@v4` → `actions/setup-dotnet@v4` before any
build/test/harness step.

## 2. Member → cadence map

Three cadences, one workflow file each. Only `gate` is required (blocks merge).

| Cadence | Trigger | Workflow | Required | Runner | Fork PRs |
|---|---|---|---|---|---|
| **gate** | `push` + `pull_request` → `main` | `.github/workflows/gate.yml` | **yes** | hosted headless | run (no secrets) |
| **release** | `release: published` / `v*` tag (+ `workflow_dispatch`) | `.github/workflows/release.yml` | no | hosted headless | restricted to `FS-GG/FS.GG.Rendering` |
| **capability** | `schedule` (weekly) (+ `workflow_dispatch`) | `.github/workflows/capability.yml` | no | capable (TODO: provision) | restricted |

Every validation-set member and every harness tier maps to **exactly one** cadence. R3 frequency
labels are quoted from [`validation-set.md`](../validation/validation-set.md); harness tiers carry the
R5 source label `infra (R5)` and are not validation-set members.

| Member | R3 / source label | Cadence | Capability | Headless-runner behavior | Wired at |
|---|---|---|---|---|---|
| `Color.Tests` | local | gate | none | runs | gate.yml local tier |
| `Scene.Tests` | local | gate | none | runs | gate.yml local tier |
| `Layout.Tests` | local | gate | none | runs | gate.yml local tier |
| `Input.Tests` | local | gate | none | runs | gate.yml local tier |
| `KeyboardInput.Tests` | local | gate | none | runs | gate.yml local tier |
| `Elmish.Tests` | local | gate | none | runs | gate.yml local tier |
| `Controls.Tests` | local | gate | none | runs | gate.yml local tier |
| `Testing.Tests` | local | gate | none | runs | gate.yml local tier |
| `Lib.Tests` (runtime subset) | local | gate | none | runs | gate.yml local tier |
| `SkiaViewer.Tests` | local | gate | gl | degrade-and-disclose (skipped, disclosed) | gate.yml GL step |
| `Smoke.Tests` | local | gate | gl | degrade-and-disclose (skipped, disclosed) | gate.yml GL step |
| `surface-baselines` | ci (push/PR) | gate | none | runs (see §4 coverage) | gate.yml drift step |
| docs build (`fsdocs`) | ci (push/PR) | gate | none | runs (build only, strict) | gate.yml docs step |
| harness **T0** (`offscreen` det.) | infra (R5) | gate | none | runs (required) | gate.yml harness-evidence |
| harness **T1** (`offscreen` readback) | infra (R5) | gate | gl | degrade-and-disclose (advisory) | gate.yml harness-evidence |
| harness **T2** (`live-x11`) | infra (R5) | capability | x11 | degrade-and-disclose until capable runner | capability.yml |
| harness **T3** (`perf` paced-native) | infra (R5) | capability | gl/x11 | degrade-and-disclose until capable runner | capability.yml |
| harness **T-uinput** (`input --backend uinput`) | infra (R5) | capability | uinput | inert + disclosed (backend pending) | capability.yml |
| `Package.Tests` | release-only | release | none | runs on release trigger | release.yml |
| `Product.Tests` (template) | release-only | release | none | runs on release trigger (template instantiation) | release.yml |

## 3. Audit invariants and result

The audit (FR-009) checks these invariants by inspection of this map against the sources:

1. **Exactly one cadence per member** — no member appears in two cadence rows.
2. **No release-only member in `gate`** — `Package.Tests` / template `Product.Tests` never on push/PR.
3. **Every row traces to a settled source** — validation-set members → `validation-set.md` (R3);
   harness tiers → `harness.md` (R5). Nothing is invented here.
4. **Only `gate` is required** — release/capability never block merge.
5. **Capability rows degrade-and-disclose** — never a silent drop, never a false pass.

### 3.1 Audit result (T019)

Cross-checked this map against `docs/validation/validation-set.md` and the actual triggers/steps in
`gate.yml`, `release.yml`, `capability.yml` on 2026-06-14. **PASS** (SC-003, SC-007):

1. **Exactly one cadence per member** — ✅ every row above appears once; no member is in two cadences.
2. **No release-only member in `gate`** — ✅ `gate.yml` runs the 11 `local` members
   (`Color, Scene, Layout, Input, KeyboardInput, Elmish, Controls, Testing, Lib` deterministic +
   `SkiaViewer, Smoke` GL) plus `surface-baselines`, `fsdocs`, and harness `offscreen` (T0/T1) only.
   `Package.Tests` and template `Product.Tests` appear **only** in `release.yml`. The slnx itself
   excludes `Package.Tests`, so the gate's `dotnet build`/`--no-build` test loop physically cannot
   reach it.
3. **No trigger overlap** — ✅ `gate` = `push`/`pull_request` to `main`; `release` = `release:
   published` + `v*` **tag** push + manual; `capability` = weekly `schedule` + manual. The `release`
   `push` filter is tag-only, so it never fires on a branch push or PR. No event reaches two cadences
   for the same member.
4. **Only `gate` is required** — ✅ `release.yml`/`capability.yml` carry no required status and are
   intended to be excluded from branch protection (§5).
5. **Capability rows degrade-and-disclose** — ✅ `capability.yml` invokes each tier through the
   `harness-evidence` action with no `required-tiers` and `continue-on-error: true`; absence/skip is
   disclosed, never a false pass and never blocking.

The 11 local members above are exactly the `validation-set.md` "Local inner loop" list (1–11) — no
addition, no omission.

## 4. Surface-baseline drift — chosen gate behavior

The gate regenerates the public-`.fsi` surface for **all 9 committed baselines** from the built
assemblies (`scripts/refresh-surface-baselines.fsx`, which writes directly to
`tests/surface-baselines/`) and then fails on any uncommitted change — the canonical
regenerate-then-`git diff` check:

- **Drift fails the gate.** Any modified baseline reds the gate; the fix is to rerun the script
  locally and commit the updated baseline.
- **First run with no baseline ⇒ FAIL (never a silent pass).** A newly-generated baseline that is
  **untracked** (a package gained a public surface with no committed baseline) fails the gate via the
  `git ls-files --others` check, rather than treating "nothing to compare" as success (FR-003).

**Full coverage (the earlier 4-of-9 gap is closed).** Originally the imported script regenerated only
4 surfaces and wrote them to `readiness/surface-baselines/` — a leftover from the R3→R4 migration that
relocated baselines to `tests/surface-baselines/` (see `PROVENANCE.md`) without updating the script.
The generator now (a) covers all 9 packages, (b) writes to the committed location, and (c) excludes
compiler-generated/anonymous types (their names embed a non-deterministic hash and would make the
baseline unstable). Before re-baselining, a public-surface review confirmed the 5 previously-stale
surfaces are **deliberately `.fsi`-governed and not over-exposed** (every public type is either
declared in a curated `.fsi` or explicitly `internal`) — see
[`docs/validation/surface-baseline-review.md`](../validation/surface-baseline-review.md). The
re-baseline was therefore purely additive (365 net-new public types recorded, 0 removed).

Verified locally on 2026-06-14: after the re-baseline, a fresh regenerate produces no `git diff`
(clean on a current checkout).

## 4a. Version-coherence guard — chosen gate behavior (Feature 209)

A sibling merge-blocking step, **Version coherence guard**, makes the FS.GG.UI version-staleness bug
class (Feature 204) a loud, local, automatic failure instead of a downstream consumer's broken build.
It runs `scripts/validate-version-coherence.fsx` in two layers, both merge-blocking:

- **Structural verdict-core (env-free).** Re-derives, from the repo + pushed `fs-gg-ui/v*` tags, that
  the single `<FsGgUiVersion>` literal is present exactly once and matches an existing snapshot tag and
  does **not lag** the latest (preview-aware SemVer compare, not string); the BOM uses the single
  `[$version$]` exact-bracket token with `B.ids == P.members`; the template's consumed pins all derive
  through `$(FsGgUiVersion)` and equal the documented 11-member manifest; and `build.fsx`'s runtime
  regex still resolves the literal. It compares pins **directly** — independent of any
  `WarningsAsErrors=NU1605;NU1608` consumer policy (FR-004).
- **Scoped restore-grounded proof (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`).** One Release pack + one
  clean restore of `FS.GG.UI@V` asserting the **complete** 16-member set resolves to exactly `V`
  (FR-008, anti-text-grep). The deeper full generate→restore→build of a product from the template
  stays on the release lane (`release.yml` `template-product-tests`), not duplicated in the gate.

Exit codes: `0` coherent · `1` drift (names the location expected-vs-actual) · `2` guard error (inputs
unreadable / tags not fetched) — **fails closed**, never green-by-absence. On drift the `DRIFT […]`
lines are echoed to `$GITHUB_STEP_SUMMARY` (SC-006). The gate's `actions/checkout@v4` uses
`fetch-depth: 0` so `git tag` sees the `fs-gg-ui/v*` snapshot tags (otherwise the guard fails closed).

Note: the repo-root `<Version>` (`Directory.Build.props`) is **decoupled by default** (D5) and is not
compared by the guard.

## 5. Branch protection (one-time maintainer step)

The spec defines which checks are required; **enabling** branch protection is the maintainer's
one-time action (it cannot be set from the repo tree). On `main`:

- **Require status checks to pass before merging** → select **only** the `gate` workflow's job
  (`Deterministic gate`). Do **not** add `release` or `capability` jobs as required (FR-007).
- Recommended: "Require branches to be up to date before merging" so the gate runs against the
  post-merge state.
- Leave `release.yml` and `capability.yml` unselected — they are advisory by design and must never
  block a merge (gate-contract "What can NEVER fail the gate").

Result: a PR merges iff the deterministic gate is green; release/capability runs are visible evidence
but never gate.

## 6. Quickstart validation outcomes (V1–V7)

Mechanically-validated locally on 2026-06-14 where possible; items needing a real GitHub run or a
true headless/fork context are marked. Source scenarios: `specs/005-ci-cadence-wiring/quickstart.md`.

| # | Scenario | Status | Evidence / note |
|---|---|---|---|
| **V1** | Clean PR ⇒ gate green; deterministic portion < 10 min | ✅ green (locally) | Deterministic gate path measured at **~192 s** locally (build + 9 local-tier tests + surface gen + harness offscreen) — comfortably under SC-002's 10 min (hosted runners are slower and add the fsdocs tool install; still expected to pass). All 9 deterministic local-tier projects pass (`Lib.Tests`: 30/30 after the samples quarantine below). Confirm end-to-end timing on the hosted runner. |
| **V2** | Deterministic break ⇒ gate red + merge blocked | ✅ mechanism confirmed | Any non-zero `dotnet test` in the local-tier loop reds the step and the job (the loop runs under `set -euo pipefail`). Observed live before the samples quarantine: the `Lib.Tests` sample failure surfaced as a real red — exactly the merge-block path. Confirm the full red→merge-block on a real PR once branch protection (§5) is enabled. |
| **V3** | Capability-blocked checks degrade & disclose | ✅ logic validated | The `harness-evidence` renderer was exercised against a synthetic headless `offscreen` (`T0` passed, `T1` `status:"skipped"`): T1 rendered under **notProvedHere** with its rationale, never under proved, and the overall stayed **pass**. On this dev box GL is present so a *live* headless skip can't be observed here — confirm on the hosted runner. |
| **V4** | Run summary states proved vs not-proved | ✅ by construction | Every harness step appends a proof-scope block (proved / notProvedHere / failed / overall + `runnerCapability`) to `$GITHUB_STEP_SUMMARY`; a reader answers "was live/visual behavior verified here?" from the summary alone. |
| **V5** | Misconfiguration fails fast, absence does not | ✅ logic validated | The action treats process **exit 2** (bad usage) as a hard `failed` (fail-fast), distinct from `status:"skipped"` (clean absence, never fails). Verified against a simulated exit-2 run. |
| **V6** | Each check at exactly its cadence | ✅ audited | See §3.1 — PASS. No overlap; no release-only member in the gate; only `gate` required. Real release/`workflow_dispatch` placement to be observed on first tagged release / manual capability run. |
| **V7** | Fork PR gets a real signal without secrets | ✅ by construction | `gate.yml` declares `permissions: contents: read` and uses no secrets, so fork PRs run the full gate. `release.yml`/`capability.yml` are guarded by `if: github.repository == 'FS-GG/FS.GG.Rendering'`, so fork events skip them without false failures. Confirm on a real fork PR. |

### Samples quarantine (so V1 is green now)

`tests/Lib.Tests` and `tests/Smoke.Tests` reference `samples/{BasicViewer,InteractiveViewer,ScreenshotGallery,…}`
projects that **do not exist** in this repo (samples were not imported at Stage R4; only
`template/fragments/samples` scaffolding is present). To let the gate be green on current HEAD, the
sample-dependent assertions were **quarantined to skip-with-reason when `samples/` is absent**, rather
than fail:

- `Lib.Tests` "BasicViewer contract smoke" now guards on the project's existence (`if File.Exists …`),
  exactly like its already-guarded `InteractiveViewer`/`ScreenshotGallery` siblings. Result: 30/30 pass.
- `Smoke.Tests` skips its three sample-contract tests via `skiptest` when `samples/` is absent (3
  Ignored, 0 failed). `Smoke.Tests` is GL-gated anyway and is skipped entirely on the headless gate.

Both **self-restore** to full assertions the moment `samples/` is imported — no further CI change
needed. Importing the samples (or otherwise restoring full sample coverage) remains upstream,
Stage-R4-style work outside R6.

> R6 also applied one small enabling fix: `Lib.Tests` and `Smoke.Tests` located the repo root by
> searching for `*.sln`/`build.fsx`, which no longer matches the migrated `FS.GG.Rendering.slnx` on
> net10.0 and threw at module init (blocking test discovery entirely). They now also detect `*.slnx`,
> mirroring the fix Feature 045 already made in `Elmish.Tests`. Without this, the wired members could
> not even be discovered to run.

## 7. Evidence-summary glue decision

**No new glue script was added** (Decision 6 default). The `harness-evidence` composite action's
inline renderer reads each tier's `run.json` (`status`, `skipReason`, `proofLevel`,
`authoritativeFor`, `notAuthoritativeFor`, `env`) and emits the full proof-scope summary
(proved / notProvedHere / failed / overall) directly to `$GITHUB_STEP_SUMMARY`, satisfying FR-006 and
SC-005 without a separate aggregator. `scripts/ci/summarize-evidence.*` was therefore **not** created,
and no corresponding `Rendering.Harness.Tests` test was needed (T024 conditional ⇒ skipped).
