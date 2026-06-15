# Feature Specification: Rebrand Package Identity (Migration Stage R8)

**Feature Branch**: `008-rebrand-package-identity`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next phase in fs.gg"

## Context

This is the next increment of the FS.GG.Rendering migration. The migration is staged R1 → R8 in
the rendering implementation plan. R1 (fresh repo), R2 (product shape), R3 (validation set), R4
(source import), R5 (test harness), R6 (CI cadence wiring, feature `005`), and R7 (bridge the old
repository, feature `007`) are done. The next — and final planned — stage is **R8 — Decide rebrand
separately**.

Every stage so far deliberately **retained** the imported package identity `FS.Skia.UI.*`. The
constitution states package identity stays `FS.Skia.UI.*` initially and that "any rebrand to
`FS.GG.UI.*` is a separate, explicit release decision, not part of ordinary work." Decision record
[`docs/product/decisions/0001-package-identity.md`](../../docs/product/decisions/0001-package-identity.md)
captured that deferral with status **deferred** and a single revisit trigger: **Stage R8**. R7's
bridge note ([`docs/bridge/package-identity-migration.md`](../../docs/bridge/package-identity-migration.md))
recorded the retained mapping precisely so R8 would have a clean, documented baseline to rename from.

R8 makes that decision and acts on it: **rebrand the product identity from `FS.Skia.UI.*` to
`FS.GG.UI.*`.** This is the explicit release decision the constitution reserved. It is unlike the
preceding documentation stages — it changes **product code**: package IDs, root namespaces, assembly
names, and the `dotnet new` template identity all move to the `FS.GG.UI.*` brand as one coherent
matrix, the imported source tree currently carries the `FS.Skia.UI.` prefix across roughly three
hundred files and ten runtime packages.

Because the rebrand is a release event, consumer continuity is a first-class concern. The
constitution's package-identity constraint requires that a rebrand "publishes replacement packages
before deprecating the old IDs." R8 therefore sequences **publish-before-deprecate**: the new
`FS.GG.UI.*` packages are produced and made available first, and only then are the old
`FS.Skia.UI.*` IDs deprecated with a forward pointer. The actual publish to and deprecation on the
public package feed (nuget.org) is a **release action owned outside this working tree**; like R7's
out-of-repo deliverables, R8 prepares the replacement packages and a copy-ready deprecation notice
plus a recorded action, and never claims the public feed was changed when it was not (Constitution
Principle VI — no overclaiming).

"Users" here are: a **maintainer** who needs the rebrand decision made explicitly and recorded, not
left implicit in a code change; a **downstream consumer** of the existing `FS.Skia.UI.*` packages
who must be able to move to the new identity without being stranded; a **template user** who runs
`dotnet new` and must get a project that restores and builds against the new identity; and a
**future auditor** who must be able to trace the rename across provenance and the bridge.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The rebrand decision is made explicit and recorded (Priority: P1)

As a maintainer, I want the deferred package-identity decision resolved as an explicit "rebrand to
`FS.GG.UI.*`" record — with the full rename matrix and the publish-before-deprecate sequencing
captured — so the brand change is a deliberate, reviewable release decision and not a silent code
edit.

**Why this priority**: The constitution reserved the rebrand as "a separate, explicit release
decision." The decision artifact is the governance keystone of R8: it authorizes every downstream
file change and gives consumers, the template, and provenance a single source of truth for *why* and
*what* changed. The decision is independently valuable — recording it correctly delivers the
"decide" half of "Decide rebrand separately" even before the first file is renamed.

**Independent Test**: Read the package-identity decision record and confirm its status has moved from
*deferred* to an accepted **rebrand** outcome, that it names the old→new identity mapping for every
package and the template, states the publish-before-deprecate rule, and records the chosen starting
version for the new package lineage — with the rationale and revisit conditions intact.

**Acceptance Scenarios**:

1. **Given** decision `0001` (previously *deferred*), **When** R8 is complete, **Then** the record
   shows an accepted **rebrand to `FS.GG.UI.*`** outcome with the date, the rationale, and a complete
   old→new identity mapping.
2. **Given** the decision record, **When** a reader asks how existing consumers are protected, **Then**
   it states that replacement `FS.GG.UI.*` packages are published **before** the old `FS.Skia.UI.*`
   IDs are deprecated, and that old IDs are deprecated (with a forward pointer), not deleted.
3. **Given** the decision record, **When** an auditor checks scope, **Then** it identifies the four
   identity facets that move together (package IDs, root namespaces, assembly names, template
   identity) and the agreed starting version for the new lineage.

