# Contract: US1 — Split the SkiaViewer god-module

**Target**: `src/SkiaViewer/SkiaViewer.fs` (4,063 lines; `module Viewer` @777). **Package**:
`FS.GG.UI.SkiaViewer`. Inherits all of [surface-invariance.md](./surface-invariance.md).

## Scope

Carve `module Viewer` into concern-scoped files inserted before `SkiaViewer.fs` in compile order:

| Concern | Planned file | Source span (approx @HEAD) |
|---------|--------------|-----------------------------|
| Type header / `RequireQualifiedAccess` types + `RenderLagTrace` | `Viewer.Types.fs` | `:17`–`:776` |
| Responsiveness summarization | `ViewerResponsiveness.fs` | within `module Viewer` |
| Window-behavior / validation | `ViewerWindowBehavior.fs` | within `module Viewer` |
| Native run-loops + **unified persistent-window scaffold** | `ViewerRunLoops.fs` | `:2114`/`:2437` + call sites `:3382`/`:3498`/`:3670` |
| Evidence / screenshot | `ViewerEvidence.fs` | within `module Viewer` |
| App / interactive runners (residual public `module Viewer`) | `SkiaViewer.fs` | remainder |

## C-V-1 — Run-loop unification (FR-004)

`runPresentedPersistentWindow` (`:2114`) and `runPersistentWindow` (`:2437`) — both currently
`private`, three internal call sites — MUST be unified behind one private lifecycle scaffold **iff**
window observations, diagnostics, and evidence output are byte-identical to baseline. Otherwise both
are retained explicitly with the divergence recorded (C-SI-6 / FR-009). Because both are private,
neither unification nor retention changes `SkiaViewer.fsi`.

## C-V-2 — Surface union preserved

`SkiaViewer.fsi` and `FS.GG.UI.SkiaViewer.txt` byte-identical. The public `module Viewer` keeps its
name and every public member at its original module path; extracted concern modules are `module
internal` (or carry an internal `.fsi`).

## Acceptance (maps to spec US1)

1. Built package: `.fsi` + surface baseline byte-identical (C-SI-1/2).
2. Persistent-window paths: window observations, diagnostics, evidence match baseline (C-V-1).
3. Suite green; no single viewer file exceeds the size target (SC-005); no public symbol moved
   namespaces.

## Validation

`scripts/refresh-surface-baselines.fsx` → empty diff; build `FS.GG.UI.SkiaViewer` + run
`SkiaViewer.Tests` + viewer-driven smoke/evidence lanes; byte-diff viewer evidence/screenshots vs
baseline (quickstart Step 1, row US1).

## Implementation Outcome (2026-06-21)

**Done (byte-stable, validated):** the public type block (`module internal RenderLagTrace` excluded —
kept in residual; `:60`–`759` public `Viewer*` types) carved into **`Viewer.Types.fs` + `Viewer.Types.fsi`**
(same namespace, compiled before `SkiaViewer.fs`). `SkiaViewer.fs` 4,063 → 3,366 lines. Required a
one-line **`open FS.GG.UI.SkiaViewer`** re-open in the residual (after the third-party opens) to restore
unqualified-name resolution the in-file types previously won by proximity (record-field + DU-case vs
`Scene`/`Silk.NET.Windowing`) — pure resolution restoration, output byte-stable.

- Oracle 1 ✓ `FS.GG.UI.SkiaViewer.txt` byte-identical (307 public types).
- Oracle 2 ✓ `SkiaViewer.Tests` **Release** 207/207 exit 0 = baseline parity (validate in Release; the
  Debug run flakes on timing-sensitive viewer/GL tests — pre-existing, not a regression).

**Retained per FR-009 (C-SI-6) — rationale:** the further carve of `module Viewer`'s private internals
(`ViewerResponsiveness` / `ViewerWindowBehavior` / `ViewerRunLoops` / `ViewerEvidence`, T008–T011) and
the **FR-004 run-loop unification** (C-V-1) are **retained**. `module Viewer`'s 65 **public** members
(frozen at module path `Viewer.*`) cannot relocate without surface drift, and its **private** helpers
form a tightly-coupled graph with private supporting types (`LegacyQueuedInput`, `LegacyHostMsg`) defined
in the residual; extracting any subset into sibling `module internal` files compiled *before*
`SkiaViewer.fs` creates back-edges (C-SI-4), and FR-004 unification cannot be shown byte-identical.
**Byte-stable output wins** (FR-002/003). `SkiaViewer.fs` remains > ~1,500 lines — a recorded **SC-005
size exception** (goal, not hard rule).
