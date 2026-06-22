# Phase 1 Data Model — Cross-Cutting Dedup + State Records

All entities below are **internal** (private by `.fsi` absence, or `internal` for cross-file Testing
helpers). None changes the public package surface (FR-009). No persisted/serialized state — these
are in-process shapes that re-express existing locals. Field names/types below restate existing
constructs; confirm exact names at implementation time.

## FrameMetricsBuilder (US1, internal — `src/Controls.Elmish/ControlsElmish.fs`)

The single internal routine that maps a frame's work-reduction carriers + metadata into the existing
**public, unchanged** 32-field `FrameMetrics` record (`ControlsElmish.fs:63–97`).

- **Inputs**: the per-frame work-reduction carriers and metadata currently spelled at the 2 full
  construction sites (`1423–1460`, `1957–1990`) — e.g. memo hit/miss, virtualization counts, damage
  triple, picture-cache triple, replay quintuple, text-cache pair, invalidation count, and the
  product/model/layout flags.
- **Output**: a fully-populated `FrameMetrics` value — the only site that names all 32 fields.
- **Validation rules**: none added; field values and their derivation are byte-identical to the
  former hand-spelled records (FR-007).
- **Relationships**: consumes the `FrameScriptState` carriers (US2) when called from `runScriptCore`.
- **Surface**: omitted from `ControlsElmish.fsi` ⇒ private. The public `FrameMetrics` type is
  untouched.
- **Note on partial sites**: the 4 `{ zero with … }` sites (`2026/2092/2132/2171`) are not full
  constructions and are out of FR-001's strict scope (research Decision 1).

## FrameState (US2, internal — `src/Controls/RetainedRender.fs`)

A record with **mutable fields** holding the 19 frame accumulators currently loose in `step`
(`RetainedRender.fs:1455+`). Shared with `init`'s cold-start seeding path (`1289–1341`).

| Field (current loose binding) | Type | Source line |
|---|---|---|
| `Tc` / TextCache | text cache | 1483 |
| `TextHits` / `TextMisses` | `int` | 1485–1486 |
| `NextId` | `uint64` | 1529 |
| `Recomputed` | `int` | 1530 |
| `ChangedBound` | `int` | 1531 |
| `Shifted` | `int` | 1535 |
| `Memo` | `MemoCache` | 1538 |
| `MemoHits` / `MemoMisses` | `int` | 1539–1540 |
| `MetadataVisited` | `int` | 1548 |
| `VirtualMaterialized` / `VirtualTotal` | `int` | 1759–1760 |
| `PcEntries` / `PcClock` | picture-cache state | 1806–1807 |
| `PictureHits` / `PictureMisses` | `int` | 1808–1809 |
| `ReplaySkippedNodes` / `ReplayNativeBytes` | `int` | 1820–1821 |

- **State transitions**: fields are mutated **in the exact same order** as the former loose mutables
  across the 8 walks (FR-002, research Decision 6). `RepaintedBoxes : ResizeArray<Rect>` (line 1545)
  is already a mutable collection and is held by reference in the record.
- **`init` convergence (FR-003)**: `init` seeds `NextId=0UL`, `Memo=Map.empty`,
  `PcEntries=Map.empty`, `PcClock=0` (lines 1289/1299/1340/1341) onto the **same** record shape so
  cold-start cache seeding and first-frame output are byte-identical.
- **Surface**: omitted from `RetainedRender.fsi` ⇒ private (the `.fsi` is already wholly `internal`).
- **Disclosure**: each mutable field carries `// mutable: hot path` (constitution III).

## FrameScriptState (US2, internal — `src/Controls.Elmish/ControlsElmish.fs`)

A record holding the **7 metric-carrier** mutables in `runScriptCore` (`1849–1865`); optionally also
the 3 workflow-state mutables (research Decision 2).

| Field | Type | Source line |
|---|---|---|
| `LastMemo` | `int * int` | 1849 |
| `LastVirtual` | `int * int` | 1853 |
| `LastDamage` | `int * int * int` | 1857 |
| `LastPicture` | `int * int * int` | 1858 |
| `LastReplay` | `int * int * int * int * int` | 1860 |
| `LastTextCache` | `int * int` | 1864 |
| `LastInvalidated` | `int` | 1865 |
| *(optional)* `Model` / `Retained` / `LastRender` | workflow state | 1840–1845 |

- **Relationships**: feeds the US1 `FrameMetricsBuilder` (the carriers are the builder's inputs).
- **Surface**: `runScriptCore` is private (no `.fsi` entry); the record is fully internal.

## InspectionValidation routine (US3, `internal` — host in `TestingVisual.fsi` `module internal`)

One shared algorithm behind both public validators: `VisualInspectionValidation.validateCheck`
(`TestingVisual.fs:995–1065`) and `RetainedInspectionValidation.validateCheck`
(`TestingRetainedInspection.fs:373–438`). Parameterized over the per-family differences.

- **Inputs / parameters**: the declared exceptions, the findings, and the family-specific knobs —
  (a) the **accepted-severity predicate** (visual: `Blocking` only, `TestingVisual.fs:1014`;
  retained: `Blocking || Warning`, `TestingRetainedInspection.fs:392`), (b) the status/severity
  result type, and (c) the diagnostic wording.
- **Algorithm (written once)**: validate declared exceptions against findings → compute
  unused/invalid → build diagnostics → derive status.
- **Output**: per-family status + diagnostics. Retained additionally derives `ReviewRequired` when a
  `Warning` is present (`TestingRetainedInspection.fs:427–428`); the parameterization MUST preserve
  that the visual family neither accepts nor emits `Warning`/`ReviewRequired` (FR-005, Edge Cases).
- **Relationships**: both public `validateCheck` functions become thin delegators; their public
  signatures are unchanged.
- **Surface**: declared `internal` in `TestingVisual.fsi` (compiles before the retained module);
  absent from the public surface baseline (research Decision 3).

## ManagedSection updater (US4, `internal` — host in `TestingVisual.fsi` `module internal`)

One shared abstraction behind the three `updateManagedSection` writers:
`VisualReadinessMarkdown` (`TestingVisual.fs:642–688`), `VisualInspectionMarkdown`
(`TestingVisual.fs:1271–1311`), `RetainedInspectionMarkdown` (`TestingRetainedInspection.fs:654–694`).

- **Inputs**: target file content, begin/end marker pair, the new section body (+ any per-writer
  separator/wording differences passed as parameters).
- **Algorithm (written once)**: count `(begin,end)` markers →
  - `(0,0)` → **append** the section (with separator), as before;
  - `(1,1)` → **replace** the region between markers, as before;
  - **else** → **fail loud** (report error / refuse to write) — never silent last-wins (FR-006, FR-011).
- **Output**: the updated content (or a loud failure).
- **Relationships**: all three public `updateManagedSection` functions delegate to it; their public
  signatures are unchanged.
- **Surface**: declared `internal` in `TestingVisual.fsi`; absent from the public surface baseline.

## Cross-entity invariants

- **Byte-identity (FR-007)**: every entity above re-expresses existing values; rendered frames and
  per-frame metrics are byte-identical to the pre-refactor baseline.
- **Surface stability (FR-009 / SC-006)**: no public symbol added/changed/removed; the four affected
  `.fsi` files and all surface baselines are byte-identical; no version bump.
- **Fail-loud (FR-011)**: preserved at the managed-section and inspection-validation sites.
- **No new project/dependency (FR-010)**: all entities live in already-existing `.fs` modules.
