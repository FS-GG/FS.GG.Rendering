# Phase 0 — Research & Root-Cause Decisions

All four conditions were investigated against the live repo. Each below records the **Decision**,
**Rationale**, and **Alternatives considered**. The standing assumption applies: the catalog count,
the ControlsGallery 52-vs-97 question, the exact identity of the 97th control, and the flaky GL test
set are **confirmed by the early baseline run** scheduled in the Foundational phase, not assumed final
here.

---

## R1 — Stale sample package pins (US1, FR-001)

**Findings.** ~64 stale `FS.GG.UI.*` pins across four samples (each with `.Core` + `.App` + `.Tests`):
- **AntShowcase** — all pinned `0.1.32-preview.1` (source moved to e.g. Controls `0.1.46`, Scene
  `0.1.37`, SkiaViewer `0.1.47`, DesignSystem `0.1.44`, Testing `0.1.37`, Themes.AntDesign `0.1.36`…).
- **SampleApps** — all pinned `0.1.0-preview.1` (not in the 202 disclosure list, but stale by the same
  rule; fixed coherently by the same mechanism).
- **SecondAntShowcase** — pinned `0.1.47-preview.1` uniformly, which is *ahead* of several source
  versions (only SkiaViewer happens to match) → still incoherent.
- **ControlsGallery** — all pinned `0.1.0-preview.1`.

Package versions are **per-project** `<Version>` literals in `src/**/*.fsproj` (no single
`FsSkiaUiVersion` property). The *Feature163* gate
(`tests/Package.Tests/Feature163PackageFeedValidationTests.fs`) reads `src/**` versions as the oracle
and asserts each sample pin equals it; today it only covers **AntShowcase**.

**Decision.** Correct pins forward using the canonical workflow, not by hand:
`tools/Rendering.Harness package-feed --mode refresh --pack --sample <each sample>` (wrapped by
`scripts/refresh-local-feed-and-samples.fsx`). `--pack` packs current sources into the local feed so
the bumped pins resolve; `refresh` rewrites every sample pin to the source version coherently. Then
**extend the *Feature163* gate to cover all four samples** (AntShowcase, SampleApps, SecondAntShowcase,
ControlsGallery) so pin coherence is enforced going forward, not just for AntShowcase.

**Rationale.** Hand-editing 64 versions is error-prone and re-introduces incoherence on the next bump.
The `package-feed` tool is the repo's source-of-truth mechanism (referenced by SkillParity), guarantees
the feed actually contains the pinned versions (`--pack`), and emits a proof artifact. Extending the
gate closes the hole that let SampleApps/SecondAntShowcase/ControlsGallery drift unguarded.

**Alternatives considered.** (a) Hand-edit each `.fsproj` — rejected: error-prone, no feed guarantee.
(b) Collapse all packages to one coherent `-p:Version` — rejected: re-litigates the 201/202 versioning
model (out of scope per spec Assumptions); the per-project versions are the working baseline.

---

## R2 — Missing design-system validation report (US2, FR-002)

**Findings.** The *Feature128* gate (`tests/Package.Tests/Feature128DesignSystemTemplateTests.fs`)
reads `specs/128-design-system-template-param/readiness/design-system-template-validation.md` and runs
GV-1..GV-7 against its contents (covered-values, per-choice `build=pass`, WCAG `overall=FAIL`/ANT
`overall=PASS`, divergent-pairing, no-overclaim note, `result: pass`). The path is under
`specs/*/readiness/` which is **gitignored** (`.gitignore` line 45), the directory does not exist in a
fresh checkout, and **nothing auto-generates it** before the gate — so GV-8 fires "report missing" and
GV-1..GV-7 cannot evaluate. The generator `scripts/validate-design-system-template.fsx` has two phases:
- **verdict-core** (always runs; **no env flag, no `dotnet new`, no build, no GL/display, no network**):
  enumerates `designSystem` choices, evaluates the pairing catalog, compares to committed oracles
  (`docs/reports/color-policy-{wcag,ant}.md`), and asserts divergence + no-overclaim disclosure. This
  phase produces exactly the tokens GV-1..GV-7 assert.
- **live scaffold+build** (`FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1`): `dotnet new` per value + real build.