---

### User Story 2 - Product code carries the new identity and still builds and validates (Priority: P1)

As a maintainer, I want every runtime package's ID, root namespace, and assembly name changed from
`FS.Skia.UI.*` to `FS.GG.UI.*` as one coherent matrix, with the product still building and the
validation set still passing — so the rebrand is real, complete, and provably non-regressing rather
than a half-applied rename that leaves a broken or confusing surface.

**Why this priority**: This is the functional core of the rebrand. A rename that flips package IDs
but not namespaces (or vice versa) produces a mixed, broken identity surface — exactly the
"mixed, confusing identity" the original decision rejected. The product must demonstrably still
build and pass its existing validations under the new identity for the rebrand to be trustworthy.

**Independent Test**: After the rename, build the solution and run the existing validation set;
confirm a repository-wide search finds no remaining `FS.Skia.UI.*` *identity* tokens in product
source (package IDs, namespaces, assembly names) except where deliberately retained and recorded,
and that public type/member surfaces are unchanged apart from the namespace prefix.

**Acceptance Scenarios**:

1. **Given** the ten runtime modules (`Color`, `Scene`, `Layout`, `Input`, `KeyboardInput`,
   `SkiaViewer`, `Elmish`, `Controls`, `Controls.Elmish`, `Testing`), **When** R8 is complete, **Then**
   each package ID, root namespace, and assembly name reads `FS.GG.UI.<Module>` and no longer
   `FS.Skia.UI.<Module>`.
2. **Given** the renamed source, **When** the solution is built, **Then** it builds successfully and
   the default-tier validation set passes with no new failures attributable to the rename.
3. **Given** the public signature files (`.fsi`) and any visibility baselines, **When** they are
   reviewed after the rename, **Then** the only change is the namespace prefix — no public type or
   member is added, removed, or otherwise altered by the rebrand.
4. **Given** an internal reference to the identity that is **not** a brand-prefix occurrence (for
   example the descriptive `SkiaViewer` module name, which denotes a SkiaSharp-backed viewer, or a
   genuine `SkiaSharp`/`Skia` dependency reference), **When** the rename runs, **Then** that
   descriptive usage is preserved and only the `FS.Skia.UI.` brand prefix is replaced.

---

### User Story 3 - The template instantiates a project on the new identity (Priority: P2)

As a template user, I want the `dotnet new` template and its template package to carry the new
identity, so a freshly generated project references `FS.GG.UI.*` packages and restores and builds
against them — the generated-consumer contract holds under the rebrand.

**Why this priority**: The repository owns the template (decision `0002`), and the template is the
product's generated-consumer check. A rebrand that renames the libraries but leaves the template
emitting `FS.Skia.UI.*` references produces broken or contradictory generated projects. P2 because
it depends on the library rename (Story 2) but is essential to a coherent release.

**Independent Test**: Instantiate a project from the template and run restore + build; confirm the
generated project references `FS.GG.UI.*` packages, the template package identity is
`FS.GG.UI.Template`, and the template's identity parameters/defaults no longer emit the old brand.

**Acceptance Scenarios**:

1. **Given** the template package, **When** R8 is complete, **Then** its package ID is
   `FS.GG.UI.Template` and its identity metadata names the new brand.
2. **Given** the template definition (identity, name, default namespace/parameter values, and any
   `replaces` tokens), **When** a project is generated, **Then** the generated project references
   `FS.GG.UI.*` and contains no `FS.Skia.UI.*` package references.
3. **Given** a generated project, **When** restore and build run against the available
   `FS.GG.UI.*` packages, **Then** they succeed — the generated-consumer contract still holds.

---

### User Story 4 - Existing consumers can move without being stranded (Priority: P2)

As a downstream consumer pinned to `FS.Skia.UI.*` packages, I want the new `FS.GG.UI.*` packages to
exist before the old IDs are deprecated, and the old IDs to be deprecated with a clear pointer to
their replacement — so I am never left on a package that simply vanished with no documented path
forward.

**Why this priority**: Consumer continuity is the whole reason the constitution mandates
publish-before-deprecate. A rebrand that deprecates or unlists the old IDs before the replacements
are available breaks existing consumers. P2 because the new packages (Story 2) must exist first, but
this is what makes the rebrand safe to release.

**Independent Test**: Confirm the replacement `FS.GG.UI.*` packages are produced, and that a
copy-ready deprecation notice (mapping each old `FS.Skia.UI.*` ID to its `FS.GG.UI.*` replacement)
exists as a **recorded action** for the public feed — with the old IDs to be deprecated (not deleted)
and the action clearly marked not-yet-applied where this repo cannot apply it.

