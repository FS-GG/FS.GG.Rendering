# Phase 1 Data Model — Coherence Entities & Lockstep Rules

The guard is a **pure verdict** over repo + git-tag inputs. This file models those inputs as entities,
the relationships that MUST hold (the lockstep invariant), and the validation rules each FR maps to.
No persistent storage; the only output is a `Verdict` value (printed, exit-coded) and a regenerated
readiness report.

## Entities

### 1. SingleVersionSource
The one literal every `FS.GG.UI.*` pin and the runtime engine resolution derive from.

| Field | Source | Example |
|-------|--------|---------|
| `version` | `<FsGgUiVersion>` in `template/base/Directory.Packages.props:9` | `0.1.50-preview.1` |
| `occurrences` | count of `<FsGgUiVersion>…</FsGgUiVersion>` literals | MUST be exactly 1 |
| `wellFormed` | parses as preview-aware SemVer (D7) | `true` |

**Rules**: exactly one literal (FR-005); parseable (FR-007 — a malformed value names itself).

### 2. CoherentSnapshotTag
An immutable marker of a published, internally-consistent `FS.GG.UI.*` set.

| Field | Source | Example |
|-------|--------|---------|
| `tag` | `git tag --list 'fs-gg-ui/v*'` | `fs-gg-ui/v0.1.51-preview.1` |
| `version` | tag suffix after `fs-gg-ui/v` | `0.1.51-preview.1` |
| `isLatest` | highest by preview-aware order among all tags (D7) | computed |

**Rules**: `SingleVersionSource.version` MUST equal some tag's `version` (FR-002 — no phantom) and
MUST NOT be strictly less than `latest.version` (FR-002 — no lag = the 204 case). CI MUST fetch tags
(`fetch-depth: 0`), else this entity is empty and the verdict fails closed, never green-by-absence.

### 3. PublishedMemberSet `P`
The full co-versioned `FS.GG.UI.*` package set the framework publishes.

| Field | Source | Cardinality |
|-------|--------|-------------|
| `members` | packable `FS.GG.UI.*` `.fsproj` under `src/**` (IsPackable=true, PackageId prefix) | 16 |

Reuses the discovery in `scripts/validate-bom-consumer.fsx` (`discoveredMembers`).

### 4. BomDependencySet `B`
The BOM/metapackage's exact-pin dependency list.

| Field | Source | Example |
|-------|--------|---------|
| `ids` | `<dependency id=…>` in `src/Meta/FS.GG.UI.nuspec` | 16 ids |
| `allSingleToken` | every `version` == `[$version$]` | `true` |
| `allExactBracket` | every `version` is `[…]` with no comma | `true` |

**Rules**: `B.ids == P.members` (full-set parity, FR-003); `allSingleToken && allExactBracket`
(FR-004) — checked **structurally**, independent of any consumer warnings-as-errors policy.

### 5. TemplateConsumedPinSet `T`
The `FS.GG.UI.*` pins a generated product consumes.

| Field | Source | Cardinality |
|-------|--------|-------------|
| `pins` | `FS.GG.UI.*` `PackageVersion Include=…` in `template/base/Directory.Packages.props` | 11 |
| `allDerive` | every pin's `Version` is `$(FsGgUiVersion)` (no hardcoded literal) | `true` |
| `expected` | the documented consumed-set manifest (the 11 product-facing members) | fixed list |

**Rules**: `T.pins ⊆ P.members` (FR-003); `allDerive` (FR-005 — a hardcoded pin is a half-bump);
`T.pins == T.expected` (an intended consumed member not silently dropped/added). The 16-vs-11 gap is
intentional (D6).

### 6. RuntimeResolution
The value `build.fsx` reads at runtime.

| Field | Source |
|-------|--------|
| `regex` | `<FsGgUiVersion>([^<]+)</FsGgUiVersion>` in `template/base/build.fsx:60` |
| `resolves` | regex still matches the literal in `Directory.Packages.props` (read against current tree) |

