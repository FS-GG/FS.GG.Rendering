# Quickstart: validate the published, readable, productName-enabled template

A runnable validation guide proving the two coupled gates (#29 publish, #26 visibility) hold for the **same**
version `V` (expected `0.1.53-preview.1`). Run top-to-bottom; capture outputs under
`specs/218-publish-readable-template/readiness/`. These are **live** checks against the real org feed and a
real consumer token — green local packs do not substitute (plan standing assumption).

## Prerequisites

- `gh` authenticated against `FS-GG` (org member; `packages: read` at least).
- `dotnet` SDK `10.0.x`.
- Org-admin access to `FS-GG` package settings **for the visibility step** (§2) — this is the operator/user.
- `V` = the released coherent-set version (read it from the merge bump / release tag; everything below uses `$V`).

```bash
V=0.1.53-preview.1   # replace with the actual released version (> 0.1.52-preview.1)
PKG=FS.GG.UI.Template
```

## 0. Baseline (before the release) — confirm the failing state #29/#26 describe

```bash
gh api orgs/FS-GG/packages/nuget/$PKG/versions --jq '.[].name'      # expect: only 0.1.52-preview.1
gh api orgs/FS-GG/packages/nuget/$PKG --jq .visibility              # expect: private
```
Expected: the pre-release world — feed serves only `0.1.52-preview.1` (→ `--productName` 127) and the package
is `private` (→ consumer install 103).

## 1. Publish gate (#29) — the feed serves a Feature-217 version

After the release tag-set is pushed (`v$V`, `fs-gg-ui-template/v$V`, `fs-gg-ui/v$V`) and `release.yml`
`publish-packages` is green:

```bash
gh api orgs/FS-GG/packages/nuget/$PKG/versions --jq '.[].name'      # expect: $V present, > 0.1.52-preview.1
```
- **Pass**: `$V` is listed. (SC-001, INV-3)
- Link the `release.yml` run URL (publish-packages "Your package was pushed") in readiness evidence.

Confirm the packed `$V` honors `--productName` (no 127). Once readable (after §2), or from a context that can
read the package:

```bash
work=$(mktemp -d); cd "$work"
dotnet new install $PKG@$V
dotnet new fs-gg-ui --productName Acme --output ./Acme   # NO -n; this is the SDD scaffold-provider form
echo "scaffold exit: $?"                                  # expect: 0  (NOT 127)
```
- **Pass**: exit 0 and `./Acme` scaffolds. (SC-003, INV-6)

## 2. Visibility gate (#26) — an org consumer token can install it

**Operator step (org admin):** flip visibility at
`https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings` → Change visibility →
**Internal** (or grant `FS-GG/FS.GG.Templates` Read). There is no `gh api` for this (research R3). Then verify:

```bash
gh api orgs/FS-GG/packages/nuget/$PKG --jq .visibility            # expect: internal
```

Prove an **ordinary consumer** (no special private-package grant) can install — the exact path #26's CI fails on:

```bash
# Honest probe: re-run the FS.GG.Templates composition CI job that 103'd, OR simulate a foreign
# consumer token holding only packages:read and run:
dotnet new install $PKG@$V
echo "install exit: $?"                                            # expect: 0  (NOT 103)
```
- **Pass**: exit 0, no "could not be authenticated"/NotFound. (SC-002, INV-8)

## 3. Combined gate (FR-004) — both hold for the same `V`

- **Pass** only if §1 (no 127) **and** §2 (no 103) both pass for the **same** `$V`. A version that is published
  but private (103) or readable but old (127) is **not** done. (INV-15)

## 4. Registry landing (FR-008)

After the `FS-GG/.github` contract-change PR merges:

```bash
gh api repos/FS-GG/.github/contents/registry/dependencies.yml --jq '.content' | base64 -d \
  | grep -nE "version:|package-version:|package-tag:|UNRELEASED|released in" | grep -i "$V\|fs-gg-ui-template"
```
- **Pass**: `version`/`package-version` == `$V`, `package-tag` == `fs-gg-ui-template/v$V`, productName note reads
  released (no "UNRELEASED on the feed"). The coherence `fs-gg-ui-template` entry's `resolved_by` names
  `fs-gg-ui-template/v$V`. (SC-005, INV-10/11/12)

## 5. Closure

```bash
gh issue view 29 --repo FS-GG/FS.GG.Rendering --json state,comments --jq '.state, (.comments[-1].body|.[0:80])'
gh issue view 26 --repo FS-GG/FS.GG.Rendering --json state --jq .state
```
- **Pass**: #29 has a `## Response` with `$V` and is `CLOSED`; #26 is `CLOSED`; both Coordination-board rows are
  `Done`; FS.GG.Templates#32 is no longer `Blocked` (its `Blocked by` on #29/#26 cleared). (SC-005, INV-14/15/16)

## Downstream confirmation (Templates-owned — link, don't run here)

`FSGG_COMPOSITION_FULL=1 tests/composition/run.sh` → `29/29` in FS.GG.Templates after it re-pins to `$V`
(SC-004). This feature links that evidence from #32; it does not perform the Templates re-pin (spec Assumption).