**Acceptance Scenarios**:

1. **Given** the rebrand, **When** the sequencing is reviewed, **Then** the replacement
   `FS.GG.UI.*` packages are produced/available **before** any deprecation of the old IDs is
   actioned.
2. **Given** the old `FS.Skia.UI.*` package IDs on the public feed, **When** the deprecation action
   is prepared, **Then** it deprecates each old ID with a forward pointer to its `FS.GG.UI.*`
   replacement and does not delete or unlist it, so existing pins keep resolving.
3. **Given** that the public package feed is owned outside this working tree, **When** R8 reports the
   deprecation, **Then** it is delivered as copy-ready content plus a recorded action and is **not**
   described as already applied to the feed (Constitution Principle VI).

---

### User Story 5 - Provenance, bridge, and docs reflect the rebrand (Priority: P3)

As a future auditor or reader, I want the provenance record, the R7 bridge notes, and the
repository's docs updated to state that identity was rebranded `FS.Skia.UI.*` → `FS.GG.UI.*` at R8 —
so no document still claims the identity was "retained, unchanged" once that is no longer true.

**Why this priority**: R7 deliberately wrote "retained — unchanged by the move" into the bridge note,
PROVENANCE, and the old-repo redirect, with explicit pointers to R8. After the rebrand those
statements are false; leaving them produces exactly the identity confusion the migration worked to
avoid. P3 because it follows the substantive rename, but it closes the loop so the record stays
honest.

**Independent Test**: Search the bridge, PROVENANCE, and docs for "retained"/"unchanged" identity
claims; confirm each is updated to the rebranded reality (or correctly scoped to the historical
import), the old→new mapping is recorded once authoritatively, and cross-references still resolve.

**Acceptance Scenarios**:

1. **Given** the R7 bridge package-identity note (which stated identity was retained), **When** R8 is
   complete, **Then** it reflects that identity was rebranded at R8, with the old→new mapping and the
   import-time retained mapping correctly scoped as history.
2. **Given** `PROVENANCE.md` (which noted "rebrand deferred to Stage R8"), **When** R8 is complete,
   **Then** it records that the rebrand occurred at R8 and maps imported `FS.Skia.UI.*` identifiers
   to their `FS.GG.UI.*` form, while still tracing imported files to their source paths/commit.
3. **Given** any bridge/decision/README cross-reference touched by the rebrand, **When** the docs are
   reviewed, **Then** every in-repo link still resolves and no document presents a stale
   "identity unchanged" claim as current truth.

---

### Edge Cases

- **Partial / incoherent rename**: a package ID renamed without its namespace and assembly name (or
  vice versa) yields a broken, mixed identity surface. The four facets — package ID, root namespace,
  assembly name, template identity — MUST move together as one matrix; a partial state is a failure,
  not an intermediate success.
- **Descriptive "Skia" usage vs. brand prefix**: only the `FS.Skia.UI.` **brand prefix** is rebranded.
  The `SkiaViewer` module name (a SkiaSharp-backed viewer) and genuine `SkiaSharp`/`Skia` dependency
  references are descriptive and MUST be preserved — a blind global replace that mangles them is a
  defect.
- **Old packages deprecated before replacements exist**: deprecating or unlisting `FS.Skia.UI.*`
  before `FS.GG.UI.*` is available strands consumers. Publish-before-deprecate is mandatory; old IDs
  are **deprecated with a forward pointer, not deleted**, so existing version pins keep resolving.
- **Public feed is owned elsewhere**: the actual publish of new packages and deprecation of old IDs
  on nuget.org happens outside this working tree. R8 prepares the packages and a copy-ready,
  recorded action and MUST NOT claim the feed was changed when it was not (Principle VI).
- **Public surface drift hidden inside the rename**: because every `.fsi` and any visibility baseline
  changes its namespace token, an accidental surface change (an added/removed/retyped public member)
  could ride along unnoticed. The review MUST confirm the only difference is the namespace prefix.
- **Internal fixtures and tooling that hard-code the identity**: package-reference / drift-check
  fixtures, tests, CI metadata, template fragments, and **runtime brand string literals** (e.g. the
  `fs-skia-ui` Elmish subscription id and the `fs-skia-ui-runtime` temp-dir name) that literally
  contain `FS.Skia.UI` or the kebab `fs-skia-ui` brand will break or silently keep the old brand
  unless included in the rename surface — and a literal mirrored by a test assertion MUST be renamed
  in lockstep with that assertion (a brand update, not a weakening).