**Decision.** Make the gate **self-provisioning from verdict-core**: in the Feature128 test fixture
(one-time setup), if the report is absent, generate it via the verdict-core path (invoke the script's
verdict-core, or a shared function it exposes) so GV-1..GV-7 evaluate a freshly-produced, current
report. The heavy live scaffold+build phase stays env-gated and optional. This satisfies FR-002 ("MUST
NOT be red by default in a fresh checkout because the report is absent") and spec Assumption #4 ("the
gate is made robust to a fresh checkout … rather than requiring every contributor to hand-run a
generator").

**Rationale.** verdict-core is deterministic, fast, and dependency-free, so running it as fixture setup
adds no environment requirement and no flakiness. Committing the report is not viable (readiness/ is
gitignored by policy). Robustifying the gate is exactly what the spec assumption directs.

**Alternatives considered.** (a) Commit the report — rejected: violates the gitignore policy for
transient readiness artifacts; would go stale silently. (b) Un-ignore the path — rejected: broader
policy change, out of scope. (c) Require contributors to run the env-gated generator — rejected by
FR-002/Assumption #4 (red-by-default in fresh checkout). (d) Wire generation into `build.fsx`/CI only —
rejected: leaves `dotnet test tests/Package.Tests` red in isolation, which is how the baseline runs it.

---

## R3 — Drifted sample assertions (US3, FR-003)

**Findings.** The global control `Catalog.supportedControls` (FS.GG.UI.Controls) grew **96 → 97**.
Effects observed by running the sample suites:
- **AntShowcase.Tests/CoverageTests.fs:22** — `Expect.equal (List.length catalog) 96` → actual 97;
  and `result.Unreferenced` non-empty because the 97th control is assigned to no page.
- **SecondAntShowcase.Tests** — `CoverageTests.fs:22` same 96→97; plus
  `Feature172CoverageRegressionTests` / `Feature173LiveResponsivenessRegressionTests` /
  `InteractionTests` fail because the 97th control has **no interaction contract and no display-only
  reason** (MissingContractOrReason non-empty; coverage not clean).
- **ControlsGallery.Tests/CoverageTests.fs:20,22,23** — asserts `52`. ControlsGallery has its **own**
  curated `CoverageMap.catalogIds()` (in `ControlsGallery.Core`), a deliberate 52-control / 10-page
  subset (per the file's own doc comment). The pin bump (US1) changes what `ControlsGallery.Core`
  resolves; whether its catalog stays 52 or now reflects 97 **must be confirmed live**.

**Decision.** Correct to **true current values without weakening**:
1. AntShowcase + SecondAntShowcase count asserts `96 → 97`.
2. Assign the new 97th control to a page in each affected sample so `Unreferenced` is genuinely empty
   (a real bijection, not a loosened check), and in **SecondAntShowcase** give it an interaction
   contract or an explicit display-only reason so `MissingContractOrReason` is genuinely empty.
3. **ControlsGallery**: the early baseline run decides the true intent. If ControlsGallery is meant to
   remain a curated 52-subset, ensure `CoverageMap.catalogIds()` returns exactly that subset and the
   bijection holds at 52; if its catalog legitimately tracks the full set, update counts to the true
   value and assign every control. Either way the assertion is set to the **real** value and the
   bijection/`Set.equal` checks stay intact — never relaxed to `>` or deleted.

**Rationale.** FR-003 and Constitution V forbid loosening/deleting. The `Unreferenced` and
`MissingContractOrReason` assertions only pass if the 97th control is actually placed and classified —
which is the honest fix, not a count edit. ControlsGallery's 52 is a designed covenant, so its fix is
intent-dependent and resolved by observation.

**Alternatives considered.** (a) Change `=` to `>=`/`>` or delete the asserts — rejected (Constitution
V). (b) Pin samples back to old packages to keep 96 — rejected: contradicts US1 and spec Assumption
("stale pins corrected forward"). (c) Exclude the 97th control from samples — rejected unless it is the
*true* current state; the samples are assumed correct/current and expectations are what lagged.

---

## R4 — SkiaViewer GL flakiness (US4, FR-004/FR-005)

**Findings.** Framework is **Expecto**; skip API is `skiptest : string -> unit`. The repo already has
the **canonical deterministic pattern** in `tests/SkiaViewer.Tests/Audit_ReplayCache.fs`: a
`rasterAvailable` probe (`try use s = SKSurface.Create(SKImageInfo(8,8)) in not (isNull s) with _ ->
false`) and a `tierSkip` helper emitting `SKIPPED(tier=…): … (Constitution VI).`. The **flaky** tests
do *not* use this: `Tests.fs` "runApp"/"persistent run" cases branch on `livePersistentTestsEnabled()`
and, when enabled on a headless host, call `Viewer.runApp`/`Viewer.run` (native window + GL context)
which fail/hang nondeterministically; the screenshot/raster tests (Feature063/086/136/140) call
`Viewer.captureScreenshotEvidence` → `SKSurface.Create` without an upfront capability probe.

**Decision.** Apply the existing `rasterAvailable`/`tierSkip` idiom uniformly to the flaky raster and
live-GL tests: probe the specific capability the test needs (offscreen raster surface; or native
window/GL context) **before** exercising it, and on absence emit a deterministic `skiptest` with a
written rationale citing Constitution VI. When the capability **is** present, run the full assertions
unchanged (no behavior loss). State the resulting skip count (SC-005).

**Rationale.** Reuses the repo's own proven, constitution-cited idiom — minimal, idiomatic
(Constitution III), and turns an intermittent red into a stable pass-or-explicit-skip. It explicitly
distinguishes "no window-system/GL here" from a real defect (Constitution VI / edge case): the probe
gates only the environment-sensitive path; a genuine failure inside an available context still fails
loudly.

**Alternatives considered.** (a) `try/catch`-swallow failures — rejected: hides real defects, forbidden
by Constitution VI. (b) Always-skip the GL tests — rejected: loses real coverage where GL *is*
available and would mask defects. (c) Retry-until-pass — rejected: masks nondeterminism instead of
making outcomes deterministic.

---

## Cross-cutting decision — verification harness

**Decision.** Use the **existing** `scripts/baseline-tests.fsx` as the sole green/red oracle; do not
introduce a new harness (spec Assumption). Determinism (SC-004) is measured by **5 consecutive**
`SkiaViewer.Tests` runs yielding an identical pass set. The early baseline run in the Foundational
phase establishes the confirmed before-state; the final run proves SC-001..SC-006.

**Open items resolved by the early baseline run (not blocking gates):** exact current catalog count;
ControlsGallery 52-vs-97 intent; identity of the 97th control; the precise flaky GL test set and
whether any "flake" is actually a defect. None are NEEDS CLARIFICATION — the spec Assumptions resolve
the policy; only the live numbers remain, by design.
