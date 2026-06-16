# Outstanding Issues — Faithfulness Audit of the Missing-Features Plan

**Report date**: 2026-06-16 17:05 UTC
**Author**: engineering verification pass (build + 5 parallel source audits)
**Repo**: FS.GG.Rendering — `main` at `612e1c6` (Ant Design Charts adoption, D2C.1)
**Scope**: A detailed, file-evidenced inventory of everything still outstanding against the
plan `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md` (Workstreams A–G),
after auditing whether the *claimed-done* work was faithfully implemented.

---

## 0. Audit method & headline

This is not a re-statement of the plan's checklists. Every claim was verified against actual
source: agents opened the spec/test/impl files (not just task checkmarks), ran the design-token
generator's `--check` drift mode, ran the Workstream-F feature tests live
(`dotnet test --filter "Feature12|Feature13"` → **32 passed, 0 skipped**), exercised the gallery
coverage check end-to-end, and the full solution was rebuilt.

**`dotnet build FS.GG.Rendering.slnx -c Release` → Build succeeded, 0 Warning(s), 0 Error(s)
(~37s).**

**Headline**: every workstream item *claimed complete* is faithfully implemented — contract
chain honored, real semantic tests driving real wired paths, no fake passes, no phantom
projects, no layering violations, no smuggled JS/React/AntV dependency, committed surface
baselines clean against a clean working tree. The outstanding work falls into three buckets:

1. **Two latent surface-drift-gate gaps** (§1) — real correctness issues that do *not* redden CI
   today, which is exactly why they are holes. **These are the only actionable defects.**
2. **One watch-item** (§2) — a pinned-package vs. live-catalog divergence that is correct today
   but will (correctly) fail later.
3. **Deliberately-deferred backlog** (§3–§9) — D2.2/D2.3, D3, G2–G4, B, A4/A7, E1, E4, F-none.
   These match the plan's own sequencing; they are not abandoned or faked.

---

## 1. Latent surface-drift-gate gaps (actionable defects)

The surface-drift gate is the plan's single most fragile CI interaction (plan §11.2). It works by
having `gate.yml` regenerate baselines via `scripts/refresh-surface-baselines.fsx` and then
`git diff --quiet -- tests/surface-baselines`. Two integrity holes exist in that machinery.

### 1.1 `FS.GG.UI.Color` is a packable public package with NO surface baseline — UNGUARDED

- **What**: `src/Color/Color.fsproj` declares `PackageId=FS.GG.UI.Color`, `IsPackable=true`, and
  ships **public** API surface (`Contrast.fsi`, `Palettes.fsi`; only `ColorPolicy.fs` is internal).
  It is registered in `FS.GG.Rendering.slnx`.
- **The gap**: `scripts/refresh-surface-baselines.fsx` has **no row** for it (its table lists 12
  packages; Color is absent), and there is **no committed**
  `tests/surface-baselines/FS.GG.UI.Color.txt`.
- **Why it's latent / why it matters**: `gate.yml` only diffs the *outputs the script itself
  produces*. Because the script never emits a Color baseline, a future public-surface change to
  `FS.GG.UI.Color` (e.g. a new public `val`, a renamed type) would pass the drift gate
  **unguarded**. The gate is silently blind to one of the twelve shipped packages.
- **Fix** (low-risk, ~1 change): add `"FS.GG.UI.Color", "Color"` to the package table in
  `scripts/refresh-surface-baselines.fsx`, run the script to generate
  `tests/surface-baselines/FS.GG.UI.Color.txt`, and commit both in the same change (per the
  plan's "regenerate baselines in the same change, never a follow-up" rule).
- **Severity**: Medium. No current breakage; closes a real coverage hole in the contract gate.

### 1.2 `readiness/surface-baselines/` does not exist but the release tier reads from it

- **What**: the release-tier test `tests/Package.Tests/SurfaceAreaTests.fs:18-19` reads committed
  baselines from `readiness/surface-baselines/`. `template/capabilities.yml` also points at
  `readiness/surface-baselines/...`.
- **The gap**: that directory **does not exist** in the repo (only `readiness/parity/` exists).
  The authoritative gate (`refresh-surface-baselines.fsx` + `gate.yml`) uses a *different*
  location, `tests/surface-baselines/`. No copy/generate step was found wiring one to the other.
- **Why it matters**: the release-tier `SurfaceAreaTests` would fail on the missing directory, or
  silently relies on a generation step that is not present in-repo. It is a second, divergent
  baseline location from the gate's — a latent split in the surface-contract machinery.
- **Fix**: decide one canonical baseline location. Either (a) point `Package.Tests` and
  `template/capabilities.yml` at `tests/surface-baselines/`, or (b) add an explicit
  generate/copy step that populates `readiness/surface-baselines/` from the canonical source.
- **Severity**: Medium (release-tier only; does not affect the required `gate.yml`).

> Note: the `internal`-modifier guard in `SurfaceAreaTests.fs:167` only matches lines beginning
> with `private `/`internal `/`public ` — it does **not** match `let private`/`let internal`.
> This is consistent with E1 (§7.1) being a cosmetic Tier-2 cleanup rather than a gate, but it
> means the redundant-modifier cleanup is unenforced.

---

## 2. Watch-item — gallery coverage test is pinned to the 52-control package

- **What**: feature 123's coverage test (`samples/ControlsGallery/.../CoverageTests.fs` +
  `CoverageMap.fs`) reads `FS.GG.UI.Controls.Catalog.supportedControls` from the **packaged**
  `FS.GG.UI.*` (v0.1.0-preview.1 from the local feed) and asserts a 52-control bijection across
  10 pages. Verified live: `coverage-check` → `52/52 controls mapped, 10 pages, 0 unreferenced,
  0 duplicated`.
