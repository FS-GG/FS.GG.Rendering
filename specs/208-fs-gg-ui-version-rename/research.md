# Phase 0 Research: Rename fs-skia-ui Version Machinery to fs-gg-ui

All Technical Context unknowns are resolved below. This is a rename of existing, well-understood
surfaces; "research" here is confirming the exact mechanism and boundaries rather than evaluating
new technology.

## R1 — Clean break vs. compatibility aliases

- **Decision**: Clean break. No alias for the old property name (`FsSkiaUiVersion`) and no legacy
  `fs-skia-ui/v*` tag retained. Pre-rename products migrate by editing one property.
- **Rationale**: ADR-0003 and issue #3 mandate a clean break. An MSBuild alias (e.g. defining
  `FsSkiaUiVersion` from `FsGgUiVersion`) would re-introduce the legacy identity into the file authors
  edit most and create a second place the version could appear — directly violating the single-source
  invariant (FR-002) the rename is meant to keep coherent. The legacy identity must stop surfacing.
- **Alternatives considered**: (a) MSBuild property alias — rejected: keeps the old name visible and
  risks two literals. (b) Dual tags (`fs-skia-ui/*` + `fs-gg-ui/*`) — rejected: FR-005 requires the
  legacy namespace return zero tags so there is one unambiguous place to look.

## R2 — Atomic property rename (avoid the half-renamed-tree failure)

- **Decision**: Rename the literal, all 13 `$(FsSkiaUiVersion)` pins, the `build.fsx` resolver regex
  (`<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>`), and the `GovernanceTests` single-source assertion
  **together in one commit**, then verify by generate→restore→build before anything else.
- **Rationale**: CPM resolves `$(FsGgUiVersion)` from the literal of the same name. If the literal is
  renamed but any pin still reads `$(FsSkiaUiVersion)`, that pin resolves to an **undefined property**
  (empty string) and restore fails fast — the Edge Case the spec calls out. Doing it atomically and
  immediately running a real restore is the only way to catch this; a text grep for "no
  `FsSkiaUiVersion`" passes on a tree that still won't restore (e.g. if a literal were left but a pin
  changed). The single-source invariant test exists precisely to fail loud here (FR-003).
- **Alternatives considered**: Rename literal first, pins later — rejected: leaves an
  intermediate non-restorable tree and risks shipping a partial rename.

## R3 — Re-tag mechanism (reproducibility preserved)

- **Decision**: Create `fs-gg-ui/v0.1.50-preview.1` at commit `57be86c` and
  `fs-gg-ui/v0.1.51-preview.1` at commit `d9f4c81` (the exact commits the existing `fs-skia-ui/v*`
  tags point at), as annotated tags carrying the same snapshot meaning, then delete the two legacy
  tags. Push the new tags and the deletions.
- **Rationale**: FR-004 requires the snapshot for each previously published coherent version be
  findable under the new namespace and resolve to the **same commit**; pinning the new tags to the
  recorded commits guarantees a reproducibility lookup is byte-identical to before the rename. The
  existing tags are annotated (`coherent FS.GG.UI.* snapshot …`); the new tags preserve that.
- **Verification**: `git tag -l 'fs-gg-ui/v*'` lists exactly the two; `git tag -l 'fs-skia-ui/v*'`
  returns nothing (FR-005); `git rev-list -n1 fs-gg-ui/v<V>` equals the pre-rename commit.
- **Alternatives considered**: `git tag <new> <old>` then keep `<old>` — rejected (FR-005 clean
  break). Lightweight tags — rejected: lose the annotated snapshot subject the existing tags carry.

## R4 — Doc-sweep boundary (what is current guidance vs. immutable history)

- **Decision**: Sweep `FsSkiaUiVersion`/`fs-skia-ui` only from **currently-shipped guidance** — the
  property-surface READMEs, `PROVENANCE.md`, `template/base/README.md`, `template/base/docs/UPGRADING.md`,
  and the 9 `src/*/README.md`. Do **not** touch `specs/**` (history, FR-009) or the package-rebrand
  provenance docs (`docs/product/decisions/0001-package-identity.md`,
  `docs/audit/mechanism-inventory.md`, `docs/bridge/package-identity-migration.md`).
- **Rationale**: FR-007 enumerates exactly the shipped surfaces; FR-009 forbids rewriting history.
  The `fs-skia-ui` strings in the decisions/audit/bridge docs describe the *prior* `FS.Skia.UI →
  FS.GG.UI` package + `dotnet new` short-name rebrand (Feature 008) and are correct as historical
  record — editing them would falsify provenance. The distinguishing test: a reference is in scope
  only if it names the **version property** or the **snapshot tag namespace** as *current* guidance.
- **FR-008 migration note**: `UPGRADING.md` is both swept and extended — it must tell an author of a
  pre-rename product to rename `FsSkiaUiVersion → FsGgUiVersion` when adopting the bumped template.
- **Alternatives considered**: Global find/replace of `fs-skia-ui` across the repo — rejected:
  rewrites history and provenance, violating FR-009.

## R5 — Template version bump magnitude

- **Decision**: A single preview increment of the template/base `<Version>` (the exact number chosen
  at implementation/release time). The bump is mandatory because the property rename is breaking for
  generated products.
- **Rationale**: FR-006 requires a bump to signal the breaking change; the Assumptions confirm a
  single preview increment is sufficient and the exact value is a release detail. Consistent with the
  repo's preview-channel versioning used by the coherent set (`0.1.5x-preview.N`).
- **Alternatives considered**: No bump — rejected (FR-006: consumers must see a new template version
  to know the property name changed). Major bump — unnecessary; preview channel is the established
  signal.

## R6 — Cross-repo sequencing (registry ids + ADR + downstream check)

- **Decision**: Execute the registry contract-id rename (`fs-skia-ui-version`/`-bom →
  fs-gg-ui-version`/`-bom` in `registry/dependencies.yml` + `docs/registry/compatibility.md`) and the
  ADR-0003 Proposed→Accepted flip in `FS-GG/.github` via `gh` + the `cross-repo-coordination` skill,
  **after** the in-repo property/tag rename is verified. Verify Templates/SDD carry no
  `FsSkiaUiVersion`/`fs-skia-ui/*` reference; file a cross-repo request only if one is found (FR-011).
- **Rationale**: Cross-repo state is owned by `FS-GG/.github`, not files in this repo, and the
  contract surface should not be flipped to the new ids before the surfaces those ids describe
  actually use the new name (avoids a registry that references a rename not yet shipped). The
  Assumptions state Templates/SDD are expected clean post-Feature-205, so this is a check-with-fallback.
- **Alternatives considered**: Write registry first — rejected: records a contract the repo has not
  yet delivered (FR-010 ties the ADR flip to "on resolution"). Vendoring the registry locally —
  rejected: cross-repo state must not live in this repo (Constitution / cross-repo protocol).
