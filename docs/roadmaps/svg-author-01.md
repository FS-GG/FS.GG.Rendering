# SVG-AUTHOR-01 — Vector content and scene authoring

Status: active; first executable window .1–.2. Route: routine.

Owner: FS.GG.Rendering. FS.GG.Templates owns generated consumers and external
compatibility. This is order 5 of the
[accepted SVG programme](https://github.com/FS-GG/.github/blob/main/docs/2026-09-07-064259-svg-game-engine-template-design-roadmap.md)
and covers C04–C06, the remaining supported-subset C03 import/authoring work,
M3, and the authoring portions of M4. SVG-PREVIEW-A/Release A is complete.

## Outcome and boundary

A developer can create vector art from blank content, edit paths and primitives,
reuse versioned assets and prefab instances, and assemble grid and freeform
scenes through public Rendering APIs. Validated documents preserve authored
content through export, reimport, migration, and atomic undo/redo.

This feature supplies source and generated-candidate qualification. Full command,
modal, rebinding, and docking behavior remains SVG-INPUT-01; gameplay remains
SVG-RUNTIME-01; browser persistence remains SVG-PRESENT-01. Public integrated
installation remains SVG-PREVIEW-B. Source merge, local candidate qualification,
public publication, and installed behavior are separate facts.

S.I.R. is off limits. Use only the recorded donor characterization and
Templates-owned external fixtures. Preserve provenance and third-party notices,
and never claim S.I.R. production adoption.

## Baseline and reuse

Planning inspected Rendering `7d60c37641c650759f0c2c761023c89bdfcbbee1`,
Templates `ccfb9e08261f4fbf309ad7099adbac72c7b7ce0a`, and programme/index
`790b133f985ae9e1c3db3e8a43fdb74c2e8db358`.

- `SvgDocument` already provides identified documents, transforms, paint,
  definitions, typed serialization, and SVG export. Its deserializer deliberately
  refuses arbitrary SVG XML.
- Retained browser rendering, selection/focus, picking, and font readiness are
  published and installed-demonstrated in Release A.
- Minimal guarded replacement, undo/redo, and immutable play snapshots exist;
  they do not constitute a complete transaction editor.
- Asset descriptors exist as vocabulary. Catalog resolution, prefab revision
  handling, import policy, authoring tools, and generated studio journeys remain.
- The recorded `polygon-clipping` 0.15.7 spike is authoring-only and remains out
  of the player dependency graph.

Reuse the single editable profile-2 authority under `models/svg-foundation/`, the
separate retained/document transition corpora, Scene's curated .NET/Fable surface,
and the existing portable/browser consumers. Do not add Rendering-to-Game,
Skia, or native dependencies to the portable closure.

## Contract choices

Keep `fsgg.svg-document/1` readable and unchanged in meaning. Add separately
versioned asset/catalog and authoring envelopes with stable IDs, content hashes,
rights metadata, explicit migration, and refusal behavior. Immutable instances
retain their accepted asset revision until an explicit update transaction.
Overrides remain distinguishable; removed overridden properties produce a
reviewable conflict. Missing references and cycles preserve the last valid state.

SVG XML import is content parsing, never DOM execution or a plugin system. Validate
before mutation, namespace imported IDs and local references, and apply one portable
policy in .NET and browser/Fable consumers. Reject DTDs, entities, scripts, event
attributes, `foreignObject`, uncontrolled URLs, arbitrary CSS/filters, and
unsupported compressed inputs. Preserve the existing 4 MiB, 10,000-node,
100,000-segment, 512-definition, depth-32, and 50,000-expanded-node limits, with
bounded parser/token checks before allocation or expansion.

## Executable milestones

- [x] **SVG-AUTHOR-01.1 — Safe versioned content crosses the authoring boundary — route: routine**

  Depends on: completed SVG-PREVIEW-A.

  Scope: Scene XML import and asset envelopes, curated Fable packaging, validation
  fixtures, and concise API documentation. Freeze identity normalization, supported
  grammar, budgets, hashes/rights, and schema-version handling.

  Acceptance: .NET and Fable import the same supported rectangle/path/gradient/
  clip/mask/symbol fixture to equivalent canonical documents. Colliding source IDs
  remain asset-local. Supported export/reimport preserves geometry, order, and
  presentation. Active, malformed, over-budget, cyclic, external-reference, and
  unknown-version inputs return located diagnostics without changing accepted
  content. Existing document-v1 and isolated packed consumers pass.

  Evidence: `SvgImport.importXml` and `SvgAsset` add the bounded inert grammar,
  asset-local identity normalization, canonical SHA-256, rights/schema/dependency checks and located
  refusal diagnostics without changing `fsgg.svg-document/1`. `Scene.Tests` covers the supported
  rectangle/path/gradient/clip/mask/symbol fixture, export/reimport, collision, malformed, active,
  external, over-budget, hash, missing/cyclic and unknown-version controls. The isolated candidate
  package produces byte-identical .NET/Fable authoring output and retains the dependency-free closure.

- [x] **SVG-AUTHOR-01.2 — Asset and scene edits commit as atomic transactions — route: routine**

  Depends on: .1.

  Scope: portable catalog/reference/prefab resolution and transactions over current
  document values; revision guards, grouped previews, cancellation, validation,
  undo/redo, and immutable play handoff. Amend the existing model authority and
  bindings when behavior changes; do not hand-edit generated Quint.

  Acceptance: shared instances update inherited values explicitly while preserving
  overrides; removed overridden properties report conflicts. Missing/cyclic inputs
  refuse commit. Multi-object transforms and brush gestures make one undo entry.
  Cancellation, stale completion, or one invalid operation changes neither document
  nor history. Undo/redo restores the whole transaction, a new edit clears redo,
  and later edits cannot mutate an existing play snapshot. Real .NET/Fable
  correspondence and negative controls cover affected transitions.

  Evidence: `SvgAuthoring` validates documents, catalogs and prefab resolution before committing one
  checkpoint/revision for a transaction. Explicit instance revision updates preserve typed overrides
  and surface removed targets as conflicts; missing/cyclic assets refuse. Tests cover grouped multi-object
  transforms, preview/cancel, stale and invalid atomic refusal, whole-group undo/redo, redo clearing and
  immutable play handoff. The portable consumer runs the same accepted and negative sequence in .NET and
  Fable beside the unchanged 384 model-derived transition corpus and ten killed correspondence mutants.

Stop after .2's native merge/readback and focused evidence, then make the next
window executable from the observed contracts.

## Remaining outcomes

- **SVG-AUTHOR-01.3 — Create and edit vector art.** Add an optional browser studio
  entry with accessible rectangle, ellipse, polygon, path/Bézier, transform,
  grouping, alignment, order, paint, snapping, cancel, undo, and round-trip flows.
- **SVG-AUTHOR-01.4 — Bounded geometry and licensed reusable content.** Put Boolean
  operations behind a terminable authoring worker with complexity limits,
  cancellation and stale-result refusal. Complete symbols/prefabs, verified fonts,
  rights metadata, and a permitted embedded-font or deliberate outline export path.
- **SVG-AUTHOR-01.5 — Assemble grid and freeform scenes.** Add terrain/region,
  boundary, object, path, prefab placement, snapping, properties, validation, and
  complete history without putting product semantics into Rendering.
- **SVG-AUTHOR-01.6 — Generated journeys and Preview-B handoff.** Compose immutable
  local producer candidates in Templates and prove blank-to-art-to-scene,
  shared-asset conflict, grouped history, round trip, invalid import, and clean/
  retained adoption. Verify browser/accessibility behavior and that studio and
  geometry-worker modules remain outside the unselected player closure.

Feature completion requires all six outcomes and generated-candidate evidence.
SVG-PREVIEW-B owns compatible publication and public installed repetition.

## Generated workspace and release impact

Affected family: opt-in SVG content in `fs-gg-fable-game` across supported direct
and SDD lifecycle routes. The `svgFoundation` option remains opt-in; authoring UI
is explicitly selected. Omitted lifecycle remains `sdd`. No wizard default,
registry activation, or coordination epoch changes here.

Before, Rendering 0.29.0 and Templates 0.11.0 provide prepared SVG scenes and
typed export. After .6, selected generated candidates create assets and editable
grid/freeform scenes through the same contracts. Installed workspaces change only
after a coherent Rendering release, dual-feed readback, Templates adoption and
publication, and clean/upgrade receiver qualification in Preview B. Future SemVer
identities follow compatibility review.

Clean acceptance uses an empty directory, isolated caches, exact candidate inputs,
and build/test/Fable/browser journeys. Existing workspaces use the bounded adopter,
never scaffold rerun; preserve authored files, assets, lifecycle provenance, and
owner guidance, refuse collisions without writes, and distinguish package rollback
from irreversible authored-format migration.

## Observation

Use the installed roadmap telemetry adapter and native CI evidence. The local
adapter currently reports its private host is not configured, so usage coverage is
unknown. This is advisory to valid delivery and cannot support an efficiency claim.
Routine delivery, current native checks, and ADR-0084 qualification selection apply.
