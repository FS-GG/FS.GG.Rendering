# Phase 0 Research — Type-Safety Hardening (Feature 183)

Five Explore passes over HEAD established the ground truth. Line numbers are at HEAD (post-182). Each
decision below is **Decision / Rationale / Alternatives considered**.

---

## D1 — Control `Kind` registry shape (US1)

**Decision.** One **internal**, eagerly-built `Map<string, ControlKindEntry>` (built once at module
load), keyed by the existing `Control.Kind: string`, carved into a new file
`src/Controls/ControlKindRegistry.fs` inserted **before** `Control.fs` in `Controls.fsproj`. The entry
record carries every per-kind datum the ~13 parallel dispatch sites currently compute (see
[data-model.md](./data-model.md) §1). Each dispatch site becomes a lookup of the entry plus its current
default for the not-found case.

**The ~13 dispatch sites it replaces** (all confirmed private/internal — none in any `.fsi`):

| Site | File:line | Produces today | Registry field / derivation |
|---|---|---|---|
| `chartValues` | `Control.fs:502` | which attribute key carries the points ("series"/"values"/graph) | `ChartSeriesKey` (option) |
| `nodeWidth`/`nodeHeight` | `Control.fs:606/613` | preview size by `richFamilies` membership | `IsRich` → 304/132 vs 240/24 |
| `faithfulContent` | `Control.fs:1930` | geometry painter per kind (~83 arms) + `emptyState` default | `Painter` |
| `renderNode` | `Control.fs:2050` | rich path vs box+label by `richFamilies` | `IsRich` |
| `directionOf` | `Control.fs:2157` | `Layout.Row` for 8 kinds else attr/`Column` | `LayoutRowKind: bool` |
| `toLayout` | `Control.fs:2351/2356` | rich/chart layout marks | `IsRich` / `IsChart` |
| `paintLeaf` | `Control.fs:2413` | scroll affordance for `"scroll-viewer"` | `HasScrollAffordance` |
| `kindOf` | `Inspection.fs:48` | `VisualInspectionNodeKind` (Text/Image/Overlay/Popup/Container/Custom) | `InspectionNodeKind` |
| `surfaceRoleOf` | `Inspection.fs:68` | `VisualInspectionSurfaceRole` | `SurfaceRole` |
| `clipStatusOf` | `Inspection.fs:89` | clip status (`scroll-viewer` → Intentional) | `ClipsContent: bool` |
| (unsupported flag) | `Inspection.fs:161` | `Kind.Contains("transform")` flag | keep inline (substring test, not a kind key) |
| `roleFor` | `Accessibility.fs:28` | `AccessibilityRole` (~18 arms) + `Custom` default | `A11yRole` |
| `validateStandardControl` | `Catalog.fs:501` | schema-vs-kind required-attr validation | `RequiredAttributes` (already in `Catalog`) |
| `applyScrollOffsets` | `ControlRuntime.fs:373` | scroll-offset stamping for `"scroll-viewer"` | `HasScrollAffordance` |
| `countVirtual` | `RetainedRender.fs:1732` | virtualization counters for `data-grid`/`data-grid-row` | `Virtualization` (option) |
| family sets | `Control.fs` `richFamilies`(~51)/`chartFamilies`(~19) | membership gates | `IsRich`/`IsChart` |

**Rationale.** The kind key is already a `string` everywhere and the public `Control.Kind` field is a
`string`; keeping the registry internal means **zero public-surface change** (Tier 2, no bump) while
collapsing ~13 disjoint edit points to one. `Catalog.fs` is the existing ~98-kind SSOT, so the registry
is the natural home to fold the family sets and required-attrs into. Every site keeps its **exact
current default/fallthrough** (`emptyState` for unknown painter, `Column` for direction, `Custom` for
a11y/inspection, no-op for scroll/virtualization) — the registry returns `None`/the default for a
missing key, never silently changing behavior.

