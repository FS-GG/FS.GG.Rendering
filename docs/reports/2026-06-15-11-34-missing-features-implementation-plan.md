# Implementation Plan — Missing & Incomplete Features

**Report date**: 2026-06-15 11:34
**Author**: engineering analysis pass
**Repo**: FS.GG.Rendering (migration stages R1–R8 complete; product source live on `net10.0`)
**Scope**: A build plan for the work that is genuinely missing or only partially landed, organized into independently shippable workstreams under the project's `Spec → .fsi → semantic tests → implementation` contract.

---

## 1. Executive summary

A focused audit (build run + four parallel source investigations) revised the headline "what's missing" picture in three important ways:

1. **The rendering harness is far more complete than the README implies.** The README (Stage R5, "in progress") says T2 live-X11 and faithful-vsync perf are *pending*. In fact both are **fully implemented** in `tests/Rendering.Harness/Live.fs` (`runLive`, `runFaithfulPerf` + a manual GL-swap `__vsyncprobe` child). What is actually missing in the harness is the **input-backend layer** (`Input.fs` — `pure` / `x11-xtest` / `uinput`), the **T-uinput executor**, and the wiring of the stubbed `input` CLI subcommand.

2. **Most accreted RetainedRender features are already implemented in code but not under contract.** Features 092, 097, 099, 103, 110, 113, 114, 116, 117, 120, 121 have real bodies in `src/Controls/RetainedRender.fs` and accreted surface in `RetainedRender.fsi`, but only some have `readiness/` evidence and **none except 091 have a `spec.md`/`plan.md`/`tasks.md`**. The missing work here is largely **conformance backfill** (the same pattern feature 091 used), not new product behavior.

3. **The layer split is genuinely greenfield.** Design-system primitives, themes, and kits are physically bundled inside the single `FS.GG.UI.Controls` assembly. Only `Light`/`Dark` themes exist; **Ant/Fluent/Material are named-only**, and **no design-specific kits exist at all**. This is the largest net-new effort.

The build is green (`dotnet build -c Release` → 0 warnings, 0 errors). 18 tests are honestly skipped (`ptest`/`ptestList`): 17 perf-corpus/baseline tests waiting on a faithful-vsync capture path + committed goldens, and 1 FSI-transcript fixture from the old repo.

This plan defines **five workstreams (A–E)**, each independently valuable, with phased tasks, file-level targets, evidence requirements, constitution-gate notes, and a suggested sequencing. Workstreams A, C, and E are low-risk and near-term; B is environment-blocked (needs a capable CI runner); D is the strategic, multi-feature effort.

---

## 2. Corrected current-state baseline

### 2.1 Build & test
| Item | State |
|---|---|
| `dotnet build FS.GG.Rendering.slnx -c Release` | **Passes** — 0 warnings, 0 errors (~56s), 10 src + 14 test/harness assemblies |
| Default local test tier (gate) | Deterministic suites pass headless: Color, Scene, Layout, Input, KeyboardInput, Elmish, Controls, Testing, Lib |
| Skipped tests | **18**: 17 perf-corpus/baseline (Feature109) + 1 FSI-transcript (TypedControlContractTests) — all honest `ptest`/`ptestList` |
| GL-gated suites | `SkiaViewer.Tests`, `Smoke.Tests` — degrade-and-disclose when GL not present |
| Imported-mechanism audit (spec 006) | **Complete** — `docs/audit/mechanism-audit.md`: 14/14 mechanisms work as advertised, 0 overstated, 2 cosmetic divergences |

### 2.2 Harness tiers — actual status (corrects the README)
| Tier | Executor | Status |
|---|---|---|
| T0 deterministic | `Tiers.runOffscreen T0` | ✅ implemented |
| T1 offscreen readback | `Tiers.runOffscreen T1` | ✅ implemented |
| T2 live X11 window+input | `Live.runLive` (Xvfb `:99` + EGL hint, xdotool/maim) | ✅ implemented |
| T3 offscreen throughput | `Perf.runPerf` | ✅ implemented |
| T3 faithful vsync (`--mode paced-native`) | `Live.runFaithfulPerf` + `__vsyncprobe` swap loop | ✅ implemented |
| **Input backends** (`pure`/`x11-xtest`/`uinput`) | — | ❌ **missing** (`Input.fs` does not exist; CLI `input` stubbed) |
| **T-uinput** kernel input | — | ❌ **missing** (no executor; env `/dev/uinput` absent) |

