# Quickstart: Validate the root-build release closure

Feature: `215-root-build-release-closure` · Date: 2026-06-28

A runnable validation guide that proves the closure end-to-end: the coherent set is **released**, the
release-only gate is **green on the real release**, the version trio is **coherent**, the registry **advertises
the released guarantee**, and **#9 is closed** with the board flipped to Done. Implementation details live in
[contracts/](./contracts/) and [data-model.md](./data-model.md); this file is the run/validation script.

Coherent-set version for this release: **`0.1.52-preview.1`** (see [research.md](./research.md) R1).

## Prerequisites

- Repo `main` contains Feature 212 root-build (`b6ac246`); branch `215-root-build-release-closure` checked out.
- `gh` authenticated for `FS-GG/FS.GG.Rendering` and `FS-GG/.github` (push + project scopes).
- .NET 10 SDK; on this dev box, strip display vars for any headless `dotnet run`
  (`WAYLAND_DISPLAY`/`DISPLAY`/`XDG_RUNTIME_DIR`) so the app window does not block.

## Step 0 — Confirm starting state

```bash
grep -n FsGgUiVersion template/base/Directory.Packages.props        # expect 0.1.51-preview.1 (pre-bump)
grep -n '<Version>' .template.package/FS.GG.UI.Template.fsproj      # expect 0.1.52-preview.1 (already ahead)
git tag --list 'fs-gg-ui/v*' | sort -V | tail -1                   # expect fs-gg-ui/v0.1.51-preview.1
git tag --list 'fs-gg-ui-template/v*' | sort -V | tail -1          # expect fs-gg-ui-template/v0.1.50-preview.1 (stale)
gh issue view 9 --repo FS-GG/FS.GG.Rendering --json state          # expect OPEN
gh pr view 25 --repo FS-GG/.github --json state,mergeable          # expect OPEN / CONFLICTING
```

## Step 1 — Bump the org version line (the only in-repo source edit)

Set `FsGgUiVersion` to the coherent-set version. **Do not** touch any `.fs`/`.fsi`/baseline (FR-011).

```bash
# template/base/Directory.Packages.props: <FsGgUiVersion>0.1.51-preview.1</FsGgUiVersion>
#                                      →  <FsGgUiVersion>0.1.52-preview.1</FsGgUiVersion>
grep -n FsGgUiVersion template/base/Directory.Packages.props        # expect 0.1.52-preview.1
```

## Step 2 — Local pre-flight (confidence only; NOT #9 evidence)

```bash
dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local
dotnet new install FS.GG.UI.Template::0.1.52-preview.1
work="$(mktemp -d)"; dotnet new fs-gg-ui --name GeneratedProduct --output "$work/GeneratedProduct"
dotnet build "$work/GeneratedProduct" && dotnet test "$work/GeneratedProduct"
( unset WAYLAND_DISPLAY DISPLAY XDG_RUNTIME_DIR; dotnet run --project "$work/GeneratedProduct/src/GeneratedProduct" )  # expect exit 0
```
**Expected**: stock build/test/run all succeed at the product root with no FAKE invocation. (See
[contracts/release-gate.md](./contracts/release-gate.md).)

## Step 3 — Make the coherent set + run the staleness guard

Create the snapshot tags for the released version, then verify coherence.

```bash
git tag -a fs-gg-ui/v0.1.52-preview.1 -m "coherent fs-gg-ui set 0.1.52-preview.1"
git tag -a fs-gg-ui-template/v0.1.52-preview.1 -m "coherent fs-gg-ui template 0.1.52-preview.1 over FS.GG.UI.* 0.1.52-preview.1"
# (push tags as part of the release in Step 4)

dotnet fsi scripts/validate-version-coherence.fsx                                   # expect exit 0
FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx   # expect full set → 0.1.52-preview.1
```
**Expected**: exit `0` (no straggler); the trio agrees. (See [contracts/coherent-set.md](./contracts/coherent-set.md).)

## Step 4 — Release for real (the load-bearing gate)

Trigger `release.yml` via a real release/tag (`v0.1.52-preview.1`), not a dry run.

```bash
# e.g. create a GitHub release on tag v0.1.52-preview.1 (push the snapshot tags too):
git push origin fs-gg-ui/v0.1.52-preview.1 fs-gg-ui-template/v0.1.52-preview.1
gh release create v0.1.52-preview.1 --repo FS-GG/FS.GG.Rendering --title "fs-gg-ui 0.1.52-preview.1" --notes "Root-buildable template coherent set; closes #9."
gh run watch --repo FS-GG/FS.GG.Rendering   # watch the release workflow
```
**Expected**: `package-tests` **and** `template-product-tests` green on the real release; `publish-packages`
runs only after both and pushes the coherent set to `nuget.pkg.github.com/FS-GG`. **Capture the green
`template-product-tests` run URL** — it is the #9 evidence. A red gate ⇒ no publish ⇒ #9 stays open.

## Step 5 — Land registry coherence (with or after the release, never before)

```bash
# In FS-GG/.github (use the cross-repo-coordination skill): rebase PR #25 to clear CONFLICTING,
# re-pin fs-gg-ui-template entry to version 0.1.52-preview.1 / tag fs-gg-ui-template/v0.1.52-preview.1,
# coherent: true, tracking FS-GG/FS.GG.Rendering#9, in both registry/dependencies.yml and
# docs/registry/compatibility.md.
gh pr merge 25 --repo FS-GG/.github --squash    # ONLY after Step 4 published
```
**Expected**: merged registry shows `fs-gg-ui-template` `root-buildable` + `coherent: true` pinned to
`0.1.52-preview.1`, tracker #9 visible; merge time ≥ publish time. (See
[contracts/registry-coherence.md](./contracts/registry-coherence.md).)

## Step 6 — Close #9 + flip the board + signal downstream

```bash
gh issue comment 9 --repo FS-GG/FS.GG.Rendering --body "Released FS.GG.UI.Template 0.1.52-preview.1 (tag fs-gg-ui-template/v0.1.52-preview.1). Green real-release template-product-tests: <run-url>. Registry coherent: FS-GG/.github#25 merged. Closes #9."
gh issue close 9 --repo FS-GG/FS.GG.Rendering
# Coordination board: set the H1 rendering item to Done (resolve item id via gh project item-list)
# Signal FS.GG.SDD: the released root-buildable template is available for its acceptance probes.
```
**Expected**: #9 CLOSED with evidence comment; board H1 rendering item = **Done**; SDD consumer can target the
released version. (See [contracts/registry-coherence.md](./contracts/registry-coherence.md).)

## Acceptance roll-up

| Success criterion | Validated by |
|---|---|
| SC-001 stock restore/build/test/run, zero FAKE | Step 2 (local) + Step 4 (real gate) |
| SC-002 gate green on the actual release | Step 4 (run URL captured) |
| SC-003 trio coherent, no straggler | Step 3 (guard exit 0) |
| SC-004 registry advertises released guarantee, no premature window | Step 5 (merge ≥ publish) |
| SC-005 #9 closed with evidence, board Done | Step 6 |
| SC-006 SDD probes can target the released template | Step 6 (downstream signal) |
