# Implementation Plan: Showcase Rendering Defect Fixes

**Branch**: `136-showcase-render-fixes` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/136-showcase-render-fixes/spec.md`

## Summary

A visual audit of the Ant Design Controls Showcase (feature 135) found seven classes of rendering defect
across all 19 pages: **wrong glyphs** (`@`→`7`, `—`/`#`/`▸`/`·` substituted), **truncated text**
(`Stable`→`STABL`), **region overlap**, **control overlap**, **overlay overprint**, **mis-structured
composite controls** (data-grid stacked vertically, menu overprint, dropdowns painting in-flow, descriptions
overlap, charts overrunning, blank QR), and **unbounded/non-scrolling content**.

Research traced the four text defects to a single root cause in the renderer: `drawTextWithFallback`
(`src/SkiaViewer/SceneRenderer.fs:242`) routes a whole string to a hand-rolled 5×7 bitmap
(`vectorGlyphPattern`) whenever the host's `SKTypeface.Default` lacks coverage for *any* character in it —
and that bitmap path uppercases everything, renders unmapped characters as a wildcard that reads as `7`, and
advances glyphs at `~0.857·size` while the geometry-only measurer (`Scene.measureText`, `0.58·size`)
disagrees, so boxes are sized too small and text is clipped mid-word. The composite-control and layout
defects are independent control/renderer/layout deficiencies.

The fix is predominantly **framework-level** (per FR-011 "framework if possible"):

1. **Deterministic bundled text rendering.** Ship a **standard set of fonts** (Noto Sans, Inter, DejaVu Sans
   + the three monospaces) as embedded assets in `FS.GG.UI.SkiaViewer`, loaded via `SKTypeface.FromStream`
   so text no longer depends on the host's `SKTypeface.Default`. A font registry resolves a family name to a
   real typeface; a **per-character fallback chain** (requested family → other bundled families → deliberate
   ASCII substitute → disclosed "tofu" box) renders correct mixed-case text with correct `@`/`#`/`—`/`·`,
   and **never** an arbitrary wrong glyph (FR-001). The 5×7 vector path is retained only as the final
   disclosed tofu renderer.
2. **Measurement reconciled to rendering.** `Scene.measureText` is made to agree with the bundled font's real
   advances through a measurement seam, killing the truncation class (FR-002).
3. **A real overlay pass.** `Control.renderTree` gains a deferred "paint last, above flow" overlay layer so
   transient surfaces (menus, combo/auto-complete/date-picker dropdowns) float above siblings at true z-order
   instead of overprinting them (FR-005), building on the existing `Overlay` container (`Control.fsi:506`).
4. **Composite-control structure fixes.** data-grid rows/headers lay out as `Row`; menu/combo rows get a
   minimum height; descriptions/QR respect their box; charts clip and handle degenerate data (FR-006/7/8).
5. **Layout bounds, clipping, and scroll.** Container painting clips children to bounds; flex honours explicit
   sizes/weights so chrome regions stop splitting space uniformly; `ScrollViewer` becomes a real clipping
   viewport (FR-003/4/9/10). The showcase **Shell** assigns explicit region sizes and a fixed nav-rail width
   (the only sample-level remediation).