CI: `gate.yml` (required, deterministic + surface-drift + docs + harness T0), `capability.yml` (advisory weekly cron; T2/T3/T-uinput; runner is still `ubuntu-latest` with a TODO to move to a display/GL/uinput-capable runner), `release.yml` (release-only package + template tests).

### 2.3 RetainedRender feature accretion — actual status
| Feature | Name | Impl in `.fs` | `readiness/` | `spec.md` |
|---|---|---|---|---|
| 091 | Wire reconciler onto render path | ✅ | ✅ | ✅ **Final** |
| 092 | Wire retained identity state (theme-reuse) | ✅ | ✅ | ❌ |
| 097 | Layout cache | ✅ | ❌ | ❌ |
| 099 | Live animation clock | ✅ | ✅ | ❌ |
| 103 | Visual-state cross-fade | ✅ | ✅ | ❌ |
| 110 | Retained-ID → authored-control-ID | ✅ | ❌ | ❌ |
| 113 | Memoization seam (DataGrid) | ✅ | ❌ | ❌ |
| 114 | Virtualization counts | ✅ | ❌ | ❌ |
| 116 | Picture cache (LRU) | ✅ | ❌ | ❌ |
| 117 | Text-measure cache (LRU) | ✅ | ❌ | ❌ |
| 120 | Fingerprint (FNV-1a) + replay cache | ✅ | ❌ | ❌ |
| 121 | Advance all clocks (no-alloc idle) | ✅ | ❌ | ❌ |
| 093 | Visual-state style layer | (style layer) | ✅ | ❌ |
| 095 | Lookless slot composition | (controls) | ✅ | ❌ |
| 096 | Runtime visual-state bridge | (controls) | ✅ | ❌ |

### 2.4 Layer architecture — actual status
- **One assembly** `FS.GG.UI.Controls` holds controls + design-system + themes (child namespace `…Controls.Theming` is the only structural seam).
- Concrete themes: **only `Theme.light` / `Theme.dark`** (`Theme.fs`). Ant/Fluent/Material **do not exist** (named-only in `layering.md`/`module-map.md`).
- Kits (`AntDesign.Form`, `.Table`, `.Result`, `.Descriptions`): **none exist**.
- `Theme` type (`Types.fsi`) is missing `Success`/`Warning` fields that `DesignTokens` carries — `Style.fs` reads tokens directly as a workaround.
- Surface baselines: **9 committed** `.txt` under `tests/surface-baselines/`; the drift gate fails on any public-type/module change or any new untracked baseline. **A layer split that adds a package requires a new row in `scripts/refresh-surface-baselines.fsx` + a committed baseline.**

---

## 3. Gap inventory → workstreams

| # | Gap | Net-new vs backfill | Risk | Env-blocked? |
|---|---|---|---|---|
| **A** | Harness input backends + T-uinput tier | Net-new code | Low–Med | uinput tier only |
| **B** | Unblock 17 perf-corpus tests (faithful-vsync goldens + capable runner) | Wiring + ops | Med | **Yes** (CI runner) |
| **C** | Spec/test backfill for accreted RetainedRender features (092–121) + close 093/095/096 | Backfill | Low | No |
| **D** | Physical layer split + concrete themes (Ant/Fluent/Material) + kits | Net-new (large) | High | No |
| **E** | Cleanup follow-ups (DF-1, memo counter narrative, renderHash alpha, FSI fixture) | Tidy | Low | No |

---

## 4. Workstream A — Harness input backends & T-uinput tier

**Goal**: Land the three declarative input backends and the kernel-input tier so the harness can prove the *input → MVU → repaint* path, completing the Stage R5 harness surface. Maps to the unfinished tasks the harness spec already names (T010 pure, T014 x11-xtest, T019/T020 uinput, T022 integration).