- **Stale retained-identity documents**: R7's bridge note, PROVENANCE, and the old-repo redirect
  assert identity was "retained/unchanged." After the rebrand these become false-as-current and MUST
  be updated or correctly scoped to history, or they contradict the new reality.
- **Version lineage discontinuity**: the new `FS.GG.UI.*` packages are a new ID lineage. Their
  starting version MUST be agreed and recorded (decision `0001`); the old IDs freeze at their last
  `FS.Skia.UI.*` version.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST resolve the deferred package-identity decision
  (`docs/product/decisions/0001-package-identity.md`) to an explicit **accepted rebrand** to
  `FS.GG.UI.*`, recording the date, rationale, the complete old→new identity mapping, the
  publish-before-deprecate rule, and the starting version for the new package lineage.
- **FR-002**: The feature MUST rename every runtime package identity from `FS.Skia.UI.<Module>` to
  `FS.GG.UI.<Module>` across all four facets — **package ID, root namespace, assembly name**, and any
  declared identity metadata — for all ten runtime modules (`Color`, `Scene`, `Layout`, `Input`,
  `KeyboardInput`, `SkiaViewer`, `Elmish`, `Controls`, `Controls.Elmish`, `Testing`) as one coherent
  matrix.
- **FR-003**: The feature MUST rename the `dotnet new` template identity and its template package to
  the new brand (`FS.GG.UI.Template`), including the template's identity, name, default
  namespace/parameter values, and any brand `replaces` tokens, so generated projects reference
  `FS.GG.UI.*` and contain no `FS.Skia.UI.*` references.
- **FR-004**: After the rename, the product MUST build successfully and the existing default-tier
  validation set MUST pass, with no new failures attributable to the rebrand.
- **FR-005**: The feature MUST preserve the public API surface unchanged apart from the namespace
  prefix — no public type or member is added, removed, or retyped by the rebrand — and any visibility
  baselines MUST be updated to the new namespace without masking an accidental surface change. Nine of
  the ten modules carry a committed surface baseline; **Color** is intentionally not baseline-tracked
  (it is omitted from `scripts/refresh-surface-baselines.fsx`), so its prefix-only invariance MUST be
  confirmed instead by inspecting that each `src/Color/*.fsi` changes only its `namespace` line.
- **FR-006**: The feature MUST replace only the `FS.Skia.UI.` **brand prefix**; descriptive uses of
  "Skia" that denote the underlying SkiaSharp technology (e.g., the `SkiaViewer` module name, genuine
  `SkiaSharp`/`Skia` references) MUST be preserved.
- **FR-007**: The feature MUST produce the replacement `FS.GG.UI.*` packages **before** any
  deprecation of the old `FS.Skia.UI.*` IDs is actioned (publish-before-deprecate), per the
  constitution's package-identity constraint.
- **FR-008**: The feature MUST produce a **copy-ready deprecation notice** mapping each old
  `FS.Skia.UI.*` package ID to its `FS.GG.UI.*` replacement, to be applied to the public feed as a
  **recorded action**; old IDs MUST be deprecated with a forward pointer (not deleted or unlisted) so
  existing consumer pins keep resolving.
- **FR-009**: Any change destined for a repository or service this feature does not own (the public
  package feed, the archived old repo, the org `.github`) MUST be delivered as copy-ready content with
  a recorded action and MUST NOT be reported as already applied when it has not been (Constitution
  Principle VI: no overclaiming).
- **FR-010**: The feature MUST update the R7 bridge package-identity note, `PROVENANCE.md`, and any
  other repository docs that assert identity was "retained/unchanged" so they reflect the rebrand,
  record the old→new identifier mapping once authoritatively, and scope the import-time retained
  mapping as history.
- **FR-011**: Every in-repo cross-reference touched by the rebrand (bridge, decision records,
  `PROVENANCE.md`, `README.md`) MUST continue to resolve — **no dead in-repo links** introduced by the
  rename.
- **FR-012**: The feature MUST record the new package lineage's starting version and that the old
  `FS.Skia.UI.*` IDs freeze at their last published version, so the version history of each brand is
  unambiguous.

### Key Entities

- **Package-identity decision (`0001`)**: the governance keystone — moves from *deferred* to
  *accepted (rebrand to `FS.GG.UI.*`)*; holds the old→new mapping, publish-before-deprecate rule, and
  starting version.
- **Runtime package identity**: the four-facet identity (package ID, root namespace, assembly name,
  identity metadata) of each of the ten runtime modules; renamed `FS.Skia.UI.<Module>` →
  `FS.GG.UI.<Module>`.
