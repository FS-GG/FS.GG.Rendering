# SVG-AUTHOR-01 — Vector content and scene authoring

Status: active; .1–.5 source delivered; next executable window .6. Route: routine.

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

## Final executable window

Planning inspected Rendering PR #1303 before its accepted merge as
`229b2dce92287b4b0eb53dd35bfa858a42c2babc` and Templates main at
`ccfb9e08261f4fbf309ad7099adbac72c7b7ce0a`.

Reuse `SvgArt`, `SvgAuthoring`, `SvgResourceInterchange`, `SvgStudio`,
`SvgGeometryWorkerHost` and their packaged assets. Their source and bounded
fixture evidence do not establish a complete generated authoring experience.
The .5 producer surface now supplies scene interchange and reusable studio seams;
the remaining window composes them into the generated Templates experience and
qualifies the resulting candidate packet without changing producer semantics.

- [x] **SVG-AUTHOR-01.5 — Assemble editable grid and freeform scenes — route: routine**

  Owner: Rendering.

  Depends on: .1–.4 native acceptance and merged-state readback. Recheck the final
  #1303 source delta; ordinary repairs do not require another planning pass.

  Touch set: Scene's authoring/art contracts and implementation; a portable scene
  envelope and validation/codec surface; SvgStudio controls and retained browser
  integration; both curated Fable/package manifests; Scene.Tests, portable
  consumers and Scene.SvgBrowser.Tests; focused API and authoring documentation.

  Contract: represent scene entities with stable identities, references to visual
  elements or prefab instances, product-defined kind identifiers and bounded typed
  properties. Rendering owns generic property descriptors, identity/reference
  validation and presentation; product adapters own terrain names, traversal,
  collision, gameplay and tactical meaning. Layers remain ordered identified
  document groups. Grid placement is an optional adapter over continuous
  coordinates, with explicit step/origin and snapping disabled for freeform work.

  Add a versioned `fsgg.svg-scene/1` interchange envelope containing the document,
  asset catalog, pinned instances, entity/property data and resource references.
  Reuse existing canonical document/catalog codecs and verified font resources.
  Preserve `fsgg.svg-document/1` and asset-catalog v1 readers. Import a legacy
  document/catalog through an explicit additive migration with no invented product
  semantics. Reject unknown versions, unresolved references, invalid properties,
  hash mismatches and excessive aggregate input before changing accepted state.
  Keep the total serialized scene envelope within 4 MiB and retain constituent
  document/font limits. Save/load here means explicit file import/export, not
  IndexedDB or gameplay persistence.

  One accepted checkpoint and transaction must include scene metadata, document,
  catalog and instances. Extend the existing authoring authority instead of keeping
  a separate metadata history. A cancelled brush/drag, invalid property or failed
  import changes none of those values. A committed multi-entity gesture creates
  one undo entry. Preserve immutable play snapshots; the handoff is not a runtime.

  Implement actual reusable scene controls for region/terrain presentation,
  boundaries, objects, paths and prefab placement, including properties,
  selection, layer order and grid/freeform snapping. Connect the existing art
  operations through usable native controls: path points/Bézier handles,
  transforms, grouping/alignment/order, presentation/gradients, catalog revisions,
  instance overrides/conflicts, Boolean operations and verified text/resources.
  Consumers supply product descriptors and sample content, not copied tool logic.

  Complete the observed browser integration:
  - Render `Preview.Candidate` while preserving accepted state/history; cancel
    restores accepted presentation. Reconcile through the same retained host.
  - Apply camera transforms to the displayed scene and inverse-transform pointer
    coordinates consistently. Reject singular cameras; keep valid selection and
    accessible focus synchronized after deletion, undo and import.
  - Connect pointer gestures and equivalent numeric/keyboard controls to the same
    transactions. Display validation, asset conflicts, font readiness and worker
    cancellation/refusal through accessible feedback.
  - Resolve and run `contentFiles/any/any/svg-geometry-worker.js` from the exact
    restored package. Remove the fixture's competing algorithm implementation;
    a thin bundler import/factory is sufficient.
  - Commit actual successful Boolean results once. Bind completion to the current
    revision and input content, reject stale results, and preserve the original
    paths until commit.

  Acceptance:
  1. In a blank scene, create a region, boundary, object, edited path and two
     instances of a newly authored asset. Exercise both an integer grid with a
     nonzero origin and fractional free coordinates through the same public APIs.
     A product descriptor supplies sample terrain/property meaning.
  2. Perform the journey through real controls, including a visible drag preview,
     Bézier edit, grouping/order/style, numeric property change and snapping.
     Camera pan/zoom affects rendering and picking; root and unaffected node
     identities survive edits. Keyboard controls produce equivalent accepted
     content and preserve meaningful focus.
  3. Update an asset by explicit new revision, preserve one instance override,
     show and resolve a removed-target conflict, and undo/redo the complete change.
     Invalid properties, cancelled gestures and stale worker completions leave
     content, metadata and history unchanged.
  4. Save/reload the scene envelope and export/reimport supported SVG with assets,
     definitions and verified text. Preserve identities, references, entity data,
     overrides, hashes and notices. Exercise legacy additive migration and
     unsupported-version refusal without partial change.
  5. Reconcile the documented C03 rendering/import/export subset against fixtures
     for paths/Béziers, fill rules, strokes, both gradients, clips, masks, symbols
     and text. Repair any required mismatch; do not narrow the accepted subset to
     whatever the importer happens to support.
  6. Run focused Scene tests, isolated packed .NET/Fable correspondence and the
     changed Chromium/Firefox/WebKit journeys. Exercise actual assistive technology
     for scene selection, properties and refusal feedback. Tests must drive
     controls and observe accepted state/rendered results, not only invoke fixture
     helpers. Retain independent geometry and hostile-input controls.
  7. Player-only builds exclude studio modules, worker code and geometry
     dependencies. Disposal clears owned listeners, capture, workers, timers and
     activated font resources.

  Evidence: `SvgScene` adds the bounded `fsgg.svg-scene/1` envelope over canonical
  document/catalog bytes, pinned instances, generic entity/property metadata and
  verified resource references; `SvgAuthoring` checkpoints metadata, document,
  catalog and instances together. Scene.Tests covers descriptor/refusal, nonzero
  grid and fractional freeform placement, migration, immutable snapshots/history,
  asset revision/conflict and the complete C03 import/export subset. The isolated
  `FS.GG.UI.Scene` candidate passes matching .NET/Fable scene correspondence and
  retains the browser/Game/Skia/native-free closure. `SvgStudioHost` exposes reusable
  document, grid and current-result Boolean commit seams, displays cancellable
  candidates, retains camera transforms across reconciliation, inverse-picks, and
  reconciles selection. Native controls cover scene roles, paths, grouping,
  alignment/order, style, grid/freeform, undo/redo and numeric edits; the browser
  fixture drives controls and commits successful worker output exactly once.
  Browser packaging restores `contentFiles/any/any/svg-geometry-worker.js` from the
  exact candidate (source SHA-256
  `bce396c9ab6296cdfa75858dd605174f3cc06191dbe858558d5bb1e98c343ac5`)
  with `polygon-clipping` 0.15.7 lock SHA-256
  `a3442c491268b0365e59752b84bb452b8f595c014d0ded3ff51535927bba25bf`;
  its competing fixture implementation is gone. Local package restore, Fable 5.17,
  Vite build and player/studio dependency inspection pass. This container lacks
  `libnspr4.so`, so local Chromium/Firefox/WebKit and Orca are explicitly unavailable;
  the authoritative workflow installs all three engines plus Orca 49.8 at
  `a4e6375bdaebbade7974b9717b353bc864602b53` and exercises selection, property and
  refusal feedback through a real headed Chromium/AT-SPI2 process.

  Delivery: merge Rendering source through its routine route and pack an exact
  immutable local candidate set from the accepted merged revision. Record
  versions, hashes, curated Fable interfaces, packaged worker/resource hashes and
  npm lock identity. Do not reuse one candidate identity for different bytes.
  This is the prerequisite packet for .6, not producer publication.