### 4.1 Current state
- `tests/Rendering.Harness/Cli.fs` (~line 112): the `input` subcommand prints "input backends pending: pure … x11-xtest … uinput wire next" and returns exit 2.
- `tests/Rendering.Harness/X11.fs` already has the live primitives: `X11.clickAt windowId x y` and `X11.sendKey windowId key` (xdotool), used by `Live.runLive`.
- `Probe.fs` already computes `facts.UinputAvailable` (checks `/dev/uinput` + `/dev/input`).
- `RunPlan.fs` already encodes the degradation rule for the uinput tier (`if facts.UinputAvailable then Run else Skip "opt-in unavailable…"`) and the no-overclaim evidence schema (`AuthoritativeFor` / `NotAuthoritativeFor`).
- Product seams exist for the `pure` backend: `ControlsElmish.captureRespondsProof`, `Perf.runScript`. Host-agnostic input abstractions exist: `ViewerPointerInput`/`MapPointer` (feature 085), `ViewerKey`/`normalizeEventWithModifiers`.

### 4.2 Design
Author `tests/Rendering.Harness/Input.fsi` + `Input.fs` defining a single declarative **input script** model interpreted by a selectable backend. The script is backend-agnostic; only the interpreter differs.

```
type InputStep =
  | Click of x:int * y:int
  | Key   of string            // "space", "Right", "a"
  | Wait  of ms:int            // deterministic; injected, never wall-clock

type InputScript = { Name: string; Steps: InputStep list }

type Backend = Pure | X11XTest | Uinput

// pure: replay steps against the MVU model via ControlsElmish.captureRespondsProof / Perf.runScript;
//       deterministic, no live desktop, ProofLevel = Deterministic, authoritative ["input-msg-dispatch"].
// x11-xtest: drive a live viewer window via X11.clickAt / X11.sendKey (reuse Live.fs window discovery),
//       capture before/after PNG, assert visible pixel change; ProofLevel = LiveHost,
//       authoritative ["real-input"; "input-to-repaint"]; degrades like T2 (Skip no-display / Fail Wayland).
// uinput: drive evdev/libinput via ydotool against the live window; requires facts.UinputAvailable;
//       ProofLevel = KernelInput, authoritative ["evdev-libinput-input-path"]; clean Skip when /dev/uinput absent.

val run : Backend -> InputScript -> facts:ProbeFacts -> selfDll:string -> outDir:string -> Evidence.Evidence
```

The **T-uinput executor** is the `Uinput` arm of `Input.run` wired through `RunPlan.plan` so the run/skip/fail-classified decision stays in the already-tested pure planner — the executor only interprets.

### 4.3 Tasks
- **A1** — `Input.fsi` contract: `InputStep`, `InputScript`, `Backend`, `run`. (Spec → .fsi first per Principle I.)
- **A2** — `pure` backend: interpret `InputScript` against the Elmish model via `captureRespondsProof`/`Perf.runScript`; emit deterministic evidence. Headless-runnable → covered by the default gate tier.
- **A3** — `x11-xtest` backend: reuse `Live.fs` Xvfb/EGL window discovery + `X11.clickAt`/`sendKey`; before/after PNG diff; degrade exactly like T2.
- **A4** — `uinput` backend + T-uinput executor: shell `ydotool`; gate on `facts.UinputAvailable`; clean Skip evidence (`status:"skipped"`, exit 0) when device absent.
- **A5** — Wire `Cli.fs` `input` subcommand: `--backend pure|x11-xtest|uinput`, `--script <name>`; replace the stub.
- **A6** — Unit tests in `Rendering.Harness.Tests`: planner decisions per backend, `NotAuthoritativeFor` never empty, clean-skip exit code, deterministic `pure` replay golden.
- **A7** — Quickstart V5 (uinput degradation) + V6, and the full integration walk (harness spec T020/T022); update `docs/harness/capability-baseline.md` with the new backend matrix.

