# Phase 1 Data Model: Root-build release closure

Feature: `215-root-build-release-closure` · Date: 2026-06-28

This feature has no runtime data model (it ships no code). The "entities" are the **artifacts and
coordination records** the closure produces and the **states** they move through. Modeling them makes the
hard ordering constraints (FR-004 coherence, FR-006 registry-after-release, FR-008 evidence-backed closure)
explicit and checkable.

## Entities

### 1. Published template release
The tagged, published `FS.GG.UI.Template` package that carries the root-build artifacts; the unit consumers
install.

| Field | Value / Rule |
|---|---|
| `packageId` | `FS.GG.UI.Template` |
| `version` | `0.1.52-preview.1` (the coherent-set version; R1) |
| `snapshotTag` | `fs-gg-ui-template/v0.1.52-preview.1` |
| `feed` | `https://nuget.pkg.github.com/FS-GG/index.json` |
| `gateEvidence` | URL of the green real-release `template-product-tests` run (see Release-gate evidence) |
| `behaviorParity` | MUST be byte/behaviorally identical to the `main`-built template (FR-003/FR-011) |

**Validation**: `version` equals `FsGgUiVersion` and the registry coherence-entry version (FR-004).
`snapshotTag` must not overwrite an existing tag (Edge Case "version already taken").

### 2. Coherent set
The agreeing trio of version numbers the Feature 209 staleness guard validates.

| Member | Where | Required value |
|---|---|---|
| Published template version | `FS.GG.UI.Template` on the feed + `fs-gg-ui-template/v*` tag | `0.1.52-preview.1` |
| Org `FsGgUiVersion` line | `template/base/Directory.Packages.props:9` | `0.1.52-preview.1` (bumped from `0.1.51`) |
| Registry coherence-entry version | `FS-GG/.github` `registry/dependencies.yml` `fs-gg-ui-template` row | `0.1.52-preview.1` |
| Framework snapshot tag | `fs-gg-ui/v0.1.52-preview.1` | must exist (guard compares pin to `fs-gg-ui/v*`) |

**Validation**: all three numbers identical AND the framework snapshot tag exists →
`scripts/validate-version-coherence.fsx` exits `0` (no straggler). Any mismatch → exit `1` (drift); unreadable
inputs / unfetched tags → exit `2` (guard error).

### 3. Release-gate evidence
The green run of the release-only gates proving stock build/test/run at the product root on the actual
release.

| Field | Rule |
|---|---|
| `source` | The **real** release run of `.github/workflows/release.yml` (not `workflow_dispatch` dry run, not local) |
| `jobs` | `package-tests` AND `template-product-tests` both green |
| `assertion` | `dotnet new install .` → `dotnet new fs-gg-ui` → stock `dotnet build`/`test`/`run` at product root, no FAKE |
| `blocking` | `publish-packages` `needs:` both gates → no publish unless both pass |

**Validation**: a green run URL exists and is citable in the #9 closing comment (FR-002/FR-008).

### 4. Registry coherence entry
The `fs-gg-ui-template` `root-buildable` surface + coherence row in the org contract registry, pinned to the
released version and tracker #9.

| Field | Rule |
|---|---|
| `contract` | `fs-gg-ui-template` |
| `surface` | records `root-buildable` (root `.slnx` + SDK pin + verb wrapper) |
| `coherent` | `true` |
| `version` / `tag` | `0.1.52-preview.1` / `fs-gg-ui-template/v0.1.52-preview.1` (== published) |
| `tracking` | `FS-GG/FS.GG.Rendering#9`, attributed to Feature 215 / Feature 212 |
| `files` | `registry/dependencies.yml` (authoritative) + `docs/registry/compatibility.md` (projection, must agree) |
| `carrier` | PR `FS-GG/.github#25` (rebased to clear CONFLICTING; re-pinned to `0.1.52`) |

**Validation**: entry version == published version (FR-005); guarantee + tracker visible on the compatibility
surface (FR-007).

### 5. Tracker #9 (closure record)
The H1 rendering Coordination-board issue whose evidence-backed closure is the finalization signal.

| Field | Rule |
|---|---|
| `issue` | `FS-GG/FS.GG.Rendering#9` |
| `closingComment` | cites released version + tag, green real-release gate URL, merged `.github#25` (FR-008) |
| `boardItem` | H1 rendering item on the FS-GG "Coordination" board → status `Done` (FR-009) |
| `downstreamSignal` | FS.GG.SDD acceptance-probe blocker marked satisfiable against the released version (FR-010) |

## State transitions

```text
Published template release:
  Drafted(in-repo, version bumped)
    → GateRunning(real release.yml: package-tests + template-product-tests)
    → [gate red]  → BLOCKED  (release not delivered; #9 stays OPEN — Edge "Partial publish")
    → [gate green] → Published(feed + fs-gg-ui-template/v0.1.52-preview.1 tag)

Coherent set:
  Mismatched(FsGgUiVersion=0.1.51, template tag=0.1.50)
    → bump FsGgUiVersion→0.1.52 + create fs-gg-ui/v0.1.52 + publish template@0.1.52
    → Coherent(guard exit 0)        [required before #9 closure]

Registry coherence entry (HARD ORDER — never before release):
  Open(PR #25, CONFLICTING, draft-pinned)
    → rebase + re-pin to 0.1.52
    → [release Published?] -- no --> WAIT (must not merge — Edge "Premature registry merge")
                           -- yes -> Merged(coherent:true, pinned 0.1.52, tracking #9)

Tracker #9:
  Open / board "In review"
    → [release Published ∧ coherent ∧ registry Merged]
    → Closed(with evidence comment) ∧ board "Done" ∧ downstream signal posted
```

## Invariants (the closure must preserve all)

1. **Coherence**: published template version == `FsGgUiVersion` == registry entry version == `0.1.52-preview.1`,
   and `fs-gg-ui/v0.1.52-preview.1` exists → guard exit 0 (FR-004/SC-003).
2. **Evidence from real release**: #9 is closed only with a green **real-release** gate run, never a dry/local
   run (FR-002/SC-002).
3. **Registry-after-release**: `.github#25` merges with or after the release — no advertise-before-publish
   window (FR-006/SC-004).
4. **No regression / no scope creep**: published template behaviorally identical to `main`-built template; the
   only source edit is the `FsGgUiVersion` property (FR-003/FR-011).
5. **All-or-nothing closure**: #9 Closed ⇔ release Published ∧ coherent ∧ registry Merged ∧ board Done ∧
   downstream signalled (FR-008/FR-009/FR-010).
