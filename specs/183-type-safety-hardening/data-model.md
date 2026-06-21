# Phase 1 Data Model — Type-Safety Hardening (Feature 183)

The new/changed types introduced by each story. Names are **proposed** (binding requirement is the
behavior + intentional-surface invariants, not exact identifiers). F#-shaped sketches; the real `.fsi`
is authored FSI-first per Constitution I.

---

## 1. `ControlKindEntry` + registry (US1, internal)

A single internal record per control kind, replacing the ~13 parallel `Kind`-keyed dispatch sites
(research D1). Lives in new `src/Controls/ControlKindRegistry.fs` (compiled **before** its consumers).

```fsharp
// internal — NOT in any .fsi; Control.Kind stays a public string
type internal ControlKindEntry =
    { Painter: Theme -> Rect -> Control -> Scene list      // faithfulContent (Control.fs:1930); None-default → emptyState
      RequiredAttributes: string list                      // Catalog validation (Catalog.fs:501)
      ChartSeriesKey: string option                        // chartValues routing (Control.fs:502): "series"/"values"/graph
      IsRich: bool                                          // richFamilies membership (nodeWidth/Height, renderNode, toLayout)
      IsChart: bool                                         // chartFamilies membership (toLayout clip — Control.fs:2356)
      LayoutRowKind: bool                                   // directionOf (Control.fs:2157): Row else attr/Column
      HasScrollAffordance: bool                             // paintLeaf + applyScrollOffsets ("scroll-viewer")
      Virtualization: VirtualizationRole option             // countVirtual (RetainedRender.fs:1732): Grid | GridRow
      InspectionNodeKind: VisualInspectionNodeKind          // kindOf (Inspection.fs:48); None-default → Custom
      SurfaceRole: VisualInspectionSurfaceRole              // surfaceRoleOf (Inspection.fs:68); default Content
      ClipsContent: bool                                    // clipStatusOf (Inspection.fs:89)
      A11yRole: AccessibilityRole }                         // roleFor (Accessibility.fs:28); default Custom

and internal VirtualizationRole = Grid | GridRow

// eager, built once at module load — read by Map lookup, no per-frame rebuild
let internal registry : Map<string, ControlKindEntry> = ...    // ~98 entries, derived from the Catalog SSOT
let internal tryEntry (kind: string) : ControlKindEntry option = Map.tryFind kind registry
```

**Validation rules / invariants.**
- Every catalog kind (~98) has exactly one entry; a test asserts `registry` keys == catalog kinds
  (both directions) so a new kind is surfaced (SC-001).
- Each dispatch site preserves its **exact current default** when `tryEntry` returns `None`:
  `Painter` → `emptyState`; `LayoutRowKind` absent → `Column`; `A11yRole`/`InspectionNodeKind` →
  `Custom`; `SurfaceRole` → `Content`; scroll/virtualization → no-op.
- The `Inspection.fs:161` `Kind.Contains("transform")` substring test is **not** a kind-key lookup —
  left inline (not folded into the registry).
- `popoverGeom`'s `withActions` is handled by US3 (flag record), not the registry.

**Relationships.** Reuses existing public types (`Scene`, `Control`, `Theme`, `AccessibilityRole`,
`VisualInspectionNodeKind`, `VisualInspectionSurfaceRole`) — introduces no new public type.

---

## 2. `SceneNodeCodecRow` + table (US2, internal)

Per-case codec rows replacing the hand-symmetric `writeSceneNode`/`readSceneNode` matches in
`src/Scene/SceneCodec.fs`. Internal (not in `SceneCodec.fsi`). Wire format **frozen** (tags 0–24).

```fsharp
type private SceneNodeCodecRow =
    { Tag: int
      Write: System.IO.BinaryWriter -> SceneNode -> unit     // writes ONLY the case payload (tag written by driver)
      Read: System.IO.BinaryReader -> SceneNode }            // reads the payload, returns the reconstructed case

// 25 rows, Tag = 0..24 contiguous, in current DU/tag order (frozen)
let private sceneNodeCodec : SceneNodeCodecRow list = [ ... ]
let private readerByTag : Map<int, BinaryReader -> SceneNode> =
    sceneNodeCodec |> List.map (fun r -> r.Tag, r.Read) |> Map.ofList
```