This is a **Tier 1** change: it intentionally alters shared-control and renderer output, so the existing G1
Controls Gallery and G2 Sample Apps golden evidence, the rendered-output drift gate, and any affected
surface-area baselines are **re-established and disclosed** (FR-012/SC-007), and the corrected showcase is
re-verified by re-capturing all 19 pages of feature-135 screenshot evidence (FR-013/SC-005).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`, `Nullable=enable`, `FS0078`-as-error).
Framework changes land in `src/`; verification runs through the `samples/AntShowcase/` consumer.

**Primary Dependencies**: SkiaSharp over OpenGL (existing). **New embedded assets** (not NuGet
dependencies): the bundled font files, declared as `<EmbeddedResource>` in `FS.GG.UI.SkiaViewer`. No new
runtime package dependency is introduced.

**Storage**: Filesystem only — re-captured per-page evidence under `artifacts/ant-showcase/<seed>/<page-id>/`
(gitignored, feature-135 harness); re-baselined golden/drift evidence under the existing baseline locations
(`readiness/`, sample golden trees) with a disclosure ledger committed under this spec.

**Testing**: Expecto via `dotnet test`. Framework-level semantic tests in the relevant `src/` test projects
(`tests/`) for the renderer (glyph coverage, measurement/advance agreement, fallback disclosure), the overlay
pass (render order / z-top / hit-test), and the composite controls and layout. Sample-level verification
re-uses the feature-135 19-page evidence suite (`samples/AntShowcase/AntShowcase.Tests`). Real GL screenshot
evidence where available; disclosed degrade on no-GL hosts (never a fabricated pass).

**Target Platform**: Linux (X11/GL) for live/interactive and GL screenshot capture; headless deterministic
paths (state replay, coverage, measurement, fallback) run anywhere including no-GL CI.

**Project Type**: Framework libraries (`src/`) verified by a consumer desktop sample (`samples/AntShowcase/`).

**Performance Goals**: Not a hot-path feature, but text now renders through real typefaces — typeface lookups
**MUST** be cached (load once per family, reuse `SKTypeface`/`SKFont`) so per-frame rendering does not
regress. The quantitative gate is unchanged from feature 135: **byte-identical evidence across two same-seed
headless runs** — now *strengthened* because bundled fonts make text host-independent.

**Constraints**:

- **Determinism is paramount**: text output **MUST NOT** depend on host system fonts. All text resolves
  through bundled assets; `SKTypeface.Default` is no longer the primary path. This is what makes the
  same-seed evidence byte-identical across hosts (FR-013/SC-005, feature-135 SC-004).
- **One semantic control set, no per-theme forks** — fixes are theme-invariant; every defect and its fix
  hold identically under antLight and antDark (Edge Cases / SC-001..005 both themes).
- **`.fsi` discipline (Principle II)**: every new public surface (font-registry API, overlay-pass entry,
  measurement seam) is declared in the module `.fsi`; no access modifiers in `.fs`.
- **Disclosure (FR-001)**: any character not rendered as authored (deliberate substitute or tofu) is
  disclosed at the use site and surfaced in the evidence record; no silent wrong glyph.
- **Re-baseline discipline (FR-012)**: framework output changes are deliberate; G1/G2 golden evidence,
  rendered-output drift gate, and affected surface baselines are re-established with a committed disclosure.

**Scale/Scope**: ~4 renderer/scene files, ~6 control geometry fixes, ~3 layout/scroll fixes, 1 overlay pass,
1 font registry + ~6 embedded font faces, 1 sample Shell pass, plus baseline re-establishment and a 19-page
re-capture. **Large** (deep renderer + control + layout surface; Tier 1).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change Classification**: **Tier 1 (contracted change).** This feature changes observable rendered output of
shared controls and the renderer, adds new public surface (font registry, overlay-pass entry point,
measurement seam), and adds embedded font assets. Tier 1 requires the full chain: spec, plan, `.fsi` updates,
surface-area baseline updates, test evidence, re-baselined golden/drift evidence with disclosure, and docs.

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Spec → FSI → Semantic Tests → Implementation** | New public surface sketched in `.fsi` + validated by use before `.fs` bodies | **PASS (planned).** The font-registry, overlay-pass, and measurement-seam surfaces are drafted in their `.fsi` and exercised through FSI/Expecto (load packed lib, resolve a family, render a string, assert glyph + advance + disclosure) before the `.fs` bodies are written. |
| **II. Visibility lives in `.fsi`, not `.fs`** | Public modules have curated `.fsi`; no access modifiers in `.fs` | **PASS (planned).** Each touched public module's `.fsi` is updated for the new surface; bitmap-fallback internals stay absent from `.fsi` (private by omission). No `private`/`internal`/`public` on top-level bindings. |
| **III. Idiomatic simplicity** | Plainest F#; exotic features justified | **PASS.** Font registry is a `Map`/record over loaded typefaces; fallback chain is a list fold; overlay pass is a second ordered traversal; composite fixes are direction/min-size arithmetic. No SRTP, reflection, type providers, or custom operators. `mutable` only for the typeface cache (hot path, disclosed at use site). |
| **IV. Elmish/MVU is the boundary for stateful/I-O work** | Stateful/I-O modeled as `Model`/`Msg`/pure `update` + edge interpreter | **PASS.** Rendering is pure scene→pixels; font loading is I/O performed once at the SkiaViewer edge (interpreter), cached, and surfaced as data. No new stateful workflow; the showcase's existing MVU is unchanged. |
| **V. Test evidence is mandatory** | Tests fail before / pass after; real evidence preferred; synthetic disclosed | **PASS.** Each defect class gets a behavior gate that fails on today's renderer and passes after (glyph correctness, measure/advance agreement, no-overprint, table structure, region non-overlap, clipped scroll). Real GL screenshots re-captured; no-GL hosts record a disclosed degrade. Re-baselines are disclosed, not silently overwritten. |
| **VI. Observability and safe failure** | Structured diagnostics; fail-fast or degrade; GL smoke distinguishes defect vs missing window-system | **PASS.** Font-load failures and per-character fallback/tofu events emit structured diagnostics and appear in the evidence record (FR-001 disclosure); a missing bundled asset fails loudly; GL/display absence degrades cleanly with a disclosed reason. |

**Engineering Constraints check**: `net10.0` ✓ · SkiaSharp-over-GL, no Vulkan ✓ · no new NuGet runtime
dependency (fonts are embedded assets; their licenses recorded in PROVENANCE) ✓ · one semantic control set,
no per-theme forks ✓ · every touched public `.fs` module keeps a curated `.fsi` ✓ · surface-area baselines
updated for the new surface ✓ · public-API change documents compatibility/migration ✓.

**Result: PASS — no unjustified violations.** The two larger items (bundled-font system; real overlay pass)
are deliberate, spec-mandated framework fixes recorded in Complexity Tracking, not violations.

## Project Structure

### Documentation (this feature)

```text
specs/136-showcase-render-fixes/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — decisions R1–R8
├── data-model.md        # Phase 1 — font registry, fallback resolution, overlay node, defect↔layer matrix, rebaseline ledger
├── quickstart.md        # Phase 1 — build/pack/run/re-capture/verify guide
├── contracts/           # Phase 1
│   ├── font-registry.md       #   standard font set, family names, fallback chain, measurement + disclosure contract
│   ├── overlay-pass.md        #   render-order / z-top / hit-test contract for transient surfaces
│   ├── composite-controls.md  #   per-control structural contracts (data-grid, menu, dropdowns, descriptions, charts, qr-code)
│   ├── layout-bounds.md       #   region non-overlap, container clipping, scroll/viewport contract
│   ├── rebaseline-ledger.md   #   which baselines/golden change + disclosure (FR-012/SC-007)
│   └── verification.md         #   19-page re-capture + defect-absence checks mapped to SC-001..007
├── checklists/
│   └── requirements.md  # (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