- **The divergence**: the *live* `src/Controls/Catalog.fs` has grown to **96 controls** via the
  132 (Ant generic controls) and 133 (chart) work. The gallery consumes the pinned 52-control
  package, so its test legitimately passes today.
- **Why it's not a defect (yet)**: 123 as scoped (52 controls) is faithful. The hard-coded
  `Expect.equal ... 52` and the bijection assertion will fail — *correctly* — only when the
  package is repacked/bumped past 52. That is the intended "keeps the gallery honest" behavior.
- **Action**: when the `FS.GG.UI.Controls` package is next repacked, expect the gallery coverage
  test to go red; either extend the gallery pages to cover the new controls (G-series work) or
  re-scope the assertion deliberately. No action needed until then.

---

## 3. Workstream B — perf-corpus skips (environment-blocked)

**Status: 18 skips, verified exactly; honestly env-blocked; partial delivery, not abandoned.**

- The skip accounting is exactly honest — documented 18 = actual `ptest`/`ptestList`:
  - `tests/Elmish.Tests/Feature109CorpusTests.fs:257` — `ptestList` over 14 corpus scenarios.
  - `tests/Elmish.Tests/Feature109BaselineReportTests.fs:150` — `ptestList` over 3 cases.
  - `tests/Controls.Tests/TypedControlContractTests.fs:79` — 1 `ptest` (FSI transcript; this is
    Workstream E4, §7.4).
- The Feature109 perf tests are genuinely pending, **not deleted or faked**.
- **Why still blocked**: these tests want the *faithful-vsync / present-timing* perf path, which
  needs the live present loop — blocked headlessly in this container
  (`docs/harness/capability-baseline.md`). `SKIPPED-TESTS.md` (R5 status, 2026-06-14) honestly
  records that the **offscreen-render-throughput** tier *was* delivered, while the vsync path
  remains pending.
- **Outstanding tasks** (plan §5.2): **B1–B3** (drive a subset of Feature109 goldens from the
  deterministic offscreen-throughput metrics so they un-skip headlessly now; commit golden
  fixtures; wire `PERF_CORPUS_REGEN`) are *doable now without new infra* and would remove most of
  the 14+3 perf skips. **B4** (provision a display/GL/uinput-capable CI runner; flip vsync-cadence
  + T-uinput from advisory to executed) is an **ops dependency outside the codebase**.
- > Note (not a discrepancy): 4 additional `skiptest` markers exist
  > (`SkiaViewer.Tests/Audit_ReplayCache.fs:46`, `SkiaViewer.Tests/Tests.fs:955` & `:989`,
  > `Smoke.Tests/Tests.fs:37`). These are runtime-conditional env-adaptive skips (degrade-and-
  > disclose), outside `SKIPPED-TESTS.md`'s static-deferral count of 18.

---

## 4. Workstream A — uinput kernel tier (environment-blocked)

**Status: pure + x11-xtest cut done & faithful; A4/A7 honestly deferred.**

- The pure backend, the distinct `InputBackend` (`Pure|X11XTest|Uinput`) type, the real `input`
  CLI subcommand, and 19 harness unit tests are all real and verified (feature 122).
- **Outstanding** (plan §4.3): **A4** (the `uinput`/`ydotool` kernel-drive executor) and **A7**
  (the full T-uinput integration walk + capability-baseline backend matrix). The `Uinput` arm
  currently returns an **honest skip** even when `/dev/uinput` is present — reason
  `"kernel-drive executor deferred to Workstream A4 (env-gated proof)"` — it does not fabricate a
  pass or hang. This is correct degrade-and-disclose behavior.
- **Why blocked**: `/dev/uinput` is absent on the dev box; proving the kernel path requires a
  capable runner (the same runner Workstream B4 needs).

---

## 5. Workstream D — concrete themes & kits (net-new, not started)

