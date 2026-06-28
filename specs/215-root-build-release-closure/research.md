# Phase 0 Research: Root-build release closure

Feature: `215-root-build-release-closure` · Date: 2026-06-28

This feature ships no new code, so "research" here resolves the open decisions that determine the release:
which version, how the existing machinery is driven, and the hard ordering/coherence constraints. Each item
below is **Decision / Rationale / Alternatives considered**, grounded in the current repo + live state
observed during planning.

## Current state observed (planning snapshot)

| Fact | Value | Source |
|---|---|---|
| `FsGgUiVersion` (template products pin this) | `0.1.51-preview.1` | `template/base/Directory.Packages.props:9` |
| `.template.package` `<Version>` | `0.1.52-preview.1` (already bumped ahead) | `.template.package/FS.GG.UI.Template.fsproj:9` |
| Latest framework snapshot tag | `fs-gg-ui/v0.1.51-preview.1` | `git tag --list 'fs-gg-ui/v*'` |
| Latest published template tag | `fs-gg-ui-template/v0.1.50-preview.1` (predates root-build) | `git tag --list 'fs-gg-ui-template/v*'` |
| Issue #9 | OPEN | `gh issue view 9 --repo FS-GG/FS.GG.Rendering` |
| Registry PR `.github#25` | OPEN, **CONFLICTING** | `gh pr view 25 --repo FS-GG/.github` |
| Root-build capability (Feature 212) | merged to `main` at `b6ac246` | spec.md context |

## R1. Coherent-set release version

**Decision**: Release the coherent set at **`0.1.52-preview.1`**. All three coherence numbers become
`0.1.52-preview.1`: the published `FS.GG.UI.Template` version, the `.github#25` registry coherence-entry
version, and the org `FsGgUiVersion` line (bumped from `0.1.51`). New snapshot tags
`fs-gg-ui/v0.1.52-preview.1` and `fs-gg-ui-template/v0.1.52-preview.1` are created over the released set.

**Rationale**:
- `.template.package/FS.GG.UI.Template.fsproj` already carries `<Version>0.1.52-preview.1</Version>`, so the
  intended next version is `0.1.52`; choosing it makes the in-repo fsproj, the release tag, and the coherent
  set agree without contradiction.
- The framework set is **already published at `0.1.51`** (`fs-gg-ui/v0.1.51-preview.1` tag exists), and the
  root-build guarantee lives in the **template**, not the framework libs. Picking the next version
  `0.1.52` cleanly carries the whole coherent set forward and avoids re-using a version a prior framework
  publish already consumed — directly satisfying the spec's "Re-release / version already taken" edge case
  (select the next coherent version rather than overwrite a tag).
- The release-time decision was confirmed with the maintainer during planning.

**Alternatives considered**:
- **`0.1.51-preview.1`** (match current `FsGgUiVersion`, no bump): rejected. Framework libs are already
  published at `0.1.51` and `fs-gg-ui/v0.1.51-preview.1` already exists; re-using it muddies the coherent-set
  story (the template would publish at `0.1.51` while the fsproj reads `0.1.52`), and the spec explicitly
  prefers the next coherent version when a version is already consumed.
- **A larger bump (e.g. `0.2.0`)**: rejected. This is a delivery/closure of an existing capability, not a new
  feature surface; a minor bump would over-signal change and contradict FR-011 (no scope creep).

## R2. How the coherent set is released (existing machinery)

