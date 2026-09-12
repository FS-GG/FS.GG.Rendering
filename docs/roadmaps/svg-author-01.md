# SVG-AUTHOR-01 — Vector content and scene authoring

Status: active; .1–.4 source delivered; next executable window .5–.6. Route: routine.

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

## Delivered studio and reusable-content window

This window follows Rendering [PR #1302](https://github.com/FS-GG/FS.GG.Rendering/pull/1302),
merged as `66bcc805534ad7ae75686ab266ca134ef52c059e`. The delivered
`SvgAuthoring` preview/commit/history and catalog APIs are source/candidate
capabilities, not installed authoring tools. The importer still omits several
renderer-supported elements needed by tools, and `UpsertAsset` must refuse changed
content under an existing immutable asset/revision.

- [x] **SVG-AUTHOR-01.3 — Create and edit vector art through an optional studio — route: routine**

  Depends on: completed .1–.2.

  Scope: add portable `SvgArt` operations beside `SvgAuthoring` and an explicit,
  disposable `SvgStudio` browser entry in the existing SvgBrowser package. Update
  both curated Fable/package surfaces, portable tests, browser journeys, API docs,
  and qualification identities. Keep one retained document host and native DOM
  controls. Tool state, camera, selection, and accepted document state stay distinct;
  history remains owned by `SvgAuthoring`.

  Provide rectangle, ellipse, polygon and path creation; quadratic/cubic handles
  and point insertion/removal; pointer and numeric translation/rotation/scale;
  group/ungroup, alignment, sibling ordering, fill/stroke and linear/radial
  gradients; deterministic document-space grid snapping and guides. Point/property
  lists provide keyboard alternatives. Extend inert import for tool-generated
  circle/ellipse/line/polygon/polyline and radial-gradient output while preserving
  all active/external/resource-limit refusals. Text/font interchange remains .4.

  Every gesture uses one transaction identity: repeated `preview`, one
  `commitPreview`, and `cancelPreview` on Escape, pointer cancellation, lost
  capture, or blur. Invalid numeric/path input changes neither content nor history.
  Build a separate `studio.html`/Fable entry; player builds import no studio module,
  loader, worker, or initialization side effect.

  Acceptance: create and edit the full selected primitive/path surface from an empty
  document through pointer and keyboard-accessible controls. A drag adds one undo
  entry; cancellation adds none; undo/redo restores the whole edit. Export/reimport
  preserves geometry, ordering, presentation, and local identities with matching
  .NET/Fable results. Unaffected retained nodes, camera, and valid selection survive
  edits. Chromium, Firefox, WebKit, an actual assistive-technology journey, isolated
  packed consumers, player/studio closure inspection, and repeated mount/dispose pass.

  Evidence: `SvgArt` provides portable primitive/path editing, transforms, grouping,
  ordering, presentation, gradients, document-space snapping and guides while
  `SvgStudio` keeps selection/camera/tool state outside accepted document history.
  The packed Fable fixture builds separate player and studio entries and drives native
  controls, one-gesture preview/commit/cancel, numeric validation, mount/dispose and
  all three browser families. The authoritative CI invocation installs and runs Orca
  through AT-SPI2; its journey activates the studio Rectangle control and verifies
  spoken selection and invalid-translation feedback. Local packaging/Fable/Vite
  compilation passed; this container cannot launch Playwright because `libnspr4.so`
  is absent, so the PR's hosted browser/Orca job remains the acceptance authority.

- [x] **SVG-AUTHOR-01.4 — Bounded Boolean geometry and licensed reusable content — route: routine**

  Depends on: .3's tool/transaction and separate studio-entry seams.

  Scope: portable geometry preparation and asset/interchange helpers; a SvgBrowser
  module worker; verified font loading and catalog controls; exact npm lock,
  third-party notices, package assets, and focused portable/browser qualification.

  Use exactly `polygon-clipping` 0.15.7 with its MIT notice inside an explicitly
  created module worker. Consumers provide a bundler-resolved worker factory; later
  Templates adoption must not copy the implementation. Permit one in-flight request,
  no queue, and terminate on success, error, cancellation, timeout, or disposal.
  Bind results to operation ID, accepted revision, and input content hash, validate
  before one atomic commit, and discard stale results.

  Flatten transformed quadratic/cubic curves adaptively with default maximum
  deviation 0.25 document units and an explicit finite 0.01–1.0 range. Use a
  conservative error bound, maximum subdivision depth 16, and preserve original
  curves until commit. Refuse above 10,000 input vertices, 20,000 output vertices,
  128 contours, or 1 MiB encoded request/result. Apply a two-second browser deadline;
  it is a refusal limit, not a hard real-time claim. Reject nonfinite points, open
  contours, unsupported self-intersections, and unrepresentable fill rules.

  Correct `UpsertAsset`: identical reinsertion is harmless, while changed content,
  rights, or dependencies requires a new revision. Instances and snapshots retain
  exact accepted revisions; inherited values, overrides, missing dependencies,
  removed-target conflicts, and undoable resolution remain explicit.

  Embed the existing unmodified Noto Sans Latin 400 WOFF2 through a distinct
  resource-aware interchange API; do not add an outline engine. Verify Fontsource
  Noto Sans 5.3.0 bytes at SHA-256
  `09aee8065d25508f23a4c3d92cd777ac869c52d93fd868a88f025d888a7937d6`
  and retain the full OFL notice. Additional fonts require an approved exact-byte
  resource manifest and redistribution/embedding provenance.

  Default `SvgImport.importXml` continues rejecting arbitrary CSS/data URLs.
  Resource-aware export may emit only the narrowly generated embedded WOFF2
  `@font-face`; import validates it and returns detached resources plus a normal
  typed document. Bound decoded fonts to 1 MiB each and 2 MiB total inside the
  existing 4 MiB document limit, checking base64 size before allocation. Activate
  browser font resources only after acceptance and always dispose them.

  Acceptance: union/intersection/subtraction pass rectangle, hole, coincident-edge,
  degenerate, and curved fixtures within the declared approximation; budget/depth/
  unsupported cases refuse without edits. Stalled, cancelled, disposed, and stale
  worker results cannot commit; success adds one undo entry and stays absent from
  player output. Immutable prefab revision and conflict journeys pass. Verified
  text exports and reopens offline with no network; wrong hash/rights/bytes, CSS,
  external URL, malformed base64, and excessive payloads fail before activation.
  Portable .NET/Fable agreement, three-browser studio behavior, isolated packages,
  and player/studio closure all pass. Geometry remains presentation authoring,
  not collision authority.

  Boundary: source and local candidate qualification only. No producer publication,
  Templates adoption, lifecycle change, or complete C03/C04–C06 claim follows.

  Evidence: `SvgGeometry` adaptively flattens supported winding contours and binds a
  bounded worker request/result to operation, revision and a portable content hash;
  `SvgGeometryWorkerHost` enforces one in-flight operation, cancellation/disposal,
  result size and the two-second deadline around exact `polygon-clipping` 0.15.7.
  Worker journeys cover union, intersection, contained-hole subtraction,
  coincident-edge XOR and a flattened curve; portable negative tests cover stale,
  open, degenerate, deviation and budget refusal. Immutable asset revisions now
  reject changed content/rights/dependencies. `SvgResourceInterchange` verifies and
  round-trips the exact Fontsource Noto Sans Latin 400 bytes detached from the typed
  document, with pre-allocation limits and browser activation/disposal. The package
  carries the exact npm locks, worker, resource manifest, WOFF2, MIT notice and full
  OFL; its portable/Fable closure remains free of browser, Game, Skia and native edges.

Stop after .3–.4's native merge/readback and make .5–.6 executable from the
delivered studio, interchange, worker, and package contracts.

## Remaining outcomes

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