D1 (layer split, 125), D2.1 (Ant theme + ~19 net-new generic controls, 132), and D2C.1 (Ant
Charts as design-language-only, 133) are all faithfully implemented and merged. The remainder of
Workstream D is genuinely not started:

### 5.1 D2.2 — Fluent theme (not started)
- `src/Themes.Fluent` does **not** exist; no `fluent` project in `FS.GG.Rendering.slnx`.
- **Deliverable** (plan §7.3): a `FS.GG.UI.Themes.Fluent` assembly providing a `Theme` instance
  over the shared `DesignSystem` slots + visual-state styling, depending **only** on
  `DesignSystem` (no control forks), reusing the D2.1 machinery (`IntentPolicy`, `StyleResolver`,
  token taxonomy). Must extend the "one control set, many themes" parity test to Fluent and add a
  committed `tests/surface-baselines/FS.GG.UI.Themes.Fluent.txt` + a row in
  `refresh-surface-baselines.fsx` in the same change.

### 5.2 D2.3 — Material theme (not started)
- `src/Themes.Material` does **not** exist; no `material` project in the slnx.
- **Deliverable**: identical shape to D2.2 for Material; same parity-test extension and surface-
  baseline obligations.

### 5.3 D3 — design-specific kits (not started)
- **No kit projects exist** (no `*kit*` project, none in the slnx).
- **Deliverable** (plan §7.4): at least `AntDesign.Form` (validation-flow layout) and
  `AntDesign.Table` (filtering/sorting/empty-state) as opinionated compositions over controls +
  the Ant theme, depending on `Controls` + the Ant theme. This is the proof that a kit is
  justified only when it adds composition/workflow behavior beyond styling. The enterprise
  page-template recipes (workbench/list/detail/form/result/exception) — already authored as
  `status: groundwork` docs under `docs/product/ant-design/templates/` in F6 — are the target
  recipes to realize as running kits.

---

## 6. Workstream G — sample applications (G1 done; G2–G4 not started)

G1 (Controls Gallery, 123) is faithfully implemented: a real public-surface app (no
`InternalsVisibleTo`), a genuine catalog-bijection coverage test (52/52, 10 pages, verified
live), and a seeded headless deterministic-evidence mode. Outstanding:

### 6.1 G2 — curated games + productivity slice (not started)
- **Deliverable** (plan §10.2): a representative slice of the archived 33 sample specs — e.g.
  Tetris + Snake + Pong (games) and Kanban + Todo + Calendar (productivity) — each with the
  seeded headless-evidence harness, chosen to maximize distinct control/input coverage. Rebrand
  `FS.Skia.UI.*` → `FS.GG.UI.*` on import; build against the public package surface only.

### 6.2 G3 — Ant restyle + enterprise templates (not started; prerequisites now landed)
- **Deliverable** (plan §10.2): re-skin the showcase under the Ant theme (D2.1 ✓) and realize the
  workbench/list/detail/form/result/exception page templates as productivity-app demos. The F-
  and D2.1-series prerequisites are now in place, so this is unblocked.

### 6.3 G4 — wire samples as evidence (not started)
- **Deliverable**: feed the deterministic sample runs into the harness/perf corpus (ties to
  Workstream B) and the advisory CI tier, so samples are *checked*, not just shipped.

> Risk to manage (plan §10.5): 33 sample specs are a large maintenance surface. Ship a curated
> G2 slice + G3 restyle first; treat the rest as a disclosed backlog, not a batch.

---

## 7. Workstream E — cleanup follow-ups (E2/E3 done; E1/E4 open)

E2 (memo-cache narrative) and E3 (`renderHash` alpha-insensitivity doc) are verified done. Two
remain:

### 7.1 E1 (DF-1) — strip redundant access modifiers (open)
- **What**: `src/Controls/RetainedRender.fs` still has `let private`/`let internal` top-level
  bindings (lines incl. 189, 192, 230, 260, 416, 537, 638, 767) and
  `src/Controls.Elmish/ControlsElmish.fs` has ~19 `let private` (lines incl. 243, 328, 434, …,
  1289). Each of these files has a paired `.fsi`, so the modifiers are redundant (constitution
  Principle II discourages doubling up; visibility is governed by the `.fsi`).
- **Why low-risk**: behavior-neutral Tier-2 tidy. Unenforced by CI (the guard at
  `SurfaceAreaTests.fs:167` does not match the `let private`/`let internal` form — see §1.2 note).
- **Outstanding**: remove the redundant modifiers; confirm build + suite green; zero surface delta.

### 7.2 E4 — FSI transcript fixture (open)
- **What**: the single non-perf skip remains at `tests/Controls.Tests/TypedControlContractTests.fs:79`
  (`ptest`, FSI transcript fixture from the old repo).
