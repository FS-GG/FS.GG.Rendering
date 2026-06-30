# Phase 1 Data Model: Republish the `game`-Profile-Bearing Template (Feature 222)

No application datastore. The "entities" are the release/coordination artifacts whose state this feature
advances, plus their validation rules and state transitions. (Mirrors Feature 218's model; the
visibility entity is dropped — resolved in 218 — and a content-ancestry rule is added.)

## Entity: Coherent release set

- **Fields**: `version V` (semver, e.g. `0.1.54-preview.1`); members = every `FS.GG.UI.*` package
  **plus** `FS.GG.UI.Template`, all packed at the same `V`; `release-tag` (`fs-gg-ui-template/v<V>` +
  sibling `v*` tags); `base-commit` (the `main` commit the tag points at).
- **Validation rules**:
  - `V > 0.1.53-preview.1` strictly (FR-001). NuGet append-only — never re-tag an existing version.
  - All members share the single `V` (coherence; `publish-packages` packs the whole set at `V`).
  - `git merge-base --is-ancestor b78e72a base-commit` is **true** (FR-002 / SC-002 content gate).
  - The two in-repo pins (`template/base/Directory.Packages.props` `<FsGgUiVersion>` and
    `.template.package/FS.GG.UI.Template.fsproj` `<Version>`, both `0.1.53-preview.1` today) both equal `V`.
- **State transition**: `unpacked (main has b78e72a)` → `tagged (release tag pushed)` →
  `published (feed serves V)`.

## Entity: `FS.GG.UI.Template` package on the org feed

- **Fields**: `package-id` (`FS.GG.UI.Template`); served `versions[]`; `visibility` (org-readable,
  inherited from Feature 218); per-version `contents` (does the packed template expose the `game` choice).
- **Validation rules**:
  - After publish, `versions[]` includes `V > 0.1.53-preview.1` (FR-003).
  - `V` is installable by an ordinary `packages: read` token with no special grant — no exit 103
    (FR-003 / SC-001).
  - `V`'s `contents` expose the `game` profile choice (scaffold-selectable; FR-004 / SC-002).
- **State transition**: `serving {…, 0.1.53-preview.1}` → `serving {…, 0.1.53-preview.1, V}` (append).

## Entity: `fs-gg-ui-template` registry entry (`FS-GG/.github`)

- **Fields**: `version` / `package-version` / `package-tag`; `profiles[]` with per-profile release-state
  (incl. `game`); `coherence` entry; the `docs/registry/compatibility.md` projection.
- **Validation rules**:
  - `version` / `package-version` / `package-tag` advance to `V` (FR-006).
  - `game` profile reads **released** at `V` (was UNRELEASED until republish; FR-006 / SC-004).
  - `coherence` entry flipped; the projection names `V` (no stale `0.1.53-preview.1` for this surface).
  - The flip **follows** a confirmed feed listing of `V` (FR-007 ordering).
- **State transition**: `game = UNRELEASED @ 0.1.53-preview.1` → `game = released @ V` (only after the
  feed serves `V`).

## Entity: Generated `game` product (evidence instance)

- **Fields**: scaffolded from `FS.GG.UI.Template@V` with the `game` profile; the minimal replaceable
  Pong-style MVU starter (`Model`/`Msg`/`update`/`view` + tick); its `GovernanceTests`.
- **Validation rules**:
  - The `game` choice is accepted (no missing-profile / unknown-choice error; FR-004).
  - No-flag launch renders a live interactive game scene (no `-- pong`-style flag; US1).
  - Builds and passes governance with **zero** `GovernanceTests` edits (FR-004 / SC-003 — family-agnostic
    entrypoint assertion from Feature 220).
  - The four non-`game` profiles unaffected: `app` → controls showcase; `headless-scene`/`governed`/
    `sample-pack` byte-identical to Feature 220's diff-verified baseline (FR-005 / SC-003).
- **State transition**: N/A (a per-release verification instance, not durable state).

## Entity: Coordination item #33 + linked issues/board

- **Fields**: issue `#33` (`Ready`, Rendering-owned, `cross-repo`+`contract-change`, Phase P1 Rendering,
  Workstream Composition); board item `#33`; the `Blocked by: FS.GG.Rendering#33` mirror on item `#31`;
  downstream `SDD#44`.
- **Validation rules**:
  - #33 closed with the published version string **and** the registry PR link recorded (FR-008 / SC-005).
  - Board item #33 → `Done`; #31 no longer `Blocked by` an open #33 (FR-008 / SC-005).
  - SDD#44 notified of the published version (FR-009 / SC-005).
  - Closure follows the registry flip (a `contract-change` item's resolution includes the registry update).
- **State transition**: `#33 Ready / #31 Blocked` → `#33 Done (closed) / #31 unblocked / SDD#44 notified`.

## Cross-entity ordering (the critical path)

```
main has b76→b78a72a  ──▶  bump pins to V, push release tag-set
                                   │  release.yml publish-packages
                                   ▼
                       feed serves V (coherent set)        ── FR-001/002/003
                                   │  live probe: no 103, game scaffold ok, governance green
                                   ▼
                       registry flip UNRELEASED→released @ V  ── FR-006/007 (publish-BEFORE-flip)
                                   │
                                   ▼
              close #33 (+version,+PR) · board #33 Done · #31 unblocked · notify SDD#44  ── FR-008/009
```
