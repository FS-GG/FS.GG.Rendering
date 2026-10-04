# SVG capability, consumer and instance contract inventory

Date: 2026-10-04. Owner: Rendering. Scope: SVG-COHERENCE-01.1 source inventory and
provisional contract decision. No runtime qualification, publication or product adoption is established.
The [owning plan](https://github.com/FS-GG/.github/blob/main/docs/roadmaps/svg-coherence-and-instancing-01.md)
remains authoritative. This report extends, rather than replaces, the
[FABLE-ADOPT-01 inventory](https://github.com/FS-GG/.github/blob/main/docs/roadmaps/evidence/fable-adopt-01.1-inventory-disposition-20261001.md).

## Observed revisions and package boundaries

The initial clean local snapshots below were read, then protected GitHub `main` refs were
read with `ls-remote` during the same window. The refresh is source evidence only; the integrator
owns delivery and any native acceptance. No checkout or active consumer worktree was changed.

| Repository | Source revision | Package observation and limit |
|---|---|---|
| Rendering | `16da43f37de9f0eea0f4a65a67e6dee44c36ef1c` | Assigned producer source; no newly packed or published artifact |
| Game | `8de4c2747d40e9993cc9a08cd50e1e2d599f69fb` | Game.Render lock resolves Scene 0.31.0; package availability is not an active product call |
| Templates | `96b9d01475935ea3f474cfd4ed4dd93b4755f726` | SvgFoundation and Studio project/lock select Scene/SvgBrowser 0.31.0 |
| SC2 | `e521fa9dc5a910d9e1f201686e126a0e9edc736e` | Browser project references Fable and product Contracts; no Scene/SvgBrowser reference |
| FourD | `af32cea8864bcc4c970fb8457ef2cde88011e829` | Browser project references product Core and Fable.Core; no Scene/SvgBrowser reference |
| FSBarV2 historical checkout | `9921f038d07f27ea0e1647ae40e71a73e7e69780` | Broker.Viz uses SkiaViewer 1.1.3; no BARC browser tree at this local HEAD |
| BAR browser inventory revision | `bcba08f6dcc100d0bfcde4c9416a8f1101dedb82` | Git object read directly from prior inventory; Browser.Client references Fable.Core 5.3.0/Elmish 5.0.2, no Scene. Current protected consumer head remains integrator readback |
| HighBarV3 | `14b5ce74244e0a10fd1468a57299f58befd66280` | Engine/gateway and F# clients; no browser SVG presentation identified in inspected source tree |
| S.I.R. (read-only) | `80e1ac9328865ec8d1ee3eeea130560ef22b1b01` | Product Feliz SVG and glyph catalog; no writes or adoption proposed |

Protected ref refresh: Rendering `16da43f37de9f0eea0f4a65a67e6dee44c36ef1c`, Templates
`96b9d01475935ea3f474cfd4ed4dd93b4755f726` and FourD
`af32cea8864bcc4c970fb8457ef2cde88011e829` remain at the initial snapshots. Game moved to
`75df459ebe7dae755eacba3850a5074eba9ae3c2`; its entire Game.Render subtree and Game.Core
SpatialGrid.fs are byte-identical to the initial revision. SC2 moved to
`827377e5c8504a410676ede265745c1a5d6a3004`; Live.fs, Tactical.fs, browser project and lock
are byte-identical to the initial revision. Their capability classifications therefore remain valid.

The canonical FS-GG/FSBarV2 protected ref is `fb00d0f77b6c7d0998f85a2039931c0c5eed2472`.
The historical checkout's origin is EHotwagner/FSBarV2, so its old origin ref is not the product's
protected authority. The exact FS-GG object was fetched without changing refs or the checkout.
Compared with the prior `bcba08f6` inventory, BarcPreview.fs, runtime.js, live-runtime.js and the
receiver adapter are byte-identical. Browser.Client's project now sets
`DisableImplicitLibraryPacksFolder=true` and its FSharp.Core 10.1.401 content hash changes
without a version change; this packaging change does not add
Scene/SvgBrowser adoption. Current caller links below use the freshly observed protected heads;
initial revisions remain retained above.

Existing clean and retained public workspaces at `svg-workspace-public-{clean,retained}-20260914`
have SvgFoundation/Studio locks resolving Scene/SvgBrowser 0.31.0. Both Tactical example locks
still resolve Scene 0.30.0. These are installed-directory observations, not a new clean build,
upgrade acceptance or proof that the current Templates source generated those directories.

## Capability and caller map

Source links below are immutable. Relative producer links are anchored to the Rendering revision
above, including tests; they describe inspected source, not test execution in this window.

| Capability and owner | Active source/caller and classification | Tests, disposition and compatibility |
|---|---|---|
| Portable document, asset metadata, affine transforms, definitions and instances — Scene | [SvgDocument.fsi](../../src/Scene/SvgDocument.fsi), [implementation](../../src/Scene/SvgDocument.fs): shared/adopted in Templates PreviewDocument, otherwise shared/unadopted in selected products | [SvgDocumentTests](../../tests/Scene.Tests/SvgDocumentTests.fs) cover reference cycles, limits, invalid viewport, export and atomic replacement. Retain accepted schema, semantic IDs and namespaced IDs; asset descriptors already contain ID/version/hash/license |
| Identified document browser — Scene.SvgBrowser | [SvgBrowser.fs](../../src/Scene.SvgBrowser/SvgBrowser.fs#L421) exports and parses a complete candidate before keyed reconciliation. [Templates Program](https://github.com/FS-GG/FS.GG.Templates/blob/96b9d01475935ea3f474cfd4ed4dd93b4755f726/templates/fs-gg-fable-game/SvgFoundation/Program.fs#L233) calls mountDocument/Replace: shared/adopted | [Browser fixture](../../tests/Scene.SvgBrowser.Tests/Program.fs), [browser assertions](../../tests/Scene.SvgBrowser.Tests/browser-test.mjs). Preserve invalid-input atomicity and DOM identity; full export remains independent of viewport culling |
| Retained Scene, interaction and accessible controls — Scene/SvgBrowser | [RetainedSvg.fsi](../../src/Scene/RetainedSvg.fsi), SvgBrowserHost.Dispatch; [Templates Player](https://github.com/FS-GG/FS.GG.Templates/blob/96b9d01475935ea3f474cfd4ed4dd93b4755f726/templates/fs-gg-fable-game/SvgFoundation/Program.fs#L87), TacticalExample, ArcadeExample and FourDReference mount shared hosts: shared/adopted | [retained tests](../../tests/Scene.Tests/SvgFoundationRetainedTests.fs), browser assertions. Retained Scene objects hold Scene content, not SymbolInstance; identified-document and retained paths are distinct existing surfaces |
| Input, animation, local session and external presentation — SvgBrowser | [SvgInputHost](../../src/Scene.SvgBrowser/SvgInputHost.fsi), [SvgAnimationHost](../../src/Scene.SvgBrowser/SvgAnimationHost.fsi), [SvgSessionHost](../../src/Scene.SvgBrowser/SvgSessionHost.fsi), [SvgExternalSessionHost](../../src/Scene.SvgBrowser/SvgExternalSessionHost.fsi). Templates Player/Arcade/FourDReference use local host; [ExternalAuthorityReference](https://github.com/FS-GG/FS.GG.Templates/blob/96b9d01475935ea3f474cfd4ed4dd93b4755f726/templates/fs-gg-fable-game/SvgFoundation/ExternalAuthorityReference.fs#L109) uses external host: shared/adopted reference, unadopted SC2/BAR | [policy tests](../../tests/Scene.SvgBrowser.Policy.Tests/ExternalSessionPolicyTests.fs), [model correspondence](../../tests/SvgSessionCorrespondence/Program.fs). FABLE-ADOPT F12 historical missing seam now has producer/reference source; this does not establish product/native acceptance |
| Studio/authoring/workspace — Scene/SvgBrowser | [SvgAuthoring](../../src/Scene/SvgAuthoring.fsi), [SvgWorkspace](../../src/Scene/SvgWorkspace.fsi), [SvgStudio](../../src/Scene.SvgBrowser/SvgStudio.fsi); Templates Studio Program calls shared Studio: shared/adopted reference | [authoring](../../tests/Scene.Tests/SvgAuthoringTests.fs), [workspace](../../tests/Scene.Tests/SvgWorkspaceTests.fs), [studio browser checks](../../tests/Scene.SvgBrowser.Tests/studio-test.mjs). Retain saves/unknown-version refusal and font/asset provenance |
| Visual working set — Scene | [SpatialWorkingSet](../../src/Scene/SpatialWorkingSet.fs#L203), [Templates ScalePlayer](https://github.com/FS-GG/FS.GG.Templates/blob/96b9d01475935ea3f474cfd4ed4dd93b4755f726/templates/fs-gg-fable-game/SvgFoundation/ScalePlayer.fs#L44): shared/adopted only where directly called | [Spatial tests](../../tests/Scene.Tests/SpatialWorkingSetTests.fs). Stable ordering and pinned selection/focus are obligations; other Player screens do not inherit culling by package reference |
| Pure Game render edge and simulation spatial index — Game | [Adapter.fs](https://github.com/FS-GG/FS.GG.Game/blob/75df459ebe7dae755eacba3850a5074eba9ae3c2/src/Game.Render/Adapter.fs) maps points/cells/routes into Scene. [SpatialGrid](https://github.com/FS-GG/FS.GG.Game/blob/75df459ebe7dae755eacba3850a5074eba9ae3c2/src/Game.Core/SpatialGrid.fs) serves simulation queries: product/simulation-specific | Optional Game.Render reuse only when product primitives fit. Never replace authoritative collision/visibility with visual bounds or place a second clock around native engines |
| SC2 Live/Tactical projection — SC2 | [Live.fs](https://github.com/FS-GG/FS.GG.SC2.Client/blob/827377e5c8504a410676ede265745c1a5d6a3004/src/SC2.Client.Browser/Live.fs#L543) clears units/recreates circles; [Tactical.fs](https://github.com/FS-GG/FS.GG.SC2.Client/blob/827377e5c8504a410676ede265745c1a5d6a3004/src/SC2.Client.Browser/Tactical.fs#L275) does likewise: shared/unadopted infrastructure plus product-specific disclosure/gateway | Product Browser.Tests realtime/tactical reconnect and authority tests remain gates. FABLE-ADOPT .6 owns migration; scene mapping must preserve observed identities, topmost hit policy, reconnect and command receipts |
| BAR JavaScript/Fable browser projection — BAR | [BarcPreview.fs](https://github.com/FS-GG/FSBarV2/blob/fb00d0f77b6c7d0998f85a2039931c0c5eed2472/src/Broker.Browser.Client/BarcPreview.fs#L25) mounts product runtime; [runtime.js](https://github.com/FS-GG/FSBarV2/blob/fb00d0f77b6c7d0998f85a2039931c0c5eed2472/src/Broker.Browser.Client/runtime.js#L397) creates product SVG; [live-runtime.js](https://github.com/FS-GG/FSBarV2/blob/fb00d0f77b6c7d0998f85a2039931c0c5eed2472/src/Broker.Browser.Client/live-runtime.js) owns external live projection: shared/unadopted infrastructure, product-specific authority | Receiver adapter imports exact product archive; generated public scaffold is a generated projection, not producer adoption. Preserve package closure, gateway/guest ABI and stock/native qualification under FABLE-ADOPT .6. Historical Broker.Viz Skia path is a separate intentionally retained desktop route |
| FourD grid, forms and projection — FourD | [interaction.mjs](https://github.com/FS-GG/FS.GG.FourD/blob/af32cea8864bcc4c970fb8457ef2cde88011e829/web/interaction.mjs#L85) creates gridcell buttons across z/w slices; [BrowserBridge](https://github.com/FS-GG/FS.GG.FourD/blob/af32cea8864bcc4c970fb8457ef2cde88011e829/src/FS.GG.FourD.Browser/BrowserBridge.fs) supplies product projection: product-specific plus shared/unadopted presentation seam | Product kernel/tutorial/skirmish tests and retained-save qualification remain gates. FABLE-ADOPT .5 owns first slice. Accessible DOM grid/forms are deliberate controls; their existence alone is not a renderer defect |
| S.I.R. glyph and tactical semantics — S.I.R. | [UnitGlyphCatalog](https://github.com/FS-GG/S.I.R/blob/80e1ac9328865ec8d1ee3eeea130560ef22b1b01/src/SIR.Client/UnitGlyphCatalog.fs#L37), [App glyphView](https://github.com/FS-GG/S.I.R/blob/80e1ac9328865ec8d1ee3eeea130560ef22b1b01/src/SIR.Client.Web/App.fs#L2896), TacticalUnitSymbolView: read-only product-specific characterization | Existing replay/battlefield and tactical browser tests remain S.I.R.-owned. Historical review SVG is an artifact, not current runtime or generic adoption evidence. No source/assets changed |

## Provisional shared instance contract

Retain the existing `SvgDocument`, `SvgDefinitionContent.Symbol`, `SvgElementContent.SymbolInstance`,
`SvgAffine`, `SvgPresentation` and semantic IDs. An instance is an existing SvgElement referencing a
symbol definition; asset identity/version/hash/license already fit SvgAssetDescriptor. Do not introduce
a parallel graph or promise a new public method before .3 prototypes.

The proposed update envelope distinguishes four independent facts: immutable asset revision;
opaque instance ID; product authority epoch/revision; and presentation revision. External host
mount generation/acquisition ID remain lifecycle fences, not replacements for asset revision.
Camera changes remain presentation-only. Ordered commands and required completions never coalesce;
replaceable pose/paint snapshots may coalesce within the admitted epoch.

Prototype bounded create/remove and transform/visibility/presentation changes on existing identified
instances, plus explicit definition replacement invalidating only dependents. Preserve document
validation, limits, atomic rejection, semantic order, full export and mount/dispose ownership. Do not
claim parent paint overrides recolor paths that already bind explicit child paint: qualify intended
inheritance and supported override points through the real glyph. Product health/disclosure/faction
rules and collision geometry stay outside Rendering; dynamic labels/bars may be separate semantic
layers after equivalent-output research.

## Narrow producer research boundaries

These are source-demonstrated costs and missing qualification, not measured performance failures.

| Boundary | Smallest reproducible comparison for .3 |
|---|---|
| Full candidate path | One symbol, 100 instances; change one transform via document Replace. Record export/parse/reconcile cost separately, check unchanged definition and unrelated node identity, compare expanded geometry and bounded-delta prototype |
| Sibling matching | Reorder keyed instances with identical output and reverse/random order; reconciler at SvgBrowser.fs line 58 uses repeated `ac.find` and writes all candidate attributes. Compare indexed matching and skipped equal attributes with ordering/identity witnesses |
| Visual entry lookup | Same visible set at start/middle/end of progressively longer input; SpatialWorkingSet uses list indexing, traversing Length and bucket append. Compare array/count/bucket construction while preserving order, pinned IDs and query results |
| Interaction lookup | Dense selectable instances with focus/selection updates; SvgBrowser.fs line 471 scans selectable objects. Measure revision-scoped identity lookup and accessibility synchronization, preserving offscreen selection and disposal |
| Workload coverage | Existing ordinary/dense shared symbol is a circle; continuous timing drives camera. Add independent 10/50/100% model pose updates, definition diversity/replacement and churn. Count source nodes and expanded geometry separately; GPU remains unknown until .7 |

No consumer incident currently supplies a failing producer reproducer for a renderer rewrite.
Two meaningfully different consumers must qualify any generalized extraction: Templates local-session
reference and an admitted external-authority SC2/BAR slice under existing product ownership.

## Representative asset recommendation

The real product rifleman is available in S.I.R.'s immutable UnitGlyphCatalog above and rendered by
App.glyphView. Its normalized 24×24 art is two paths: one filled chevron and one stroked rifle line,
with 9 explicit path commands (2 moves, 6 lines and 1 close).
It has 2 leaf nodes under one group, depth 2 including that group, no definitions/masks/clips and
no embedded text. Path strings joined with one newline and no trailing newline have SHA-256
`3e8212a74c065ed837eda6effa56b594e3c5f27a8bdde018b883266657aba4a1`.
Catalog file SHA-256 is `3ff311aef068da1f19bfcf9bfc39eeacb6aca6db203c7a14e37c5235c9e8597c`.

The historical [accessible review SVG](https://github.com/FS-GG/S.I.R/blob/80e1ac9328865ec8d1ee3eeea130560ef22b1b01/docs/assets/svg-player-review/accessible-default.svg)
contains a selected rifleman unit with 24 total nodes: 3 groups, 14 rectangles, 2 paths,
2 circles, 1 polygon, 1 line and 1 text. It adds health, facing, weapon direction,
selection/focus and accessible naming to the same art. Full artifact SHA-256 is
`cb14eb9153162096d8461087a2db27644583e57e0a36856ebdbc13f2a87b9f40`.
This is a realistic tactical composition control, not a detailed articulated soldier model.

S.I.R.'s [LICENSE](https://github.com/FS-GG/S.I.R/blob/80e1ac9328865ec8d1ee3eeea130560ef22b1b01/LICENSE)
is GNU AGPL version 3; no narrower permissive glyph license was found. The programme integrator selected this exact
glyph and historical composition for isolated private research, retaining the original license and
provenance. They are excluded from published producer package payloads. This selection establishes
the bounded fixture disposition, not a license change or S.I.R. adoption. If the selected product requires a detailed humanoid or articulated art, obtain and freeze
that actual target asset as a second complexity case; this catalog does not establish that coverage.
The circle remains the historical low-complexity control, never the representative substitute.
No S.I.R. asset was copied by this inventory window; fixture staging belongs to the next research window.

## Next executable boundary and validation

The .2 owner can prepare immutable manifest slots for source/package/bundle/asset/workload hashes,
node/path/segment/nesting/definition complexity and independent instance-update distributions.
The selected S.I.R. fixture can be staged privately with its original license/provenance before .3;
freeze versioned command streams/seeds and asset hashes before measurement.
Then allocate the serialized container browser/CLR window and execute the small comparisons above;
this inventory itself needs no build, browser, server, host visit or GPU operation.

Validation in this window: exact source/lock inspection, asset XML structural counts, whitespace and
local producer/backlink resolution checks. Existing tests are cited but were not run. Public types,
detailed articulated asset coverage, package publication, installed upgrades and native
consumer/GPU acceptance remain open under their existing owners.

## Proposed .3 screening window

Admission: wait for the .2 owner's first successful browser validation of the existing page queue,
checkpoint/export and identity/lifetime/refusal controls. Root then allocates one serialized container
runtime window. Use .2's immutable manifest and combined results format, preserve its existing
specimens and historical software profile, and label acceleration/driver/GPU unknown. No Scene
implementation is admitted by this proposal.

Start with three independent measured repetitions per included cell, with warm-up and fixed bounded
duration declared in the workload manifest. Alternate variant order. Capture raw browser update CPU
and rAF samples, cold-load separately, node/geometry counts, accepted output/identity and lifecycle
witnesses. Freeze percentile method and command seed before runs. A negative control must fail the
appropriate gate before any positive timing is accepted.

| Experiment | Bounded cells and purpose |
|---|---|
| Baseline geometry | Circle control and selected rifleman symbol at 1, 100, 500 instances, static; shared-symbol versus equivalent expanded paths. Keep the selected 24-node tactical composition as a separately named appearance cell at 100 instances |
| Independent motion | Selected rifleman at 100 and 500 instances; 10% and 100% transform changes through existing document replacement; compare camera-only with instance motion using identical seeded routes. Add 50% only if screening leaves a nonlinear question |
| Definition diversity | 100 instances of one definition versus 100 distinct definition identities with equivalent geometry, plus one shared definition replacement. Report validation/export/parsing/reconciliation and dependent changes separately |
| Index input order | Constant 100 visible entries in 1,000 and 10,000-entry worlds, visible set at beginning/end and shuffled; query parity, pinned identities and input-order costs. Middle-order cell follows only if edge results differ |
| Churn/interaction | 100 instances; bounded spawn/remove burst and selection/focus during motion. Check stale epoch refusal, retained offscreen focus/export and final owned listeners/frames/definitions |

Use existing API surfaces for baseline data first. If .2 does not yet expose a listed workload, record
that capability gap and extend only its fixture/workload seam under the harness owner's touch-set;
do not silently synthesize a new producer API. Detailed humanoid/articulated pose, higher-count
saturation, 1,000 visible instances, combined masks/effects and GPU claims remain explicitly outside
this first screening window. Their omission limits the claims, not the owning plan's later matrix.
A measured dominant cost plus equivalent-output evidence selects a later prototype; no optimization
is accepted from source hypotheses alone.