Framework changes are concentrated in the renderer, scene, controls, and layout, plus one sample-level pass.

```text
src/
├── SkiaViewer/
│   ├── Fonts.fs / Fonts.fsi            # NEW — font registry: embedded-asset load, family→typeface, per-char fallback chain, typeface cache
│   ├── SceneRenderer.fs                # drawTextWithFallback rewritten to use the registry; vector path demoted to disclosed tofu; advance from real metrics
│   ├── SkiaViewer.fsi / SkiaViewer.fs  # surface the measurement seam + any fallback-disclosure exposure
│   └── assets/fonts/*.ttf              # NEW — embedded: NotoSans-Regular/Bold, NotoSansMono, Inter-Regular/Bold, JetBrainsMono, DejaVuSans/Mono
├── Scene/
│   └── Scene.fs / Scene.fsi            # measureText reconciled to real advances via injected measurer seam (default heuristic retained for pure callers)
├── Controls/
│   ├── Control.fs / Control.fsi        # directionOf (data-grid rows=Row); rowsGeom min-row-height; descriptionsGeom/qrCodeGeom bounds; chart clip+degenerate; renderTree overlay pass; paintNode container clipping
│   └── Widgets/ (Navigation, Collections, Overlay, …)  # any per-widget geometry touched by the above
├── Layout/
│   └── Layout.fs / Layout.fsi          # flex honours explicit basis/weight (stop uniform split); real ScrollViewer viewport metrics
└── (surface-area baselines per touched public module — updated)

samples/AntShowcase/AntShowcase.Core/
└── Shell.fs                            # SAMPLE fix: explicit region sizes (app bar/feedback/status), fixed nav-rail width, content flex-grow + scroll

tests/                                  # framework semantic tests (renderer/overlay/composite/layout)
samples/AntShowcase/AntShowcase.Tests/  # 19-page evidence re-verification (feature-135 suite, re-baselined)
readiness/ + sample golden trees        # re-established baselines + committed disclosure ledger
```