**Alternatives considered.**
- *Convert `Control.Kind` from `string` to a closed DU* — the "real" fix, but a **surface-breaking**
  redesign touching every construction site and the wire/authoring format. Out of scope (spec Out of
  Scope); the registry keys off the existing string and gets most of the exhaustiveness benefit.
- *Expose the registry publicly for external kind registration* — out of scope; internal dispatch only.
- *Per-concern sub-tables instead of one entry record* — rejected: one entry per kind is the single
  source of truth the report asks for; multiple tables reintroduce the "edit several places" hazard.

**Exhaustiveness mechanism.** Adding a kind = one `ControlKindEntry`. A test enumerates the catalog's
~98 kinds and asserts each has a registry entry (and vice-versa), so a missing kind is a **test**
failure (SC-001). Full DU-style compile exhaustiveness is only available with the closed-DU redesign
above (out of scope).

---

## D2 — `SceneNode` DU normalization form (US2) — **the pivotal decision**

**Decision.** Add named fields to the **19 bare-tuple cases preserving exact arity and field types** —
e.g. `Rectangle of (float*float*float*float)*Color` → `Rectangle of bounds:(float*float*float*float) * fill:Color`;
`Text of (float*float)*string*Color` → `Text of position:(float*float) * text:string * fill:Color`;
`Translate of (float*float)*Scene` → `Translate of offset:(float*float) * scene:Scene`. **Do not
flatten** inner tuples into separate fields and **do not retype** `(float*float*float*float)` to `Rect`
or `(float*float)` to `Point`. (Full per-case before/after in [data-model.md](./data-model.md) §3.)

**Rationale.** Adding field names while preserving arity/types is **source-compatible** in F#:
- Positional construction `Rectangle((x,y,w,h), color)` still compiles.
- Positional matching `Rectangle(bounds, color)` **and** nested `Rectangle((x,y,w,h), color)` still
  compile.
- The 6 already-named cases (`Circle`, `FilledEllipse`, `Chart`, …) are untouched.

Therefore the blast-radius agent's "≈326 match/construction sites across 55 files" — which assumed
**flattening** (an arity change) — collapses to **near-zero source edits**: consumers, samples
(PackageReference-only, FR-015), the template, and generated products recompile unchanged against the
bumped package. The only edits are the DU declaration + `Scene.fsi` + (optionally) sites that *want* to
adopt named patterns. This is the minimal change that satisfies the report's literal goal — "named
fields throughout" — at the lowest risk, consistent with "behavior byte-stable."

**Alternatives considered.**
- *Aggressive: flatten tuples and/or retype to `Rect`/`Point`* — produces the "nicest" DU but is a
  genuine arity/type change that breaks ~every match/construction site (incl. the codec), risks
  wire-format and behavior drift, and cascades into samples/template/products as **source** edits.
  **Rejected / explicitly deferred** (spec Out of Scope; FR-010 retain) — disproportionate risk for a
  readability gain. Recorded as a possible future feature.
- *Leave the DU as-is and only do the codec table* — rejected: the report's sub-goal 2 explicitly
  includes case-styling normalization, and the maintainer chose full appetite. Name-preserving form
  delivers it safely.