### 4.4 Acceptance / evidence
- `harness input --backend pure --script <name>` runs headless in the gate, deterministic, emits `run.json` with non-empty `NotAuthoritativeFor`.
- `harness input --backend uinput` on a machine without `/dev/uinput` exits 0 with `status:"skipped"` and a disclosed reason (no hang, no fake pass).
- `x11-xtest` proves input→repaint on a GL/X11 runner; skips cleanly headless.
- No product API change (harness-only); surface-drift gate unaffected.

### 4.5 Risks
- `ydotool` daemon model (`ydotoold`) may need a running socket; the executor must detect its absence and skip rather than hang. The kernel tier remains CI-inert until a capable runner exists (see Workstream B).

---

## 5. Workstream B — Unblock perf-corpus tests (faithful-vsync goldens + capable runner)

**Goal**: Remove the 17 `ptest`/`ptestList` skips in `Feature109CorpusTests.fs` / `Feature109BaselineReportTests.fs` by landing the deterministic perf-capture path they depend on and committing the perf goldens — and by moving the advisory capability CI to a runner that can actually exercise the live present loop.

### 5.1 Current state
- The faithful-vsync measurement **already exists** (`Live.runFaithfulPerf` + `__vsyncprobe`, `harness perf --mode paced-native`); measured baseline locks p50 ≈ 8.33 ms to a 119.93 Hz vblank (`docs/harness/capability-baseline.md`).
- The skipped tests need: (a) committed perf-golden fixtures under `docs/reports/_baselines/**`, (b) a **deterministic** perf-capture path the tests can call, and (c) byte-identical determinism.
- `capability.yml` runs T2/T3/T-uinput weekly but on `ubuntu-latest` (no display/GL/uinput) — a documented TODO; those tiers are currently inert/degrade-and-disclose.
- Spec 006 audit notes frame-rate-cap is verified for *throughput* but strict vsync cadence is `notAuthoritativeFor` the T3 throughput tier.

### 5.2 Tasks
- **B1** — Decide the golden source: drive Feature109 goldens from the harness's deterministic offscreen-throughput metrics (already headless + deterministic) rather than the vsync path, OR keep vsync goldens behind the capable-runner gate. **Recommendation**: split — offscreen-derived metric goldens become headless-deterministic and un-skip now; vsync-cadence assertions stay advisory until B4.
- **B2** — Add a deterministic perf-capture entry the tests call (stable seed, injected frame deltas, no wall-clock), emitting the metric shape the goldens compare. Reuse `Perf.runPerf` percentiles.
- **B3** — Commit the golden fixtures; wire the `PERF_CORPUS_REGEN` regeneration path into `scripts/` and document it. Un-skip the offscreen-derived subset (`ptestList` → `testList`).
- **B4** — Provision a display/GL/uinput-capable CI runner (self-hosted or a configured image); point `capability.yml` at it; flip the vsync-cadence + T-uinput assertions from advisory to actually-executed. **Ops dependency — outside the codebase.**
- **B5** — Update `SKIPPED-TESTS.md` as each subset un-skips (honest running tally).

### 5.3 Acceptance / evidence
- The offscreen-derived Feature109 subset runs green in the default gate (no skip), deterministic across runs.
- Remaining vsync-cadence tests run on the capable runner and are no longer `ptest` there; they degrade-and-disclose (not fail) on headless gate.
- `SKIPPED-TESTS.md` count drops from 18 toward 1 (the FSI fixture, handled in Workstream E).

### 5.4 Risks
- **Determinism is the hard part**: perf goldens must not encode machine-specific timing. Keep goldens to *counts/structure/relative* invariants (frames, allocations bucketed, work-reduction ratios), not absolute milliseconds. The faithful-vsync wall-clock numbers stay evidence artifacts, never golden-compared.
- Capable-runner provisioning is an ops cost and may not be available; B1–B3 are designed to deliver value **without** it.

---

## 6. Workstream C — Spec/test conformance backfill for accreted RetainedRender features

**Goal**: Bring the already-implemented features (092, 097, 099, 103, 110, 113, 114, 116, 117, 120, 121) and the visual-state features (093, 095, 096) under the canonical `Spec → .fsi → semantic tests → implementation` contract — exactly the **conformance-backfill** pattern feature 091 established. No new product behavior; the job is to author the missing `spec.md`/`plan.md`/`tasks.md`, confirm the `.fsi` surface is intentional, and confirm the semantic tests exist and exercise the wired path.

