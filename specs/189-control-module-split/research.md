# Phase 0 Research: Control.fs / ControlInternals Decomposition

**Feature**: 189-control-module-split | **Date**: 2026-06-22

All current-tree line references re-confirmed 2026-06-22 against `src/Controls/Control.fs` (3,513
lines): `module internal ControlInternals` L124–~3066; shared helper prelude L124–625; chart/widget
geometry L626–~1860; `faithfulContent` L1868/dispatch L1884; `renderScene` L2036; `toLayout` L2163;
`evaluateLayout` L2268; `paintLeaf` L2298; `hashScene` L2405–2853; `required` L353; public
`module Control` L3085; the 30 tail modules L3358–3511.

---

## Decision D1 — Split mechanism: sibling `module internal` files, not a spanning module

**Decision**: Decompose `module internal ControlInternals` into a set of **sibling `module internal`
modules**, each in its own `.fs`+`.fsi` pair, compiled in producer→consumer order. Extract the
**shared helper prelude** (L124–625: `measureText`/`setMeasureTextHook` seam, `chartValues`,
`styleClassesOf`, `visualStateOf`, `fittedFontSize`, `ellipsize`, `accessibility`, `required`,
`lowerSlots`, `nodeWidth`, …) **first**, into a foundational internal module (working name
`ControlPrimitives`), because the geometry and content functions call it.

**Rationale**:
- F# does **not** allow a non-namespace module to span multiple files. Feature 188 confirmed this for
  `module Scene`; `module internal ControlInternals` has the same limit. So "extract into `SceneHash`"
  means "new sibling internal module", not "continuation of `ControlInternals`".
- The geometry `*Geom` functions are **not clean leaves**: they are `let private` inside
  `ControlInternals` and call prelude helpers (`measureText`, `chartValues`, `fittedFontSize`,
  `ellipsize`, theme/pill helpers). Moving geometry to a sibling module breaks `private` visibility
  unless the prelude they depend on is reachable. Extracting the prelude into its own internal module
  (members module-internal, not `private`) compiled first makes geometry a clean consumer.
- Visibility cost is **surface-neutral**: every new module is `module internal` (precedent:
  `ControlKindRegistry`, `Reconcile`, `RetainedRender`), reached by tests via the existing
  `InternalsVisibleTo Controls.Tests` (`Controls.fsproj:19`); nothing reaches the public
  `FS.GG.UI.Controls.txt` baseline. The `private`→module-internal change on prelude helpers does not
  add an access keyword to a top-level `.fs` binding (Constitution II) — visibility is expressed by
  `.fsi` presence/omission for each internal module.

**Alternatives considered**:
- *`[<CompilationRepresentation(ModuleSuffix)>]` to "span" `ControlInternals`* — rejected as a
  misreading of the 188 lever. In 188 `ModuleSuffix` resolved an FS0250 **type/module name collision**
  (a type `Scene` next to `module Scene`), not module-spanning. It is held in reserve here **only** if
  a residual type/module name collision arises during extraction; it is not the primary mechanism.
- *Keep everything in one file, just reorder* — rejected: does not meet SC-001 (≤ ~1,500-line files)
  and leaves the god-module intact.

---

## Decision D2 — Registry painter placement: metadata stays early, painter table assembled late

**Decision**: Keep the existing `ControlKindRegistry` **metadata predicates** (`isRich`, `isChart`,
`chartSource`, `layoutRow`, `hasScrollAffordance`, `virtualizationOf`, `inspectionNodeKind`,
`surfaceRole`, `a11yRole`) and the metadata `registry: Map` where they are (compiled early at
`Controls.fsproj:48`, since `Control.fs:499` and others read `chartSource` early). Add the **`Painter`
field to `ControlKindEntry`** (FR-005) but **assemble the painter-populated table in `ContentRender`**,
a new internal module compiled *after* `ChartGeometry`/`WidgetGeometry`. `ContentRender` owns
`faithfulContent` (now a registry lookup) and the painter table.

**Rationale**:
- F# file order is dependency order. The painter functions ARE the geometry functions, which compile
  late. If the painter-populated table lived in the early `ControlKindRegistry.fs`, it would reference
  not-yet-compiled geometry — a back-edge that won't compile.
