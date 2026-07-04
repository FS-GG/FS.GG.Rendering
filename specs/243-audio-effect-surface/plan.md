# Implementation Plan: Audio effect surface + fs-gg-audio product skill

**Branch**: `243-audio-effect-surface` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/243-audio-effect-surface/spec.md`

## Summary

Close the game-default audio gap (FS-GG/FS.GG.Rendering#92) with the smallest honest slice: a
pure `AudioEffect` request surface (`PlaySfx` / `PlayMusic` / `StopMusic` / `SetMasterVolume`)
added as an `Audio` module to the existing dependency-light `FS.GG.UI.Canvas` package, plus a
**pure record-only interpreter** that folds requested effects into ordered `AudioEvidence` for the
headless path. A profile-gated `fs-gg-audio` product skill (mirroring `fs-gg-game-core`) teaches
the request → host-interpret pattern. The real audio-*output* backend in the SkiaViewer host is an
explicit deferral behind the seam — **not** a shipped stub.

> **Live-verification note (adapted).** The plan template's "confirm root-cause hypotheses with an
> early live smoke run" clause targets bug-fix features; this is greenfield, so there are no
> root-cause hypotheses to confirm. The equivalent honest-observation obligation here is the
> **template instantiation check** (quickstart §2): actually run `dotnet new fs-gg-ui --profile
> game` and `--profile app` and observe the skill lands (game) / is absent (app) — deterministic
> manifest tests can be green while the real scaffold still misplaces or leaks the skill (the
> lesson of Features 175 and 228). `/speckit-tasks` MUST schedule that real instantiation, not just
> the manifest unit tests.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: **None new.** The surface lives in `FS.GG.UI.Canvas`, which references
only `Scene` and has zero PackageReferences. No audio library is added (deferred with the real
backend).

**Storage**: N/A for the minimal slice. `AudioEvidence` is a pure in-memory value; optional
`readiness/*.json` persistence reuses the existing run-evidence convention and is deferred.

**Testing**: Semantic tests through the packed/built library (FSI-style), in
`tests/Canvas.Tests/`; skill/manifest/surface coherence via existing `tests/Package.Tests/`
(`Feature231SkillManifestTests`, `SurfaceAreaTests`) and the `SkillParity` harness; real template
instantiation per quickstart §2.

**Target Platform**: Cross-platform .NET; **headless-safe** — no audio device required anywhere in
this feature.

**Project Type**: Library capability (F# module) + template/skill wiring. Single-project surface
change (Canvas) plus template config + skill docs.

**Performance Goals**: N/A — pure value construction; the record-only interpreter is an O(n) fold
over a request batch.

**Constraints**: No audio-device dependency ships; interpreter never blocks or throws (Principle
VI); non-audio profiles byte-unchanged w.r.t. audio (FR-013).

**Scale/Scope**: ~1 new module (`Audio.fsi`/`.fs`, ~4 DU cases + a handful of pure functions), 1
skill + wrapper pair, 1 manifest row, 1 template copy block, 1 surface-baseline update, a small
semantic-test set. No new package.

**Change tier**: **Tier 1 (contracted change)** — adds public API to `FS.GG.UI.Canvas`. Requires
`.fsi` first, surface-baseline update, test evidence, and doc updates.

## Constitution Check

*GATE: must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | How this plan satisfies it |
|-----------|--------|----------------------------|
| I. Spec → FSI → Semantic Tests → Impl | ✅ | `contracts/Audio.fsi` is the FSI-first sketch; exercise in FSI (quickstart §1); semantic tests before `Audio.fs`. |
| II. Visibility lives in `.fsi` | ✅ | `src/Canvas/Audio.fsi` is the sole public surface; no access modifiers in `.fs`; `readiness/surface-baselines/FS.GG.UI.Canvas.txt` regenerated + gated by `SurfaceAreaTests`. |
| III. Idiomatic simplicity | ✅ | A plain DU + pure total functions. No custom operators, SRTP, reflection, providers, or non-trivial CEs. No justification block needed. |
| IV. Elmish/MVU boundary for stateful/IO | ✅ | `AudioEffect` **is** the `Effect`/`Cmd<Msg>` data the consuming product's `update` emits; the record-only interpreter is the edge. Audio is a stateless request vocabulary — it plugs into the product's MVU rather than owning its own Model/Msg. |
| V. Test evidence mandatory | ✅ | Semantic tests fail-before/pass-after; evidence is **real** (records actual requested values), so **no `Synthetic` disclosure** is needed. The deferred real-output backend is disclosed as a deferral, not faked. |
| VI. Observability & safe failure | ✅ | Interpreter clamps out-of-range volume and treats `StopMusic`-when-idle as a no-op; never throws, never blocks, never touches a device in headless. |

**Gate result: PASS. No violations → Complexity Tracking is empty.**

FR-012 decision recorded: **skill-only, no `template/capabilities.yml` catalog row** — mirrors
`fs-gg-game-core`/Canvas. Rationale in `research.md` R1.

## Project Structure

### Documentation (this feature)

```text
specs/243-audio-effect-surface/
├── plan.md              # This file
├── spec.md              # Feature spec
├── research.md          # Phase 0 — decisions R1–R6
├── data-model.md        # Phase 1 — entities
├── quickstart.md        # Phase 1 — validation guide
├── contracts/
│   └── Audio.fsi        # Phase 1 — FSI-first surface contract
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source code / repo touch-points (concrete)

```text
src/Canvas/
├── Audio.fsi                    # NEW — public audio request surface (contract → shipped)
├── Audio.fs                     # NEW — DU + pure smart ctors + record-only interpreter
└── Canvas.Lib.fsproj            # EDIT — add Audio.fsi/Audio.fs to compile order

readiness/surface-baselines/
└── FS.GG.UI.Canvas.txt          # EDIT (regenerated) — + Audio module/types

template/base/
├── src/Product/Product.fsproj   # EDIT (comment only) — note audio ships via existing Canvas gate
└── docs/api-surface/Canvas/
    └── Audio.fsi                # NEW (optional doc copy) — alongside Loop.fsi/Rng.fsi

template/product-skills/fs-gg-audio/
└── SKILL.md                     # NEW — canonical skill body (mirrors fs-gg-game-core)

.agents/skills/fs-gg-product-audio/SKILL.md   # NEW — Codex-active wrapper
.claude/skills/fs-gg-product-audio/SKILL.md   # NEW — Claude-active wrapper

template/skill-manifest/skill-manifest.json   # EDIT (regenerated) — + fs-gg-audio row (sha256)
.template.config/template.json                # EDIT — + fs-gg-audio copy block (game/sample-pack)

tests/Canvas.Tests/                           # NEW tests — pure surface + record-only interpreter
tests/Package.Tests/                          # (existing gates cover manifest/parity/surface)
```

**Structure Decision**: Fold the audio surface into `FS.GG.UI.Canvas` (module `Audio`), reusing
the existing `profile in [game, sample-pack]` Canvas package gate — no new package, no capability
catalog row (skill-only, mirroring game-core). The record-only interpreter is pure and co-located
with the DU; the SkiaViewer real-output backend is a documented deferral. Full rationale:
`research.md` R1–R2.

## Deferred (explicit, bounded follow-ups)

- **Real audio-output backend** in the SkiaViewer host (device output, mixing, decoding, an audio
  library dependency). When built, it consumes the same `AudioEffect` values and brings its own
  dependency edge — the pure surface (`Audio.fsi`) does not change (FR-006). Tracked as a
  follow-up to #92.
- **`readiness/audio-evidence.json` persistence** — optional; the pure `AudioEvidence` value is
  the primary evidence for the minimal slice.
- **Richer audio vocabulary** (spatial/3D, ducking, DSP) — additive DU extension later.

## Phase 2 handoff

`/speckit-tasks` will generate a dependency-ordered `tasks.md`. Ordering must honor Principle I:
(1) `Audio.fsi` + FSI sketch, (2) semantic tests (red), (3) `Audio.fs` (green), (4) surface
baseline regen, (5) skill body + wrappers + template copy block + manifest regen, (6) **real
template instantiation** check (game vs app) per the live-verification note, (7) doc copy + skill
currency/de-leak. Skill wiring (5–7) depends on the surface existing (2–3) so the skill cites real
API.