### 6.1 Why this matters
- The constitution's Principle I requires the contract chain; importing code ahead of spec created a recorded deviation. Each un-specced accreted feature is an open instance of that deviation.
- The audit (spec 006) found two **cosmetic narrative** defects that backfill should fix in passing: the memo-cache disabled-path counter comment (`RetainedRender.fsi:172/182`) overstates ("every node a miss" vs the real 0/0 bypass), and frame-rate-cap vsync scoping.
- Several features already have `readiness/` evidence (092, 093, 095, 096, 099, 103) and audit tests (`Audit_Reconcile/MemoCache/PictureCache/TextCache/Fingerprint/AnimationClock`); backfill mostly *documents and cross-links* existing proofs.

### 6.2 Tasks (one Spec-Kit feature folder per item; `/speckit-*` reduces to a conformance pass)
- **C1** — 092 (theme-reuse / `StateByIdentity` active read-write, `RetainedInit`, first-frame collisions). Has readiness; write `spec.md`/`plan.md`/`tasks.md`; confirm `themeChanged` invalidation + `firstFrameCollisions`.
- **C2** — 097 (layout cache; `layoutDirtySet` → `Layout.evaluateIncremental`; `RemeasuredNodeCount`/`LayoutInvalidatedNodeCount`). **No readiness** — author readiness + spec; lean on `Layout.Tests/Audit_IncrementalLayout`.
- **C3** — 099 + 121 (animation clock advance + no-alloc idle; `advance`, `updateClockForState`, `sampleOnPaint`, `advanceStateClocks` reference-equal path). 099 has readiness; 121 needs it.
- **C4** — 103 (cross-fade two-snapshot composite; `From` fade-out under own-scene fade-in). Has readiness.
- **C5** — 110 (retained-ID → authored-control-ID; `authoredControlIds`). No readiness.
- **C6** — 113 (memo seam, DataGrid). **Fix the counter narrative** (`RetainedRender.fsi:172/182`) per the audit while writing the spec. No readiness.
- **C7** — 114 (virtualization counts; `countVirtual`). No readiness.
- **C8** — 116 (picture cache LRU; `walkPictures`, `PictureCacheCap=256`). No readiness.
- **C9** — 117 (text-measure cache LRU; `measureTextCached`). No readiness.
- **C10** — 120 (FNV-1a `hashScene` + `unionArea` + `CachedSubtree` replay boundaries). No readiness. Also captures the audit's out-of-scope flag: `SceneEvidence.renderHash` alpha-insensitivity (route to Workstream E or a 120 follow-up).
- **C11** — 093/095/096 (visual-state style layer / lookless slot composition / runtime visual-state bridge). Each has readiness but no `spec.md`; close them. **Prerequisite framing for Workstream D** (the style/visual-state layer is what themes plug into).

Each item: assert the `.fsi` surface delta for that feature is intentional and that the surface-drift gate stays green (these are all `internal` → zero public baseline delta, like 091).

### 6.3 Acceptance / evidence
- Every feature above has `spec.md` + `plan.md` + `tasks.md`; `/speckit-analyze` reports cross-artifact consistency.
- Each spec's "Constitution Check" records the import-before-spec deviation (Principle I) consistent with 091's plan.
- Memo-cache counter comment corrected; tests still green; **zero public-surface-baseline delta**.

### 6.4 Risks
- Low. The main hazard is *scope creep* — backfill must not silently change behavior. Any behavior change discovered (e.g. a counter that should tally) is split into its own Tier-1/2 follow-up, not folded into the doc pass.

---

## 7. Workstream D — Physical layer split, concrete themes, and kits

**Goal**: Execute the four-layer architecture the product committed to in `docs/product/layering.md` and `module-map.md`: separate **design-system primitives**, **themes**, and **design-specific kits** out of the monolithic `Controls` assembly, then deliver real themes (Ant/Fluent/Material) and at least one kit — proving the "one semantic control set, many themes" rule with running code.

