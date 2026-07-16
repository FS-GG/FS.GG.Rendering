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

- [x] **[HIGH] F-CTL-1** — thread `theme.ControlHeight` into `radioGroupChangedMessages`
      (`Controls.Elmish/ControlsElmish.fs`), delete the `28.0` literal, and correct the false
      "SAME `min 28`" comments. *Done:* `theme` threaded `bindingMessagesFor` → `activationValueFor`
      → `activationValueComputers` → `radioGroupChangedMessages`; cap now `min theme.ControlHeight`.
      Controls.Elmish builds clean; full Elmish.Tests suite green (271 passed, no regressions).
- [x] **[HIGH] F-CTL-1 regression test** — add a headless click-routing test for a tall radio
      group (rows > 28px) asserting the *payload* index, not just that a message fired. *Done:*
      `tests/Elmish.Tests/FCtl1RadioGeometryTests.fs` drives the real retained pointer route over a
      120px / 3-item group (rows = 40 > `theme.ControlHeight`), derives every expectation from
      `theme.ControlHeight`, guards against going vacuous, and probes the lower "green" band where the
      old `28.0` cap overshot into "blue". Verified it REDS on the reintroduced literal and greens on
      the fix; full Elmish.Tests suite 272 passed.
- [x] **[MED] F-DIAG-1** — block the `summarize` status ladder on `Error`/`Fatal` severity
      independent of category (`Diagnostics.fs:395-405`); reclassify fatal `Framebuffer` failures
      off `RenderingLimitation` (`Host/Diagnostics.fs:185`). Add a fixture pairing
      `RenderingLimitation`/`BackendCost` with `Error`. *Done:* `summarize` now derives an
      `unresolvedErrorCount` — non-excepted `Error`-severity groups NOT already routed off `Accepted`
      by an existing rung (`ReadinessBlocker`→Blocked, `DeveloperAction`→ReviewRequired,
      `Environment`→EnvironmentLimited) — and the ladder blocks on it, so a classified Error can never
      fall through to `Accepted`; the floor honors accepted exceptions. `Host/Diagnostics.fs`
      Framebuffer now mirrors `FrameRender`: Info/Warning→`RenderingLimitation`,
      Error/Fatal→`ReadinessBlocker` (the fatal `startupFailed Framebuffer` FBO-0 wrap failure now
      blocks). Fixtures `renderingLimitationError`/`backendCostError` added; readiness tests assert
      Blocked (verified RED without the fix: 2 fail), benign warnings still Accepted, exception clears
      the floor; host-mapping tests assert the fatal→`ReadinessBlocker` reclass and that the benign
      informational present-mode note stays `RenderingLimitation`. Diagnostics.Tests 18/18,
      SkiaViewer.Tests 354 passed, Controls/Testing/Elmish suites green.

### Phase 2 — make guards derive from source (retire the "constant-checking gate" class)

- [x] **[HIGH] F-DOCS-1** — change `Feature242DocsCurrencyTests.fs:86` to assert the doc version
      equals `$(FsGgUiVersion)` instead of banning a hardcoded literal; update `README.md:41` and
      `docs/usage.md:37,44-45` to `0.10.0`. *Done:* the currency gate now reads `<FsGgUiVersion>` from
      `template/base/Directory.Packages.props` and asserts every version the front-door docs state
      (backtick "framework version `X`" prose + `--version X` commands) equals the pin, with a
      non-vacuous floor; the frozen `0.1.0-preview.1` ban is gone. README/usage.md updated to `0.10.0`.
      Verified the gate REDS on a drifted version and greens on the pin; Feature 242 list 7/7 passed.
