# Phase 1 Data Model: Release Coherence Entities

This feature ships no runtime data structures; the "entities" are the release artifacts and the
records that must agree about them. Modeled here as the state each must reach and the invariants that
bind them.

## Entities

### 1. Published template package

- **id**: `FS.GG.UI.Template`
- **version**: `0.1.50-preview.1` (strictly > `0.1.17-preview.1`; not pre-existing on the feed)
- **location**: `~/.local/share/nuget-local/FS.GG.UI.Template.0.1.50-preview.1.nupkg`
- **carries**: `lifecycle` choice symbol (`spec-kit|sdd|none`, default `spec-kit`); `initGit` bool
  opt-in (default `false`); **no** `skipGitInit`; no auto post-actions
- **emits profiles**: `app`, `headless-scene`, `governed`, `sample-pack`
- **pins** (in packed `template/base/Directory.Packages.props`): `FsSkiaUiVersion=0.1.50-preview.1`
- **validation**: install by id resolves this version (FR-004); `spec-kit` output byte-identical to
  prior baseline (FR-005); default generation spawns no process / creates no repo (FR-006)

### 2. Coherent-set tag

- **name**: `fs-gg-ui-template/v0.1.50-preview.1` (annotated)
- **binds**: published template `0.1.50-preview.1` ↔ framework `FS.GG.UI.* 0.1.50-preview.1`
  (itself anchored by `fs-skia-ui/v0.1.50-preview.1`)
- **invariant**: the tagged tree reproduces the published package set — a from-tag repack regenerates
  `FS.GG.UI.Template.0.1.50-preview.1.nupkg` and the framework packages already on the feed (FR-009)
- **immutability**: never moved; collision surfaces and a distinct name is chosen (FR-002, edge case)

### 3. Registry row (`fs-gg-ui-template`)

- **location**: `FS-GG/.github` → `registry/dependencies.yml` (authoritative) +
  `docs/registry/compatibility.md` (projection)
- **fields to reach agreement**: contract id `fs-gg-ui-template`; recorded version `0.1.50-preview.1`;
  coherent flag → recorded as a coherent release; tag reference `fs-gg-ui-template/v0.1.50-preview.1`;
  `tracking` link to the rendering tracking issue / this feature; `resolved_by` pointing at the
  publishing commit + tag
- **invariant**: authoritative row and projection say the **same** version/tag/coherent state (FR-007)

### 4. Cross-repo request (dependent)

- **issue**: `FS-GG/FS.GG.SDD#1` — "[cross-repo] Scaffold path must own git-init/chmod after fs-gg-ui
  Feature 205 (side-effect-free generation)" (state: OPEN at planning)
- **resolution**: a `## Response` citing the published version `0.1.50-preview.1` and the tag
  `fs-gg-ui-template/v0.1.50-preview.1` — the package carrying the side-effect-free + lifecycle
  surface is now installable, unblocking SDD's scaffold-path work (FR-008)

### 5. Board item (sequencing)

- **board**: org-level `Coordination` Projects v2, P1 Rendering
- **item**: "Publish FS.GG.UI.Template carrying the new parameter; tag the coherent set"
- **terminal state**: Done — set only after FR-001..FR-010 hold; the "blocked by lifecycle symbol"
  relation cleared (FR-011)

## State transitions (release lifecycle)

```text
                 ┌─────────────────────────── on any failure ──────────────────────────┐
                 ▼                                                                       │
 [not-coherent / in-progress] ──pack+publish──▶ [PUBLISHED] ──tag──▶ [TAGGED] ──reconcile──▶ [COHERENT]
        (record shows in-progress)                                                       │
                                                                                         └─▶ board: Done
```

- A release is **COHERENT** only when package + tag + registry/projection + request response all
  agree. Until then the cross-repo record MUST read **in-progress / not-yet-coherent** (FR-010) —
  never falsely coherent.
- Hard ordering: PUBLISHED (US1 gates green) → TAGGED (US2) → COHERENT (US3 reconciliation). US3 must
  not run before US1+US2 evidence is complete.

## Cross-entity invariants

- **I1 (version agreement)**: the version in the package, the tag's `v<semver>`, and the registry row
  are identical (`0.1.50-preview.1`) — FR-009.
- **I2 (coherent base)**: the framework set the template pins is itself the coherent published
  snapshot (`fs-skia-ui/v0.1.50-preview.1`); template coherence is declared on top of it, not over an
  incoherent base (FR-009, edge case "framework set drift").
- **I3 (no false coherence)**: if any of publish / tag / registry / request cannot complete, the
  record stays in-progress (FR-010).
- **I4 (non-regression)**: `spec-kit` default output is byte-identical to the prior published
  baseline across all four profiles (FR-005, edge case "default-output drift").