This is the strategic, highest-risk workstream and should be phased: **D-phase 1 splits assemblies without behavior change; D-phase 2 adds themes; D-phase 3 adds kits.**

### 7.1 Current coupling (what must be untangled)
- `Theme` and `ResolvedStyle` live in `Controls/Types.fsi`; `Style.resolve` (the only resolver) is hard-coded to `Theme` + `DesignTokens.Light/Dark`.
- Controls construct `ResolvedStyle` by reading `Theme` fields directly (e.g. `Control.fs` baseStyle blocks). `Control.render: theme:Theme -> …` threads `Theme` everywhere.
- `DesignTokens.fs` is generated from `design-tokens.tokens.json`; `Theming.fs` (mode+accent→RolePalette→Theme) already sits in a child namespace.
- `Theme` lacks `Success`/`Warning` (tokens have them) — fix during the move.

### 7.2 Phase D1 — split assemblies (behavior-neutral)
Create and move (compile order preserved):

1. **`FS.GG.UI.DesignSystem`** (new) — `DesignTokens.*`, the design-system types from `Types.*` (`Theme`, `ResolvedStyle`, `StyleVariant`, `VisualState`), `Style.*`. Depends on `Scene` only. Add the missing `Theme.Success`/`Warning` fields here.
2. **`FS.GG.UI.Themes.Default`** (new) — `Theme.fs` (light/dark), `Theming.*`, the tokens JSON + generation tooling. Depends on `DesignSystem`.
3. **`FS.GG.UI.Controls`** (refactor) — drop theme/style files; add `ProjectReference` to `DesignSystem`; `Control.render` imports `Theme` from `DesignSystem`. Behavior identical.

**Critical cross-cutting work (do not skip):**
- **Surface baselines**: adding 2 packages requires 2 new rows in `scripts/refresh-surface-baselines.fsx` and 2 committed `.txt` baselines, plus regenerating the (now-smaller) `Controls` baseline. The drift gate (`gate.yml` step 4) **fails on untracked baselines** — this is the most likely thing to redden CI.
- **`FS.GG.Rendering.slnx`**: add both new projects.
- **Public-surface migration**: moving `Theme`/`ResolvedStyle` from `FS.GG.UI.Controls` to `FS.GG.UI.DesignSystem` is a **public namespace change** for consumers — record it as a decision (`docs/product/decisions/`), update the template, and note it for the rebrand/bridge docs.
- **Package identity**: stay consistent with the Stage R8 `FS.GG.UI.*` scheme.

### 7.3 Phase D2 — concrete themes
4. **`FS.GG.UI.Themes.AntDesign`**, **`…Themes.Fluent`**, **`…Themes.Material`** (new, one assembly each) — each provides a `Theme` instance (color/typography/spacing/radius/shadow values) over the shared `DesignSystem` slots + visual-state styling, depending only on `DesignSystem`. **No control forks** — they style the existing `Button`/`TextBox`/etc. Prove the layering rule with a test that renders the *same* `Control` tree under all themes and asserts behavior/accessibility identical, visuals differ.
- Prerequisite: Workstream C11 (093/095/096 visual-state style layer) defines the slot/visual-state vocabulary themes target — **land C11 before D2**.

### 7.4 Phase D3 — design-specific kits
5. **`FS.GG.UI.Themes.AntDesign.Kit`** (or a `Kits` assembly) — implement at least `AntDesign.Form` (validation-flow layout) and `AntDesign.Table` (filtering/sorting/empty-state) as opinionated compositions over controls + theme. Depends on `Controls` + the Ant theme. This is the proof that a kit is justified only when it adds *composition/workflow behavior beyond styling* (layering.md decision rule).