**Validation rules / invariants.**
- `sceneNodeCodec.Length = 25` and `{ r.Tag } = {0..24}` (contiguous) — asserted by test.
- **Write** stays an exhaustive `match node` that emits the tag then calls the case's `Write` — `FS0025`
  (incomplete match) escalated to **error** makes a missing case a compile error.
- **Read** is `readerByTag.[reader.ReadInt32()]` (or the table arm); the prior wildcard
  `| tag -> failwithf "Unknown scene-node tag %d"` is retained **only** for genuinely-unknown tags
  (corrupt input), not as a stand-in for a missing case.
- An **every-case round-trip test** constructs one value of all 25 cases and asserts serialize→deserialize
  is identity **and** byte-stable vs the captured baseline bytes (read-side symmetry guard, FR-002/005).
- The 3 `writeXOption`/`readXOption` near-clones fold into generic `writeOption`/`readOption` (no wire change).

---

## 3. Normalized `SceneNode` DU (US2, public — `Scene.fs:391` / `Scene.fsi`)

Add named fields to the **19 bare-tuple cases**, preserving exact arity and field types (research D2).
The 6 already-named cases (`Empty`, `Group`, `Circle`, `FilledEllipse`, `Chart`, `CachedSubtree`) are
unchanged.

| Tag | Case — current | → named (arity/type preserved) |
|---|---|---|
| 2 | `Rectangle of (float*float*float*float) * Color` | `Rectangle of bounds:(float*float*float*float) * fill:Color` |
| 3 | `PaintedRectangle of Rect * Paint` | `PaintedRectangle of bounds:Rect * paint:Paint` |
| 6 | `Ellipse of Rect * Paint` | `Ellipse of bounds:Rect * paint:Paint` |
| 7 | `Line of Point * Point * Paint` | `Line of startPoint:Point * endPoint:Point * paint:Paint` |
| 8 | `Path of PathSpec * Paint` | `Path of path:PathSpec * paint:Paint` |
| 9 | `Points of Point list * Paint` | `Points of points:Point list * paint:Paint` |
| 10 | `Vertices of VertexMode * Vertex list * Paint` | `Vertices of mode:VertexMode * vertices:Vertex list * paint:Paint` |
| 11 | `Arc of Rect * float * float * Paint` | `Arc of bounds:Rect * startAngle:float * sweepAngle:float * paint:Paint` |
| 12 | `Text of (float*float) * string * Color` | `Text of position:(float*float) * text:string * fill:Color` |
| 13 | `TextRun of TextRun` | `TextRun of run:TextRun` |
| 14 | `Image of (float*float*float*float) * string` | `Image of bounds:(float*float*float*float) * source:string` |
| 15 | `ClipNode of Clip * Scene` | `ClipNode of clip:Clip * scene:Scene` |
| 16 | `RegionNode of Region * Paint` | `RegionNode of region:Region * paint:Paint` |
| 17 | `ColorSpaceNode of ColorSpace * Scene` | `ColorSpaceNode of colorSpace:ColorSpace * scene:Scene` |
| 18 | `PerspectiveNode of PerspectiveTransform * Scene` | `PerspectiveNode of transform:PerspectiveTransform * scene:Scene` |
| 19 | `PictureNode of Picture` | `PictureNode of picture:Picture` |
| 21 | `Translate of (float*float) * Scene` | `Translate of offset:(float*float) * scene:Scene` |
| 22 | `SizedText of (float*float) * string * float * Color` | `SizedText of position:(float*float) * text:string * size:float * fill:Color` |
| 23 | `GlyphRun of GlyphRun` | `GlyphRun of run:GlyphRun` |

**Invariants.** Source-compatible: positional construction/matching still valid (no consumer/sample/
template/product source edit). Wire format unchanged (same fields, same order). `Scene.fsi` text updated
to match (reviewed surface diff). **Out of scope (FR-010 / future):** flattening the inner tuples or
retyping `(float*float*float*float)`→`Rect` / `(float*float)`→`Point` — an arity/type change that breaks
construction sites.

