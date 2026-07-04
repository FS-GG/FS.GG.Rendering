# Implementation Plan: Persistence (save/load) effect surface + fs-gg-persistence product skill

**Branch**: `244-persistence-effect-surface` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/244-persistence-effect-surface/spec.md`

## Summary

Close the game-default persistence gap (FS-GG/FS.GG.Rendering#93) with the smallest honest slice: a
pure `PersistenceEffect` request surface (`Save` / `Load` / `DeleteSlot`) added as a `Persistence`
module to the existing dependency-light `FS.GG.UI.Canvas` package, carrying a versioned
`SaveEnvelope` whose payload is **opaque to the framework** (the product serializes its own `Model`),
plus a **pure record-only interpreter** that folds requested effects into ordered
`PersistenceEvidence` for the headless path. A profile-gated `fs-gg-persistence` product skill
(mirroring `fs-gg-game-core` / `fs-gg-audio`) teaches the request -> host-interpret pattern and the
versioned-envelope recipe. The real file-backed backend in the SkiaViewer host — including the
load-*result* Msg it dispatches back to the model — is an explicit deferral behind the seam, **not**
a shipped stub. This is the direct analog of Feature 243 (audio); it reuses that feature's placement,
skill-wiring, and evidence precedents wholesale.

> **Live-verification note (adapted).** The plan template's "confirm root-cause hypotheses with an
> early live smoke run" clause targets bug-fix features; this is greenfield, so there are no
> root-cause hypotheses to confirm. The equivalent honest-observation obligation here is the
> **template instantiation check** (quickstart §2): actually run `dotnet new fs-gg-ui --profile
> game` and `--profile app` and observe the skill lands (game) / is absent (app) — deterministic
> manifest tests can be green while the real scaffold still misplaces or leaks the skill (the lesson
> of Features 175, 228, and 243). `/speckit-tasks` MUST schedule that real instantiation, not just
> the manifest unit tests.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: **None new.** The surface lives in `FS.GG.UI.Canvas`, which references only
`Scene` and has zero PackageReferences. No filesystem/serialization library is added (deferred with
the real backend).

**Storage**: N/A for the minimal slice. `PersistenceEvidence` is a pure in-memory value; optional
`readiness/*.json` persistence reuses the existing run-evidence convention and is deferred. The
payload is opaque product-serialized data — the framework never touches storage.

**Testing**: Semantic tests through the packed/built library (FSI-style), in `tests/Canvas.Tests/`;
skill/manifest/surface coherence via existing `tests/Package.Tests/` (`Feature231SkillManifestTests`,
`SurfaceAreaTests`) and the `SkillParity` harness; real template instantiation per quickstart §2.

**Target Platform**: Cross-platform .NET; **headless-safe** — no filesystem/writable save location
required anywhere in this feature.

**Project Type**: Library capability (F# module) + template/skill wiring. Single-project surface
change (Canvas) plus template config + skill docs.

**Performance Goals**: N/A — pure value construction; the record-only interpreter is an O(n) fold
over a request batch.

**Constraints**: No filesystem dependency ships; interpreter never blocks or throws (Principle VI);
payload stays opaque (framework never parses it); non-persistence profiles byte-unchanged w.r.t.
persistence (FR-014).

**Scale/Scope**: ~1 new module (`Persistence.fsi`/`.fs`, 3 DU cases + envelope record + a handful of
pure functions), 1 skill + wrapper pair, 1 manifest row, 1 template copy block, 1 surface-baseline
update, a small semantic-test set. No new package.

**Change tier**: **Tier 1 (contracted change)** — adds public API to `FS.GG.UI.Canvas`. Requires
`.fsi` first, surface-baseline update, test evidence, and doc updates.

## Constitution Check

*GATE: must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | How this plan satisfies it |
|-----------|--------|----------------------------|
| I. Spec -> FSI -> Semantic Tests -> Impl | OK | `contracts/Persistence.fsi` is the FSI-first sketch; exercise in FSI (quickstart §1); semantic tests before `Persistence.fs`. |
| II. Visibility lives in `.fsi` | OK | `src/Canvas/Persistence.fsi` is the sole public surface; no access modifiers in `.fs`; `readiness/surface-baselines/FS.GG.UI.Canvas.txt` regenerated + gated by `SurfaceAreaTests`. |
| III. Idiomatic simplicity | OK | A plain DU + a record + pure total functions. No custom operators, SRTP, reflection, providers, or non-trivial CEs. No justification block needed. |
| IV. Elmish/MVU boundary for stateful/IO | OK | `PersistenceEffect` **is** the `Effect`/`Cmd<Msg>` data the consuming product's `update` emits; the record-only interpreter is the edge. Persistence is a stateless request vocabulary — it plugs into the product's MVU. The load-*result* Msg is a deferred host concern, not part of this surface. |
| V. Test evidence mandatory | OK | Semantic tests fail-before/pass-after; evidence is **real** (records actual requested values incl. opaque payload), so **no `Synthetic` disclosure** is needed. The deferred real backend is disclosed as a deferral, not faked. |
| VI. Observability & safe failure | OK | Interpreter clamps out-of-range version, treats unknown-slot `Load`/`DeleteSlot` as recorded no-op-class requests, carries payload verbatim; never throws, never blocks, never touches a filesystem in headless. |

**Gate result: PASS. No violations -> Complexity Tracking is empty.**

FR-013 decision recorded: **skill-only, no `template/capabilities.yml` catalog row** — mirrors
`fs-gg-game-core` / `fs-gg-audio` / Canvas. Rationale in `research.md` R1.

## Project Structure

### Documentation (this feature)

```text
specs/244-persistence-effect-surface/
├── plan.md              # This file
├── spec.md              # Feature spec
├── research.md          # Phase 0 — decisions R1-R6
├── data-model.md        # Phase 1 — entities
├── quickstart.md        # Phase 1 — validation guide
├── contracts/
│   └── Persistence.fsi  # Phase 1 — FSI-first surface contract
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source code / repo touch-points (concrete)

```text
src/Canvas/
├── Persistence.fsi                # NEW — public persistence request surface (contract -> shipped)
├── Persistence.fs                 # NEW — DU + record + pure smart ctors + record-only interpreter
└── Canvas.Lib.fsproj              # EDIT — add Persistence.fsi/.fs to compile order (after Audio.fs)

readiness/surface-baselines/
└── FS.GG.UI.Canvas.txt            # EDIT (regenerated) — + Persistence module/types

template/base/
├── src/Product/Product.fsproj     # EDIT (comment only) — note persistence ships via existing Canvas gate
└── docs/api-surface/Canvas/
    └── Persistence.fsi            # NEW (optional doc copy) — alongside Loop.fsi/Rng.fsi/Audio.fsi

template/product-skills/fs-gg-persistence/
└── SKILL.md                       # NEW — canonical skill body (mirrors fs-gg-audio)

.agents/skills/fs-gg-product-persistence/SKILL.md   # NEW — Codex-active wrapper
.claude/skills/fs-gg-product-persistence/SKILL.md   # NEW — Claude-active wrapper

template/skill-manifest/skill-manifest.json         # EDIT (regenerated) — + fs-gg-persistence row (sha256)
.template.config/template.json                      # EDIT — + fs-gg-persistence copy block (game/sample-pack)

tests/Canvas.Tests/                                 # NEW tests — pure surface + record-only interpreter
tests/Package.Tests/                                # (existing gates cover manifest/parity/surface)
```

**Structure Decision**: Fold the persistence surface into `FS.GG.UI.Canvas` (module `Persistence`),
reusing the existing `profile in [game, sample-pack]` Canvas package gate — no new package, no
capability catalog row (skill-only, mirroring game-core/audio). The record-only interpreter is pure
and co-located with the DU; the SkiaViewer real file backend (and its load-result Msg) is a
documented deferral. Full rationale: `research.md` R1-R2.

## Deferred (explicit, bounded follow-ups)

- **Real file-backed backend** in the SkiaViewer host (actual save-file read/write, and the
  load-*result* `Msg` it dispatches back to the model). When built, it consumes the same
  `PersistenceEffect` values and brings its own dependency edge — the pure surface
  (`Persistence.fsi`) does not change (FR-007). Tracked as a follow-up to #93.
- **`readiness/persistence-evidence.json` persistence** — optional; the pure `PersistenceEvidence`
  value is the primary evidence for the minimal slice.
- **Richer persistence vocabulary** (enumerate slots, save metadata/thumbnails, migration helpers) —
  additive DU extension later.

## Phase 2 handoff

`/speckit-tasks` will generate a dependency-ordered `tasks.md`. Ordering must honor Principle I:
(1) `Persistence.fsi` + FSI sketch, (2) semantic tests (red), (3) `Persistence.fs` (green), (4)
surface baseline regen, (5) skill body + wrappers + template copy block + manifest regen, (6) **real
template instantiation** check (game vs app) per the live-verification note, (7) doc copy + skill
currency/de-leak. Skill wiring (5-7) depends on the surface existing (2-3) so the skill cites real
API.
