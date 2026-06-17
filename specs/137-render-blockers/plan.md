# Implementation Plan: Render Blockers — Clipping, Overlay & Scroll

**Branch**: `137-render-blockers` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/137-render-blockers/spec.md`

## Summary

Feature 136 deferred three rendering defect classes (control/region overlap + spill, overlay overprint,
unbounded/non-scrolling content) because the shared enabler — clipping a container's **children** to the
container's bounds — broke the retained renderer's picture-cache parity (3 `Audit_PictureCache` tests) and was
reverted. **Root cause (confirmed by code reading, not theory):** `RetainedRender.step` has a scene-emit walk,
`assemble` (`RetainedRender.fs:1252-1271`), that rebuilds the painted scene as flat `own @ children` whenever
there are replay hits or an active clock — a **fifth composition site** that the 136 attempt did not route
through the shared clip rule. So with the cache **on** (rows hit → `assemble` runs) the scene was unclipped,
while with the cache **off** (fast path, `sceneList = SubtreeScene` at `:1250`) it carried the clip → `flat off
≠ flat on`. The cache fingerprints themselves are unaffected (a `data-grid-row` is a leaf; clipping only wraps
a container's *children*).

The fix is therefore mechanical and low-risk, not a cache-internals rewrite:

1. **Container clipping (US1, the blocker).** Re-introduce the shared `ControlInternals.composeContainerScene`
   (own paint + children clipped to the node's box) and route **all** composition sites through it: the full
   `Control.renderTree` paint, the four retained build/carry sites, **and** the `assemble` emit walk. With one
   shared rule used everywhere, full ≡ retained and `cache-on ≡ cache-off` hold by construction; the
   `Audit_PictureCache` parity/present-but-dead/effectiveness trio is the oracle.
2. **Overlay pass (US2).** A deferred z-top overlay group: in-flow nodes paint first, overlay/transient nodes
   paint last at true coordinates, outside ancestor clips. Reproduced identically in `renderTree` and the
   retained `assemble`/`SubtreeScene` so parity holds; `nearestAuthored`/`hitTest` consult the overlay group
   first. Empty overlay group ⇒ byte-identical to the pre-overlay pass.
3. **ScrollViewer viewport (US3).** `ScrollViewer` becomes a real clipping viewport (clip content to box +
   scroll offset + affordance), built on the container-clip model; the sample `Shell.fs` keeps only
   compositional region sizing.
4. **Re-baseline + re-verify (US4).** Re-establish the affected golden/drift/surface baselines as disclosed
   intended changes and re-capture the 19 showcase pages (both themes); a disclosed no-GL degrade where no
   display exists.

This is a **Tier 1** change: it intentionally alters shared-control/renderer output and adds the overlay-pass
public entry, so baselines are re-established and disclosed (continuing 136's FR-012/FR-013).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`, `Nullable=enable`, `FS0078`-as-error). All
changes land in `src/`; verification runs headless plus (where a display exists) the `samples/AntShowcase/`
consumer.

**Primary Dependencies**: SkiaSharp over OpenGL (existing). No new runtime dependency. Builds directly on
feature 136's bundled-font text path and the existing `RetainedRender`/picture-cache (features 091/116/120)
and `Overlay` container (`Control.fsi`).

**Storage**: Filesystem only — re-captured per-page evidence under `artifacts/ant-showcase/<seed>/<page-id>/`
(gitignored, feature-135 harness); re-baselined golden/drift evidence with a disclosure ledger under this spec.

**Testing**: Expecto via `dotnet test`. The decisive gate is the existing `Audit_PictureCache` parity trio
plus new semantic tests (container-bounds non-overflow, full ≡ retained parity with clipping, overlay z-order
+ hit-test, ScrollViewer viewport). Real GL screenshots where available; disclosed degrade on no-GL hosts.

**Target Platform**: Linux (X11/GL) for live/interactive + GL screenshot capture; all the new behaviour
(clipping, overlay ordering, hit-test, scroll geometry, cache parity) is headless-deterministic and runs
anywhere including no-GL CI.

**Project Type**: Framework libraries (`src/`) verified by a consumer desktop sample (`samples/AntShowcase/`).

**Performance Goals**: Not a hot-path feature. The clip composition is one extra `ClipNode` per non-empty
container; the picture cache MUST keep its hit rate (the `Audit_PictureCache` effectiveness margin is the
gate). The quantitative gate is unchanged from 135/136: byte-identical evidence across two same-seed headless
runs.