**Decision**: Drive the existing `.github/workflows/release.yml`. It packs the entire
`FS.GG.Rendering.slnx` **plus** `.template.package/FS.GG.UI.Template.fsproj` at a single
`-p:Version=$VER` (one coherent set, exactly as `scripts/dev-repack.fsx` does for the local feed), runs the
two release-only gates, then publishes `artifacts/packages/*.nupkg` to `https://nuget.pkg.github.com/FS-GG/index.json`
with `GITHUB_TOKEN` (`packages: write`). Trigger via a real release/tag (resolve `$VER` from the release
`tag_name` / `GITHUB_REF_NAME`); a `workflow_dispatch` without `version` is a pack-only **dry run** that does
not push (so it is not valid #9 evidence).

**Rationale**: Assumption in spec — the publish mechanism already exists and is the same one used for prior
`fs-gg-ui` releases; finalization triggers it, it does not build new release infra. The workflow already
overrides every package version with `-p:Version`, so the in-repo `<Version>` numbers are not load-bearing
for *what* is published — the **release tag is**.

**Alternatives considered**: hand-packing/pushing locally (`dotnet pack ... -o ~/.local/share/nuget-local`
then `dotnet nuget push`) — rejected as #9 evidence: FR-002/SC-002 require the gate to run **on the real
release**, which only the workflow path provides. Local packing is retained only as pre-flight (R5).

## R3. Release-only gate — the load-bearing evidence

**Decision**: The required #9 evidence is a **green run of `release.yml`'s `template-product-tests` job on
the actual release** (plus `package-tests`). That job does `dotnet new install .` → `dotnet new fs-gg-ui
--name GeneratedProduct` → stock `dotnet build`/`dotnet test`/`dotnet run` at the product root, under
`set -euo pipefail`. `publish-packages` `needs: [package-tests, template-product-tests]`, so a publish only
happens after both gates pass — a partial publish on gate failure cannot occur (and if a publish step itself
fails after a green gate, #9 stays open until a fully green release exists — Edge Case "Partial publish").

**Rationale**: This is the real-consumer path the spec demands (US1 independent test): install the published-
shape template, scaffold, run stock verbs at the root with no FAKE knowledge. `dotnet run` exits 0 in the
headless runner via the entrypoint's `UnsupportedEnvironment` safe-degrade (Feature 212 research R4).

**Alternatives considered**: relying on the prior local 12-combination live verification from Feature 212 —
rejected as sufficient closure evidence: the spec's Edge Case "Release gate must run on a real release, not a
dry run" is explicit that a locally demonstrated gate does not close #9.

## R4. Version coherence / staleness guard (Feature 209)

**Decision**: After bumping `FsGgUiVersion` → `0.1.52-preview.1` and creating the `fs-gg-ui/v0.1.52-preview.1`
tag, run `dotnet fsi scripts/validate-version-coherence.fsx` (structural verdict, exit 0 = coherent) and the
`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1` restore-grounded proof. The guard must report **no straggler**: the pin
matches an existing `fs-gg-ui/v*` tag, does not lag the latest tag, the BOM exact-bracket token is consistent,
and the 11-member template pin set agrees.

**Rationale**: FR-004/SC-003 require the published template version, the registry coherence-entry version, and
`FsGgUiVersion` to be identical so the guard does not flag the release. The guard compares `FsGgUiVersion`
against `fs-gg-ui/v*` tags — so the `fs-gg-ui/v0.1.52-preview.1` snapshot tag must exist for the bumped pin to
be coherent (ordering: create the tag as part of the release, before/with running the guard).

**Alternatives considered**: bumping `FsGgUiVersion` without creating the matching `fs-gg-ui/v0.1.52` tag —
rejected: the guard would (correctly) flag the pin as referencing a non-existent snapshot (exit 1).

## R5. Local pre-flight before the real release

**Decision**: Before triggering the release, do a non-load-bearing local pre-flight to de-risk: pack the
template to the local feed (`dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o
~/.local/share/nuget-local`), `dotnet new install`, scaffold each profile, and run stock build/test; and run
the coherence guard locally. This is dev confidence only — it is **not** #9 evidence (R3).

**Rationale**: Keeps the real release likely-green on first try without weakening the "evidence must come from
the real release" rule. The headless `dotnet run` pre-flight must strip `WAYLAND_DISPLAY`/`DISPLAY`/
`XDG_RUNTIME_DIR` on this dev box (live Wayland session) so the app window does not block until timeout.

**Alternatives considered**: skipping pre-flight and relying solely on the release gate — workable but
higher-risk (a red real-release gate is costly to iterate on); pre-flight is cheap insurance.

## R6. Registry coherence (`FS-GG/.github#25`) — content + ordering

**Decision**: Land `.github#25` carrying, in the org contract registry: on the `fs-gg-ui-template` contract,
the `root-buildable` surface and a coherence entry marked `coherent: true`, pinned to `version:
0.1.52-preview.1` / `tag: fs-gg-ui-template/v0.1.52-preview.1`, referencing tracker `#9` — in both the
authoritative `registry/dependencies.yml` and its `docs/registry/compatibility.md` projection. The PR is
currently **CONFLICTING**, so it must be **rebased** to clear the conflict and **re-pinned** to `0.1.52`
(its draft predates this version choice). It lands **with or after** the release, never before (FR-006).

**Rationale**: ADR-0001 registry coherence / contract C5 is the cross-repo contract of record. Landing the PR
before the artifact is published would advertise a guarantee no package satisfies (Edge Case "Premature
registry merge"); SC-004 requires "no window in which the guarantee is advertised but unreleased". Use the
`cross-repo-coordination` skill for the registry edits + ordering.

**Alternatives considered**: merging `#25` as-is now (before release) — rejected by FR-006 and the staleness
risk (its pinned version may differ from the released `0.1.52`).

## R7. Closure: issue #9 + Coordination board (US3)

**Decision**: After US1 (released + green gate) and US2 (coherent registry merged) hold, close issue #9 with a
closing comment that cites (a) the released template version `0.1.52-preview.1` / tag
`fs-gg-ui-template/v0.1.52-preview.1`, (b) the green real-release `template-product-tests` run URL, and (c) the
merged `.github#25`. Then set the H1 rendering item on the FS-GG "Coordination" board to **Done**, and post the
downstream unblock signal so FS.GG.SDD's acceptance probes can target the released template.

**Rationale**: FR-008/FR-009/SC-005 require evidence-backed closure and the board flip; US3 is the cross-repo
handshake that lets SDD's H4 follow-on dequeue against a real released template. Per the board "draft items are
dedupe trackers" note, read the Coordination board before assuming any item is untracked.

**Alternatives considered**: closing #9 on merge of Feature 212 (capability merged) — rejected: the spec is
explicit that "merged" ≠ "delivered"; closure requires released-artifact evidence.

## Open items / NEEDS CLARIFICATION

None remaining. The one pivotal decision (R1, release version) was resolved with the maintainer to
`0.1.52-preview.1`. The exact Coordination-board item id and the precise SDD downstream issue number are
operational lookups for `/speckit-tasks` (resolve via `gh project`/`gh issue` at execution time), not design
unknowns.