- [x] **[MED] F-DS-1** — enumerate the theme's intent vocabulary in `StyleCatalog.emittedPairings`
      (`ColorPolicy/StyleCatalog.fs:165`) instead of pinning `intent = ""`; fix (or explicitly
      waive with evidence) the two Ant `default`-border pairings that fall below 3.0. *Done:*
      `StyleCatalog` now enumerates an `intents` vocabulary (`primary`/`default`/`dashed`/`text`/
      `link`/`danger`) as a `scenarios` axis beside `variants`, so `emittedPairings` drives the
      resolver through each theme's `IntentPolicy` chrome (domain 132 → 264 combos, matching the
      long-standing "264 collapse to ~25" comment) — the Ant neutral `default`/`dashed`/`text`/`link`
      chrome now reaches the catalog rather than dedup-collapsing onto the base. The two sub-3.0
      borders (`#d9d9d9` on `#f5f5f5` = 1.29 light; `#424242` on `#000` = 2.09 dark) are the only
      GraphicOrUi failures surfaced; they are **waived with evidence** — Ant's neutral border is
      intentionally subtle (control identified by label + surface + elevation, which this renderer
      does not model; recolouring Ant's canonical token would break Ant fidelity). A new blocking
      boundary gate (companion to the Text gate) asserts every emitted GraphicOrUi pairing passes
      `wcag` except the enumerated `antNeutralBorderWaiver`, checked both ways so the waiver can
      neither grow silently nor go stale (verified RED when either entry is dropped). Emitted
      ant-light/ant-dark drift reports regenerated; Controls.Tests 1023 passed, Package.Tests 436
      passed.

### Phase 3 — add a "required floor" to fail-open-on-empty seams

- [x] **[MED] F-BUILD-1** — make `Audit.evaluate` fail when a required artifact is absent, not only
      `PresentInvalid` (`Build/Evidence.fs:150-160`); mark which `recognized` kinds are required.
      *Done:* `Sensing.recognized` now carries a per-artifact `required` flag; the required set is the
      contract-backed headless baseline (`layout-evidence.txt` + `headless-scene-evidence.txt`,
      evidence-output-contract.md §EvidenceGraph "required-for-profile"). A new `Sensing.missingRequired`
      reports required artifacts with no present node (a malformed baseline is caught by its token
      contract, not double-counted). `Audit.evaluate` folds absent-required into the verdict as a
      product-evidence defect, and `GeneratedRunner.run "EvidenceGraph"` exits non-0 on it too; both
      reports disclose the missing baseline. An empty `readiness/` now audits **FAIL**, not a vacuous
      PASS. Public surface unchanged (all new logic private to the assembly). `EvidenceTests` flipped the
      old "empty surface passes" case to assert the floor (verified it FAILs the evidence-less surface,
      names both baselines, classes it product-evidence-defect) and added a partial-baseline guard;
      the honest-fail malformed test now writes a complete baseline so it isolates the present-invalid
      path. Build.Tests 71 passed, Package.Tests 436 passed (public-surface baseline unaffected).
- [x] **[MED] F-BUILD-2** — require a non-empty/structured payload for token-less `recognized`
      kinds (`Build/Evidence.fs:73-79`). *Done:* every previously token-less kind now carries the
      stable structural key/value tokens its real writer (`template/base/src/Product/EvidenceCommands.fs`)
      emits on **every** code path (ok/failure/unsupported), so a present-but-vacuous artifact is caught
      as malformed instead of falling through `stateOf`'s "any non-whitespace byte is valid" branch:
      `layout` → `command=--layout-evidence`/`overlap-status=`/`measurement-mode=`; `scene` →
      `size=`/`capabilities=`/`hash=` (the `SceneEvidence` metadata value); `launch` →
      `command=--launch-evidence`/`mode=`; `screenshot`/`pixel-readback` →
      `command=--…-evidence`/`evidence-kind=`; both `bounded-smoke` files → `smoke=bounded-viewer`/
      `diagnostic-mode=`. No entry uses the empty-token fallback any more. Public surface unchanged
      (all logic private to the assembly). `EvidenceTests` fixtures switched to realistic writer-shaped
      baselines, and a new F-BUILD-2 guard asserts a one-byte `layout-evidence.txt` now senses
      `PresentInvalid` (naming the missing token) and audits `verdict=FAIL` as malformed-not-absent
      (verified RED against the pre-fix token-less list). Build.Tests 72 passed, Package.Tests 436 passed.
- [x] **[MED] F-TEST-1** — emit `generated-tests-ran=false` (or `ReviewRequired`) when
      `TestsExist = false` (`Testing/TestingEvidence.fs:170-187,327`). *Done:* `verifyGeneratedTests`
      now adds a leading `not check.TestsExist` branch — the absent case yields a
      `no-generated-tests` failure class, a "no generated tests exist to establish authority"
      diagnostic, and `Authoritative = false`, so `buildValidationContractOutput` emits
      `generated-tests-ran=false` / `authoritative=false` / `failure-class=no-generated-tests`
      instead of minting a "tests ran" proof over zero tests. Public surface unchanged (logic-only).
      `Testing.Tests` added two guards (the unit case flips non-authoritative; the contract case
      asserts the `generated-tests-ran=false` emission) — verified RED on the pre-fix fall-through;
      full Testing.Tests suite 109 passed.
- [x] **[MED] F-TEST-2** — require at least one `Required` region/coverage/text fact before an
      inspection can resolve to `Accepted` (`TestingVisual.fs:1109-1117`,
      `TestingRetainedInspection.fs:421-422`). *Done:* both `validateCheck` bodies now compute a
      `hasInspectionEvidence` floor and downgrade a self-declared `Accepted` that clears it. Visual
      requires a required region/text fact (`RequiredRegionIds` non-empty, or a `Required` region or
      text run) **or** a rule-produced finding — an artifact that declares nothing `Required` and
      whose rules fire nothing falls to `Incomplete`. Retained requires an inspected damage transition
      (both `Transition` and `Damage` present) **or** a rule-produced finding, else falls to
      `ReviewRequired`. The floor is deliberately met by a real rule-produced finding so the
      exception-accepted overlay/broad-damage cases (a genuine reviewed finding) stay `Accepted`; both
      downgrades emit a disclosing "…would be vacuous" diagnostic. Public surface unchanged (logic-only;
      rule-findings split out from `check.Artifact.Findings` so a self-declared finding cannot spoof the
      floor). `Testing.Tests` added four guards — the two vacuous cases (verified RED: 2 fail without the
      fix) and two non-over-block guards (a satisfied required region / an inspected transition keep
      `Accepted`); full Testing.Tests 113 passed, Package.Tests 436, Controls.Tests 1023 green.
- [x] **[LOW] F-TEST-3** — treat an empty `RequiredScenarioIds` as `ReviewRequired`
      (`TestingCompositor.fs:183-184` + Feature159/160/161 mirrors); add pixel-content check to
      `VisualCompleteness` (`TestingVisual.fs:189-190`). *Done:* every scenario-gated readiness
      validator now carries an empty-required-set floor that fails closed rather than certifying
      vacuously — an empty `RequiredScenarioIds` is treated identically to all-scenarios-missing:
      `CompositorTimingAssertions.validateSummary` → `Incomplete`, `CompositorDamageReadiness.validate`
      / `Feature159`/`Feature160`/`Feature161` → their `Rejected`-equivalent, each with a disclosing
      "must declare at least one required scenario" diagnostic. (`Feature160`/`Feature161` were truly
      fail-open — an empty set + one accepted iteration/artifact minted `Accepted`; the compositor and
      `Feature159` mirrors already fell to a *vacuous* `FallbackOnly`, now made an explicit fail-closed.)
      `VisualCompleteness.validateOne` gains a conservative `isBlankCapture` pixel check: a correctly-sized,
      decodable, but all-alpha-zero PNG now records `VisualCaptureBlocked` (`"blank screenshot …"`) instead
      of `VisualCaptureComplete`, so it blocks readiness (waivable via the existing accepted-exception gate)
      rather than passing as real evidence; opaque/solid-colour content still passes (Opaque short-circuit).
      Public surface unchanged (all logic-only; no `.fsi` edits). Six new guards added across
      `Feature156/157/159/160/161` helper tests (empty-required floor) and `Feature164` (blank PNG),
      each verified RED against the unfixed source (stash → 6 fail). Testing.Tests 119 passed,
      Package.Tests 436 passed, Rendering.Harness.Tests 308 passed.

### Phase 4 — performance (per-frame text path)

- [x] **[MED] F-CORE-2** — resolve each string once in `buildShapedGlyphRunData` and reuse; drop
      the third `resolveText` in `drawText` (`SceneRenderer.fs:278-289`). *Done:* the shaping path now
      surfaces the resolution it already computes — `shapeTextWithResolution` (installed path resolves
      once; fallback paths return `[]`), `buildShapedGlyphRunDataResolved` returns `GlyphRunData *
      ResolvedChar list`, and `shapeText`/`buildShapedGlyphRunData` are thin wrappers over them (public
      surface additive, `.fsi` updated). `drawText` now calls the resolved builder and reuses the returned
      list for fallback-event disclosure instead of calling `Fonts.resolveText` a second time — the
      per-frame resolve in the paint path drops from two to one, with byte-identical disclosure (the
      reused list is the exact value the shaper resolved). New `FCore2TextResolveTests` pins that the
      reused resolution equals a standalone `resolveText` (non-vacuous: fixture carries substituted/tofu
      disclosure), that the resolved builder returns the same glyph run as the plain builder, and that the
      non-installed path resolves nothing. Existing Feature136 render-path disclosure tests (tofu counts,
      per-frame scoping) stay green through the rerouted `drawText`. SkiaViewer.Tests 357 passed,
      Rendering.Harness.Tests 308 passed.
- [x] **[MED] F-CORE-3** — keep `codepoints`/`clusters`/`points` as arrays for O(1) indexing in
      `shapeText` (`Fonts.fs:479-509`); cache/reuse the `SKShaper` instead of `new` per call. *Done:*
      the installed-provider shaping body (`shapeTextWithResolution`) now indexes the shaper's
      `Codepoints`/`Clusters`/`Points` (and the per-char resolution) as arrays — the per-glyph
      `List.tryItem` positional lookups inside the `List.mapi` (O(n²) glyph assembly) are gone, replaced
      by an `Array.mapi` with bounds-checked O(1) indexing; the string-clamping (`sourceAt`/`resolvedFace`
      guarding cluster ranges) is preserved because shaper clusters are UTF-8 byte offsets that can exceed
      `String.Length`. A new module-level `shaperCache` (`Dictionary<SKTypeface, SKShaper>`, reference
      identity, bounded by `typefaceCache`) is consulted via `cachedShaper` instead of `new SKShaper(...)`
      per call; `Shape` mutates the shaper's HarfBuzz buffer so callers serialize on the instance
      (`lock shaper`). `disposeCaches` now disposes cached shapers too, and an internal `shaperCacheCount`
      (added to `.fsi`) surfaces the bound. New `FCore3ShaperReuseTests`: the reuse guard shapes a long
      string 50× per font and asserts exactly one shaper is cached (two for two typefaces, zero after
      teardown) — verified RED against the pre-fix per-call `new SKShaper` (count stuck at 0); the
      assembly guard shapes a long ASCII string and asserts every glyph cluster indexes into the source,
      clusters are non-decreasing (LTR), advances are non-negative, and the run reaches its total advance
      at the final glyph (the array width-boundary case). SkiaViewer.Tests 359 passed,
      Rendering.Harness.Tests 308 passed.

### Phase 5 — structural debt

- [ ] **[MED] F-CORE-1 / F-CTL / F-SYM god modules** — per
      `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`: extract the
      rich-text engine out of `Symbology.fs`, the frame loop out of `ControlsElmish.fs`, and stop
      landing new features inside `SkiaViewer.fs`.
      *Partial (F-SYM done):* the ~540-line label / rich-text LAYOUT engine (weight-aware fit, whitespace
      word-wrap, inline styled runs, laid-out paragraphs, and the per-phase label-motion transforms) is
      extracted out of `Symbology.fs` into a new dependency `module internal LabelLayout`
      (`src/Symbology/LabelLayout.fs`), and the public label text types (`LabelRun`/`LabelAlign`/
      `LabelParagraph`/`LabelText`/`LabelMotion`) moved with it — same namespace, so the public surface
      baseline is byte-identical and the four call sites reroute through `LabelLayout.{labelDispatch,
      motionLabelNodes,lineHeightOf,restPhase}`. `Symbology.fs` drops 1508 → 835 lines (under the plan's
      ~1,500 exit floor); `Token`, auto-label projection, and the three grammars stay behind. A new
      api-surface mirror (`docs/api-surface/Symbology/LabelLayout.fsi`, types-only after the internal
      module strips) keeps a generated product's shipped label surface complete. Verified byte-identical:
      Symbology.Render golden PNGs 20/20 and SymbologyBoard 28/28 unchanged, surface baseline unchanged;
      Symbology.Tests 486, Package.Tests 436, full solution builds.
      *Update (2026-07-15, follow-through):* the §7 **golden-image gate now exists** — `Rendering.Harness.GoldenImage`
      + `GoldenImageGateTests` (#816), a fail-closed per-pixel corpus comparison behind the in-process CPU
      raster, byte-identical in-repo and proven non-vacuous by an injected-regression test. Behind it,
      **`SkiaViewer.fs` split 3,126 → 2,612** (#817) via two gate-verified cuts, zero public-surface change:
      three self-contained modules (`DiagnosticsFiltering`/`WindowBehaviorValidation`/`HostCapability`) and
      the 16-member evidence-writer cluster (→ `ViewerEvidence.fs`). The `ControlsElmish.fs` frame-loop state
      extraction was already landed (Feature 186 `FrameScriptState`; `Perf.runScriptCore` down to 3 structural
      mutables). *Remaining:* `SkiaViewer.fs`'s strongly-connected core — the three big hot-path loops
      (`runPresentedPersistentWindow`/`runGeneratedApp`/`runInteractiveViewerWithWindowBehaviorCore`) + the
      public `runApp*` entry points, bidirectionally coupled to the shared launch/window helpers
      (public `failureFromDiagnostic`/`classifyWindowObservation` ↔ private `makeFailure`/`presentedFor`/
      `tryObserved`). No clean topological cut remains; further reduction is incremental threaded work, each
      cut verified against the golden-image gate.
      Note: the plan doc's Phase 1 (harness `Compositor`/`Cli`/`ValidationLanes` data-table refactor) and
      Phases 4–5 (`Scene.fs` → 490, `Control.fs` → 987) already landed piecemeal across earlier features.
- [x] **[MED] F-CTL-2** — route DataGrid `cellFontSize` through `Style.resolve`
      (`DataGridGeometry.fs:19,39`) so a theme can rescale grid-cell text. *Done:* `cellText` now
      builds a `baseStyle` at `cellFontSize` and paints `mkTextW` at the RESOLVED `FontSize`/`FontWeight`
      via `Style.resolve theme baseStyle classes state` — the last cell-family site painting a raw
      literal is gone (mirrors the #383/#384 radio/slider/button seam). `ContentRender` threads the
      cell's attached style classes + visual state into `cellGeom`/`headerCellGeom`, so a `StyleClass.Font`
      (or a state overlay) now rescales grid-cell text instead of being dropped. `resolve theme base []
      Normal = base`, so every shipped unthemed grid is byte-identical. New `FCtl2DataGridFontTests`
      pins both halves: the 11.0 byte-identity anchor (plain body + header cell) and the newly-live seam
      (a `Font` class reaching the body- and header-cell labels) — verified RED against the raw literal
      (2 fail, anchors stay green) and green on the fix. Controls builds clean; Controls.Tests 1027 passed.
- [x] **[LOW-MED] F-CORE-4** — synchronize (or document as strictly single-threaded) the
      process-wide render statics in `Host/OpenGl.fs:507-524` and `Host/FrameCache.fs:22`.
      *Done:* the statics are strictly single-threaded (Issue #180: Silk drives the window from one
      thread and the run mutates them without a lock), so rather than lock the per-frame hot path this
      turns the unstated single-thread assumption into an ENFORCED invariant. A new
      `module internal RenderThread` (`Host/RenderThread.fs`, compiled before `FrameCache.fs`) records
      the loop thread on `GlHost.run` entry (`claim`) and clears it in the teardown `finally` (`release`);
      the accessors an off-thread caller could actually reach — `GlHost.lastPresentTiming`,
      `GlHost.setLiveAuthoringSizeOverride`, and `FrameCache.{current,replace,release,beginRun}` — call
      `RenderThread.verify` first, which `invalidOp`s (naming the offending seam) when touched off the
      owning thread. The private per-frame carriers are lexically inside the loop callback and have no
      external accessor, so guarding the boundary accessors covers the whole off-thread reach surface
      with no hot-path cost. The guard is inert between runs (no owner claimed), so the Issue #177
      direct-call `FrameCache` lifetime tests and any pre-run accessor read are unaffected. Public
      surface unchanged (all internal). New `FCore4RenderThreadTests` drives the guard directly — the
      owning thread reads normally, an off-thread `verify`/`FrameCache.current` fails loudly naming the
      seam, `ownerThreadId` tracks claim/release, and the unowned case stays inert — verified RED against
      a no-op guard (2 fail). SkiaViewer.Tests 362 passed, Package.Tests 436, Rendering.Harness.Tests 308.
- [x] **[LOW-MED] F-DIAG-2** — inject a clock into `summarize` for `ExpiresOn` evaluation
      (`Diagnostics.fs:356`) so the expiry boundary is testable and the purity claim holds.
      *Done:* the verdict logic moved into a pure `summarizeAt (now: DateOnly) …` core (a total function
      of its inputs — `ExpiresOn` is the only date-sensitive input); `summarize` is now a thin adapter
      that supplies `DateOnly.FromDateTime(DateTime.UtcNow)`, the single ambient-clock read on the verdict
      path (public surface additive — `summarizeAt` added to `Diagnostics.fsi`, `summarize` unchanged so
      every existing caller is untouched). `writeArtifacts` now reads the clock ONCE and threads the same
      `now` into both its `summarizeAt` calls, closing a latent inconsistency where its two `summarize`
      calls each read `UtcNow` separately and could straddle a day boundary within one write. New
      `Feature169ReadinessTests` guards drive `summarizeAt` across an exception's expiry boundary
      deterministically — valid the day before and ON expiry (`expires >= now`), expired (ReviewRequired,
      count 0) the day after — and pin the adapter equal to `summarizeAt` at today's date. Diagnostics.Tests
      20/20 (was 18), full solution builds, Testing.Tests 119, SkiaViewer.Tests 362, Package.Tests 436 green.

### Phase 6 — ungated-narrative sweep (low individual stakes)

- [x] **[LOW] F-DOCS-2** — fix the "17 libraries + BOM" count (16 + BOM = 17) in `README.md:40`,
      `usage.md:68`, `module-map.md:29`. *Done:* verified against source — the `.slnx` ships 17
      packable `FS.GG.UI.*` products, one of which is the `FS.GG.UI`/`Meta` BOM (`ColorPolicy` is
      `IsPackable=false`), so the library count is 16. All three docs corrected `17 → 16`. Rather than
      leave the count ungated (the exact "unguarded prose rots" class), `Feature242DocsCurrencyTests.fs`
      now DERIVES the expected count from the same slnx-parsed `packableIds` it already uses (packable
      minus the BOM) and asserts each front-door doc's "N libraries plus/+ the … BOM" prose equals it,
      non-vacuously per doc — so adding or retiring a library forces the count prose to move with it.
      Verified the gate REDS on a drifted `17` and greens on `16`; Build.Tests 75 passed.
- [x] **[LOW] F-DOCS-3** — point `CLAUDE.md` at the active spec (or gate the pointer against the
      latest feature). *Done:* CLAUDE.md's SPECKIT-managed "current plan" pointer, stale at spec 251
      while 253 had landed, updated to `specs/253-audio-host-seam/plan.md` (the highest-numbered spec
      with a `plan.md`; 252/254 have none). Rather than pin a literal, `Feature242DocsCurrencyTests.fs`
      now DERIVES the expected pointer from source — the highest-numbered `specs/<id>/` that actually
      has a `plan.md` (monotonic speckit numbering ⇒ latest planned) — and asserts CLAUDE.md's pointer
      equals it and resolves to a real file, so the pointer can neither rot to an older spec nor dangle
      at a plan-less one. Non-vacuous (fails closed if CLAUDE.md states no pointer). Verified the gate
      REDS on the drifted `251` pointer (naming actual vs. expected) and greens on `253`; Build.Tests
      76 passed.
- [ ] **[LOW] F-DOCS-4** — correct the org-synced `global.json` claim in `Directory.Build.props`.
      *Deferred — not fixable in this repo.* `Directory.Build.props` is DISTRIBUTED from FS-GG/.github
      (`dist/dotnet/Directory.Build.props`; header lines 5-7: "DO NOT EDIT in a consumer repo — edits are
      overwritten on the next sync and fail the drift check"). The stale claim at `:74` ("… Rendering … have
      NO `global.json` at all") went false when this repo adopted the org SDK pin (`global.json`, SDK
      `10.0.301`, commit 9c59d862 / .github#557). A local edit would red the sync drift check and be
      overwritten on the next sync. The fix belongs upstream in the org canonical, then synced down → file
      against FS-GG/.github (see `cross-repo-coordination`).
- [x] **[LOW] F-CTL-3** — update `Controls/skill/SKILL.md:244` from `Key ?? Kind` to `Key ?? path`.
      *Done:* the shipped skill prose no longer states the superseded collision-prone model. Feature 232
      unified every seam onto `Key ?? path` (`src/Controls/Diagnostics.fs:196,221`), so
      `src/Controls/skill/SKILL.md:244` is corrected to `Key ?? path` AND its now-false "collapse to one
      stamp id / marks them ALL" claim is rewritten to the real hazard — unkeyed same-kind siblings resolve
      by positional path, so a structural insert/remove shifts their ids and their focus/hover/press identity
      is unstable across that change (mirrors the in-code `unkeyedInteractiveSiblings` diagnostic wording).
      The identical stale prose in the sibling shipped skill `src/Diagnostics/skill/SKILL.md:181-188` (same
      superseded model, "share one stamp id" symptom) is corrected the same way in the same PR so the finding
      cannot reopen next door. Neither body is manifest-hashed (the skill manifest hashes the `.agents/`
      wrappers, which point at the bodies by relative path) nor byte-mirrored; skill-parity passed
      (critical=0/high=0/warning=0), skill-refs ok (21 published skills, 109 internal bodies all resolve).
      Docs-only; no code or public-surface change.
- [x] **[LOW] F-DS-2** — either wire `contrastRequiredRatio` into the resolver/gate or remove the
      dead published token (`DesignTokens.fs:24,45`, `.fsi:46,87`). *Done (wired into a gate):* the
      published token — "the minimum foreground/background contrast ratio the theme MUST satisfy"
      (`DesignTokens.fsi:45,86`) — was inert prose no runtime code read (the WCAG gates hardcode the
      fixed 7.0/4.5/3.0 role tiers, `Contrast.fs`). Rather than delete a real, satisfiable constraint,
      `Feature127ColorPolicyTests.fs` now ENFORCES it: for each default theme it asserts the shipped
      foreground-on-background contrast (`Contrast.ratio`) clears the theme's own declared
      `contrastRequiredRatio`, with a non-vacuous floor (`required > 1.0`) and a guard that the token
      primitives are exactly what the built theme paints (`Theme.fs:14-15,34-35`). Measured 14.03 (light)
      / comparable (dark) against the 4.5 declared floor; verified RED when the token is raised above the
      achievable ratio (the message surfaces the real 14.03 measured). Public surface unchanged
      (test-only). Controls.Tests Feature127 21 passed.
- [x] **[LOW] F-DS-3** — fix `Style.fsi:41` "eight `VisualState` cases" → nine. *Done:* the DU
      (`Types.DesignSystem.fs`) carries nine cases (`FocusedHover`/`Validation` landed after the prose
      was written). THREE hand-maintained copies stated the stale "eight" and were corrected to nine:
      `src/DesignSystem/Style.fsi:41`, the real skill body `src/DesignSystem/skill/SKILL.md:104` (the
      `.claude/skills/fs-gg-design-system/SKILL.md` is a 12-line pointer stub, not this body), and the
      product-facing api-surface MIRROR `template/base/docs/api-surface/DesignSystem/Style.fsi:42` — the
      last of which no gate would have caught, since the M-MIR mirror gate strips `//` comments before
      comparing and this `.fsi` is not in `inRepoExactCopies` (an ungated third copy, F-DS-3's exact
      failure class). Rather than pin a literal, a new reflection-derived gate
      (`tests/Controls.Tests/FDs3VisualStateCountTests.fs`) DERIVES the count from
      `FSharpType.GetUnionCases(typeof<VisualState>)` and asserts all three prose spots spell that
      cardinal, so adding/retiring a case forces the prose to move with it (non-vacuous: the count must
      be >1 and spellable, and each marker phrase must be present). Verified RED against the reintroduced
      "eight" in each of the three files (each names actual-vs-expected) and green on the fix. Both
      `.fsi` changes are comment-only (public surface unchanged). Controls.Tests 1032 passed.
- [x] **[LOW] F-DIAG-3, F-DIAG-4, F-DIAG-6** — persisted-vs-returned status divergence, `.jsonl`
      synthesized-record omission, `AnimationTick.SubId` excluding `interval`. *Done:*
      **F-DIAG-3** — `writeArtifacts` now writes the per-record `.jsonl` FIRST, then persists the summary
      `.md` and (last) the machine-read `.json`, each re-rendered against the write failures known so far.
      A summary write failure is a `DeveloperAction` that flips the verdict to `ReviewRequired`; the old
      order rendered the on-disk summaries from the pre-failure `initial`, so a `.jsonl` write failure that
      the caller was *returned* as `ReviewRequired` was persisted on disk as `accepted`. Each persisted
      summary is now built exactly like the returned one (`persistedSummary ()`), so the on-disk
      `artifactWriteDiagnostics` array / `.md` "Artifact Write Warnings" section agree too — not just the
      status. The sole residue is inherent (a summary artifact cannot record its own write failure) and is
      documented in-code; `.json` is written last so it discloses the most. **F-DIAG-4** — the
      invalid/unmatched-exception synthesis is factored into a private `synthesizeExceptionProblems` shared by
      `summarizeAt` and `writeArtifacts`, so the `.jsonl` carries the exact verdict-bearing records the summary
      folds in (an unmatched exception that drove `ReviewRequired` is no longer absent from the records
      artifact). **F-DIAG-6** — the `AnimationTick` SubId is now keyed on `interval.Ticks`, so a changed
      interval reads as a new subscription (old timer disposed, new period started) instead of Elmish keeping
      the stale timer under a fixed id; an unchanged interval keeps the same id so a steady tick is not
      churned. New guards: `Feature169ArtifactTests` gains the F-DIAG-3 persisted-vs-returned (status + the
      dedicated write-diagnostics disclosure) and F-DIAG-4 `.jsonl`-synthesis cases; `AnimationTickTests`
      asserts the interval-keyed id and same-interval/changed-interval behavior. Verified RED against the
      stashed pre-fix source (Diagnostics 2 fail, Elmish 1 fail) and green on the fix. Diagnostics.Tests 22,
      Elmish.Tests 272, Package.Tests 436.
- [ ] **[LOW] F-DIAG-5** — dead public `DiagnosticReadinessImpact` type. *Deferred — the fix trips a
      required breaking-change gate.* The DU is genuinely dead (grep-confirmed: referenced nowhere but its
      own declaration + the frozen Feature-169 contract spec), so removing it is correct hygiene. But
      `FS.GG.UI.Diagnostics` is a published package (`Diagnostics.fsproj` `<Version>0.4.0-preview.1</Version>`)
      and deleting a public type is a CP0002 break: the **required** `api-compatibility-gate` (ADR-0101/0103,
      `enforce_admins` on, so not `--admin`-bypassable) reddens and its documented remedy is to cut a SemVer
      major for the package. Forcing a coordinated package-major cut to delete six lines of dead code is the
      wrong trade to bundle into a LOW narrative-sweep and merge autonomously — the removal belongs in the
      package's next planned major (or a dedicated, coordinated version bump), not here. Confirmed the gate
      blocks it: the removal red the `api-compatibility-gate` on PR #814; reverting the removal cleared it.
      **F-DIAG-3..4..6** landed in the same PR (they are internal, non-breaking, public-surface unchanged).

---

## Meta-observation

The prior review closed with "documents with a gate/test behind them stayed true; narrative
snapshots rotted." This review confirms the corollary and sharpens it: **a gate is only as true
as what it compares against.** The gates that derive their expectation from source of truth (the
`.slnx`-derived cadence, the token drift `--check`, the module-map currency test) held perfectly
across 434 commits. The gates that pin a *constant* — the docs version literal, the contrast
`intent = ""` — rotted exactly like the prose they were meant to protect. The pattern to
generalize repo-wide: **guards should assert `derived == source`, never `text != known-bad-literal`.**