**Constraints**:

- **Single composition rule.** Container clipping MUST be expressed once (`composeContainerScene`) and used by
  every paint-assembly site so full ≡ retained and cache-on ≡ cache-off cannot drift. The fifth site
  (`assemble`) is the specific 136 miss.
- **Cache parity is the hard gate.** The feature is not done until the three `Audit_PictureCache` tests
  (`cache-on ≡ cache-off`, present-but-dead, effectiveness) are green WITH clipping enabled.
- **Overlays escape ancestor clips.** An overlay node painted in the deferred group MUST NOT be clipped by its
  in-flow ancestors (it floats above the flow at true coordinates).
- **Parity-preserving overlays.** The overlay group MUST be reproduced identically in the retained path; an
  empty overlay group MUST be byte-identical to the pre-overlay render.
- **`.fsi` discipline (Principle II)**: the overlay-pass public entry and any scroll-viewport metrics are
  declared in the module `.fsi`; no access modifiers in `.fs`. `composeContainerScene`/clip helpers stay in
  `module internal ControlInternals`.
- **Theme-invariance + determinism**: every fix holds identically under antLight/antDark and preserves the
  byte-identical same-seed evidence.

**Scale/Scope**: ~1 shared composition helper reused at 6 sites (`renderTree` + 4 retained build/carry +
`assemble`), 1 overlay pass (render + hit-test, mirrored in retained), 1 `ScrollViewer` viewport, baseline
re-establishment + 19-page re-capture. **Medium-large** (touches the retained-render parity machinery, but via
a single shared rule rather than cache internals).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change Classification**: **Tier 1 (contracted change).** Changes observable rendered output of shared
controls/renderer and adds new public surface (overlay-pass entry; possibly scroll-viewport metrics). Requires
the full chain: spec, plan, `.fsi` updates, surface-area baseline updates, test evidence, re-baselined
golden/drift evidence with disclosure, and docs.

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Spec → FSI → Semantic Tests → Implementation** | New public surface sketched in `.fsi` + validated by use before `.fs` bodies | **PASS (planned).** The overlay-pass entry (and any scroll metric) is drafted in `.fsi`, then the semantic tests (z-order/hit-test, container-bounds, viewport, cache parity) are written against it and MUST fail/were-failing before the bodies land. |
| **II. Visibility lives in `.fsi`, not `.fs`** | Public modules have curated `.fsi`; no access modifiers in `.fs` | **PASS (planned).** `composeContainerScene` and clip/overlay helpers live in `module internal ControlInternals`; only the overlay-pass public entry joins `Control`'s public `.fsi`. Surface baseline updated (Tier 1). |
| **III. Idiomatic simplicity** | Plainest F#; exotic features justified | **PASS.** The fix is a single shared composition function reused at every site, a second ordered traversal for overlays, and a clip+offset for the viewport — no SRTP/reflection/type providers. |
| **IV. Elmish/MVU is the boundary for stateful/I-O work** | Stateful/I-O modeled as Model/Msg/pure update + edge interpreter | **PASS.** Rendering stays pure scene→pixels; the retained cache is existing interpreter-edge state, unchanged in shape. No new stateful workflow. |
| **V. Test evidence is mandatory** | Tests fail before / pass after; real evidence preferred; synthetic disclosed | **PASS.** The 3 `Audit_PictureCache` tests already fail with the naive clip (documented in 136) and MUST pass after; new behaviour gates (bounds non-overflow, overlay z-order, viewport) fail on today's renderer and pass after. Re-captures are real GL or a disclosed no-GL degrade — never fabricated. |
| **VI. Observability and safe failure** | Structured diagnostics; fail-fast or degrade; GL smoke distinguishes defect vs missing window-system | **PASS.** Cache parity divergence surfaces through the existing `WorkReduction`/audit counters; GL/display absence degrades cleanly with a disclosed reason. |

**Engineering Constraints check**: `net10.0` ✓ · SkiaSharp-over-GL, no Vulkan ✓ · no new NuGet runtime
dependency ✓ · one semantic control set, no per-theme forks ✓ · every touched public `.fs` keeps a curated
`.fsi` ✓ · surface-area baselines updated ✓ · public-API change documents compatibility/migration ✓.

