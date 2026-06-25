# Implementation Plan: Symbology Legibility Linter (M7 — linter thread)

**Branch**: `194-symbology-legibility-linter` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/194-symbology-legibility-linter/spec.md`

**Source design**: [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) — roadmap milestone **M7 (governance)**, first of its three independent threads. M1–M5 (spec 192) and M6 (spec 193, the live board) are complete; the **Badge/Ring alternative grammars** and **label text** (the other two M7 threads) stay backlog and out of scope here (FR-016).

## Summary

Ship a **pure legibility linter** that scores a produced symbol set — the `Token` channel values of a roster, optionally paired with each unit's `Motion` — against the fixed §4 channel-grammar capacities and returns a structured, deterministic **legibility report**: per-channel a distinct-levels-used-vs-capacity usage summary, a list of findings (each with a machine-readable channel identity, a severity, a human-readable message, and the offending unit indices), and an overall verdict (`Clean` / `HasWarnings`). The linter turns the subjective "is this board overloaded?" eyeball question into a reproducible, pinnable answer over the same channel vocabulary the renderer already uses, and is wired into the `fs-gg-symbology` design loop's CRITIQUE step as the mechanical complement to the human check.

Technical approach (grounded against the tree on 2026-06-25):

- A **new pure module `Legibility`** lands inside the existing pure package **`src/Symbology/`** (new `Legibility.fsi` + `Legibility.fs`, namespace `FS.GG.UI.Symbology`). It depends only on the channel/`Token` types already declared in `Symbology.fsi` (and, transitively, `FS.GG.UI.Scene` for `Color`/`PathSpec`) — **no rendering, no raster, no GL, no IO** (FR-012). It is *not* placed in `src/Symbology.Render/`, which is the only component allowed to pull raster/host machinery.
- The fixed **capacity table** (FR-002) is encoded as in-module data drawn from the §4 grammar the `fs-gg-symbology` skill already documents: per channel, its `ChannelKind` (categorical / ordered / continuous) and reliable level count. The linter and the renderer thus reason over one shared channel vocabulary.
- **Two pure entry points** (FR-001/FR-010): `Legibility.score : Token list -> Report` (static board; motion-load skipped) and `Legibility.scoreAnimated : (Motion * Token) list -> Report` (animated board; adds the whole-board motion-load check). Both are pure functions of their input — no wall-clock, randomness, or IO (SC-001).
- The checks: **categorical overload** (faction-hue incl. each distinct `Custom` colour, class-silhouette, identity-sigil, inspection-dash, boolean-mount, speed-beads) when distinct discrete levels exceed capacity (FR-003); **out-of-domain** values (normalised magnitudes outside `[0,1]`, speed beyond the legible bead maximum, non-finite floats) as errors (FR-004); **degenerate unit** (`R <= 0`, the grammar's placeholder case) as an error that is reported and *skipped over*, never thrown (FR-005); **continuous channels exempt** from overload (heading, health, and the magnitude-read stroke-width/interior-gradient/size channels), flagged only out-of-domain (FR-009); **whole-board motion load** (>1 distinct simultaneously-active rhythm) when motion is supplied (FR-010). The linter never mutates and never raises on valid input (FR-008).
- **Specification-first, Tier 1**: `Legibility.fsi` is authored and FSI-exercised before any `.fs` body; Expecto semantic tests fail-before/pass-after through the public surface; the symbology surface baseline `readiness/surface-baselines/FS.GG.UI.Symbology.txt` is regenerated to capture the new module, with **zero drift** on every other baseline and zero change to the existing `token`/`animate`/`gallery`/`filmstrip` rendering behaviour (FR-013/SC-007).
- **Loop integration** (US2/FR-015): the `fs-gg-symbology` CRITIQUE step is updated to invoke the linter on the symbol set the current mapping produces, authored in the canonical `src/Symbology/skill/SKILL.md` and mirrored to every skill tree, gated by `scripts/check-agent-skill-parity.fsx`. The previously approved M5/M6 roster lints clean (FR-014/SC-005), proving the mechanical check agrees with prior human approval.

> **Standing assumption — root-cause hypotheses are unverified until exercised.**
> This is a *greenfield additive* pure module, not a defect fix, so there is no root-cause map. The
> linter is pure logic with **no GL/raster/IO**, so it is fully exercisable headlessly. The analogue of
> the live-smoke mandate is an **early FSI/test smoke** (Foundational phase): once `Legibility.fsi` and a
> first `.fs` stub exist, load the public surface in FSI (or run a single Expecto test) and confirm
> `score` and `scoreAnimated` run end-to-end on a hand-built `Token list` and return a `Report` — before
> building out US1/US2. Treat that smoke — not this plan's narrative — as the confirmation the surface is
> usable. The roster-lints-clean assertion (FR-014) is verified by a real test over the in-tree M5/M6
> mapping, not assumed from this plan.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (existing, consumed via public types only): the channel/`Token` types of `FS.GG.UI.Symbology` (`Token`, `Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`) declared in `src/Symbology/Symbology.fsi`; transitively `FS.GG.UI.Scene` for `Color`/`PathSpec` (already referenced by `Symbology.fsproj`). **No new third-party dependency**; the linter adds nothing to the package's dependency closure beyond what `Symbology` already references.

**Storage**: None. The linter performs no IO; its entire output is the returned `Report` value (FR-001/FR-012).

**Testing**: Expecto + FsCheck, matching the existing `tests/Symbology.Tests/`. A new `tests/Symbology.Tests/LegibilityTests.fs` exercises the public `Legibility` surface (overload, out-of-domain, degenerate, determinism, machine-readable findings, empty/all-identical/motion-load edge cases). A new test in `tests/SymbologyBoard.Tests/` asserts the **approved M5/M6 roster lints clean** (FR-014/SC-005), reusing the in-tree `Roster.mapUnit` already compiled there.

**Target Platform**: Linux/CI headless (pure CPU logic; no GL, no window system). Fully reproducible across processes (SC-001).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). One **new module added to the existing `src/Symbology/` library** + new tests in two existing test projects. No new project, no new sample, no new dependency.

**Performance Goals**: Design-time tool, not a render hot path. Scoring is `O(N · channels)` over an `N`-unit board with distinct-level counting via `Set`/`List.distinct`. No fps/throughput guarantee is asserted.

**Constraints**: **Purity is the hard constraint** (FR-001/FR-012) — `score`/`scoreAnimated` are deterministic functions of their input with no wall-clock, randomness, or IO; scoring the same set twice (in-process or across processes) yields an identical `Report` (SC-001). **Advisory** (FR-008): the linter never mutates a symbol, never alters a rendered board, and never raises on valid input (including valid-but-overloaded input); degenerate/out-of-domain units become findings, not exceptions. **Zero surface drift** on the existing core scene / viewer / controls / canvas baselines and zero change to the existing symbology rendering surface behaviour (FR-013/SC-007). **Roster agreement** (FR-014): the approved M5/M6 set lints clean.

**Scale/Scope**: M7 linter thread only. ~2 source files (`Legibility.fsi`, `Legibility.fs`) + 1 new test file + 1 roster-clean test + the regenerated symbology baseline + the mirrored CRITIQUE skill edit. Deferred (FR-016): Badge/Ring alternative grammars and label text (the other two M7 threads).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | `Legibility.fsi` is authored and FSI-exercised before any `.fs` body; Expecto semantic tests call the public `Legibility.score`/`scoreAnimated` surface (not internals) and fail-before/pass-after; the `.fs` body is written against the now-stable signature. |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | The new module ships a curated `Legibility.fsi` as its sole public-surface declaration; the `.fs` carries **no** `private`/`internal`/`public` modifiers on top-level bindings (helpers are simply omitted from the `.fsi`). The symbology surface baseline is regenerated to capture the new surface (Tier 1). |
| **III. Idiomatic Simplicity** | PASS | Pure functions over records/DUs/lists; distinct-level counts via `Set`/`List.distinct`; no SRTP, reflection, type providers, custom operators, or non-trivial CE. Any local `mutable` accumulator (none anticipated) would be disclosed at the use site. |
| **IV. Elmish/MVU boundary** | PASS (N/A) | The linter is a **pure, stateless, IO-free function** (`Token list -> Report`); it has no multi-step state, IO, retries, or background work, so the MVU-boundary obligation does not attach. Purity is itself the contract the tests assert (SC-001). |
| **V. Test Evidence Mandatory** | PASS | Expecto semantic tests over the public surface, fail-before/pass-after, all **real** (pure logic is fully exercisable headlessly — no GL/raster, so no synthetic substitute is needed or used). Determinism asserted by scoring the same set twice and comparing reports. |
| **VI. Observability & Safe Failure** | PASS | Safe failure *as data*: degenerate (`R <= 0`) and out-of-domain units become findings and scanning continues — the linter never throws on valid input (FR-008/FR-005). No startup/GL/IO paths exist to instrument; the report's severities are the actionable, structured signal. |
| **Change Classification** | **Tier 1** | Adds public API surface (the `Legibility` module + its types) to the existing `FS.GG.UI.Symbology` package. Requires the full chain: spec, plan, `.fsi`, **surface-baseline update**, test evidence, docs/skill update. Existing core/rendering surfaces show **zero drift**. |
| **Engineering Constraints** | PASS | `net10.0`; F#-only; new module in the existing **pure** package referencing only its own `Token` types + already-referenced `FS.GG.UI.Scene`; **no new dependency**; `.fsi` provided; surface baseline maintained; `FS.GG.UI.*` identity untouched; SkiaSharp/GL backend untouched (the linter touches no raster path). |

**Gate result: PASS** — no violations; Complexity Tracking left empty. This is a Tier 1 change because it adds a curated public surface to an existing package; the discipline (FSI-first, `.fsi` curation, baseline regeneration with zero drift elsewhere) mirrors spec 192's surface handling.

## Project Structure

### Documentation (this feature)

```text
specs/194-symbology-legibility-linter/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — capacity-table numbers, channel-kind classification, motion-load
│                        #   interpretation, module-home decision, roster-clean validation
├── data-model.md        # Phase 1 — Channel, ChannelKind, ChannelSpec, Severity, Finding, ChannelUsage,
│                        #   Verdict, Report; the capacity table; check semantics per channel
├── quickstart.md        # Phase 1 — build + FSI smoke + run tests + per-SC validation + baseline refresh
├── contracts/           # Phase 1 — the Legibility public-surface contract (the .fsi sketch)
│   └── legibility-api.md
├── checklists/          # (pre-existing)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Symbology/                       # EXISTING pure package FS.GG.UI.Symbology (Scene-only)
├── Symbology.fsi                    #   EXISTING — Token + channel DUs + module Symbology (unchanged)
├── Symbology.fs                     #   EXISTING — token/animate/gallery/filmstrip (UNCHANGED behaviour)
├── Legibility.fsi                   #   NEW — public surface: Channel/ChannelKind/ChannelSpec/Severity/
│                                    #     Finding/ChannelUsage/Verdict/Report + table + score/scoreAnimated
├── Legibility.fs                    #   NEW — pure implementation of the capacity table + the checks
└── skill/SKILL.md                   #   EDIT — CRITIQUE step (§ "fixed feedback loop") invokes the linter