- **Outstanding** (plan §8): add a current FSI transcript under this repo and un-skip the test per
  its named un-skip trigger. This is the last skip outside the env-blocked perf corpus.

---

## 8. Workstream F — Ant Design adoption (COMPLETE)

For completeness: **F1–F6 (features 126–131) are all faithfully implemented and merged** — token-
taxonomy generator (`scripts/generate-design-tokens.fsx`, real DTCG→F#, `--check` ran clean),
`ColorPolicy` (wcag reuses `Contrast.verdict`; ant policy + reports), `--designSystem` template
parameter, the central `StyleResolver` **genuinely wired** into `buttonGeom`
(`src/Controls/Control.fs:1163`, intent threaded from `faithfulContent` at `:1674`), public-surface
promotion (`StyleResolver.fsi` + generated `DesignTokensExt.fsi`, baseline rows present, decision
record 0004), and the 11 pattern docs + 6 templates + advisory skill + central source-of-truth
hub. "No React/DOM" is honored (every React/DOM mention is an explicit *not-adopted* disclaimer).
**No outstanding items in Workstream F.**

> Correction to the plan's hint: the F4 resolver call site is `Control.fs:1163`, not the ~826 the
> plan estimated (826 was the *original* location of the structural-base literals that were
> relocated into `StyleResolver.baseStyleFor`).

---

## 9. Workstream C — RetainedRender conformance backfill (COMPLETE)

For completeness: **all 14 feature folders (092, 093, 095, 096, 097, 099, 103, 110, 113, 114,
116, 117, 120, 121) are faithfully backfilled** — non-stub spec/plan/tasks (308 tasks `[X]`, 0
open), ~243 real Expecto/FsCheck test cases driving the actual wired paths against real
production symbols, zero public-surface delta. Test density varies (121 has 2 tests; 114/116 have
27–28), but each exercises its feature's real symbol with meaningful oracles. **No outstanding
items in Workstream C.**

---

## 10. Prioritized outstanding-issues summary

| # | Item | Workstream | Type | Env-blocked | Severity | Effort |
|---|---|---|---|---|---|---|
| 1 | `FS.GG.UI.Color` missing surface baseline (unguarded gate) | cross-cutting | **Defect (latent)** | No | Medium | Low |
| 2 | `readiness/surface-baselines/` missing (release-tier reads it) | cross-cutting | **Defect (latent)** | No | Medium | Low |
| 3 | Gallery coverage pinned to 52 vs live 96 catalog | G1 | Watch-item | No | Low | n/a until repack |
| 4 | B1–B3 offscreen-derived perf goldens (un-skip subset) | B | Deferred | No | Med | Med |
| 5 | E1 (DF-1) redundant access modifiers | E | Deferred (tidy) | No | Low | Low |
| 6 | E4 FSI transcript fixture (last non-perf skip) | E | Deferred | No | Low | Low |
| 7 | D2.2 Fluent theme | D | Deferred (net-new) | No | Med | Med |
| 8 | D2.3 Material theme | D | Deferred (net-new) | No | Med | Med |
| 9 | D3 Ant kits (Form/Table) + enterprise templates | D | Deferred (net-new) | No | Med–High | High |
| 10 | G2 curated games + productivity slice | G | Deferred (net-new) | GL interactive only | Med | High |
| 11 | G3 Ant restyle + enterprise template demos | G | Deferred (net-new) | No | Med | Med–High |
| 12 | G4 samples-as-evidence wiring | G | Deferred | No | Low | Med |
| 13 | A4/A7 uinput kernel executor + integration | A | Deferred | **Yes** (`/dev/uinput`) | Med | Med |
| 14 | B4 capable CI runner (vsync/uinput executed) | B | Deferred (ops) | **Yes** (ops) | Med | Ops |

**Recommended next cut** (low-risk, no env, closes real holes first): items **1 + 2** (surface-
baseline gaps) and **5 + 6** (E1/E4 tidy) — all headless, behavior-neutral or doc-only. Then the
strategic net-new arc continues with **D2.2/D2.3** (Fluent/Material — reuse all D2.1 machinery),
**D3** (kits), and **G3** (Ant restyle, the visible payoff). **B1–B3** removes most of the 18
skips without new infra. The env-blocked items (A4/A7, B4) wait on a capable runner.

---

### Appendix — verification artifacts
- Build: `dotnet build FS.GG.Rendering.slnx -c Release` → 0 warnings / 0 errors / ~37s.
- Live test run (Workstream F): `dotnet test --filter "Feature12|Feature13"` → 32 passed, 0 failed,
  0 skipped.
- Generator drift: `dotnet fsi scripts/generate-design-tokens.fsx --check` → exit 0,
  "design-tokens: up to date".
- Working tree: `git status -sb` clean against `origin/main`; `git diff -- tests/surface-baselines`
  empty.
- Source plan: `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md`.