**Result: PASS — no unjustified violations.** The overlay-pass entry is a deliberate, spec-mandated framework
addition (continuing 136's R4 decision), recorded in Complexity Tracking, not a violation.

## Project Structure

### Documentation (this feature)

```text
specs/137-render-blockers/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions R1–R4 (root cause + approaches)
├── data-model.md        # Phase 1 — composition rule, overlay group, clip-aware emit, viewport
├── quickstart.md        # Phase 1 — build/test/verify guide (cache-parity-first)
├── contracts/
│   ├── container-clipping.md   # the shared composition rule + cache-parity contract
│   ├── overlay-pass.md         # z-top render order + hit-test, parity-preserving (extends 136)
│   ├── scroll-viewport.md      # ScrollViewer clip + offset + affordance contract
│   └── rebaseline-ledger.md    # which baselines change + disclosure (FR-010)
├── checklists/
│   └── requirements.md  # (created by /speckit-specify)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Controls/
│   ├── Control.fs / Control.fsi   # composeContainerScene (ControlInternals); renderTree paint routes
│   │                              #   through it; deferred overlay group in renderTree; nearestAuthored/
│   │                              #   hitTest consult overlays first; ScrollViewer viewport geometry;
│   │                              #   overlay-pass public entry in the public .fsi
│   └── RetainedRender.fs          # route the 4 build/carry sites AND the `assemble` emit walk (:1269)
│                                  #   through composeContainerScene; mirror the overlay group in assemble
├── Layout/
│   └── Layout.fs / Layout.fsi     # ScrollViewer viewport metrics (only if a surfaced metric is needed)
└── (surface-area baselines per touched public module — updated)

samples/AntShowcase/AntShowcase.Core/
└── Shell.fs                       # SAMPLE: compositional region sizing only (if still needed after clipping)

tests/
├── Controls.Tests/                # Audit_PictureCache (the cache-parity gate) + new clipping/overlay/scroll suites
└── Layout.Tests/                  # viewport metrics if surfaced
samples/AntShowcase/AntShowcase.Tests/  # 19-page re-verification (feature-135 suite, re-baselined)
```

**Structure Decision**: Fix at the layer that owns each defect. The renderer/control composition
(`Control.fs` + `RetainedRender.fs`) owns clipping and the overlay pass; the layout/control owns the scroll
viewport; only compositional chrome sizing is sample-level. The whole feature hinges on one shared
`composeContainerScene` used at every assembly site — the discipline that keeps full ≡ retained and
cache-on ≡ cache-off true by construction.

## Phased delivery (internal sequencing within this one feature)

- **P-A — Container clipping with cache parity (US1, the blocker, MVP).** Re-introduce
  `composeContainerScene`; route `renderTree` paint, the 4 retained build/carry sites, AND the `assemble`
  emit walk through it. Confirm the 3 `Audit_PictureCache` tests pass and add a container-bounds non-overflow
  + full ≡ retained parity test. *This alone unblocks control/region-overlap and spill.*
- **P-B — Overlay pass (US2).** Deferred z-top overlay group in `renderTree` and mirrored in retained
  `assemble`/`SubtreeScene`; overlays escape ancestor clips; `nearestAuthored`/`hitTest` consult overlays
  first; empty group ⇒ byte-identical. Overlay z-order + hit-test tests green; parity preserved.
- **P-C — ScrollViewer viewport (US3).** Real clipping viewport (clip + offset + affordance) on the
  container-clip model; viewport test green; bounded-page test green.
- **P-D — Re-baseline + full re-verification (US4, FR-010/FR-011).** Re-establish G1/G2 golden, drift gate,
  and affected surface baselines with a disclosure ledger; re-capture all 19 pages (both themes) and confirm
  zero instances of the seven defect classes — or a disclosed no-GL degrade.

## Complexity Tracking

> No Constitution Check violations. The one non-trivial addition is justified here.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Real deferred overlay pass + clip-consistent retained emit** | The overprint defect (FR-004) needs paint-last-above-flow ordering; container clipping (FR-001) must hold across full + retained + the `assemble` emit walk or `cache-on ≡ cache-off` breaks (the 136 regression). A single shared composition rule used at every site is the minimal correct fix. | Clipping only in `renderTree` (not the retained `assemble`) is exactly what broke parity in 136. A per-sample overlay workaround does not fix the framework for other consumers (rejected in 136 R4). |

New public surface: the overlay-pass entry on `Control` (+ any scroll-viewport metric). No new project, no new
NuGet dependency. Builds entirely on existing features 091/116/120 machinery and the 136 font/text path.
