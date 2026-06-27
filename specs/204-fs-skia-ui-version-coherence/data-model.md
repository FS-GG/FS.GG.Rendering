# Phase 1 Data Model: fs-skia-ui-version Coherence

These are the entities the feature reasons about. They are not F# types (no new public surface) —
they are the artifacts and records whose *states* this feature drives from incoherent → coherent.

## Entity: `fs-skia-ui-version` contract

The cross-repo agreement binding the template's `FsSkiaUiVersion` pin to a coherent `FS.GG.UI.*`
package set.

| Field | Meaning | Source of truth |
|-------|---------|-----------------|
| `coherent` | Boolean — is the contract safe to depend on? | `FS-GG/.github` → `registry/dependencies.yml` (+ `docs/registry/compatibility.md` projection) |
| `tracking` | Link to the request issue | the registry row |
| `pinned-version` | The single `FsSkiaUiVersion` value | `template/base/Directory.Packages.props` |
| `snapshot` | The immutable record the pin refers to | git tag + `contracts/snapshot-manifest.md` + lockfile |

**State transition** (the whole feature): `coherent: false` (open request) → *[US1+US2 verified]* →
`coherent: true` (request closed with `## Response`). The transition is **one-directional and gated**:
it may fire only when every US1/US2 acceptance scenario holds (FR-007). If a reproducible snapshot
cannot be produced, it stays `false`.

## Entity: Pinned package set (template)

The `FS.GG.UI.*` `<PackageVersion>` entries in `template/base/Directory.Packages.props`, all bound to
`$(FsSkiaUiVersion)`.

**Validation rules**:
- Every pinned ID MUST correspond to a package that actually ships (exists in the feed at the pinned
  version). *Current violation*: `FS.GG.UI.Color` (retired, `IsPackable=false`) and
  `FS.GG.UI.SkillSupport` (no producer) are pinned but do not exist → **remove**.
- Every ID the seed `<PackageReference>`s MUST resolve to the single pinned version with no conflict.
- Exactly one FS.GG.UI version *literal* exists — the `$(FsSkiaUiVersion)` value; all pins reference
  the property (SC-003, FR-004).

## Entity: Reproducible snapshot

The immutable record the pin refers to (US2). Composed of:

| Part | Artifact | Reproducibility role |
|------|----------|----------------------|
| Source tag | annotated git tag `fs-skia-ui/v<version>` at the resolution commit | re-checkout the framework source that packs the set |
| Manifest | `contracts/snapshot-manifest.md` — 16 `FS.GG.UI.*` IDs @ `<version>` | human-readable record the registry/issue reference |
| Lockfile | committed `packages.lock.json` + `RestoreLockedMode` | byte-reproducible *restore* of the resolved graph |

**Validation rule**: restoring the pinned template from a clean state twice yields the identical
resolved `FS.GG.UI.*` set (SC-002), and every package in the set carries the same `<version>` (US2 AS1).

## Entity: Per-profile verification run

One `generate → restore → build → evidence` execution per profile.

| Field | Meaning |
|-------|---------|
| `profile` | one of `app`, `headless-scene`, `governed`, `sample-pack` |
| `restore-ok` | no NU1101 (missing package) and no version conflict |
| `build-ok` | no compile error attributable to Scene-API drift |
| `evidence-ok` | the product emits its expected scene/evidence output; governance green |

**Validation rule**: the contract is coherent only when **all four** profiles have
`restore-ok ∧ build-ok ∧ evidence-ok` under the single pin (SC-001; edge case — partial success does
not justify a flip).

## Entity: Cross-repo request `FS-GG/FS.GG.Rendering#1`

| Field | Initial | Target |
|-------|---------|--------|
| `state` | OPEN | CLOSED |
| `## Response` | absent | present — names option taken (git tag + lockfile) + links evidence |
| labels | `cross-repo`, `cross-repo:request`, `blocked` | request resolved (blocked removed / closed) |

**Validation rule**: the `## Response` + close MUST agree with the verified build/snapshot evidence and
MUST NOT precede US1/US2 (FR-006, FR-007).

## Entity: Compatibility registry (`FS-GG/.github`)

`registry/dependencies.yml` (authoritative) + `docs/registry/compatibility.md` (projection). The two
MUST be updated **together** so they never disagree (edge case "registry and issue disagree").

## Cross-entity invariant (the coherence loop)

```
pinned-version  ==  snapshot.version  ==  every-feed-package.version (for the 16 real IDs)
        ∧  all-four-profiles: restore-ok ∧ build-ok ∧ evidence-ok
   ⇒  registry.coherent = true  ∧  issue#1 = CLOSED(## Response)
```

The implication is **only** valid left-to-right and only when the left side fully holds. The registry
row, the issue, the manifest, and the pin must all name the same version; any disagreement means the
set is not coherent and the flip is premature.