tests/
├── Symbology.Tests/                 # EXISTING — add LegibilityTests.fs + register in Program.fs
│   └── LegibilityTests.fs           #   NEW — US1 overload/out-of-domain/degenerate/determinism/edge cases
└── SymbologyBoard.Tests/            # EXISTING — add one test: approved roster lints clean (FR-014/SC-005)

readiness/surface-baselines/
└── FS.GG.UI.Symbology.txt           # REGENERATE — gains Legibility.* entries; ALL OTHER baselines unchanged

.claude/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the CRITIQUE edit)
.agents/skills/fs-gg-symbology/      # pointer wrapper -> canonical (inherits the CRITIQUE edit)
template/product-skills/fs-gg-symbology/SKILL.md   # EDIT — mirror the CRITIQUE update (full adapted copy)

FS.GG.Rendering.slnx                 # no new project to register (module lands in existing Symbology.fsproj)
```

**Structure Decision**: The linter is a **new module inside the existing pure `src/Symbology/` package**, not a new project and not part of `src/Symbology.Render/`. Rationale (FR-012): the linter depends only on the channel/`Token` types already declared in `Symbology.fsi` and must stay in the pure layer; placing it beside the types it scores keeps one shared channel vocabulary and adds no project, no dependency, and no raster reach. `.fsproj` compile order is `Symbology.fsi → Symbology.fs → Legibility.fsi → Legibility.fs` (the linter references the channel types but not the `Symbology` rendering functions). Because the module is added to a packable library, this is **Tier 1**: the symbology surface baseline is regenerated to capture `Legibility.*`, with zero drift on the `Scene`/`SkiaViewer`/`Controls`/`Canvas` baselines and zero change to the `token`/`animate`/`gallery`/`filmstrip` rendering behaviour (FR-013/SC-007). Tests land in the two existing test projects (core linter behaviour in `Symbology.Tests`; the roster-clean agreement in `SymbologyBoard.Tests`, which already compiles the approved M5/M6 mapping).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

## Implementation Status (2026-06-25 — COMPLETE)

All 33 tasks in [tasks.md](./tasks.md) are complete (`[X]`). The feature shipped exactly as planned: a
pure `Legibility` module in the existing `src/Symbology/` package, wired into the design-loop CRITIQUE step.

**Delivered**

- `src/Symbology/Legibility.fsi` + `Legibility.fs` — the curated public surface (`Channel`, `ChannelKind`,
  `Severity`, `Finding`, `ChannelSpec`, `ChannelUsage`, `Verdict`, `Report`, `table`, `score`,
  `scoreAnimated`); registered in `Symbology.fsproj` after `Symbology.fs`. Pure: no wall-clock, RNG, or IO.
- `tests/Symbology.Tests/LegibilityTests.fs` — 15 semantic tests (C1–C12, C14), fail-before/pass-after.
- `tests/SymbologyBoard.Tests/BoardTests.fs` — +2 tests: the approved M5/M6 roster lints `Clean` (C13/
  FR-014/SC-005) and a deliberately overloaded derivative surfaces a concrete `Warning`.
- CRITIQUE-step linter guidance added to the canonical `src/Symbology/skill/SKILL.md` and mirrored to
  `template/product-skills/fs-gg-symbology/SKILL.md`.
- `readiness/surface-baselines/FS.GG.UI.Symbology.txt` regenerated (gains 13 `Legibility.*` entries).

**Evidence**

| Gate | Result |
|---|---|
| Build (`FS.GG.Rendering.slnx`) | clean, 0 warnings |
| FSI smoke (`score`/`scoreAnimated` end-to-end) | returns a `Report`, no exception |
| `Symbology.Tests` | 49 passed (34 prior + 15 new) |
| `SymbologyBoard.Tests` | 7 passed (5 prior + 2 new) — **roster lints Clean** |
| Surface-baseline drift (SC-007) | only `FS.GG.UI.Symbology.txt` changed; zero drift elsewhere |
| Skill-parity (`check-agent-skill-parity.fsx`, SC-008) | passed — critical=0 high=0 warning=0 |
| Full no-regression suite vs baseline | no new reds; rendering tests unchanged |

**Pre-existing reds (NOT regressions — present in the T002 baseline, see `readiness/baseline.md`)**:
`tests/Package.Tests` (8 failed) and `samples/ControlsGallery/ControlsGallery.Tests` (2 failed). Unchanged
by this feature, which touches only the Symbology package, its two test projects, the skill docs, and the
Symbology surface baseline.

**Deferred (FR-016, unchanged)**: the Badge/Ring alternative grammars and label text (the other two M7
threads) remain backlog.
