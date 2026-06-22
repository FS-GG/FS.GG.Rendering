# Phase 1 Data Model: Control.fs Decomposition

**Feature**: 189-control-module-split | **Date**: 2026-06-22

This is a structural refactor (Pattern E) plus one extended dispatch record (Pattern A). The "data
model" is therefore (1) the post-split **module/file topology**, (2) the **extended
`ControlKindEntry`**, and (3) the small **combinator/visitor shapes** introduced. No persisted data.

---

## 1. Module / file topology (target)

`module internal ControlInternals` (currently L124–~3066 of `Control.fs`) is decomposed into sibling
`module internal` files. **Producers precede consumers** in `Controls.fsproj` (F# file order =
dependency order). Names in `()` are working names finalized at the compile probe (research D1/D5).

| # | New file (`.fs`+`.fsi`) | Module | Source range moved | Depends on | Consumed by |
|---|--------------------------|--------|--------------------|------------|-------------|
| 0 | `ControlKindRegistry` *(extended)* | `module internal ControlKindRegistry` | existing + `Painter` field on `ControlKindEntry` | Scene/Types | ContentRender (painter table), all match…Kind sites |
| 1 | `ControlPrimitives` | `module internal ControlPrimitives` | L124–625 shared prelude | Types/Attributes/Scene/Layout | ChartGeometry, WidgetGeometry, ContentRender, LayoutEval, NodeAssembly, Control |
| 2 | `ChartGeometry` | `module internal ChartGeometry` | chart `*Geom` (L626–~1030, incl. `emptyState`, `pillGeom`) + `withPoints` | ControlPrimitives | ContentRender |
| 3 | `WidgetGeometry` | `module internal WidgetGeometry` | widget/layout/container `*Geom` (~L1032–1860) | ControlPrimitives, ChartGeometry (shared `pillGeom`/`emptyState`) | ContentRender |
| 4 | `SceneHash` | `module internal SceneHash` | `hashScene` L2405–2853 → `SceneHasher` visitor | Scene/Types, Hashing | NodeAssembly, RetainedRender |
| 5 | `ContentRender` | `module internal ContentRender` | `faithfulContent` L1868–1990 + painter table | ControlPrimitives, ChartGeometry, WidgetGeometry, ControlKindRegistry | NodeAssembly |
| 6 | `LayoutEval` | `module internal LayoutEval` | `toLayout` L2163, `evaluateLayout` L2268, `evaluateLayoutIncremental` | ControlPrimitives, Layout | NodeAssembly, ControlRuntime, RetainedRender |
| 7 | `NodeAssembly` | `module internal NodeAssembly` | `paintLeaf` L2298, `paintNode`, `renderScene` L2036 | ContentRender, LayoutEval, SceneHash, ControlPrimitives | Control, ControlRuntime, RetainedRender |
| 8 | `Control` *(residual)* | `module internal ControlInternals` residue + `module Control` (L3085) + 30 tail modules (L3358–3511) + `Control.Helpers` (US4) | what remains | thin delegations to #1–#7 | public package surface |

**Preserved names (internal callers resolve unchanged)**: `renderScene`, `paintNode`, `paintLeaf`,
`evaluateLayout`, `evaluateLayoutIncremental`, `toLayout`, `hashScene`, `faithfulContent`, `required`,
`chartValues`, `measureText`. Whether a caller writes `ControlInternals.renderScene` today, the move
keeps an equivalent qualified path (or an `open`/alias) so no call site changes shape (US2/US3
acceptance). Exact qualification is settled at the compile probe.

**Ordering invariant (INV-ORDER)**: no file may reference a module compiled later. The risk edges are
`ContentRender → geometry` (handled: geometry is #2/#3, ContentRender #5) and `registry painter →
geometry` (handled by D2: painter *table* is in ContentRender #5, not the early registry #0).

**Story-boundary edge (`NodeAssembly → faithfulContent`)**: `NodeAssembly` (#7) calls `faithfulContent`,
which lives in `ContentRender` (#5, before it). `ContentRender` is therefore **created in US2** as a
byte-identical relocation of `faithfulContent` (inline `match`, no painter table yet) so `NodeAssembly`'s
US2 extraction compiles — relocating `faithfulContent` only in US3 would leave it in the residual
`Control.fs` (#8, *after* `NodeAssembly`), a forward-reference back-edge. **US3 then transforms**
`ContentRender`'s inline dispatch into the painter table (the Pattern-A change), it does not create the
file. The stub compile probe (D5) cannot catch this edge because stubs carry no real cross-references.

---

## 2. `ControlKindEntry` (extended) — FR-005 / FR-007

Existing record (`ControlKindRegistry.fsi`) gains one field:

```
type ControlKindEntry =
    { IsRich: bool
      IsChart: bool
      ChartSource: ChartDataSource option
      LayoutRow: bool
      HasScrollAffordance: bool
      Virtualization: VirtualizationRole option
      InspectionNodeKind: VisualInspectionNodeKind
      SurfaceRole: VisualInspectionSurfaceRole
      A11yRole: AccessibilityRole
      Painter: Painter }          // NEW (FR-005)
```

- **`Painter`** = `Theme -> Rect -> Control<'msg> -> Scene list` (the uniform shape every former
  `faithfulContent` arm reduces to; see C2). The **value** filling the painter for each kind is bound in
  `ContentRender` after geometry compiles (research D2).