---

## 4. Named flag records (US3)

One record per function (research D4). Public records → their package's surface; internal/private → no
public bump.

```fsharp
// --- FS.GG.UI.SkiaViewer (public) ---
type DamageValidationFlags =          // validateDamage (OpenGl.fs:522 / .fsi:299), replaces 5 trailing bools
    { VisibleChange: bool; FullFrameInvalidation: bool
      StaleDamage: bool; IncompleteDamage: bool; AmbiguousDamage: bool }

type WindowObservationInputs =        // classifyWindowObservation (SkiaViewer.fs:935 / .fsi:118), replaces 4 flags
    { ExternalObservationAttempted: bool; ExternalWindowMatched: bool option
      CaptureAttempted: bool;            CaptureSucceeded: bool option }

// --- FS.GG.UI.Scene (public) ---
type DamageNodeCounts =               // damageRegion (Scene.fs:2000 / .fsi:1276): group the 3 adjacent int counters
    { Repainted: int; Shifted: int; Unaffected: int }
// new signature keeps named ids/cause/threshold params; the 3 transposable counters become one record

// --- FS.GG.UI.Controls (internal — no public bump) ---
type internal PromotionInputs =       // promotionDecision (RetainedRender.fs:768 / .fsi:618, val internal)
    { BoundaryId: string; ObservedStabilityFrames: int; ObservationWindow: int
      ExpectedSavedWork: int; MeasuredOverhead: int; ParityPassed: bool }

type internal DamageSetInputs =       // damageRegionSet (RetainedRender.fs:731 / .fsi:593, val internal)
    { FrameWidth: int; FrameHeight: int; FullFrameInvalidation: bool; Cause: string; Boxes: Rect list }

// popoverGeom (Control.fs:1755, private): withActions:bool → small private record or a 2-case DU (PopoverKind)
```

**Validation rules / invariants.**
- Field semantics and the **values passed are unchanged** → identical results (verdicts, observations,
  damage regions, promotion decisions) byte-for-byte (FR-005). The records only name the arguments.
- Call-site updates: `validateDamage` (1 internal site, `OpenGl.fs:562`); `classifyWindowObservation`
  (tests only); `damageRegion` (**cross-package** `Controls/Inspection.fs:460` + ~6 test files);
  `promotionDecision`/`damageRegionSet` (1 internal + several test files); `popoverGeom` (3 sites in
  `Control.fs`). All must compile and produce identical output.
- Whether `damageRegion` keeps its remaining params positional or also groups ids/cause/threshold is a
  design choice for tasks; the **minimal** change is grouping just the 3 transposable counters.

---

## 5. Surface & version state (cross-cutting, US2/US3)

| Package | Current `<Version>` | Changes | Bump? | Baseline `.txt` |
|---|---|---|---|---|
| `FS.GG.UI.Scene` | `0.1.36-preview.1` | DU field names (US2) + `damageRegion`/`DamageNodeCounts` (US3) | **yes** | `FS.GG.UI.Scene.txt` — reviewed diff (new `DamageNodeCounts` type; field names in `.fsi` text) |
| `FS.GG.UI.SkiaViewer` | `0.1.46-preview.1` | `validateDamage`+`classifyWindowObservation` flag records (US3) | **yes** | `FS.GG.UI.SkiaViewer.txt` — reviewed diff (new `DamageValidationFlags`/`WindowObservationInputs` types) |
| `FS.GG.UI.Controls` | `0.1.45-preview.1` | US1 internal registry; internal flag records; recompiles vs bumped Scene | **no public** | `FS.GG.UI.Controls.txt` — **unchanged** |
| other 9 packages | (various) | none | no | unchanged |

**Feed/sample/template alignment** (polish): `dev-repack.fsx --sample samples/SecondAntShowcase`
repacks the solution coherently to the local feed and retargets the sample's PackageReference pins; the
template inherits versions at generation. Known reds (`Package.Tests`, `ControlsGallery`) stay as the
baseline stale-feed set.