- [ ] **SVG-AUTHOR-01.6 — Generated authoring journeys and Preview-B handoff — route: routine**

  Owner: Templates; Rendering retains producer defects and this feature's single
  completion ledger. .github owns the asynchronous unified progress projection.

  Depends on: .5 accepted merged producer source and retained immutable candidate
  packet. Templates may prepare composition while .5 runs, but qualification
  requires the final packet. Producer repairs return to Rendering, followed by a
  new exact candidate and affected receiver rerun.

  Touch set: Templates-owned authoring composition for
  `templates/fs-gg-fable-game/SvgFoundation`; a separate studio project/entry,
  product schema and neutral sample content; candidate packaging/restore helpers;
  `tests/composition/fable-game` authoring and retained-receiver journeys;
  `scripts/apply-svg-foundation-preview.sh` and baseline manifests; build/use/
  migration documentation. Change public provider pins only at their later
  publication boundary.

  Composition decision: retain `--svgFoundation true` as the explicit selection.
  Add a separately built/opened studio entry to the selected candidate payload;
  keep the generated player entry free of studio imports and initialization.
  No new lifecycle, wizard default or provider activation is needed.

  Use an explicit internal candidate-pack path that stages the authoring payload
  and exact producer pins into a local template candidate. Keep ordinary released
  template/provider pins at their published identities until Preview B. The
  candidate staging must be reproducible from committed Templates sources and
  the supplied packet; do not hand-edit a generated receiver to make it pass.

  Build the candidate from archive inputs, not sibling ProjectReferences, source
  links or ambient caches. Restore the worker, font manifest/bytes and notices
  from the exact Rendering package; any generated build adapter may locate or
  byte-copy those package assets into ignored build output, but must not maintain
  another implementation. Use the producer's exact npm dependency/lock identity.
  Preserve the existing public Preview-A qualification lane; add a clearly
  identified authoring-candidate lane rather than relabel public evidence.

  Generated product code owns two small content/property adapters:
  a grid scene with regions/boundaries and a continuous scene with fractional
  objects/paths. Both consume the same Rendering scene/studio APIs. Reuse the
  existing tactical compatibility fixture only for recorded disclosed behavior.
  No S.I.R. access, source dependency, inferred donor behavior or production
  adoption claim is allowed.

  Acceptance:
  1. In an empty directory with no sibling checkouts and isolated tool/package
     caches, install the retained local template candidate and create a selected
     workspace. Its documented commands restore locked inputs, build/test,
     Fable-compile, build the player and studio separately, and serve both.
     Default unselected creation retains its supported behavior.
  2. Run the complete blank-to-art-to-scene journey through generated UI:
     create/edit art, save it as an asset, place two instances, author a grid and
     freeform scene, edit properties, resolve a revision conflict, use Boolean
     geometry and verified text, undo/redo, save/reload and export/reimport.
     Reopen the exported font-bearing content offline with no external fetch.
     Check actual content and rendering, not only the presence of controls.
  3. Repeat the affected generated journey in Chromium, Firefox and WebKit.
     Exercise one real keyboard/assistive-technology route. Reuse unchanged
     producer evidence only with exact source/payload binding; generated
     composition changes need their own observations.
  4. Qualify direct creation and the supported installed SDD 1.7.0 provider route,
     including explicit none, omitted/default sdd and typed/profile-2 receiver
     handling. Preserve lifecycle/owner-skill materialization and existing model
     correspondence where applicable. Retain the wizard 0.11.1 two-step path:
     create the supported fable-game baseline, then explicitly apply the bounded
     authoring candidate adopter. Do not claim the public wizard emits authoring.
  5. Start retained qualification from actual public Templates 0.11.0 SVG bytes.
     Extend the bounded adopter with that exact baseline and the new managed
     studio/config files; preserve the established 0.10.0 route through its
     supported staged transition. Preserve authored game/source/content files,
     lifecycle provenance and owner guidance. Refuse modified managed-file
     collisions before writes, inject interruption and recover, and restore the
     original managed bytes on explicit rollback.
  6. Distinguish workspace adoption from content migration. Read old document/
     catalog formats through .5's explicit migration; retain originals/exports.
     Package rollback must never reinterpret newer authored content or delete it.
     Report unsupported scene-format downgrade explicitly.
  7. Inspect emitted player artifacts and dependencies: no studio, geometry worker,
     editor initialization or authoring-only npm closure. Confirm the studio uses
     the package worker/resource hashes from the packet. Existing generated root
     build/test and public Preview-A lanes remain valid.

  Evidence: retain one candidate packet linking Rendering merged revision and
  archives, Templates merged source and candidate archive, exact restore/tool/
  npm locks, baseline public archives, generated-file hashes and journey results.
  Map C03–C06 and the scoped M3/M4 authoring obligations to concrete assertions;
  source-only, fixture-only and unavailable observations remain distinct.
  Document commands, sample schemas, rights/notices, adoption and rollback limits.

  Publication boundary: .6 qualifies local installed candidate artifacts only.
  It does not publish Rendering or Templates, update public registry/provider
  pins, activate a lifecycle/default, or close Release B. SVG-PREVIEW-B owns
  coherent producer publication and required feed readback, Templates adoption/
  publication, and public clean/upgrade repetition after its other prerequisites.

  Stop condition: close .6 only after Templates native acceptance/readback and
  all required candidate journeys pass. Repair producer gaps in Rendering and
  receiver gaps in Templates without duplicating completion checkboxes.
  If a required capability or observation remains missing, keep the feature open
  and state the concrete gap; all existing checked boxes alone are insufficient.

After .5 and .6 meet their stated authorities, record SVG-AUTHOR-01 complete at
the source/generated-candidate boundary, with public installation pending
SVG-PREVIEW-B. Update unified section 0 asynchronously after authoritative
readback and before selecting SVG-INPUT-01 under programme-wide authorization.
Do not expand that next feature in this plan.

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
