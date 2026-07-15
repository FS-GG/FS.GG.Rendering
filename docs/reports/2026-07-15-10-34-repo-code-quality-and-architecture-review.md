# Repository code-quality & architecture review

**Date:** 2026-07-15 10:34 (+0200)
**Scope:** full repository at HEAD `1857996b` (main) — 20 `src/` projects (~62k lines F#),
19 test projects, build/packaging/CI/release/template infrastructure, and documentation/spec
coherence.
**Method:** six parallel deep-read reviews (core rendering; controls & Elmish-host;
design-system & theming; symbology/testing/governance-build; Elmish-adapter & diagnostics;
build/CI/docs). Each review re-verified the findings of the prior review
([`2026-07-02-14-07-repo-code-quality-and-architecture-review.md`](./2026-07-02-14-07-repo-code-quality-and-architecture-review.md),
434 commits earlier) against current source and hunted new issues. Every finding below was
verified against source at review time; `file:line` references are to `1857996b`.

---

## Executive summary

The repository is genuinely well-architected, and it is getting healthier under review. The
layering is physically true (verified against `.fsproj`: `Scene` is BCL-only, `DesignSystem →
Scene` only, `Themes.* → DesignSystem` only, `SkiaViewer` is the sole SkiaSharp/Silk.NET/GL
toucher, and `Controls` references neither Themes nor SkiaViewer). The Elmish adapter's purity
claim holds exactly. Signature-file discipline is honest — every `.fs` lacking a `.fsi` is
`module internal` by design.

Most importantly: **of the prior review's twelve top-priority findings, every one with a gate
behind it is now closed.** P1 (three coexisting control-id schemes) is unified onto
`Key ?? structural-path`; P3 (dark-mode resolved by string-matching `theme.Name`) now reads
`theme.Success`/`theme.Warning`; P4 (six test projects in no CI cadence) is fixed by *deriving*
the cadence from the `.slnx` with a dedicated meta-guard; P5 (release validating stale bits),
P7 (`wcag` passing body text at 3.0:1), P9/P12 (missing token generator & ADRs), and R1/R2/R4/
R5/R6 and the R7 disposal leak are all fixed. The repo demonstrably lives its "put a guard
behind the thing" thesis.

The problems that remain cluster into four systemic classes rather than random defects:

1. **Guards that check a *constant* instead of *deriving from source* re-rot.** The exemplary
   fix in this repo — CI cadence now greps the `.slnx` instead of hardcoding a list — is the
   template to follow. Where a gate still pins a literal, drift walks back in: the docs-currency
   gate bans one obsolete version string, so the version prose rotted again (F-DOCS-1, HIGH); the
   contrast gate hardcodes `intent = ""`, so half the themed surface is never checked (F-DS-1).
2. **Fail-open on *absent/empty* input.** The "honest failure" principle is strong for
   present-but-wrong and weak for absent. `EvidenceAudit` passes on an empty `readiness/` dir;
   `generated-tests-ran=true` is emitted when zero tests exist; inspection verdicts fall through
   to the artifact's self-declared status; readiness checks over an empty required-set are
   vacuously green.
3. **God modules at the seams are *growing*.** `SkiaViewer.fs` 2,858 → 3,126; `ControlsElmish.fs`
   2,361 → 2,865; `Symbology.fs` 1,435 → 1,509. Acknowledged debt, but the P10 fix and a
   ~780-line rich-text engine both landed *inside* the already-overloaded files.
4. **Per-frame perf residue in the text path** survived the R8 fix — `SKShaper` is off the draw
   path now, but the string is still re-resolved 2–3× per frame and glyph assembly is O(n²).

The highest-leverage fixes are, once again, mostly "make the guard derive from source" and "add
a required floor," not "rewrite the thing."

---

## Top priorities (cross-area, ranked)

| # | Finding | Area | Severity |
|---|---------|------|----------|
| 1 | Radio-group click math hardcodes `28.0` while the painter uses `theme.ControlHeight` (32.0) → wrong option dispatched, passes tests green | Controls | HIGH |
| 2 | README/`usage.md` advertise `0.1.58-preview.1`; real pin & latest tag are `0.10.0` (~9 releases stale); currency gate is green-by-construction | Docs/CI | HIGH |
| 3 | Contrast gate pins `intent = ""` → entire `IntentPolicy` surface never checked; two Ant `default`-button borders fall below the gate's own 3.0 floor | Design system | MEDIUM |
| 4 | Readiness `summarize` status ignores `Error` severity by itself; fatal framebuffer-wrap GL-init failure classified `RenderingLimitation` → can report `accepted` | Diagnostics | MEDIUM |
| 5 | `EvidenceAudit`/`EvidenceGraph` fail open on absent evidence (empty `readiness/` → `Verdict.Pass`) | Build | MEDIUM |
| 6 | `generated-tests-ran=true` emitted with zero tests; inspection/readiness verdicts pass on self-declared status or empty required-sets | Testing | MEDIUM |
| 7 | `drawText` re-resolves the whole string 2–3×/frame; `shapeText` glyph assembly is O(n²) + fresh `SKShaper` per call | Core / perf | MEDIUM |
| 8 | God modules grew: SkiaViewer 3,126 / ControlsElmish 2,865 / Symbology 1,509 | All | MEDIUM |
| 9 | DataGrid `cellFontSize = 11.0` used raw (no resolver pass) → theme cannot rescale grid-cell text | Controls | MEDIUM |

---

## Prior-review findings — verification at HEAD

All twelve prior top-priorities plus the eight core-rendering (R*) findings were re-verified.
Summary: **17 fixed, 3 partial (residue is acknowledged debt or an open perf hotspot),
0 regressed structurally** (one docs regression, F-DOCS-1, is a re-opening of P11's *class* via a
weak gate).

| Prior ID | Status | Evidence |
|---|---|---|
| P1 — three control-id schemes | **FIXED** | Feature 232 unified live dispatch/focus/hover onto `Key ?? structural-path`; `Focus.fs:55`, `Control.fs:722`, `RetainedRender.fs:252` all derive the same id. `RetainedId` is a separate, legitimate picture-cache identity bridged via `retainedCanonicalId` (`RetainedRender.fs:1627`). |
| P2 — canvas paint-blind fingerprint | **FIXED** | `Canvas/Elements.fs:187-219` `mixNode` is wildcard-free, folds `mixPaint` into every painted node. |
| P3 — dark-mode string-match | **FIXED** | `DesignSystem/Style.fs:17-19` reads `theme.Success`/`theme.Warning`; the sibling `Theming.fs:47` bug (always-`Theme.light` seed) also fixed. |
| P4 — six test projects in no cadence | **FIXED** | `gate.yml:169` derives the deterministic tier from the `.slnx`; `Build.Tests/CadenceCoverageTests.fs` asserts `deterministic ∪ GL == slnx`. |
| P5 — release validates stale bits | **FIXED** | `release.yml:369-372` packs `FS.GG.UI.*` at the `<FsGgUiVersion>` pin; `template-product-tests` restores at the pin. |
| P6 — stubbed Path API / runBounded | **FIXED** | `Scene.fs:219-233` `combine` returns `Result` (Union→Winding, Xor→EvenOdd, Intersect/Difference→typed Error); `runBounded` uses its scene. |
| P7 — wcag passes body text at 3.0:1 | **FIXED** | `ColorPolicy.fs:63-68` maps `AaLarge → Failed`. |
| P8 — vacuous / self-fulfilling tests | **PARTIAL (class persists)** | Pattern re-surfaces in evidence seams — see F-TEST-1/2/3, F-BUILD-1. |
| P9 — "GENERATED" with no generator | **FIXED** | `scripts/generate-design-tokens.fsx` exists and is runnable; `--check` clean (no drift). |
| P10 — radio/tabs/numeric dispatch `""`/`0.0` | **PARTIAL** | Tabs & numeric fixed and correct; the radio fix introduced F-CTL-1 (HIGH). |
| P11 — README/usage contradict pipeline | **PARTIAL / re-opened** | Feed, package list, retired modules fixed & gated; version string regressed — F-DOCS-1. |
| P12 — ADRs 0011-0014 missing | **FIXED** | Present as org pointer stubs; org ADRs cited via full URLs, no dangling relative links. |
| R3 — SkiaViewer god module + dead code | **PARTIAL** | Dead `runPersistentWindow` gone; the two `interpretEffects` are not verbatim dupes. Residue: file grew to 3,126 lines (F-CORE-1). |
| R7 — statics + undisposed `lastGoodFrame` | **PARTIAL** | Disposal fixed & correctly ordered (`Host/FrameCache.fs`, `OpenGl.fs:1873`). Single-instance statics remain (F-CORE-4). |
| R8 — text re-shaped every frame | **PARTIAL** | `SKShaper` off the draw path; two redundancies remain (F-CORE-2/3). |

---

## New findings

### Controls

**F-CTL-1 — radio-group click hardcodes `28.0`; painter uses `theme.ControlHeight` (32.0) → wrong option. HIGH.**
Click: `Controls.Elmish/ControlsElmish.fs:754` — `let rowH = min 28.0 (bounds.Height / float n)`.
Paint: `Controls/Internal/WidgetGeometry.fs:76` — `let rowH = min theme.ControlHeight (box.Height / float (List.length items))`.
`controlHeight = 32.0` in every shipped theme (`DesignTokens.fs:25`). Whenever a radio group is
laid out with rows taller than 28px (the normal legible case), paint places rows at 32px while
the click divides by 28. *Failure:* a 3-option group of height 120 → paint band for option 1 is
`[Y+32, Y+64]`; a click at `y = Y+60` (visually option 1) computes `floor(60/28) = 2` and
dispatches **option 2**. Because a *valid* message still fires, `BoundIds`-guarded tests pass
green — the exact success-shaped hazard the repo guards against, at the payload level. Also a
layering leak (a visual constant baked into behavior). The in-code comments at `:717`/`:737`
claiming the click uses "the SAME `rowH = min 28` `radioGeom` paints" are doubly false (paint
caps at 32, not 28). Slider (`:645`) and tabs (`:778`) were verified correct. *Fix:* thread
`theme.ControlHeight` in and delete the literal.

**F-CTL-2 — DataGrid `cellFontSize` unthemeable. MEDIUM.**
`Controls/Internal/DataGridGeometry.fs:19` (`cellFontSize = 11.0`) is used raw at `:39` with no
`Style.resolve` pass, so a theme cannot rescale grid-cell text. The radio (`WidgetGeometry.fs:90`,
`12.0`) and slider (`13.0`) font literals feed through `Style.resolve` as `baseStyle`, so a theme
*class* can override them — LOW for those, MEDIUM for DataGrid.

**F-CTL-3 — stale `Key ?? Kind` guidance in shipped skill. LOW.**
`Controls/skill/SKILL.md:244` still tells authors "visual state stamps by `Key ?? Kind`" — the
old collision-prone model, superseded by `Key ?? path` in Feature 232. The in-code diagnostic
(`Diagnostics.fs:221`) was updated; the shipped skill was not.

### Docs / CI

**F-DOCS-1 — front-door version prose ~9 releases stale; currency gate green-by-construction. HIGH.**
`README.md:41` and `docs/usage.md:37,44-45` advertise `0.1.58-preview.1` (including a copy-paste
`dotnet add package … --version 0.1.58-preview.1`). The real pin is `0.10.0`
(`template/base/Directory.Packages.props:9`) and the latest tag is `fs-gg-ui/v0.10.0`. The gate
meant to catch this — `Build.Tests/Feature242DocsCurrencyTests.fs:86` — only asserts the docs do
*not* contain the literal `0.1.0-preview.1`; it never compares against `$(FsGgUiVersion)`. This
is P11's failure class re-opened by a constant-checking guard. *Fix:* assert doc version ==
`$(FsGgUiVersion)`, mirroring the `.slnx`-derived cadence fix that worked for P4.

**F-DOCS-2..5 — ungated narrative drift. LOW.**
"17 libraries plus the BOM" over-counts (16 libraries + 1 BOM = 17 total) in `README.md:40`,
`usage.md:68`, `module-map.md:29`. `CLAUDE.md` points at `specs/251-keyboard-host-boundary/plan.md`
as "the current plan" though specs 252/253/254 have since landed. The org-synced comment in
`Directory.Build.props` asserts "Rendering … has NO `global.json`" but this repo carries one
(`global.json`, SDK `10.0.301`). None of these are gated against reality.

### Design system

**F-DS-1 — contrast gate never exercises `IntentPolicy`. MEDIUM.**
`ColorPolicy/StyleCatalog.fs:165` builds the "styles that reach the screen" catalog with
`intent` hardcoded to `""`, so every Ant theme falls through its `IntentPolicy` to identity
(`AntIntentPolicy.fs:53,103`) and the neutral chrome for `default`/`dashed`/`text`/`link`/`danger`
— the whole point of Features 132/173 — is never measured. Concrete latent failures the gate
*would* flag: Ant light `default` border `#d9d9d9` on `#f5f5f5` = 1.29; Ant dark `default` border
`#424242` on `#000` = 2.09 (both below the module's own 3.0 non-text floor). *Fix:* enumerate the
theme's intent vocabulary the way `kinds`/`variants`/`states` are enumerated.

**F-DS-2 — dead published `contrastRequiredRatio` token. LOW.**
Generated into `DesignTokens.fs:24,45`, declared public in `DesignTokens.fsi:46,87` ("…the theme
*must satisfy*"), sourced from `design-tokens.tokens.json` — but no code reads it; `Contrast`/
`ColorPolicy` hardcode 7.0/4.5/3.0. A published token documenting a constraint nothing enforces.

**F-DS-3 — `Style.fsi` says "eight `VisualState` cases"; there are nine. LOW.**
`DesignSystem/Style.fsi:41`. The DU (`Types.DesignSystem.fs:16-26`) has nine
(`FocusedHover` + `Validation` added later). Code is total; only the doc is stale.

### Diagnostics

**F-DIAG-1 — readiness status ignores `Error` severity by itself; fatal framebuffer failure → `accepted`. MEDIUM.**
`Diagnostics/Diagnostics.fs:395-405` derives status purely from *category* (`unclassified` →
ReviewRequired; `ReadinessBlocker` → Blocked; `DeveloperAction` → ReviewRequired; environment →
EnvironmentLimited; else Accepted). There is no branch keyed on `Severity = Error` alone, so a
fully-classified `RenderingLimitation`/`Error` yields `Accepted`. This is reachable: the
`Framebuffer` stage maps *unconditionally* to `RenderingLimitation`
(`SkiaViewer/Host/Diagnostics.fs:185`) — unlike `FrameRender`, which escalates non-warnings to
`ReadinessBlocker` — and `OpenGl.fs:932/934` emit a **fatal** framebuffer-wrap failure
("could not wrap the default framebuffer (FBO 0)"). If startup diagnostics are aggregated
through `summarize`, the readiness token is `accepted` despite a fatal. *Fix:* block on
`Error`/`Fatal` severity independent of category; reclassify fatal `Framebuffer` failures.

**F-DIAG-2 — `summarize` reads `DateTime.UtcNow`; contradicts the "pure evaluation" claim. LOW-MEDIUM.**
`Diagnostics.fs:356`. Everything else in the module is genuinely pure; this lone ambient-clock
read (for `DiagnosticException.ExpiresOn`) sits on the honesty-critical path — same inputs can
yield different verdicts across the expiry boundary, with no injectable clock.

**F-DIAG-3..6 — persisted artifacts can't disclose their own write failures (returned-vs-persisted status divergence); `.jsonl` omits synthesized exception-problem records; dead public `DiagnosticReadinessImpact` type; `AnimationTick.SubId` excludes `interval` (interval change won't restart the tick). LOW.**
`Diagnostics.fs:674-698,418,695`; `Diagnostics.fsi:18-22`; `Elmish/AnimationTick.fs:20`.

### Build / Testing (governance & evidence)

**F-BUILD-1 — `EvidenceAudit`/`EvidenceGraph` fail open on absent evidence. MEDIUM.**
`Build/Evidence.fs:150-160` — `Audit.evaluate` produces failures only for `PresentInvalid`
nodes; nothing in `recognized` is *required*. Empty `readiness/` → `Graph.sense = []` →
`evaluate [] = Verdict.Pass` → `run "EvidenceAudit"` writes `verdict=PASS`, exit 0. A generated
product emitting zero evidence passes the gate named *Audit* green. (Presence-completeness is
enforced out-of-band by `build.fsx`/`ReadinessFileDiscovery`, moderating severity, but the
isolated gate is fail-open.) *Fix:* add a required-artifact floor. Sub-finding F-BUILD-2: a
token-less `recognized` entry treats any non-whitespace byte as valid (`Evidence.fs:73-79`), so
~7 of 12 kinds are near-vacuous even when present.

**F-TEST-1 — `verifyGeneratedTests` success-shaped with zero tests. MEDIUM.**
`Testing/TestingEvidence.fs:170-187` — diagnostics only fire on `TestsExist && not TestsRan` or
`TestsRan && not VerifyRan`. With `TestsExist = false`, `Authoritative = true` and the readiness
token `generated-tests-ran=true` is emitted (`:327`). A product shipping zero generated tests
produces a "proof" that tests ran.

**F-TEST-2 — inspection validators trust self-declared status. MEDIUM.**
`Testing/TestingVisual.fs:1109-1117` and `TestingRetainedInspection.fs:421-422` — when no rule
fires and there are no invalid exceptions, the status is the artifact's own
`ReadinessStatus`. Every rule only bites on facts the artifact itself marks `Required`; there is
no floor requiring at least one required region/coverage/text fact. An artifact declaring nothing
`Required` with empty `Findings` and `ReadinessStatus = Accepted` is returned `Accepted`.

**F-TEST-3 — readiness verdicts pass on empty required-sets; `VisualCompleteness` accepts a blank PNG. LOW.**
`TestingCompositor.fs:183-184` and the Feature159/160/161 `DeriveStatus` mirrors return
`Accepted`/`Positive` when `RequiredScenarioIds = []` (vacuously "all present"). `TestingVisual.fs:189-190`
counts a fully-transparent, correctly-sized, decodable PNG as `VisualCaptureComplete` (blank
detection is delegated to the human-review gate, so not fully vacuous).

### Core rendering / performance

**F-CORE-1 — `SkiaViewer.fs` is a 3,126-line god module. MEDIUM.**
Grew from 2,858. Holds request/option validation, the bounded loop, the persistent-window loop,
three `runGeneratedApp`/interactive-host families, effect interpretation, evidence writing, and
rasterization. The effect-dispatch knot (source of #429/#535) must be reasoned about across 3k
lines.

**F-CORE-2 — `drawText` re-resolves the whole string 2–3×/frame. MEDIUM (per-frame hotspot).**
`SkiaViewer/SceneRenderer.fs:278-289` calls `Fonts.buildShapedGlyphRunData` (which resolves at
`Fonts.fs:595` and again via `realMeasure`→`resolveText` at 617/310), then `drawText` resolves a
third time at `:282`. Each `resolveText` is O(len) with a per-char `cachedFont` lock. Every
`Text`/`TextRun` node in an animated scene pays this 3× every frame; idle-skip only spares
structurally-unchanged scenes.

**F-CORE-3 — O(n²) glyph assembly + fresh `SKShaper` per call. MEDIUM.**
`Fonts.fs:479-509` — `codepoints`/`clusters`/`points` are converted from arrays to lists then
indexed positionally with `List.tryItem` inside a `List.mapi` over n glyphs ⇒ O(n²); plus a
`new SKShaper(...)` per call (`:448`). Reachable per layout measurement via the installed
real-metrics measurer. *Fix:* keep them as arrays and index O(1).

**F-CORE-4 — process-wide render statics defeat concurrency and are unsynchronized. LOW-MEDIUM.**
`Host/OpenGl.fs:486,507-524` and `Host/FrameCache.fs:22` are plain module mutables read/written
from the render loop with no synchronization (unlike `Fonts.gate`). Single-instance is
acknowledged epic debt; the *unsynchronized reads* are a latent data-race surface if any
diagnostic reader runs off-thread.

**F-CORE-5 — bounded on-screen GL frames don't paint the scene; "frame N presented" counts window frames. LOW.**
`SkiaViewer.fs:1856-1870` — the bounded on-screen `renderHandler` only increments a counter;
only the offscreen `.png` path (`:1793`) rasterizes the scene. Disclosed in the `.fsi` and
comments, so the PNG evidence is real; flagged because the `"frame {n} presented"` diagnostic
string alone reads as stronger evidence than it is.

---

## Verified healthy (spot-checked, not stubbed)

- **Layering & purity:** Scene/Canvas/Layout are Skia/Silk/GL-free (grep + `.fsproj`). The Elmish
  adapter (`Elmish/Elmish.fs`) `init`/`update` are pure over pure `Viewer.update`; all IO deferred
  to the host `interpretEffects` boundary the adapter never touches. `Style.resolve`/
  `StyleResolver.resolve` are pure, total folds.
- **Honest failure:** `Path.combine` returns typed errors for unimplementable boolean ops;
  `Layout.evaluate` collapses to empty bounds + Error on Yoga failure; `Symbology.Render.toPng`
  fails loud on any non-`ReferencePassed` verdict.
- **Real seams, not facades:** the #457 unresolved-id diagnostic closes the silent-unbound-click
  hazard; `respondsProofOf` routes real input through the real route and compares real
  before/after scenes (an inert host yields `Inert`); DataGrid/OverlayState are real state
  machines; diagnostics `summarize` never deletes the original diagnostic and fails closed on
  unclassified/invalid.
- **Gated infrastructure:** CI cadence derived from `.slnx` with a meta-guard; release OIDC/
  dual-publish wiring correct; `.fsi` packed into the nupkg and checked; token generation real and
  drift-gated; module-map kept honest by `Feature242DocsCurrencyTests`.

---

## Recommendations (highest leverage first)

1. **Fix F-CTL-1** — thread `theme.ControlHeight` into `radioGroupChangedMessages`, delete the
   `28.0` literal, correct the false comment. Small, self-contained, active user-facing bug.
2. **Convert two green-by-construction gates to derive-from-source** — assert doc version ==
   `$(FsGgUiVersion)` (F-DOCS-1); enumerate the theme intent vocabulary in the contrast gate
   (F-DS-1). Mirror the `.slnx`-derived cadence fix that already worked; retires both findings and
   the whole "constant-checking guard" class.
3. **Add a required floor to the fail-open-on-empty seams** (F-BUILD-1, F-TEST-1/2/3): fail
   `EvidenceAudit` when a required artifact is absent, not only malformed; make
   `generated-tests-ran` false when no tests exist; make readiness over an empty required-set
   `ReviewRequired`; block the diagnostics status ladder on `Error`/`Fatal` severity independent of
   category (F-DIAG-1).
4. **Budget god-module decomposition** (plan exists at
   `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`). At minimum stop
   landing new features *inside* `SkiaViewer.fs`/`ControlsElmish.fs`/`Symbology.fs`; extract the
   rich-text engine out of Symbology and the frame loop out of ControlsElmish.
5. **Fix the text-path hotspots** (F-CORE-2/3) — resolve once and reuse; keep glyph arrays as
   arrays for O(1) indexing; cache/reuse the `SKShaper`.
6. **Sweep the ungated narrative** (F-DOCS-2..5, F-DS-2/3, F-CTL-3, F-DIAG-3..6) — individually
   low-stakes, collectively the tail of the "unguarded prose rots" theme.

---

## Roadmap

Ordered by leverage. Each task is independently shippable; the phases are a suggested sequence,
not a hard dependency chain. Severity in brackets.

### Phase 1 — active correctness (do first)

- [ ] **[HIGH] F-CTL-1** — thread `theme.ControlHeight` into `radioGroupChangedMessages`
      (`Controls.Elmish/ControlsElmish.fs:754`), delete the `28.0` literal, and correct the false
      "SAME `min 28`" comments at `:717`/`:737`.
- [ ] **[HIGH] F-CTL-1 regression test** — add a headless click-routing test for a tall radio
      group (rows > 28px) asserting the *payload* index, not just that a message fired.
- [ ] **[MED] F-DIAG-1** — block the `summarize` status ladder on `Error`/`Fatal` severity
      independent of category (`Diagnostics.fs:395-405`); reclassify fatal `Framebuffer` failures
      off `RenderingLimitation` (`Host/Diagnostics.fs:185`). Add a fixture pairing
      `RenderingLimitation`/`BackendCost` with `Error`.

### Phase 2 — make guards derive from source (retire the "constant-checking gate" class)

- [ ] **[HIGH] F-DOCS-1** — change `Feature242DocsCurrencyTests.fs:86` to assert the doc version
      equals `$(FsGgUiVersion)` instead of banning a hardcoded literal; update `README.md:41` and
      `docs/usage.md:37,44-45` to `0.10.0`.
- [ ] **[MED] F-DS-1** — enumerate the theme's intent vocabulary in `StyleCatalog.emittedPairings`
      (`ColorPolicy/StyleCatalog.fs:165`) instead of pinning `intent = ""`; fix (or explicitly
      waive with evidence) the two Ant `default`-border pairings that fall below 3.0.

### Phase 3 — add a "required floor" to fail-open-on-empty seams

- [ ] **[MED] F-BUILD-1** — make `Audit.evaluate` fail when a required artifact is absent, not only
      `PresentInvalid` (`Build/Evidence.fs:150-160`); mark which `recognized` kinds are required.
- [ ] **[MED] F-BUILD-2** — require a non-empty/structured payload for token-less `recognized`
      kinds (`Build/Evidence.fs:73-79`).
- [ ] **[MED] F-TEST-1** — emit `generated-tests-ran=false` (or `ReviewRequired`) when
      `TestsExist = false` (`Testing/TestingEvidence.fs:170-187,327`).
- [ ] **[MED] F-TEST-2** — require at least one `Required` region/coverage/text fact before an
      inspection can resolve to `Accepted` (`TestingVisual.fs:1109-1117`,
      `TestingRetainedInspection.fs:421-422`).
- [ ] **[LOW] F-TEST-3** — treat an empty `RequiredScenarioIds` as `ReviewRequired`
      (`TestingCompositor.fs:183-184` + Feature159/160/161 mirrors); add pixel-content check to
      `VisualCompleteness` (`TestingVisual.fs:189-190`).

### Phase 4 — performance (per-frame text path)

- [ ] **[MED] F-CORE-2** — resolve each string once in `buildShapedGlyphRunData` and reuse; drop
      the third `resolveText` in `drawText` (`SceneRenderer.fs:278-289`).
- [ ] **[MED] F-CORE-3** — keep `codepoints`/`clusters`/`points` as arrays for O(1) indexing in
      `shapeText` (`Fonts.fs:479-509`); cache/reuse the `SKShaper` instead of `new` per call.

### Phase 5 — structural debt

- [ ] **[MED] F-CORE-1 / F-CTL / F-SYM god modules** — per
      `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`: extract the
      rich-text engine out of `Symbology.fs`, the frame loop out of `ControlsElmish.fs`, and stop
      landing new features inside `SkiaViewer.fs`.
- [ ] **[MED] F-CTL-2** — route DataGrid `cellFontSize` through `Style.resolve`
      (`DataGridGeometry.fs:19,39`) so a theme can rescale grid-cell text.
- [ ] **[LOW-MED] F-CORE-4** — synchronize (or document as strictly single-threaded) the
      process-wide render statics in `Host/OpenGl.fs:507-524` and `Host/FrameCache.fs:22`.
- [ ] **[LOW-MED] F-DIAG-2** — inject a clock into `summarize` for `ExpiresOn` evaluation
      (`Diagnostics.fs:356`) so the expiry boundary is testable and the purity claim holds.

### Phase 6 — ungated-narrative sweep (low individual stakes)

- [ ] **[LOW] F-DOCS-2** — fix the "17 libraries + BOM" count (16 + BOM = 17) in `README.md:40`,
      `usage.md:68`, `module-map.md:29`.
- [ ] **[LOW] F-DOCS-3** — point `CLAUDE.md` at the active spec (or gate the pointer against the
      latest feature).
- [ ] **[LOW] F-DOCS-4** — correct the org-synced `global.json` claim in `Directory.Build.props`.
- [ ] **[LOW] F-CTL-3** — update `Controls/skill/SKILL.md:244` from `Key ?? Kind` to `Key ?? path`.
- [ ] **[LOW] F-DS-2** — either wire `contrastRequiredRatio` into the resolver/gate or remove the
      dead published token (`DesignTokens.fs:24,45`, `.fsi:46,87`).
- [ ] **[LOW] F-DS-3** — fix `Style.fsi:41` "eight `VisualState` cases" → nine.
- [ ] **[LOW] F-DIAG-3..6** — persisted-vs-returned status divergence, `.jsonl` synthesized-record
      omission, dead `DiagnosticReadinessImpact`, `AnimationTick.SubId` excluding `interval`.

---

## Meta-observation

The prior review closed with "documents with a gate/test behind them stayed true; narrative
snapshots rotted." This review confirms the corollary and sharpens it: **a gate is only as true
as what it compares against.** The gates that derive their expectation from source of truth (the
`.slnx`-derived cadence, the token drift `--check`, the module-map currency test) held perfectly
across 434 commits. The gates that pin a *constant* — the docs version literal, the contrast
`intent = ""` — rotted exactly like the prose they were meant to protect. The pattern to
generalize repo-wide: **guards should assert `derived == source`, never `text != known-bad-literal`.**
