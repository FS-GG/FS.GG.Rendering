# Quickstart: Publish the Template & Tag the Coherent Set

A runnable validation guide for the publish → verify → tag → reconcile release. Run from the repo
root on `206-publish-template-coherent-set`. Gates reference
[`contracts/publish-verification.md`](./contracts/publish-verification.md) (PV-1..PV-6) and
[`contracts/coherent-set.md`](./contracts/coherent-set.md). **Do not** tag or reconcile until every PV
gate is green (see [`contracts/cross-repo-resolution.md`](./contracts/cross-repo-resolution.md)).

## Prerequisites

- `dotnet` (net10.0 SDK), `git`, `gh` (authenticated — `gh auth status`).
- Framework set `FS.GG.UI.* 0.1.50-preview.1` present on the feed (`ls ~/.local/share/nuget-local/ |
  grep 0.1.50-preview.1`) — the coherent base from feature 204.
- Tag `fs-gg-ui-template/v0.1.50-preview.1` does **not** yet exist (`git tag --list`).

## Step 0 — Non-regression baseline

```sh
dotnet fsi scripts/baseline-tests.fsx          # record green/red; disclose any pre-existing reds
```

## Step 1 — Bump the template package version (the only in-repo edit)

```sh
# .template.package/FS.GG.UI.Template.fsproj
#   <Version>0.1.17-preview.1</Version>  ->  <Version>0.1.50-preview.1</Version>
grep '<Version>' .template.package/FS.GG.UI.Template.fsproj
```

## Step 2 — Publish to the feed

```sh
# Fail loudly if the version already exists (FR-002) — do not overwrite:
test ! -f ~/.local/share/nuget-local/FS.GG.UI.Template.0.1.50-preview.1.nupkg \
  || { echo "version already on feed — choose next unused"; exit 1; }

dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local
ls ~/.local/share/nuget-local/FS.GG.UI.Template.0.1.50-preview.1.nupkg     # expect: present
```

## Step 3 — Install & verify from the package (PV-1, PV-2)

```sh
dotnet new install FS.GG.UI.Template::0.1.50-preview.1
dotnet new fs-gg-ui --help        # template listed; lifecycle + initGit options present; no skipGitInit
```

## Step 4 — Byte-identical default + lifecycle variants (PV-3, PV-5)

```sh
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report
# expect: spec-kit byte-identical to baseline for every profile (zero diffs);
#         sdd/none emit the Spec-Kit-absent set; unknown values rejected.
```

## Step 5 — Side-effect-free default (PV-4)

```sh
dotnet new fs-gg-ui --profile headless-scene -o /tmp/fsgg-sef
test ! -d /tmp/fsgg-sef/.git && echo "OK: no repo created, no process spawned"   # SC-003
# Cross-check manifest invariants:
dotnet test tests/Package.Tests --filter Feature205TemplateSideEffectTests
dotnet test tests/Package.Tests --filter Feature204LifecycleTemplateTests
```

## Step 6 — Per-profile restore/build + reproducibility (PV-6a, pre-tag)

```sh
for p in app headless-scene governed sample-pack; do
  dotnet new fs-gg-ui --profile "$p" -o "/tmp/fsgg-$p"
  ( cd "/tmp/fsgg-$p" && dotnet restore && dotnet build )    # zero NU1101 / zero version conflict
done
# reproducibility: two clean caches resolve identically (SC-005)
rm -f /tmp/fsgg-app/packages.lock.json
dotnet restore /tmp/fsgg-app --packages /tmp/cacheA --force-evaluate
cp /tmp/fsgg-app/packages.lock.json /tmp/lockA.json
rm -f /tmp/fsgg-app/packages.lock.json
dotnet restore /tmp/fsgg-app --packages /tmp/cacheB --force-evaluate
cp /tmp/fsgg-app/packages.lock.json /tmp/lockB.json
diff /tmp/lockA.json /tmp/lockB.json && echo "reproducible"   # zero diff between the two caches
```

## Step 7 — Tag the coherent set + from-tag reproduction (US2 — only after PV-1..PV-5 + PV-6a green)

```sh
git tag -a fs-gg-ui-template/v0.1.50-preview.1 \
  -m "coherent fs-gg-ui template snapshot: FS.GG.UI.Template 0.1.50-preview.1 over FS.GG.UI.* 0.1.50-preview.1"
git push origin fs-gg-ui-template/v0.1.50-preview.1
# PV-6b (post-tag) — from-tag repack reproduces the package (FR-009):
git checkout fs-gg-ui-template/v0.1.50-preview.1 -- .template.package/FS.GG.UI.Template.fsproj
```

## Step 8 — Reconcile the cross-repo record (US3 — gated on Steps 0–7)

Follow [`contracts/cross-repo-resolution.md`](./contracts/cross-repo-resolution.md):

1. **XR-A/XR-B** — flip the `fs-gg-ui-template` row + projection in `FS-GG/.github` to record the
   coherent release at `0.1.50-preview.1` / tag `fs-gg-ui-template/v0.1.50-preview.1`.
2. **XR-C** — post the `## Response` on `FS-GG/FS.GG.SDD#1` citing the published version + tag.
3. **XR-D** — move the P1 Rendering board item to Done; clear the "blocked by lifecycle symbol" relation.

```sh
gh issue view 1 --repo FS-GG/FS.GG.SDD       # confirm response posted
```

## Done when

- A by-id install resolves `0.1.50-preview.1` exposing `lifecycle` + `initGit` (SC-001).
- `spec-kit` default is byte-identical across all profiles (SC-002); default scaffold creates zero
  repos / spawns zero processes (SC-003).
- All four profiles restore+build from the tag against one framework version (SC-004), reproducibly
  (SC-005).
- Registry row + projection record the coherent release and link tracking; `FS-GG/FS.GG.SDD#1` shows
  the `## Response` citing the version + tag (SC-006).
- Any failure left the cross-repo record **in-progress**, never falsely coherent (FR-010).