- **Genericity ripple (FR-005 deviation, preferred form)**: `ControlKindEntry` is currently
  **non-generic** (metadata only). A `Painter: Theme -> Rect -> Control<'msg> -> Scene list` **field on
  the record** introduces a free `'msg`, forcing the record — and the registry `Map` and **every**
  existing metadata reader — to become `ControlKindEntry<'msg>`. To avoid that ripple, the **preferred**
  shape is a **sibling `painters: Map<string, Painter>`** keyed by kind, not a field on the entry; the
  field-on-entry form is used only if a non-generic painter shape (boxing/existential wrapper) keeps it
  clean. Record the chosen form as the FR-005 deviation rationale at the compile probe.
- **Completeness oracle (FR-007 / SC-007)**: extend `Feature183KindRegistryTests.fs` so that, in
  addition to "registry keys == catalog kinds", it asserts **every catalog kind resolves a `Painter`**
  (no kind falls through to a default/empty painter). A catalog kind with no painter fails the test
  loudly.
- **Fallback for non-catalog runtime kind (FR-007 / Edge Case)**: `faithfulContent` for a kind absent
  from the registry returns the **same default** the pre-refactor `match … | _ -> <default>` arm
  produced — preserved verbatim, not a new empty painter.

---

## 3. Introduced shapes

### C2 — `Painter` (in `ControlKindRegistry`, used by `ContentRender`)
```
type Painter = Theme -> Rect -> Control<'msg> -> Scene list
```
Each entry closes over the per-kind argument extraction that `faithfulContent` did inline
(`chartValues control`, `styleClassesOf`, `visualStateOf`, label/intent), so float/dispatch order per
kind is byte-preserved (Edge Case "float/dispatch-order"; US3 acceptance #1).

### C3 — `withPoints` combinator (in `ChartGeometry`) — FR-002 / SC-002
```
withPoints (theme: Theme) (box: Rect) (caption: string) (pts: ChartPoint list)
           (body: ChartPoint list -> Scene list) : Scene list
// ≡  match pts with [] -> emptyState theme box caption | nonEmpty -> body nonEmpty
```
Collapses ONLY the ~17 shared empty-points guards; divergent bodies stay in the `body` lambda. Empty
input yields a byte-identical `emptyState` scene.

### C4 — `SceneHasher` visitor (in `SceneHash`) — FR-003 / SC-004
A structured walk over `SceneNode` replacing the 25-case inline `goNode`/`goScene`, preserving the
FNV-1a `mutable h` accumulator and the exact mix order (tag → fields → children). Public-internal
entry `hashScene: Scene list -> uint64` unchanged. Canary: `hashScene [] = emptySceneListFingerprint`
byte-identical.

---

## 4. The 6 parallel `match …Kind` sites (FR-006 / SC-003)

Routed through the single registry table; each becomes one table read.

| Site (current) | Current dispatch | After |
|----------------|------------------|-------|
| `Control.fs:1884` (`faithfulContent`) | 60+ `match control.Kind` → `*Geom` | registry `Painter` lookup (→ `ContentRender`) |
| `Control.fs:112` / `:259` / `:354` / `:537` | kind-string `match` (slot regions, required, kind split) | read registry metadata where it is the *same* fact; `required` (L353) retained at site (FR-012) |
| `ControlRuntime.fs:375` | `hasScrollAffordance control.Kind` | already registry — confirm single read |
| `Catalog.fs:501` | `match schema.Kind, control.Kind` | registry-backed where it duplicates kind metadata |
| `Inspection.fs:70` | `match control.Kind` | registry `inspectionNodeKind`/`surfaceRole` |
| `RetainedRender.fs:1820` | `virtualizationOf c.Kind` | already registry — confirm single read |

> Note: some sites (`ControlRuntime.fs:375`, `RetainedRender.fs:1820`) already read the registry from
> feature 183; US3 confirms they are a single table read and eliminates the remaining *disjoint*
> faithful-geometry/`match control.Kind` switches. Genuinely non-kind-metadata matches (e.g. `required`
> validation, slot-region structure) are **retained at their sites** (FR-012) — not force-fitted into
> the table.

---

## 5. Validation rules (carried from spec)

- **INV-1**: `evaluateLayout` `root`/`boundsById` byte-identical to a pre-refactor full evaluate
  (FR-004 / SC-004).
- **Scene equivalence**: every `*Geom` / painter / `renderScene` output byte-identical where
  construction guarantees it; otherwise within the reviewed golden-hash delta (FR-009 / SC-004).
- **Surface neutrality**: `FS.GG.UI.Controls.txt` empty diff target; bump iff non-empty (FR-010 /
  SC-006).
- **Fail-loud retained**: unknown kind → pre-refactor default; missing painter → oracle fails;
  `required`-attribute violation still surfaces (FR-012).