**Structure Decision**: Fix at the layer that owns each defect (FR-011). The renderer (`SkiaViewer`) and
scene (`Scene`) own the text defects → bundled-font registry + measurement reconciliation. The controls
(`Control.fs`) own composite-structure, item-overprint, and the overlay pass. The layout engine (`Layout.fs`)
owns clipping, flex distribution, and scroll. Only the chrome-region sizing — pure composition — is fixed in
the sample `Shell.fs`. This keeps every framework-caused defect fixed once, for all consumers, with the
sample touched only where the cause is genuinely compositional.

## Phased delivery (internal sequencing within this one feature)

- **P-A — Probe + bundled-font foundation (US1 text, MVP slice)**: build a standalone probe to confirm
  (a) whether `SKTypeface.Default` has glyphs in the headless sandbox today and (b) that embedded
  `SKTypeface.FromStream` loading works (probe-driven, per repo practice). Add `Fonts.fs` registry + the
  embedded font set; rewrite `drawTextWithFallback` to resolve real typefaces with the per-character fallback
  chain; demote the 5×7 path to disclosed tofu. *Correct `@`, mixed case, no wrong glyphs.*
- **P-B — Measurement reconciliation (US1 truncation)**: wire the measurement seam so `Scene.measureText`
  agrees with the bundled font's advances; box sizing no longer clips. `Stable`/`Upload`/`Refresh`/numeric
  labels render in full. Glyph + measurement framework tests green.
- **P-C — Composite-control structure (US3)**: data-grid `Row` direction; menu/combo min-row-height;
  descriptions/QR bounds; chart clip + degenerate-data guards. Per-control framework tests green.
- **P-D — Overlay pass (US2 overlays)**: deferred z-top overlay layer in `renderTree`; dropdowns/menus float
  above neighbours; hit-test respects z-order. Overlay-pass tests green.
- **P-E — Layout bounds, clipping, scroll + Shell (US2 regions, US4)**: container clipping in `paintNode`;
  flex honours explicit basis; real `ScrollViewer` viewport; sample `Shell.fs` region sizing + fixed nav
  width + content scroll. Region-non-overlap and bounded/scroll tests green.
- **P-F — Re-baseline + full re-verification (FR-012/FR-013)**: re-establish G1/G2 golden evidence, the
  rendered-output drift gate, and affected surface baselines with the committed disclosure ledger; re-capture
  all 19 showcase pages (both themes) and confirm zero instances of all seven defect classes (SC-001..007).

## Complexity Tracking

> No Constitution Check violations. The two non-trivial additions below are spec-mandated framework fixes,
> justified here.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Bundled standard font set + registry (embedded assets, per-character fallback chain)** | The wrong-glyph + all-caps + truncation defects all stem from depending on the host's `SKTypeface.Default` and a coverage-poor 5×7 bitmap. FR-001 requires authored characters to render correctly and determinism (SC-005) requires host-independent text. | Patching only the 5×7 vector path keeps blocky all-caps text (fails "render as authored" / credibility) and using `SKTypeface.Default` keeps output host-dependent (breaks byte-identical evidence). Bundling fixes coverage *and* determinism at the root for every consumer. |
| **Real deferred overlay pass in `renderTree`** | The renderer has a single in-flow pass with no z-layer, so open dropdowns/menus overprint neighbours (FR-005). A correct, reusable fix needs paint-last-above-flow ordering. | Reserving in-flow space per dropdown in the sample is a per-sample workaround that does not fix the framework defect for other consumers and cannot represent true floating surfaces; it fails FR-011's "framework if possible". |

New embedded font assets add ~1.5–2 MB to `FS.GG.UI.SkiaViewer`; licenses (OFL/free) are recorded in
PROVENANCE. No new NuGet runtime dependency; no new project added to `FS.GG.Rendering.slnx`.
