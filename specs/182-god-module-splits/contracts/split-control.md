# Contract: US2 — Split the Control god-module

**Target**: `src/Controls/Control.fs` (3,570 lines; `module internal ControlInternals` @124–3134,
~3,010 lines; 170 `*Geom` bindings; **17** `match pts with` chart preambles). **Package**:
`FS.GG.UI.Controls`. Inherits all of [surface-invariance.md](./surface-invariance.md).

## Scope

Divide `ControlInternals` into concern files inserted before `Control.fs` in compile order:

| Concern | Planned file |
|---------|--------------|
| Chart geometry (`*Geom` chart family) + `withPoints` combinator + shared bar-layout helper | `ChartGeometry.fs` |
| Widget geometry (`*Geom` widget family) | `WidgetGeometry.fs` |
| Scene hash / fingerprint | `SceneFingerprint.fs` |
| Layout evaluation | `LayoutEval.fs` |
| Node assembly | `NodeAssembly.fs` |
| Assembly glue + public `module Control` (residual) | `Control.fs` |

## C-C-1 — `withPoints` combinator (FR-005)

The **17** repeated `match pts with [] -> emptyState …` chart preambles MUST be hoisted into a
`withPoints` combinator (a plain higher-order function — no SRTP/CE, Constitution III) plus a shared
bar-layout helper. A call site lands only if its produced scene, scene-hash, and fingerprint are
byte-identical to baseline; a call site that genuinely diverges is left explicit (C-SI-6 / FR-009).

## C-C-2 — Geometry families move, do not collapse

The 170 `*Geom` bindings are **relocated** into `ChartGeometry`/`WidgetGeometry` for legibility, not
merged. Per the 180/181 lesson, families that diverge in detail are not force-collapsed — size and
legibility are the goal, not line reduction.

## C-C-3 — Surface union preserved

`Control.fsi` and `FS.GG.UI.Controls.txt` byte-identical. `ControlInternals` stays `internal`; extracted
files are `module internal`; public `module Control` keeps its name and members at original paths.

## Acceptance (maps to spec US2)

1. Built package: `.fsi` + surface baseline byte-identical (C-SI-1/2).
2. Any chart control laid out + hashed: scene, scene-hash, fingerprint byte-identical (C-C-1).

## Validation

`scripts/refresh-surface-baselines.fsx` → empty diff; build `FS.GG.UI.Controls` + run `Controls.Tests`;
byte-diff every chart control's scene-hash / fingerprint / inspection output vs baseline (quickstart
Step 1, row US2).

> **Coordination**: US5 also touches `src/Controls/`. Serialize US2 and US5 for a clean per-story
> `FS.GG.UI.Controls.txt` diff.

## Implementation Outcome (2026-06-21) — RETAINED per FR-009 (C-SI-6)

**Retained (not split), maintainer-approved scope decision.** `module internal ControlInternals`
(`Control.fs:124`–3134, ~3,010 lines) is a **flat sea of `let` bindings with no nested-module seams**
(verified: 0 nested `module` blocks in the span). Its bindings are tightly interdependent; extracting any
cohesive subset (`ChartGeometry`/`WidgetGeometry`/`SceneFingerprint`/`LayoutEval`/`NodeAssembly`) into
sibling `module internal` files compiled *before* `Control.fs` risks back-edges/cycles across that flat
graph (C-SI-4). The **FR-005 `withPoints` hoist** over the **17** `match pts with [] -> emptyState`
chart preambles is a behavior-affecting rewrite that cannot be cheaply shown byte-identical across all 17
chart scene/scene-hash/fingerprint outputs. Per the spec's binding rule **byte-stable output wins**
(FR-002/003) and the maintainer's scope decision (do the tractable stories US4/US5/US6, retain the
high-coupling US2/US3), `Control.fs` is **left in its current form**. `FS.GG.UI.Controls.txt` stays
byte-identical (no edit). Recorded SC-005 size exception; SC-006 dedup retained-with-reason (FR-005).