### 7.5 Tasks
- **D1.1** Create `DesignSystem` project; move tokens/types/style; add `Success`/`Warning`; preserve compile order. Build green.
- **D1.2** Create `Themes.Default`; move `Theme`/`Theming`/tokens JSON; build green.
- **D1.3** Refactor `Controls` to reference `DesignSystem`; fix imports; behavior-neutral. Full existing test suite green.
- **D1.4** Update `.slnx`, `scripts/refresh-surface-baselines.fsx` (+rows), regenerate + commit all baselines; gate green.
- **D1.5** Decision record + template update + bridge note for the namespace move.
- **D2.1–D2.3** Ant / Fluent / Material theme assemblies + the "one control set, many themes" parity test.
- **D3.1** `AntDesign.Form` + `AntDesign.Table` kits + tests.

### 7.6 Acceptance / evidence
- Build + full test suite green after each phase; **surface-drift gate green** with committed baselines for every new package.
- A parity test renders an identical `Control<'msg>` tree under Light/Dark/Ant/Fluent/Material and asserts: identical behavior/accessibility contract, divergent resolved visuals.
- At least one kit composes multiple controls with workflow behavior, with its own semantic tests.
- `module-map.md` rows for design-system/themes/kits move from "embedded in Controls" to "owned assembly."

### 7.7 Risks
- **High blast radius**: every control touches `Theme`. Phase D1 must be a pure move (no behavior change) verified by the existing suite before any theme work.
- **Surface-drift gate** will fail loudly until baselines are regenerated — sequence D1.4 immediately with the moves, never separately.
- **Consumer break**: the namespace move is a breaking change for any downstream app/template; coordinate with the bridge/template docs. Consider type aliases or `[<assembly: TypeForwardedTo>]` if backward source-compat is desired.
- Scope: D2/D3 are large. Ship D1 (the split) as its own release; themes/kits can follow incrementally (one theme at a time).

---

## 8. Workstream E — Cleanup follow-ups

Low-risk tidy items surfaced by the audit and the 091 plan, batchable opportunistically.

- **E1 — DF-1**: strip redundant `internal`/`private` access modifiers from top-level bindings in `RetainedRender.fs` (the `.fsi` already governs visibility; constitution Principle II discourages doubling up). Behavior-neutral; recorded as a bounded Tier-2 in the 091 plan.
- **E2 — memo-cache narrative**: re-scope the `RetainedRender.fsi:172/182` comments to describe the real disabled-path bypass (0/0, not "every node a miss"). Pairs with Workstream C6.
- **E3 — `SceneEvidence.renderHash` alpha-insensitivity**: the audit found an opacity-only change didn't alter the hash. Decide: extend the hash to be alpha-sensitive, or document the limitation. Route as a 120 follow-up (Workstream C10) or standalone.
- **E4 — FSI transcript fixture**: add a current FSI transcript under this repo and un-skip `TypedControlContractTests.fs:79` (the last non-perf skip), per its named un-skip trigger.

---

## 9. Cross-cutting concerns (apply to every workstream)

1. **Constitution contract** — author `spec.md` → `.fsi` (if surface changes) → semantic tests → implementation, in that order, for net-new work (Workstreams A, D). For backfill (C), follow the 091 pattern and record the import-before-spec deviation. Tier-classify each change (1 = contracted/observable; 2 = internal/behavior-neutral).
2. **Surface-drift gate** is the single most fragile CI interaction. Any new public package or renamed public type/module across the 9 (soon more) baselines fails `gate.yml` step 4 until `scripts/refresh-surface-baselines.fsx` is updated (new row) and baselines are regenerated + committed. Treat baseline regeneration as part of the same change, never a follow-up.
3. **No-overclaim evidence** (harness) — every new tier/backend must populate a non-empty `NotAuthoritativeFor`, degrade cleanly (Skip/Fail-classified, never hang or fake), and disclose what it does *not* prove.
4. **Determinism** — no wall-clock, no `Math.random`; perf goldens compare structure/counts/ratios, never absolute milliseconds.
5. **Docs sync** — update `README.md` (harness status is currently stale re: T2/vsync), `SKIPPED-TESTS.md` (running tally), `docs/harness/capability-baseline.md`, `module-map.md` (layer dispositions), and add decision records under `docs/product/decisions/` for the namespace move.

---

## 10. Suggested sequencing & roadmap