- **Template identity**: the `dotnet new` template and its template package (`FS.Skia.UI.Template` →
  `FS.GG.UI.Template`), including identity parameters/defaults — validates the generated-consumer
  contract under the new brand.
- **Replacement packages**: the produced `FS.GG.UI.*` artifacts that must exist before old IDs are
  deprecated.
- **Deprecation notice / recorded action**: copy-ready old→new mapping for the public feed, applied
  outside this tree, never claimed as applied here.
- **Provenance & bridge records**: `PROVENANCE.md` and the R7 bridge note — updated from "retained"
  to the rebranded reality while preserving import lineage.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The package-identity decision record shows an accepted **rebrand to `FS.GG.UI.*`**
  outcome (no longer *deferred*) with a complete old→new mapping, the publish-before-deprecate rule,
  and the new starting version — readable as the single authoritative decision.
- **SC-002**: 100% of the ten runtime modules expose package ID, root namespace, and assembly name as
  `FS.GG.UI.<Module>`; a repository-wide search finds zero `FS.Skia.UI.*` **brand-prefix identity**
  tokens in product source except deliberately retained, recorded descriptive usages.
- **SC-003**: The solution builds and the default-tier validation set passes after the rename, with
  zero new failures attributable to the rebrand.
- **SC-004**: A project generated from the template references only `FS.GG.UI.*` packages (template
  package ID `FS.GG.UI.Template`) and completes restore + build — zero `FS.Skia.UI.*` references in
  the generated output.
- **SC-005**: The public API surface differs from before the rebrand only by the namespace prefix —
  zero added, removed, or retyped public members — verifiable against the `.fsi`/baselines (the nine
  baseline-tracked modules via the normalized baseline diff; **Color** via `.fsi` inspection, as it is
  not baseline-tracked).
- **SC-006**: Replacement `FS.GG.UI.*` packages exist before any old-ID deprecation is actioned, and
  the deprecation notice maps every old ID to its replacement with old IDs deprecated (not deleted) —
  zero consumers left without a documented forward path.
- **SC-007**: Every deliverable destined for a repository/service this feature does not own (public
  feed, old repo, org `.github`) is marked as a recorded action with copy-ready content and none is
  described as already applied — zero overclaims.
- **SC-008**: Zero repository documents present a stale "identity retained/unchanged" claim as current
  truth after R8, and every in-repo cross-reference touched by the rebrand resolves — zero dead
  in-repo links.

## Assumptions

- **Stage identity**: "next phase in fs.gg" is read as the next sequential migration stage. R1–R7 are
  complete (R7 = feature `007`, the bridge); the next and final planned stage in the R1→R8 roadmap is
  **R8 — Decide rebrand separately**. (The earlier terse requests "next phase in fs.gg" → R6 and
  "next part of ff.gg" → R7 establish this reading.)
- **Decision outcome is rebrand**: the R8 decision is taken as **rebrand to `FS.GG.UI.*`, decide and
  execute** (confirmed for this spec). R8 both records the decision and applies the rename to product
  code, the template, and docs in this repository.
- **Brand prefix only**: the rebrand replaces the `FS.Skia.UI.` identity prefix with `FS.GG.UI.`.
  Descriptive technology references to Skia/SkiaSharp (including the `SkiaViewer` module name) are not
  brand tokens and are retained.
- **Public feed is external**: publishing the new `FS.GG.UI.*` packages and deprecating the old IDs on
  the public feed (nuget.org) is a release action owned outside this working tree. R8 prepares the
  packages and a copy-ready, recorded action; it does not (and cannot) apply the public-feed change
  itself, and never claims it did (Principle VI).
- **Templates are owned here**: decision `0002` keeps the template co-located with the rendering
  product, so the template rename is in scope for R8 and validated via the generated-consumer check.
- **Constitution governs**: the rebrand follows the package-identity constraint
  (`FS.Skia.UI.*` → `FS.GG.UI.*` is an explicit release decision; publish replacements before
  deprecating old IDs) and Principle VI (no overclaiming on out-of-repo/out-of-feed actions).
- **No behavior change intended**: the rebrand changes identity, not product behavior or public API
  shape. Any behavior or surface change beyond the namespace prefix is out of scope and would be a
  regression.
- **Scope boundary**: R8 covers the identity rebrand and its consumer/template/provenance
  consequences. New product features, further CI changes (settled at R6), and any work beyond making
  the identity coherent under the new brand are out of scope.