- Splitting "metadata table (early)" from "painter table (late)" keeps every early consumer
  (`Control.fs:499` `chartSource`, `ControlRuntime.fs:375` `hasScrollAffordance`,
  `RetainedRender.fs:1820` `virtualizationOf`, `Inspection.fs`/`Catalog.fs` reads) resolving against
  the unchanged early predicates, while the behavior-sensitive painter routing lands in one late site.
- FR-005 ("`ControlKindEntry` gains a painter field") is satisfied: the **record type** carries
  `Painter`, defined early (its types `Control<'msg>`/`Theme`/`Rect`/`Scene` all exist early via
  `Types.fs`); only the **value** that fills it is bound late in `ContentRender`. SC-003's "one registry
  table read" is met — `faithfulContent` and the 6 `match …Kind` sites each become a single table
  lookup.

**Painter signature** (data-model.md C2): `Painter: Theme -> Rect -> Control<'msg> -> Scene list`,
matching the current `faithfulContent` arm shape `geomFn theme box (… control)`. The per-kind arm's
argument extraction (`chartValues control`, `styleClassesOf`, `visualStateOf`, label/intent) is closed
over inside each painter entry so the table value has a uniform `Theme -> Rect -> Control -> Scene list`
shape — preserving the exact per-kind float/dispatch order (Edge Case "float/dispatch-order").

**Alternatives considered**:
- *Move all of `ControlKindRegistry.fs` after geometry* — rejected: breaks the early metadata
  consumers (`Control.fs:499` etc.), forcing a cascade of reorders or duplicate predicate definitions.
