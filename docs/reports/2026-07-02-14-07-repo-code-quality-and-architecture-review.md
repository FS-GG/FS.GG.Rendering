# Repository code-quality & architecture review

**Date:** 2026-07-02 14:07 (+0200)
**Scope:** full repository at `e39d1ce` (post-feature-231) — all 19 `src/` projects (~38k lines F#), all 18 test projects (~51k lines), build/packaging/CI/template infrastructure, and documentation/spec coherence.
**Method:** six parallel deep-read reviews (core rendering; design system & theming; controls & Elmish; build/packaging/CI; tests & diagnostics; docs & spec coherence). Every finding below was verified against source at review time; file:line references are to `e39d1ce`.

---

## Executive summary

The repository's foundations are unusually strong: layering is physically true (verified against `.fsproj` references — DesignSystem→Scene only, Themes→DesignSystem only, SkiaViewer is the only Skia/GL toucher), `.fsi` signature discipline is near-universal, the wire format is frozen with exhaustive matches and round-trip oracles, retained rendering is byte-identical to rebuild rendering by shared construction, and the release pipeline uses OIDC Trusted Publishing with fail-closed version-coherence guards. Purity claims in the Elmish adapter and style resolver are genuine.

The problems cluster into five systemic classes rather than random defects:

1. **Guards that drifted from reality with no meta-check.** The CI gate's hardcoded test list misses six test projects entirely; the release gate can validate the *previous* release's bits; the cadence map lists retired projects; `module-map.md` ("the authoritative answer") lists modules retired weeks ago; "GENERATED — do not edit" headers point at a regeneration command that doesn't exist.
2. **Success-shaped stubs on the public surface.** `Path.combine` ignores its boolean operation; `Path.segment` is a no-op; `Viewer.runBounded` ignores its `scene` argument; `Animation.Color` is never applied; `horizontalStack`/`verticalStack`/`dock` ignore their configs; `SceneEvidence.render` with `Format = Png` writes a hash to a `.png` path. This contradicts the repo's own (otherwise well-enforced) "honest failure, never success-shaped stubs" principle.
3. **Identity-scheme fragmentation in the interaction pipeline.** Three coexisting control-id schemes mean unkeyed controls silently lose focus/hover visuals and keyboard activation.
4. **Correctness gaps in the visual pipeline.** A paint-blind cache fingerprint can serve stale pixels; dark-mode resolution string-matches `theme.Name = "dark"` so Ant/custom dark themes get light-mode colors; the `wcag` policy passes normal text at 3.0:1 against its own declared 4.5 bar.
5. **God modules concentrated at the seams.** `SkiaViewer.fs` (2,858 lines, including ~234 lines of dead loop code), `ControlsElmish.fs` (2,361), `Symbology.fs` (1,435) each mix 3–5 separable concerns.

A recurring meta-observation from the docs review: **documents with a gate/test behind them stayed true; narrative snapshots rotted within days** at the current spec-kit cadence (231 features in ~3 weeks). The highest-leverage fixes are therefore mostly "put a guard behind the thing" rather than "rewrite the thing."

---

## Top priorities (cross-area, ranked)

| # | Finding | Area | Severity |
|---|---------|------|----------|
| P1 | Unify the three control-id schemes (`Key ?? Kind` vs `Key ?? path` vs `RetainedId`) — unkeyed controls lose focus/hover/keyboard behavior | Controls | HIGH |
| P2 | Canvas `cached` fingerprint ignores paint and collapses ~8 node kinds to a constant → picture-replay cache serves stale frames | Rendering | HIGH |
| P3 | `Style.fs` resolves success/warning/dark by `theme.Name = "dark"` string match; `Theme.Success`/`Warning` fields never read | Design system | HIGH |
| P4 | Six test projects (`Build`, `Canvas`, `Symbology`, `Symbology.Render`, `SymbologyBoard`, `Rendering.Harness`) never run in any CI cadence | CI | HIGH |
| P5 | Release "generated product" gate restores the template pin (0.1.58) instead of the tag being published (0.1.61) — gate can validate stale bits | CI | HIGH |
| P6 | Stubbed public API: `Path.combine`/`segment` return wrong geometry silently; `runBounded` ignores its scene | Rendering | HIGH |
| P7 | `wcag` policy passes body text at 3.0:1 via `AaLarge` with no size evidence, contradicting its printed 4.5 threshold | Color policy | HIGH |
| P8 | Vacuous/self-fulfilling tests: 3 always-green sample smokes; Feature093 baselines rewritten then existence-checked | Tests | HIGH |
| P9 | `DesignTokens.fs` claims "GENERATED" but no generator emits it and the cited command doesn't exist | Design system | HIGH |
| P10 | Radio-group/tabs/numeric-input clicks dispatch `""`/`0.0` (documented as KNOWN GAPS, shipped unfixed) | Controls | HIGH |
| P11 | README/usage.md contradict the shipped release pipeline (feed, version, retired `Color`/`Input` modules, 8 of 17 packages listed) | Docs | HIGH |
| P12 | ADRs 0011–0014 are cited normatively by workflows/specs/commits but exist nowhere in-repo | Docs | HIGH |

---

## 1. Core rendering pipeline (Scene, Canvas, Layout, SkiaViewer, Shared)

Architecture conforms to `docs/product/layering.md`: Scene is BCL-only; Canvas and Layout reference only Scene (+Yoga); SkiaViewer is the sole Skia/Silk.NET/GL toucher; injection seams (`setRealTextMeasurer`, `setRealPngRasterizer`) keep dependency direction correct. Violations below are localized, not structural.

### High

- **R1. Paint-blind cache fingerprint → stale frames.** `src/Canvas/Elements.fs:36-66`: the FNV fold behind `CacheBoundary.Fingerprint` ignores `Paint` on `PaintedRectangle`/`Points`/`Line`/`Path`/`ClipNode`/`PerspectiveNode` and hashes `FilledEllipse`, `Ellipse`, `Arc`, `Vertices`, `TextRun`, `GlyphRun`, `RegionNode`, `PictureNode` to a constant (`| _ -> step h 18UL`). `PictureReplayCache.paintBoundary` (`src/SkiaViewer/PictureReplayCache.fs:152-158`) replays the recorded `SKPicture` on fingerprint equality — change a cached subtree's stroke color, dash pattern, arc sweep, or glyph text under the same `cached` key and stale pixels render. The Controls layer's `SceneHash.hashScene` mixes every field including paint, confirming this hash is an under-built outlier.
- **R2. Stubbed path API exported as real.** `src/Scene/Scene.fs:143-158`: `Path.combine` produces the same command concatenation for `Union`/`Intersect`/`Difference`/`Xor`; `Path.segment` never extracts a sub-path. `Scene.fsi:64-67` documents both as plain "public contract functions" with no stub disclosure.
- **R3. `SkiaViewer.fs` is a 2,858-line god module with dead code.** Three parallel hand-rolled window loops (`runPresentedPersistentWindow` ~322 lines at :891-1212, `runBounded` ~140 at :1986-2125, `runInteractiveViewerWithWindowBehaviorCore` ~177 at :2292-2468). `runPersistentWindow` (:1214-1447, ~234 lines) has **no call site**. `interpretEffects` is duplicated verbatim at :2198-2223 and :2319-2344.
- **R4. `Viewer.runBounded` ignores its scene.** `SkiaViewer.fs:1986-1987` (`ignore scene`); the render handler only counts frames, so `runUntilFirstFrame`/`runForFrames` report "frame presented" evidence for a window that never draws the scene.

### Medium

- **R5.** `Animation.Color` tween is public but never applied — `applyAt` samples only `Opacity`/`Transform`; `Color` affects only `isSettled` (`src/Scene/Animation.fs:204-229`).
- **R6.** `Layout.evaluate` runs the full pure layout pass on every call purely to harvest diagnostics, and a third pass on Yoga fallback (`src/Layout/Layout.fs:548-573`); the incremental path multiplies this cost.
- **R7.** Process-wide mutable statics make the viewer single-instance-only (`OpenGl.fs:362-384`, `SceneRenderer.fs:22,179`), unenforced and undocumented; teardown skips disposing `lastGoodFrame` (a GPU-backed `SKImage`).
- **R8.** Text re-shaped from scratch every frame — new `SKShaper` per string per draw (`Fonts.fs:395-397`), no shaping cache, plus O(n²) `List.tryItem` inside `List.mapi` glyph assembly (`Fonts.fs:406-446`).
- **R9.** Per-frame disk I/O: `Image` nodes re-decode from disk every paint with no image cache (`SceneRenderer.fs:327-340`); `Scene.diagnostics` does `File.Exists` probes in the "dependency-light" package (`Scene.fs:345-346`), making results machine-dependent.
- **R10.** `SceneEvidence.render` with `Format = Png` returns/writes a hash string as PNG evidence (`Evidence.fs:90-96`) — the "success-shaped non-image" feature 221 claimed to eliminate.
- **R11.** `withNativeWindowEnvironment` nulls `WAYLAND_DISPLAY` process-wide for the whole event loop (`SkiaViewer.fs:501-514`), visible to all threads, undisclosed at the API.
- **R12.** Cross-package duplication: `directionOf`/`scriptOf` verbatim in `Scene/TextShaping.fs:31-69` and `SkiaViewer/Fonts.fs:303-341` (drift hazard — fingerprints hash these); `RenderLagTrace` implemented twice with divergent capture behavior.
- **R13.** `GlResources` ledger (`OpenGl.fs:43-227`) is a public "cleanup-order proof" that never tracks a real resource — the actual teardown is a separate untyped `finally`, so tests exercise a model of the code, not the code.

### Low

`GraphValidation.hasCycle` worst-case exponential on dense DAGs and `CycleDetected` reports all node ids (`GraphValidation.fs:27-53`); `horizontalStack`/`verticalStack`/`dock` ignore their configs and `DockPosition` is never read (`Layout.fs:1245-1247`); dead `nodePairs` allocation (`Layout.fs:422`); `LayoutBounds` structurally duplicates `Scene.Rect`; stringly `inputDispatch = "false"` fields (`SkiaViewer.fs:2196`); O(n²) duplicate-id scan in `SceneCodec.resourceVerdicts` (`SceneCodec.fs:587`); `Scene.empty` contains one node, breaking structural-equality expectations in `shouldPresent`.

### Done well

`.fsi` discipline with genuine `internal` modules; frozen wire format (tags 0..24, no-wildcard matches, round-trip oracle); single shared `SceneRenderer.paintNode` for interactive + evidence paths; careful native lifetime handling in `PictureReplayCache`/framebuffer resize/Yoga teardown; small pure decision kernels (`shouldPresent`, `Loop.advance`, `decideDamageScopedRender`); disclosed impurity seams defaulting to honest failure; invariant-culture deterministic hashing.

---

## 2. Design system, themes, color policy, symbology

### High

- **D1. Dark-mode by string match.** `src/DesignSystem/Style.fs:15-21`: `isDark theme = theme.Name = "dark"`; success/warning resolve from `DesignTokens.Dark/Light` and the `Theme.Success`/`Warning` fields (added feature 125) are **never read**. `AntTheme.antDark` (`Name = "AntDesign Dark"`) and anything from `Theming.toTheme` (pins `Name = "light"`) get light-mode Default tokens. Contradicts `Style.fsi:23-25`'s contract; the comment at `Style.fs:12` ("success/warning colours are NOT Theme fields") has been false since feature 125.
- **D2. WCAG policy under-gates text.** `src/ColorPolicy/ColorPolicy.fs:59-63` counts `AaLarge` (≥3.0) as `Passed` while `Pairing` carries no font size and `wcag.Threshold Role.Text = 4.5` is what the report prints — a body-text pairing at 3.2:1 renders `Threshold 4.50 | AaLarge` and counts toward "Overall: PASS" under WcagCertified authority.
- **D3. "Generated" tokens with no generator.** `src/DesignSystem/DesignTokens.fs:1-2` says "GENERATED — do not edit. Regenerate via: ./fake.sh build -t RefreshSurfaceBaselines"; `fake.sh` doesn't exist and `generate-design-tokens.fsx:29` explicitly skips the `light`/`dark` groups. Parity with the DTCG source is currently true (verified by hand, all 24 values) but guarded only by literals frozen a third time in tests.

### Medium

- **D4.** The "ant" contrast policy's thresholds are invented — its own comment concedes the 2.5 GraphicOrUi bar exists "so at least one shared pairing changes verdict under ant vs wcag" (`ColorPolicy.fs:70-84`), against CLAUDE.md's rule that Ant facts come from the LLM-sources hub.
- **D5.** `AntIntentPolicy` is unreachable from the product render path — `WidgetGeometry.fs:292` hardcodes `StyleResolver.resolveDefault`; Ant intent visuals exist only in tests. The theme layer cannot deliver its intent language through Controls.
- **D6.** `Hover`/`FocusedHover` set `Fill = theme.Accent` without adjusting `Foreground` (`Style.fs:75,80`) → ~2.9:1 text contrast in the default light theme, below the theme's own declared 4.5.
- **D7.** Layer-ownership inversion: Ant concrete values (`#1677ff`, per-component tokens) compile into the DesignSystem assembly while their DTCG source lives in the *Default* theme project; raw hexes are repeated instead of DTCG `$value` references (`#1677ffff` ×8).
- **D8.** Dark alias feedback colors are not dark-adapted: dark `feedback.errorText = #b91c1c` ≈ 3.2:1 on the dark canvas, under the file's own 4.5 bar; `Map`/`Component` token groups have no dark variants at all.
- **D9.** Symbology Speed channel has three domains: validator accepts 0..6, capacity table says 4, all renderers clamp to 4 (`Symbology.fs:244` etc.) — `Speed = 6` scores `Clean` but renders as 4, the silent channel collapse the legibility scorer exists to catch.
- **D10.** `Symbology.fs` is 1,435 lines mixing grammar geometry, a full rich-text layout engine (~725 lines), label motion, and auto-label projection; budget-cap/ellipsis logic implemented three times; animate/overlay and gallery/filmstrip geometry duplicated verbatim.

### Low

`DesignTokensExt` Map/Component/Space/Elevation layers are public API with zero product consumers, `Elevation` values are unparsed strings; stale self-contradictory generated headers; Symbology hardcodes an Ant-**v4** palette (`#1890ff`) while the design system standardized on v5 (`#1677ff`), plus a dark-only `labelInk`; `Theming.toTheme` silently drops `FocusRing` and erases mode (feeds D1); stringly `kind`/`intent` resolver inputs with silent fallback and a hardcoded `FontSize = 15.0` ignoring `theme.FontSize`.

### Done well

Contrast math is exactly WCAG 2.x (correct sRGB knee, luminance weights, alpha composited before measuring, honest `Indeterminate` for gradients); the resolver is a closed ordered last-writer-wins fold, small/total/testable; `DesignTokensExt` generation is exemplary (paired .fs/.fsi from one walk, mode-parity guard, `--check` drift gate wired into tests and verified passing); ColorPolicy's no-overclaim Authority machinery; Symbology honors Scene-only dependency with per-game mapping kept in consumer data; `Symbology.Render` is a model 31-line fail-loud bridge.

---

## 3. Controls, Controls.Elmish, Elmish, KeyboardInput

### High

- **C1. Three coexisting control-id schemes.** Layout/hit-test/dispatch emit `Key ?? path` ids (`Control.fs:488,525`, `Focus.fs:283` — feature 098's unified scheme), but focus/runtime-stamping key by `Key ?? Kind` (`Focus.fs:52-53`, `ControlRuntime.fs:271,369`, `ControlsElmish.fs:969,1220,1394,1537`), plus the separate `RetainedId` domain. For any unkeyed control the ids can never match: hover produces `HoveredControl = "0.3"` while the visual stamp computes `"button"`; `routeFocusedKey` filters bindings across schemes so keyboard activation of an unkeyed focused control dispatches nothing; unkeyed same-kind siblings collapse onto one focus stop — which a code comment at `ControlsElmish.fs:903-906` admits.
- **C2. Phantom transient-widget ids.** `WidgetLowering.focusScope` fabricates stops `surfaceId + "-item-1/2"` and `InitialFocus` ids no control carries (`Widgets/WidgetLowering.fs:45-50`); `DatePicker`/`SplitButton` declare `triggerId = rootId + "-trigger"` in metadata but create the trigger Button **without** that key (`Widgets/Pickers.fs:57-60`, `Widgets/Buttons.fs:72-75`) — structurally guaranteed `MissingOverlayAnchor` when open.
- **C3. Wrong-value dispatch shipped as KNOWN GAPS.** The activation-value registry (`ControlsElmish.fs:574-583`) covers only slider/switch/check-box; its own audit comment states radio-group/tabs clicks send `""` and numeric-input sends `0.0`.

### Medium

- **C4.** `ControlsElmish.fs` (2,361 lines) mixes effect interpreters, pointer/keyboard/text routing, the activation registry, the live frame loop (~470 lines), the `Live` script bridge, and the `Perf` driver; `runScriptCore` alone is ~360 lines.
- **C5.** Four near-identical ~28-field `FrameMetrics` blocks defeat feature 186's "single construction site" (`ControlsElmish.fs:2173-2345`); `buildFrameMetrics` takes 21 positional args with adjacent bool/int params and two deliberately-doubled arguments — a swapped argument type-checks silently.
- **C6.** No key-repeat model: a held key re-emits `CommandResolved` every OS repeat with no initial-press/repeat distinction (`KeyboardInput.fs:114-130`); no `repeat` handling in the viewer host either.
- **C7.** `parseModifiers` splits on `'+'` so the literal plus key and chords targeting it normalize to `Unknown ""` (`KeyboardInput.fs:288-314`), despite the "Pure, total" FR-016 claim.
- **C8.** `ViewerKey` omits Tab/Home/End/PageUp/PageDown/Delete; traversal/scroll ride stringly `Unknown` with ad-hoc `"Shift+"` prefix parsing at multiple sites (`ControlsElmish.fs:1179-1189,1766-1769`, `Pointer.fs:93-104`); Ctrl+Tab falls through unhandled.
- **C9.** Derived state and effects stored inside models (`KeyboardModel.StateDisplay`/`RecentEffects`, `ControlRuntimeModel.RecentEffects`) — duplicated sources of truth.
- **C10.** Global mutable statics feed nominally pure seams: unsynchronized `TextMeasureHookHolder` (`ControlPrimitives.fs:24-42`); a load-bearing mid-tuple assignment reading a GL-host mutable static (`ControlsElmish.fs:1579`).
- **C11.** `Animation.tickSubscription` uses a constant `SubId` so a changed interval never takes effect, and the `Timer` dispatches from a ThreadPool thread with no marshalling (`Elmish/AnimationTick.fs:11-39`).
- **C12.** `src/Elmish/ElmishAdapter` never updates `UserModel` after init, so `render` always sees the initial model; its only consumer is one test (`Elmish.fs:28-34`) — vestigial; finish or remove.

### Low

Vestigial public surface kept alive only by contract tests (`KnownControl`/`KnownEvent`/`KnownAttribute`, identity `lowerStandard`/`lowerCustom`, all-`None` `TextInput.interpretEffect`, a no-op record copy in `Pointer.fs:146`); `Control.dispatch` materializes all tree-wide matches before `List.truncate 1` and a disabled container doesn't block children's bindings (`Control.fs:809-830`); `Charts2.fs` re-spells attrs in 14 modules instead of reusing `ChartAttrs`.

### Done well

Purity discipline is genuinely good — all frame-loop mutation consolidated in one documented `FrameLoopState` at the interpreter edge, closed effect DUs interpreted at one seam, and the deterministic `Perf` driver reuses the exact live-loop primitives. Layering contract respected: no colors/spacing in controls, no per-theme control forks, consistent `Props`/`defaults`/`view` widget pattern. Byte-identical retained rendering by shared construction. Robust edge handling in pure reducers (stale focus recovery, release-without-press, drag cancel, scroll clamp). Idiomatic F# throughout with requirement-traceable comments.

---

## 4. Build, packaging, CI, template

### High

- **B1. Six test projects never run in CI.** `gate.yml:80` hardcodes `for p in Scene Layout KeyboardInput Elmish Controls Diagnostics Testing Lib`; `Build.Tests`, `Canvas.Tests`, `Symbology.Tests`, `Symbology.Render.Tests`, `SymbologyBoard.Tests`, `Rendering.Harness.Tests` are in the solution (so they compile) but appear in no cadence — gate, GL loop, release, or capability. The cadence map (`docs/ci/cadence-map.md:60-68`) still lists retired `Color.Tests`/`Input.Tests` and omits `Diagnostics.Tests`; its "every member maps to exactly one cadence" invariant is broken in both directions. Fix: derive the loop from the slnx and refresh the map.
- **B2. Release gate can validate the previous release.** `release.yml:75-105` packs the solution at the tag version into a runner-local feed, but the scaffolded product restores at the template pin `FsGgUiVersion = 0.1.58-preview.1` (`template/base/Directory.Packages.props:9`) — release tags are at `v0.1.61-preview.1`, so recent releases published member packages the template gate never restored (0.1.58 came from nuget.org). `validate-version-coherence.fsx` doesn't track the `v*` lane. Fix: fail when `$VER ≠ FsGgUiVersion` (or rewrite the pin before restore) and extend the coherence guard.

### Medium

- **B3.** Version literals scattered across ~18 files (16 differing `<Version>` values from 0.1.0 to 0.1.48) are inert in CI (overridden by `-p:Version`) but a local `dotnet pack` without it emits a mixed-version incoherent set — the exact failure class the BOM exists to prevent. Delete per-project `<Version>` lines.
- **B4.** Seven dead central-package pins with misleading comments citing nonexistent projects (`Directory.Packages.local.props:44-66`: Fake.*, XParsec, FSharp.SystemTextJson, FileSystemGlobbing, DiffPlex) — silent supply-chain surface.
- **B5.** `FS.GG.UI.Template.fsproj:22-25` packs nearly the entire repository (`..\**\*` with `NoDefaultExcludes=true`) as package content — `src/**`, `tests/**`, CI workflows, evidence artifacts — though `template.json` only materializes `template/**`, `.specify/**`, `.agents/skills/**`.
- **B6.** Template emits `RestoreLockedMode` gated on `ContinuousIntegrationBuild` (`template/base/Directory.Build.props:11`) — a signal GH Actions never sets, and the exact gate the repo itself migrated away from (ADR-0006/feature 213); generated products' "byte-reproducible restore in CI" promise is inert by default.
- **B7.** Pinning inconsistent where it matters most: unpinned `dotnet tool install fsdocs-tool` in the required gate (`gate.yml:131`); tag-pinned `NuGet/login@v1` minting the publish credential (`release.yml:198`) while the org dispatch workflow is SHA-pinned.
- **B8.** `${{ github.event.release.tag_name }}` / `${{ inputs.version }}` interpolated directly into `run:` blocks in the job holding `id-token: write` (`release.yml:74-77,150-153`); refnames permit `;`, `$`, backticks. Exposure limited to tag-pushers, but route through `env:` and add the `VERSION_RE` validation used elsewhere.
- **B9.** No NuGet caching anywhere despite committed lockfiles providing exact cache keys — the required gate does locked restore + full build + fsdocs + full Release pack + clean restore per PR.
- **B10.** ADRs 0011–0014 load-bearing but absent in-repo (see Docs, X3).

### Low

`apicompat-check.sh` parses feed JSON with grep and assumes ordering (`:104-108`); release workflow can run twice per release (both `release: published` and `push: tags` fire; concurrency serializes rather than dedupes; idempotency rests on `--skip-duplicate`); hardcoded personal nuget.org account fallback `|| 'Paradigma11'` (`release.yml:200`); skill catalog hand-synced in two copies plus a tri-plicated `sha256Text` (deliberate and parity-gated, but a shared module would remove the manual sync); `emit-harness-readiness.sh` always exits 0 regardless of inner failures.

### Done well

Fail-closed discipline is real (`validate-version-coherence.fsx` exit-2 guard errors, self-checked comparator, expected/actual/fix triples; surface gate fails on untracked baselines). BOM design with exact `[$version$]` brackets and membership parity tests. Release security architecture: OIDC Trusted Publishing, `id-token` scoped to one job, canonical-repo guards, secretless gate for fork PRs. SHA-pinned cross-repo dispatch with a run-time-minted App token and a *tested* shell script. CPM + lockfile hygiene with documented ownership split. The skill manifest/materialize pipeline (content-addressed manifest → incremental MSBuild fan-out → `--enforce` in gates) is coherent and well staged.

---

## 5. Tests, Testing, Diagnostics, readiness

### High

- **T1. Permanent green no-ops.** `tests/Lib.Tests/Tests.fs:859-911`: three sample smokes assert `Expect.isFalse (File.Exists project)` in the `else` branch of `if File.Exists project` — can never fail; `samples/BasicViewer`, `InteractiveViewer`, `ScreenshotGallery` don't exist, so all three have passed vacuously since import, violating SKIPPED-TESTS.md's own "never marked passing, never weakened" principle and absent from that ledger.
- **T2. Self-fulfilling golden.** `Feature093ParityTests.fs:58-86` unconditionally rewrites `specs/093-*/readiness/parity/*.scene.txt` each run, then T020 asserts only existence/length on files it just wrote; no test reads them back.
- **T3. Contract-by-string-matching is pervasive.** ~266 `File.ReadAllText`/`File.Exists` assertions; tests assert `stringContains` on `.fsi` source text and markdown wording; production layout is constrained by it (`src/Testing/Testing.fs:448-450` keeps code in a file *because* a test reads the file text). These survive behavioral regressions and fail on cosmetic rewording.
- **T4. Riskiest layers unexercised by default.** SkiaViewer's live-window/GL/present tier is opt-in (`FS_SKIA_RUN_LIVE_PERSISTENT_TESTS`); the only test that actually packs and restores a real consumer is behind `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE=1`. GL rendering, present path, and pack/restore regress silently on CI (compounding B1).
- **T5. Dead governance module with stale baseline list.** `build/Governance/PackageSurface.fs` has zero consumers; it names 8 baselines while 16 are committed and the live gate asserts only 3 (Layout, Controls, Build) — 13 of 16 committed surface baselines gate nothing.

### Medium

- **T6.** Tests mutate committed repo files and write into the source tree (`Lib.Tests/Tests.fs:189-195,617-620` rewrites a committed readiness file every run; ~12 feature files write into `specs/*/readiness/**`), producing a ~130-line `.gitignore` lattice and dirty-worktree/parallel-run risk. SKIPPED-TESTS.md itself defers this to "Stage R6".
- **T7.** Shipped `FS.GG.UI.Testing` surface is 1,469 exported names including `Feature159/160/161Readiness` modules — public API coupled to internal spec numbers; `TestingTypes.fs` (762 lines) is a ~60-record grab-bag.
- **T8.** `module private Evidence` copy-pasted in ≥9 feature files despite TestSupport existing; `readRepo` re-declared twice; `RestoreLockTests` hand-rolls its own repo-root walk.
- **T9.** Wall-clock assertions are flake risks: two `< 100ms` stopwatch gates in `DataGridTests.fs:30-48`, `< 2000ms` in Layout tests.
- **T10.** SKIPPED-TESTS.md is maintained and honest for its 18 `ptest` skips but omits T1's hidden passes and three long-dormant `skiptest` Smoke.Tests contracts ("Stage R4 pending" persisting through feature 231).
- **T11.** Hand-rolled JSON in `src/Diagnostics/Diagnostics.fs:438-535` (documented as intentional for byte-stability) duplicates the parallel `ReadinessFormatting` stack in `TestingVisual.fs` — two hand-maintained readiness serializers; `summarize` runs `aggregate` twice.

### Low

Constant-restating token tests pin literals to themselves; `readiness/parity/screenshots/` contains no screenshots, only a regenerated text file; latent `Replace("Module","")` mangling in the surface gate's name normalization (`SurfaceAreaTests.fs:25-27`); a tautological path assertion (`:49`); solution pinned to exactly 38 projects unexplained; 1,600+-line multi-concern test files alongside per-feature siblings.

### Done well

SurfaceAreaTests is a real bidirectional public-API drift gate with one authoritative refresh script. SkiaViewer skip discipline records skips as "not a pass (Constitution VI)" with meaningful assertions retained in skipped arms. Symbology determinism tests pin SHA-256 of canonical bytes as literals. Feature109's golden pattern (regen env-gated, committed-golden compare, byte-identity re-run) is the model to generalize. `src/Diagnostics` core is a clean pure library with graceful artifact-write degradation. Feature 180's readiness-validator consolidation genuinely deduplicated. A written skip ledger with un-skip conditions exists at all — rare and valuable.

---

## 6. Documentation & spec coherence

### High

- **X1. Front door contradicts the shipped pipeline.** `README.md:33` / `docs/usage.md:37,263` say "0.1.0-preview.1... not on a public feed yet" while `release.yml` dual-publishes to nuget.org via Trusted Publishing (merged 2026-07-01) and the latest tag is `v0.1.61-preview.1`. `README.md:54` says only Light/Dark ship while `FS.GG.UI.Themes.AntDesign` is packable and accepted (ADR-0006). usage.md references retired `Color`/`Input` modules and lists 8 of 17 packages — omitting the theming packages a styling consumer needs — despite being touched 2026-06-30.
- **X2. module-map.md is authoritative-but-wrong.** Frozen at 2026-06-16: lists `Color` and `Input` (both retired in feature 179), omits 7 currently packable products, and every disposition still reads pre-import. Its layer *dependency* claims, however, verify exactly against `.fsproj` references.
- **X3. ADRs 0011–0014 unresolvable in-repo.** The release workflow, template.json comments, lifecycle headers, and specs 229–231 all cite them normatively; `docs/product/decisions/` ends at 0010 and nothing says where the rest live (org-level `FS-GG/.github`, inferable only from an epic reference). The in-repo decision log silently shares a number space with an external one.
- **X4. Spec status is write-once Draft.** 130 of 134 specs say `Status: Draft` (zero say done/superseded); feature number 204 is duplicated across two directories; spec 229 is explicitly reversed by 230's "Maintainer correction" yet carries no supersession marker; `Feature Branch:` lines point at squash-deleted branches; no specs index, 95 unexplained numbering gaps.

### Medium

- **X5.** README's roadmap pointer leads to `docs/reports/` — a graveyard of feature-171-era analyses, with the only "roadmap"-named file marked "Active (scheduled)" though its deliverables shipped.
- **X6.** Reports raise issues that later get fixed in code but never closed in the reports (the input-lag thread across 9 files, features 167–176); a committed color-policy report is a standing FAIL (2.99 vs 3.00); three timestamp conventions coexist in the directory name scheme.
- **X7.** CLAUDE.md's managed pointer targets the *completed* feature 231 plan as "the current plan" — between cycles this pointer is always one feature stale and never describes the repo generally.
- **X8.** `docs/product/README.md` indexes 3 of 10 ADRs, omitting the load-bearing DesignSystem split and Ant theme decisions.

### Low

ADR formatting split between two header/status styles; Ant hub says 96 controls, the registry has 97, snapshot date aging; `layering.md:45` presents `Themes.Fluent`/`Themes.Material` as if they exist; early migration specs' premises inverted by later history with no forward pointer (PROVENANCE.md handles the same history correctly).

### Done well

Zero dead relative links across all root/docs markdown (script-checked). The load-bearing architecture is physically true and matches its ADRs. README/usage code snippets match the live `.fsi` surface, including the transitive-Fable.Elmish caveat. CONTRIBUTING.md commands verified accurate. PROVENANCE.md is exemplary lineage with honest adaptation notes. The machine-checked docs (ant-design hub with its `refs` blocks and coverage honesty tests) drifted far less than the narrative ones — the drift pattern itself validates the gating approach.

---

## Cross-cutting themes and recommendations

1. **Close the guard-drift class with meta-guards (highest leverage, lowest cost).**
   - Assert "gate's test list == slnx test folder" (fixes B1 permanently, not just today).
   - Extend `validate-version-coherence.fsx` to the `v*` release lane and fail the release job on pin≠tag (B2).
   - Wire the flat `DesignTokens` block into `generate-design-tokens.fsx --check` (D3) and fix the header.
   - Add the missing entries to SKIPPED-TESTS.md and delete the three vacuous smokes (T1) — or make them real against the samples that do exist.
2. **Kill success-shaped stubs on the public surface.** Either implement or fail loud with typed diagnostics: `Path.combine`/`segment` (R2), `runBounded`'s ignored scene (R4), `Animation.Color` (R5), stack/dock configs (Layout), `SceneEvidence.render` Png-as-hash (R10). The repo has a good honest-failure idiom (`Evidence.renderPng`) — apply it uniformly.
3. **Unify control identity.** One shared id function (`Key ?? path`) used by focus, runtime stamping, binding routing, and widget metadata; give transient widgets real keys for their fabricated trigger/item ids (C1, C2). This is the root cause behind most unkeyed-control behavior loss.
4. **Fix the three visual-correctness holes before they're load-bearing:** paint-aware Canvas fingerprint (R1 — consider reusing `SceneHash`'s full fold), theme-field-based dark/success/warning resolution (D1, plus `Theming.toTheme` mode preservation), and size-evidence or hard 4.5 gating in the `wcag` policy (D2).
5. **Schedule the god-module splits** already implied by their internal structure: `SkiaViewer.fs` (delete `runPersistentWindow` dead code first — free 234 lines), `ControlsElmish.fs` (routing / frame loop / perf driver), `Symbology.fs` (extract the label engine). The `.fsi` files make these low-risk mechanical moves.
6. **Make CI cover what actually ships:** add the six missing test projects (B1), consider a scheduled (not per-PR) cadence for the opt-in GL and package-consumer smokes (T4), add NuGet caching keyed on lockfiles (B9), pin `fsdocs-tool` and SHA-pin `NuGet/login` (B7), route event values through `env:` (B8).
7. **Give narrative docs an update trigger or demote them.** Regenerate/verify module-map.md from the slnx; fix README/usage.md's feed/version/package list (X1); land in-repo stubs for ADRs 0011–0014 (X3); make spec `Status:` reflect merged/superseded or add a specs index (X4); repoint CLAUDE.md's plan reference between cycles (X7).

## Overall assessment

This is a disciplined codebase whose architectural spine — layering, purity seams, signature files, wire-format rigor, deterministic evidence — is verifiably intact and better-enforced than most. Its characteristic failure mode is not sloppy code but **unguarded assertions of state**: lists, headers, statuses, and gates that were true when written and had no mechanism to stay true at a 231-features-in-three-weeks cadence. The high-severity correctness items (control-id schism, paint-blind cache, dark-theme string match, WCAG under-gating, silent stubs, CI coverage holes) are individually small fixes; the durable win is adding the handful of meta-guards in §Recommendations-1 so this class stops recurring.
