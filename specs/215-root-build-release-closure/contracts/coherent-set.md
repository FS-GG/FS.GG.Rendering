# Contract: Coherent-set version agreement (Feature 209 staleness guard)

**Owner**: `scripts/validate-version-coherence.fsx` (Feature 209) + `template/base/Directory.Packages.props`
**Satisfies**: FR-004 · SC-003 · US1 (acceptance 4)

The published template version, the org `FsGgUiVersion` line, and the registry coherence-entry version MUST
be the **same** value — `0.1.52-preview.1` — and the matching framework snapshot tag MUST exist, so the
Feature 209 guard reports no straggler.

## The trio (all == `0.1.52-preview.1`)

| Member | Location | Action |
|---|---|---|
| `FsGgUiVersion` | `template/base/Directory.Packages.props:9` | EDIT `0.1.51-preview.1` → `0.1.52-preview.1` |
| Published template version | `FS.GG.UI.Template` on feed + `fs-gg-ui-template/v0.1.52-preview.1` | release.yml `-p:Version` + snapshot tag |
| Registry coherence-entry version | `FS-GG/.github` `registry/dependencies.yml` | pin `version: 0.1.52-preview.1` (PR #25) |
| Framework snapshot tag (guard input) | `fs-gg-ui/v0.1.52-preview.1` | CREATE (guard compares pin to `fs-gg-ui/v*`) |

## Guard invocation + verdict

```bash
# structural verdict
dotnet fsi scripts/validate-version-coherence.fsx
#   0 = coherent (no straggler)   1 = drift (≥1 conjunct false)   2 = guard error (inputs/tags unreadable)

# restore-grounded proof (packs FS.GG.UI.* + BOM at the pin, restores a clean consumer, asserts full set == pin)
FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx
```

Structural conjuncts that must all hold: pin well-formed (single literal); pin matches an existing
`fs-gg-ui/v<V>` tag; pin does not lag the latest `fs-gg-ui/v*` (SemVer preview-aware); BOM exact-bracket token
consistent; the 11-member template pin set == packable `FS.GG.UI.*` set == BOM dependency set; `build.fsx`
regex still matches the literal.

## Ordering invariant

The `fs-gg-ui/v0.1.52-preview.1` tag MUST exist **before** the guard runs, or the bumped pin will (correctly)
be flagged as referencing a non-existent snapshot (exit 1). Create the framework + template snapshot tags as
part of the release, then run the guard.

## Verification

- `dotnet fsi scripts/validate-version-coherence.fsx` exits `0`.
- The restore-grounded smoke (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`) resolves the complete member set to
  exactly `0.1.52-preview.1`.
- The push-gate `version-coherence` step in `.github/workflows/gate.yml` is green on `main` after the bump.