**Surface impact.** Surface baselines are **type-name level** (the refresh script extracts exported
*type names*, sorts, dedups) — so adding field names does **not** change `FS.GG.UI.Scene.txt` by itself;
it changes the `Scene.fsi` **text** (and possibly a `PublicSurfaceTests` assertion that inspects the
DU). Both are reviewed as the intentional diff. Scene is bumped once (shared with US3's `damageRegion`).

---

## D3 — `SceneNode` codec table & symmetry guard (US2)

**Decision.** Replace the two private hand-symmetric matches (`writeSceneNode`@761, `readSceneNode`@877,
tags **0–24** frozen) with a per-case **codec table** keyed by tag — a list/array of
`{ Tag; Write: BinaryWriter -> SceneNode -> unit; Read: BinaryReader -> SceneNode }` (one row per case) —
driving both directions. Keep tags, field order, and primitive encodings **byte-identical** (frozen wire
format). The read side becomes a tag-keyed lookup instead of a wildcard `| tag -> failwithf` match.

**Symmetry enforcement (FR-002).** Two layers, since full compile-time DU-exhaustiveness on the *read*
side isn't expressible in F# without reflection:
1. **Write side already compile-enforced.** `writeSceneNode`'s `match node` is exhaustive; `FS0025`
   (incomplete match) is escalated to **error** in `Directory.Build.props`, so adding a `SceneNode` case
   without a write arm **fails to compile** today and continues to.
2. **Read side guarded by an every-case round-trip test.** A test constructs one value of **every**
   `SceneNode` case (a list the test asserts covers all 25), serializes, and round-trips it; a case the
   read table omits fails the round-trip. A companion assertion checks `table.Length = 25` and tags are
   `0..24` contiguous. Net: adding a case forces the write arm (compile) + the codec-table row + the
   case in the round-trip list (test) — "compile + test enforced," eliminating silent writer/reader
   drift.

**Rationale.** This is the strongest enforcement F# affords without reflection or a code generator, and
it keeps the wire format provably frozen (the every-case round-trip test also captures bytes for the
behavior diff). The 3 `writeXOption`/`readXOption` near-clones over generic `writeOption` are folded to
direct `writeOption`/`readOption` calls with inline value-codecs (no wire change) as an incidental
cleanup.

**Alternatives considered.**
- *Reflection-driven exhaustiveness (enumerate `FSharpType.GetUnionCases`)* — gives runtime
  completeness checking but adds reflection (Constitution III friction) and isn't compile-time either;
  the round-trip test is simpler and also pins bytes.
- *Source generator / type provider* — over-engineering for 25 stable cases; explicitly avoided.
- *Leave read as a wildcard match* — rejected: that **is** the hazard sub-goal 2 targets.

---

## D4 — Named flag-record shapes & the public/internal split (US3)

**Decision.** Replace the positional flag/positional tails on the six functions with small named records.
Shapes in [data-model.md](./data-model.md) §4; public-vs-internal split (the bump driver):

| Function | Pkg | Visibility | Flags/positional tail → record | Surface? |
|---|---|---|---|---|
| `validateDamage` | SkiaViewer | **public** (`OpenGl.fsi:299`) | 5 bools (`visibleChange`/`fullFrameInvalidation`/`staleDamage`/`incompleteDamage`/`ambiguousDamage`) → `DamageValidationFlags` | **yes → bump SkiaViewer** |
| `classifyWindowObservation` | SkiaViewer | **public** (`SkiaViewer.fsi:118`) | 2 bool + 2 `bool option` → `WindowObservationInputs` | **yes → bump SkiaViewer** |
| `damageRegion` | Scene | **public** (`Scene.fsi:1276`) | 10 positional (3 int counters, `cause: string option`, `maximumDirtyPercentage: float option`) → group the counters into `DamageNodeCounts` (+ keep ids/cause/threshold) | **yes → bump Scene**; update cross-package call at `Controls/Inspection.fs:460` |
| `promotionDecision` | Controls | `internal` (`RetainedRender.fsi:618`) | trailing `parityPassed: bool` (+ int metrics) → `PromotionInputs` | internal `.fsi` text changes; **no public bump** |
| `damageRegionSet` | Controls | `internal` (`RetainedRender.fsi:593`) | `fullFrameInvalidation: bool` mid-tail → `DamageSetInputs` | internal `.fsi` text; **no public bump** |
| `popoverGeom` | Controls | `private` (`Control.fs:1755`) | `withActions: bool` → small record or a 2-case DU | none |

**Rationale.** Each record names the flags at the call site so a transposition is a compile error
(SC-003) while passing the **same values** → byte-identical verdicts/regions/decisions (FR-005). The
public/internal split is what determines bumps: only the three public functions move a surface, in
exactly two packages (SkiaViewer, Scene). `damageRegion` is the one **cross-package** caller
(`Controls/Inspection.fs:460`) — that call updates in lockstep but Controls' *public* surface is
unchanged, so Controls needs no public bump (it recompiles against bumped Scene).

**Alternatives considered.**
- *One mega "flags" record shared across functions* — rejected: couples unrelated functions; per-function
  records keep each contract honest.
- *Leave `damageRegion` (no bools, just positional)* — the report lists it under "boolean-trap / long
  positional param lists"; its 3 adjacent `int` counters are the transposition hazard. Grouping just the
  counters into `DamageNodeCounts` is the minimal fix; if it proves to ripple too far into evidence
  artifacts, retain per FR-010.

---

## D5 — Bump set, cascade, feed/sample alignment, and known reds (cross-cutting)

**Decision / facts** (from the surface/feed Explore pass):

- **Definitive bump set**: `FS.GG.UI.Scene` (US2 DU field names + US3 `damageRegion`) and
  `FS.GG.UI.SkiaViewer` (US3 `validateDamage` + `classifyWindowObservation`). `FS.GG.UI.Controls` has
  **no public** surface change (US1 internal; `promotionDecision`/`damageRegionSet` are `internal`;
  `popoverGeom` private) — it recompiles against bumped Scene and is **not** bumped unless an `internal`
  `.fsi` change is judged to warrant it (default: no public bump). Current versions: Scene
  `0.1.36-preview.1`, SkiaViewer `0.1.46-preview.1`, Controls `0.1.45-preview.1`.
- **Surface oracle**: `dotnet fsi scripts/refresh-surface-baselines.fsx` regenerates all 12
  `readiness/surface-baselines/*.txt`; `git diff` of that dir is the reviewed record. For this Tier-1
  feature the **expected** diff is limited to `FS.GG.UI.Scene.txt`/`FS.GG.UI.SkiaViewer.txt` *if* new
  flag-record types are introduced (type-name level) — and possibly **empty** for the DU-field-name part
  (names aren't type-name-level). The `.fsi` git diff is the finer-grained intentional record. Live gate:
  `tests/Package.Tests/SurfaceAreaTests.fs`, `tests/*/PublicSurfaceTests.fs`.
- **Full sweep**: `dotnet fsi scripts/baseline-tests.fsx --config Release --out <path>` globs all 16
  `*.Tests.fsproj` (incl. Release-only `Package.Tests` + sample lanes) under `DISPLAY=:1`.
- **Feed + sample alignment**: `dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase`
  packs the whole solution coherently to `~/.local/share/nuget-local`, retargets the sample's
  `PackageReference` pins, and restores. Samples/template are **PackageReference-only** (FR-015) → no
  source edits, just the version pin. `SecondAntShowcase` is the actively-maintained post-merge sample.
- **Known pre-existing reds** (baseline-not-regression, per `specs/182-…/readiness/baseline/known-reds.md`):
  `tests/Package.Tests` (8 failed / 109) and `samples/ControlsGallery/...Tests` (2 failed / 34) — both
  stale-local-feed pins from features 180/181. The other 14 lanes are green. A story is green only if the
  sweep reproduces **exactly** this set (same counts); a new red is a regression (SC-006).

**Rationale.** Pins the acceptance mechanics so `/speckit-tasks` can schedule baseline capture, the
per-story behavior/surface diffs, and the polish-phase bump/feed alignment deterministically, exactly as
features 179–182 did.

**Alternatives considered.** *Bump every package for tidiness* — rejected: the merge flow bumps only
what changed; over-bumping churns the feed and samples for no surface reason. Bump Scene + SkiaViewer
only.
