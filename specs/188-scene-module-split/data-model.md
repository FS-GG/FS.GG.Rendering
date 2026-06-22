# Phase 1 Data Model: Scene.fs Module Split

This is a structural refactor — the "entities" are the module/file topology and the finding-identity
model the FR-006 dedup keys on. No new runtime data types are introduced (US1 re-homes existing types
verbatim).

## Module / file topology (post-split)

| File (NEW unless noted) | Namespace / module | Contents | Surface impact |
|---|---|---|---|
| `Types.fs` / `.fsi` | `namespace FS.GG.UI.Scene` (namespace-level types) | The ~770-line type wall: `Size`, `Color`, `Point`, `Rect`, stroke/paint/path/shader/filter types, `Paint`, `PathSpec`, `Clip`, `Region`, text+shaping types (`FontSpec`…`GlyphRun`), `Vertex`, `SceneElementKind`, `SceneNode`, layout-evidence types, all `VisualInspection*` + `Retained*` + `Damage*` record/union types, `RenderDiagnostic`, etc. | **None** — `FullName`s unchanged. |
| `Scene.fs` / `.fsi` (existing, shrunk) | `module FS.GG.UI.Scene.Scene` (+ `Colors`, `Paint`, `Path`) | Builder surface: `empty`/`group`/`rectangle`/`circle`/`path`/`text`/`image`/`clipped`/`chart`/… plus thin public delegations to `Text.Shaping` for the shaping/measurer entry points. | None (delegations preserve names) or per gate. |
| `TextShaping.fs` / `.fsi` | `module FS.GG.UI.Scene.Text.Shaping` | ONE private parameterized shaped-text core; `buildGlyphRun`/`buildFallbackShapedText`/`glyphRunDataFromShapedText` re-expressed over it; shared fingerprint/direction/script helpers; the `realTextMeasurer` mutable cell + set/measure (single owner). | Candidate diff (version-bump gate). |
| `Inspection.fs` / `.fsi` | `module FS.GG.UI.Scene.VisualInspection` + `module FS.GG.UI.Scene.RetainedInspection` | The two inspection modules moved verbatim + the **finished** dedup-collapse logic. | None (names preserved); **behavior** delta = dedup. |
| `Evidence.fs` / `.fsi` | `module FS.GG.UI.Scene.SceneEvidence` + `module FS.GG.UI.Scene.LayoutEvidence` | The two evidence modules moved verbatim. | None (names preserved). |
| `Scene.fsproj` (existing) | — | `<Compile>` order updated (see contracts/module-topology.md). | Build-graph only. |

**Invariant (FR-001/SC-001)**: after the split, `Scene.fs` and every new file are at or below the
~1,500-line guideline; `Scene.fs` no longer contains the type wall, the glyph trio, the
`realTextMeasurer` seam, or the four inspection/evidence modules.

## Finding-identity model (FR-006 dedup key)

- **`stableFindingId : ruleId:string -> affectedIds:string list -> string`** (Scene.fs L1813).
  Identity = `cleanToken ruleId` when no affected ids, else `cleanToken ruleId + ":" +
  (affectedIds |> filter non-empty |> map cleanToken |> sort |> String.concat "+")`.
  This token is the **dedup key**: two findings are duplicates iff their `FindingId` is equal.
- **`cleanToken`** (L1711 visual / L1881 retained — currently duplicated per path): normalizes a
  token. The dedup completion must apply the same *collapse* rule on both paths (dedupe by `FindingId`,
  keep first, preserve unique findings — SC-003 uniformity). The identity-*key* function may differ per
  path: the retained `stableFindingId` (L1921) takes `ruleId, transitionId, affectedIds` while the
  visual one (L1813) takes `ruleId, affectedIds` — each path collapses within its own identity scope;
  do not force a single key function across the two.
- **`duplicateIds : 'a list -> 'a list`** (L1836 / L2043): countBy → ids with count > 1. Today feeds
  *diagnostic strings* only. Post-change, the finished dedup collapses the duplicate findings
  themselves (keep first occurrence of each `FindingId`, preserving order and every unique finding).

**State transition (the only behavior change)**:

```
findings : VisualInspectionFinding list      (may contain >1 with same FindingId)
        ── finish FR-006 dedup ──▶
findings' : VisualInspectionFinding list      (at most one per FindingId; unique findings untouched)
```

- Same transition applied to `RetainedInspectionArtifact.Findings`.
- **Guard (FR-009 / US3 acceptance #3)**: collapse MUST NOT drop a unique real finding, reorder in a
  meaning-changing way, or weaken a fail-loud diagnostic. A malformed/degenerate scene still surfaces
  its genuine finding with the same diagnostic.

## Validation rules (from requirements)

- **Re-home resolvability (FR / Edge Case)**: all 17 consumers resolve every type from
  `FS.GG.UI.Scene.*` after the split — verified by full-solution compile (`dotnet build … -c Release`).
- **Byte-equivalence (FR-006/SC-004)**: glyph runs, shaped-text results, and fingerprints are
  byte-identical before/after US1+US2 for the existing text corpus; rendered frames equivalent for
  scenes unaffected by the dedup.
- **Semantic-equivalence (FR-006/FR-012)**: inspection/evidence artifacts compared on parsed
  structure (status/counts/headers), equal except the reviewed dedup delta.
- **Surface (FR-007/SC-006)**: regenerated `FS.GG.UI.Scene.txt` differs only by intended, reviewed
  changes (target: empty diff); version bump iff diff non-empty.
- **Test parity (FR-008/SC-005)**: identical red/green set vs the captured baseline, except the
  reviewed FR-006 expected-output updates; no assertion weakened; no test deleted/skipped.