- *A separate `painters: Map<string, Painter>` not on `ControlKindEntry`* — **now the preferred form,
  not just a fallback.** Beyond ordering, the decisive issue is **genericity**: `ControlKindEntry` is
  currently non-generic (metadata only), so a `Painter: Theme -> Rect -> Control<'msg> -> Scene list`
  field would carry a free `'msg`, forcing the record — plus the registry `Map` and every existing
  metadata reader — to become `ControlKindEntry<'msg>`. A sibling `painters: Map<string, Painter>` keyed
  by kind avoids that ripple entirely while still meeting SC-003 ("one table read"). FR-005 ("the entry
  gains a painter field") is satisfied in spirit by the registry owning the painter; the field-on-entry
  literal form is used only if a non-generic painter shape (boxing/existential wrapper) keeps the record
  non-generic. The chosen form and its genericity rationale are recorded against FR-005 as an accepted
  deviation at the Foundational compile probe.

---

## Decision D3 — `hashScene` recast as a `SceneHasher` visitor, proven on golden hashes

**Decision**: Recast `hashScene` (L2405–2853, a 25-case inline `goNode`/`goScene` walk with closure
mixers over the FNV-1a accumulator) as a structured `SceneHasher` visitor over `SceneNode` in the new
`SceneHash` module. Preserve the exact mix order (tag → fields → children) and the `mutable h` FNV-1a
accumulator (`// mutable: hot path`). Keep the public-internal name `hashScene` and its
`Scene list -> uint64` shape so `RetainedRender` and the fingerprint path reference it unchanged.

**Rationale**:
- `hashScene` is the second-worst function in the file and the one most entangled with the
  `RetainedRender` picture-cache/fingerprint hot path. A visitor makes the 25 cases legible without
  changing the traversal contract.
- The recast **intends to reorder nothing**; the target is byte-identical hashes for the scene corpus.
  Where a recast legitimately reorders a mix (e.g. a uniform child-fold replacing a hand-inlined case),
  the change is captured under the §7 **golden-hash review gate** — an explicit "hashes changed,
  reviewed" record (`readiness/golden-hash-review.md`) proving it is an intentional reorder that breaks
  no picture-cache invariant — never silently asserted (FR-003/FR-009/SC-008).

**Verification**: `Feature120FingerprintTests.fs` + `Feature091/174RetainedRenderTests.fs` over the
scene corpus must stay green on byte-identical hashes (or carry a reviewed expected-output update).
`emptySceneListFingerprint = hashScene []` (L2853) is the canary — it must be byte-identical.

**Alternatives considered**:
- *Leave `hashScene` inline, only relocate it* — rejected: the spec/report explicitly call the visitor
  recast (Pattern A) the value here; pure relocation leaves the worst readability offender intact.
- *Generic reflective hasher* — rejected (Constitution III: reflection needs justification; the typed
  mixers are faster and order-explicit on the hot path).

---

## Decision D4 — `withPoints` collapses only the shared empty-state skeleton

**Decision**: Factor the ~17 repeated `match pts with [] -> emptyState theme box caption | _ -> <body>`
preambles in the chart `*Geom` functions into one `withPoints` combinator:
`withPoints theme box caption pts (fun pts -> <body>)` (exact shape in data-model.md C3). Only the
genuinely-shared guard skeleton is collapsed; each divergent body stays inline in the lambda.

**Rationale**: Carry-forward lesson (feature 180/181): a record-of-functions or over-eager combinator
can *increase* line count and indirection. `withPoints` is a thin HOF wrapping exactly the duplicated
guard, so it nets reduction and produces a byte-identical empty-state scene (US1 acceptance #3,
SC-002). Widget `*Geom` functions that don't share the empty-points guard are **not** forced through it.

**Alternatives considered**:
- *A geometry record/typeclass abstraction* — rejected by the 180/181 lesson and Constitution III.

---

## Decision D5 — Story sequencing and the empirical compile probe

**Decision**: Sequence US1 (geometry, byte-identical) → US2 (hash/layout/assembly, golden-hash gated)
→ US3 (registry painter + 6 match sites, most behavior-sensitive) → US4 (tail collapse, conditional).
Before US1, the Foundational phase runs a **full-solution compile probe** of the proposed
`Controls.fsproj` topology (empty/stub modules in the new order) to fix the load-bearing relative
ordering (`ContentRender` vs `NodeAssembly`; painter-field vs separate-map per D2) **empirically**,
exactly as 188 fixed `TextShaping` vs `Scene` order by delegation direction.

**Rationale**: The internal back-edge risk (Edge Case) is real and F#-order-specific. Proving the
skeleton compiles before moving bodies de-risks every later story and converts an unverified ordering
hypothesis into a checked fact (per the plan's standing-assumption note).

**Alternatives considered**: *Move bodies and fix ordering reactively* — rejected: a mid-extraction
back-edge forces large rollbacks; a cheap skeleton probe front-loads the one hard constraint.

---

## Decision D6 — US4 is conditional on a measured net reduction

**Decision**: Implement `Control.Helpers` (data-driven tail-module bodies) only if, measured on the
actual diff, it nets a real line reduction in `Control.fs` while preserving every public tail-module
`create`/`text` surface (thin delegations). If indirection ≥ duplication removed, **drop US4**; US1–US3
stand alone (FR-008 / SC-001 footnote / US4 acceptance #2).

**Rationale**: Lowest payoff, only slice touching public surface, explicit carry-forward lesson (180
SC-005). The 30 modules are 3–4 lines each (~155 lines total, L3358–3511) — the reduction headroom is
small, so the measured gate is the honest call.

**Verification**: `PublicSurfaceTests.fs`/`TypedControlContractTests.fs` + the regenerated surface
baseline (empty diff for the tail modules); a `Control`-value equality check per entry point.

---

## Resolved unknowns

| Unknown (from spec) | Resolution |
|---------------------|------------|
| Internal-module mechanism (sibling vs ModuleSuffix) | **Sibling `module internal` files** (D1); ModuleSuffix only as a collision fallback. |
| Where the painter table lives (back-edge) | **`ContentRender`, compiled after geometry**; metadata predicates stay early (D2). |
| Whether hash bytes change | **Target byte-identical**; any legitimate reorder gated by §7 golden-hash review (D3). |
| Exact affected test project list | `Controls.Tests`, `Package.Tests` (`SurfaceAreaTests`), `Elmish.Tests`, `ControlsGallery.Tests`, `Rendering.Harness.Tests` — confirmed at task time. |
| US4 ship/drop | **Conditional on measured net reduction** (D6). |

## Build / test / baseline commands

- Build: `dotnet build FS.GG.Rendering.slnx -c Release`
- Full test (GL suites need X11): `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release`
- Focused: `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release`
- Surface baseline refresh: `dotnet fsi scripts/refresh-surface-baselines.fsx`
  → `readiness/surface-baselines/FS.GG.UI.Controls.txt`
- Pre-refactor baseline capture (FR-014): `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config
  Release --out specs/189-control-module-split/readiness/baseline/test-baseline.md`