```
Near term (no env dependency, high confidence)
  C11 (093/095/096 visual-state specs)  ──┐  prerequisite for D2
  C1–C10 (RetainedRender backfill)        │  parallelizable; low risk
  A1–A2, A5–A6 (pure input backend + CLI) │  headless-runnable now
  E1, E2, E4 (cleanups)                   │
                                          ▼
Mid term
  A3 (x11-xtest backend)        — needs GL/X11 runner to *prove*, skips clean otherwise
  B1–B3 (offscreen-derived perf goldens, un-skip subset)   — removes most of the 17 skips
  D1.1–D1.5 (assembly split, behavior-neutral)             — strategic; ship as its own release
                                          ▼
Later (env- or scope-gated)
  A4, A7 (uinput backend + integration)   — needs /dev/uinput
  B4 (capable CI runner)                  — ops; flips vsync/uinput from advisory to executed
  D2 (Ant/Fluent/Material themes)         — after C11 + D1
  D3 (kits)                               — after D2
  E3 (renderHash alpha)                   — decide + implement
```

**Recommended first cut (one to two iterations):** C11 + a batch of C backfills + A1/A2/A5/A6 (pure input) + E1/E2/E4. All headless, all low-risk, all move the skip count and the contract-coverage forward without new infrastructure. Then commit to D1 as a dedicated effort.

---

## 11. Risk register

| Risk | Workstream | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| Surface-drift gate reddens on package add/type move | D (also A docs) | High | Med | Regenerate + commit baselines in the same change; add script rows up front |
| Layer split changes behavior unintentionally | D1 | Med | High | Pure-move discipline; full existing suite must pass before any theme work |
| Namespace move breaks consumers/template | D1 | High | Med | Decision record + `TypeForwardedTo`/aliases; coordinate bridge + template |
| Perf goldens encode machine timing → flaky | B | Med | High | Golden only counts/ratios/structure; keep ms as artifacts not goldens |
| Capable CI runner never provisioned | A4/B4 | Med | Med | A1–A3/B1–B3 deliver value headlessly; kernel/vsync tiers stay advisory + honest-skip |
| `ydotool`/`ydotoold` hangs instead of skipping | A4 | Low | Med | Detect daemon/socket absence → Skip; bounded timeout on shell-out |
| Backfill silently masks a real behavior bug | C | Low | Med | Behavior changes split into their own Tier-1/2 feature, never folded into docs |

---

## 12. Definition of done (per workstream)

- **A**: `harness input` runs all three backends; `pure` is in the gate green; `uinput`/`x11-xtest` honest-skip headless; harness unit tests cover planner + non-empty `NotAuthoritativeFor`; capability-baseline updated.
- **B**: offscreen-derived Feature109 subset un-skipped + green deterministically; `SKIPPED-TESTS.md` updated; vsync/uinput tiers run on capable runner (when provisioned).
- **C**: every listed feature has spec/plan/tasks; `/speckit-analyze` consistent; zero public-surface delta; memo counter narrative fixed.
- **D**: assemblies split, full suite + drift gate green; multi-theme parity test passes; ≥1 kit with tests; map/decision docs updated.
- **E**: DF-1 applied; memo comment fixed; FSI fixture un-skipped; renderHash decision recorded.

---

### Appendix — key source references
- Harness: `tests/Rendering.Harness/{Cli,Live,Perf,Probe,RunPlan,Tiers,X11,Evidence,Domain}.fs(i)`; `docs/harness/capability-baseline.md`; `specs/004-rendering-harness/`.
- RetainedRender: `src/Controls/RetainedRender.fsi` (445 lines) / `.fs` (1376 lines); `src/Controls/Reconcile.fsi/.fs`; `specs/{091,092,093,095,096,099,103}-*/`.
- Layers: `src/Controls/{Types,DesignTokens,Theme,Theming,Style}.fs(i)`, `design-tokens.tokens.json`; `docs/product/{layering,module-map}.md`.
- Build/CI/audit: `.github/workflows/{gate,capability,release}.yml`; `scripts/refresh-surface-baselines.fsx`; `tests/surface-baselines/*.txt`; `SKIPPED-TESTS.md`; `docs/audit/mechanism-audit.md`; `specs/{005,006}-*/`.