**Rules**: the regex MUST still match (FR-005 — a renamed/half-renamed property breaks runtime
resolution, the 208 half-bump class).

### 7. RestoreProof (live, FR-008)
Grounds the structural facts in a real restore.

| Field | Source |
|-------|--------|
| `feedVersion` | `V` packed from source = `SingleVersionSource.version` |
| `resolvedMembers` | `FS.GG.UI.*` resolved when a clean consumer references `FS.GG.UI@V` |
| `allAtV` | every resolved member == `V` |
| `cleanBuild` | clean consumer builds |

**Rules**: `allAtV` over the **complete** member set (FR-008 — no silent partial graph); reuses
`validate-bom-consumer.fsx`'s clean-consumer layer.

### 8. Verdict (output)
The pure result.

| Field | Type |
|-------|------|
| `ok` | bool (false ⇒ exit non-zero, blocks merge) |
| `failures` | list of `{ location; expected; actual; rule }` — never a bare "incoherent" (FR-007) |
| `provenance` | `verdict-core` (structural) \| `live` (restore-grounded) |

### Explicitly excluded input (documented, D5)
- **RepoRootVersion** = `Directory.Build.props:17` `<Version> = 0.1.0-preview.1` — **NOT** compared.
  Decoupled by default. Listed here so the exclusion is visible; joins the lockstep set only if D5's
  reversal trigger fires (FR-005).

## The Lockstep Invariant

```
SingleVersionSource.version  ==  CoherentSnapshotTag(latest).version        (FR-001/002)
                             ∧  ∃ tag with that version                     (FR-002, no phantom)
B.ids == P.members  ∧  B.allSingleToken  ∧  B.allExactBracket               (FR-003/004)
T.pins ⊆ P.members  ∧  T.allDerive  ∧  T.pins == T.expected                 (FR-003/005)
RuntimeResolution.resolves                                                  (FR-005)
RestoreProof.allAtV over the full member set  ∧  RestoreProof.cleanBuild    (FR-008)
```

Any conjunct false ⇒ `Verdict.ok = false` with a `failure` naming the location expected-vs-actual.

## FR → rule traceability

| FR | Entity / rule |
|----|---------------|
| FR-001 | SingleVersionSource.version == latest tag version |
| FR-002 | CoherentSnapshotTag: exists ∧ not-lagging (preview-aware) |
| FR-003 | `B.ids == P.members` ∧ `T.pins ⊆ P.members` ∧ consumed manifest |
| FR-004 | `B.allSingleToken ∧ B.allExactBracket` — structural, policy-independent |
| FR-005 | `occurrences == 1` ∧ `T.allDerive` ∧ `RuntimeResolution.resolves` |
| FR-006 | Verdict.ok=false ⇒ non-zero exit in `gate.yml` (merge-blocking) |
| FR-007 | Verdict.failures carry `{location, expected, actual}` |
| FR-008 | RestoreProof (live) — complete set resolves to `V` |
| FR-009 | tag-exists ∧ pin==tag — published-but-unpinned / pinned-but-unpublished both fail |
| FR-010 | (process) cross-repo registry note via `cross-repo-coordination` (D8) |

## State transitions (coherence lifecycle)

```
COHERENT ──(framework/tag advances, pin not bumped)──▶ STALE-PIN   (204 case; FR-002 red)
COHERENT ──(literal bumped by hand, tag/feed not cut)─▶ PHANTOM    (FR-002 red, no tag)
COHERENT ──(one BOM pin / member pin edited)──────────▶ HALF-BUMP  (FR-003/004/005 red)
COHERENT ──(new src/** member, BOM/template unwired)──▶ SKEW       (FR-003 red; SC-004)
STALE-PIN/PHANTOM/HALF-BUMP/SKEW ──(coherent bump op: literal+tag+feed atomic)──▶ COHERENT
```

The guard rejects every non-COHERENT state at the gate; the atomic bump operation (D2/D3) is the only
transition back to COHERENT.
