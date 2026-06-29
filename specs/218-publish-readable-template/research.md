# Phase 0 Research: Publish & Make-Readable the productName-Enabled Template

All NEEDS CLARIFICATION from Technical Context resolved below. Each decision is grounded in the
live repo/feed/registry state observed on 2026-06-29.

## R1 — What version is published, and how is it cut?

**Decision**: Cut the next coherent-set **preview** version, expected `0.1.53-preview.1` (the merge bump
from the current `0.1.52-preview.1`), via the repo's existing **`speckit-merge` → tag-push → `release.yml`**
path. The only hard constraint is `> 0.1.52-preview.1` (FR-001); the exact literal is fixed by the merge
bump, not hard-coded here.

**Rationale**: `release.yml`'s `publish-packages` job derives the version from the pushed tag (`v<V>` →
strip `v`), packs `FS.GG.Rendering.slnx` + `.template.package/FS.GG.UI.Template.fsproj` at `-p:Version=V`,
and `dotnet nuget push`es the set to `nuget.pkg.github.com/FS-GG` with `secrets.GITHUB_TOKEN`
(`packages: write`, `--skip-duplicate`). The current feed top is `0.1.52-preview.1` (verified:
`gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions` → `0.1.52-preview.1`). Feature 217 is on
`main` (`6df0d39`), so a release cut from `main` carries it. The repo never hand-publishes; the merge flow
bumps + tags.

**Alternatives considered**: (a) Manual `dotnet nuget push` from a workstation — rejected: bypasses the
`package-tests`/`template-product-tests` release gates and the canonical-repo guard, and would not match
how 0.1.52 was published. (b) A bespoke "republish 0.1.52" — rejected: versions are immutable and 0.1.52
predates Feature 217; FR-001 requires a strictly greater version.

## R2 — Which tags must the release push? (the release tag-set)

**Decision**: A complete coherent-set release pushes **three** tags at `V` (= `0.1.53-preview.1`):
1. `v0.1.53-preview.1` → triggers `release.yml` → **publishes** the coherent set (libraries + template) to the org feed.
2. `fs-gg-ui-template/v0.1.53-preview.1` → triggers `template-dispatch.yml` → **notifies** FS.GG.Templates (FR-010, the Feature-216 App-token sender).
3. `fs-gg-ui/v0.1.53-preview.1` → the **snapshot** tag the Feature-209 coherence mirror in `Package.Tests` re-derives from (`git tag --list`; `release.yml` checks out `fetch-depth: 0` for exactly this).

**Rationale**: The three tag families all exist at `0.1.50/51/52` (verified: `git tag --list`). The glob
separation is deliberate (`template-dispatch.yml` header: the `/` in `fs-gg-ui-template/v*` makes it unable
to match `v*`, so `release.yml` and the dispatch never collide). Missing tag #2 only skips the Templates
*notification* (Templates can still re-pin manually off #29's reply — FR-010 is SHOULD, not a blocker for
FR-004). Missing tag #3 risks the coherence-mirror guard.

**Alternatives considered**: Pushing only `v<V>` — viable for the publish itself but drops the Templates
dispatch and the snapshot tag; rejected as incomplete relative to the established 215/216 release shape.

## R3 — How is the package made readable (the #26 visibility gate)?

**Decision**: Change the **`FS.GG.UI.Template`** org package visibility **`private → internal`** (org-wide
read), matching the already-public `FS.GG.*.Cli` packages' reachability. This is an **admin-gated GitHub UI
action** at `https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings` — **there is no
GitHub REST endpoint to change package visibility**, so it cannot be scripted via `gh api` (the API can only
*read* visibility: `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility` → currently
`"private"`). The fallback that satisfies FR-003 equally is granting `FS-GG/FS.GG.Templates` repo *Read*
on the package (same settings page → Manage Actions access).

**Rationale**: Verified live — `FS.GG.UI.Template` visibility is `private` (repo `FS-GG/FS.GG.Rendering`)
while `FS.GG.SDD.Cli` is `public`; a consumer `GITHUB_TOKEN` with `packages: read` therefore reads the CLIs
but 103s on the template. Package visibility is **per-package and persists across versions** — so publishing
`0.1.53` under the existing private package leaves it private; the flip is a separate, one-time action
(this is precisely why FR-004 treats publish and visibility as independent gates). `internal` is the
least-exposure option that still grants org-wide read (the spec's stated preference); the org's CLIs being
public means `internal` is not weaker than the surrounding norm.

**Consequence for sequencing**: because the flip is manual/admin, `/speckit-tasks` MUST model it as an
**operator step** (the user, as org admin) with a machine-checkable acceptance (`… --jq .visibility ==
"internal"`), not as an automatable task — mirroring how other admin-gated Coordination items are handled.

**Alternatives considered**: (a) Make it `public` like the CLIs — acceptable but broader than needed; the
issue prefers `internal`. (b) Per-repo Read grant to Templates only — narrower, but doesn't unblock other
org consumers (SDD) and is more bookkeeping; kept as the FR-003 fallback if org policy forbids `internal`.

## R4 — How is "done" proven? (evidence model — the hard part)

**Decision**: Evidence is **live cross-repo proof**, captured under `specs/218-publish-readable-template/readiness/`:
- **Feed serves V** (SC-001): `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` lists `V`.
- **No exit 103** (SC-002): from a context authenticated as an *ordinary* org consumer (`packages: read`, no special grant), `dotnet new install FS.GG.UI.Template@V` exits 0. The honest probe is the **FS.GG.Templates composition CI** (`#26`'s exact failing job) re-run, or an equivalent foreign-token install.
- **No exit 127** (SC-003): `dotnet new fs-gg-ui --productName <P>` (no `-n`) against `V` exits 0 and scaffolds.
- **Registry updated** (SC-005) and **#29/#26 closed**, board → `Done`.

**Rationale**: Feature 175's lesson, restated in the plan's standing assumption: a green local pack and a
passing in-repo `template-product-tests` do **not** prove the *foreign-token* path. The two exit codes (127,
103) are the actual cross-repo CI symptoms quoted in #29/#26 and are the only authoritative pass/fail
signals. The publish and visibility flip both happen **off the workstation** (CI on tag push; org admin UI),
so some evidence is necessarily captured from run URLs / `gh api` reads rather than a local command —
disclosed as such (Principle V).

**Deferred-evidence rule**: if the live FS.GG.Templates composition re-run is not executable within this
feature (it is Templates-owned per #29's "Downstream"), the feature's terminal evidence is: feed lists `V`
+ visibility `internal` + a local foreign-token-simulating install succeeds + `--productName` scaffolds +
registry/issue/board closure. The `29/29` composition (SC-004) is then confirmed by Templates and linked,
not blocked on here (spec Assumption).

## R5 — What exactly changes in the `FS-GG/.github` registry? (FR-008)

**Decision**: Update the `fs-gg-ui-template` entry in `registry/dependencies.yml` and its
`docs/registry/compatibility.md` projection:
- `version` `0.1.52-preview.1 → V`, `package-version` `0.1.52-preview.1 → V`, `package-tag`
  `fs-gg-ui-template/v0.1.52-preview.1 → fs-gg-ui-template/vV`.
- The `productName` parameter's feed note — currently *"UNRELEASED on the feed (lands next fs-gg-ui-template
  release)"* / *"lands in the next coherent fs-gg-ui-template release after 0.1.52-preview.1"* — flips to
  **released in V**.
- The coherence block entry `- id: fs-gg-ui-template` (`coherent: true`) `resolved_by` advances to the new
  `fs-gg-ui-template/vV` tag; record that the package is now org-readable (visibility `internal`) so the
  consumer half (Templates CI install) is no longer auth-blocked.

**Rationale**: Verified live: the registry already *documents* `productName` (additive, owner rendering)
and explicitly marks it UNRELEASED, with `version`/`package-version` pinned at `0.1.52-preview.1`
(`dependencies.yml` lines ~123–160, coherence entry ~280 `coherent: true`). ADR-0001 makes this registry
update the mandatory landing point of the `contract-change`. The update lands as a PR against `FS-GG/.github`
(cross-repo), linked from #29's resolution.

**Alternatives considered**: Updating only the contract entry and not the productName note — rejected: the
note is the consumer-visible "is it usable yet?" signal SDD/Templates read; it must say released.

## Summary table

| # | Unknown | Decision |
|---|---------|----------|
| R1 | Version + cut mechanism | `0.1.53-preview.1` (>`0.1.52`), via `speckit-merge` → tag → `release.yml` publish-packages |
| R2 | Release tag-set | `v<V>` (publish), `fs-gg-ui-template/v<V>` (Templates dispatch), `fs-gg-ui/v<V>` (coherence-mirror snapshot) |
| R3 | Visibility mechanism | `private → internal` via org package-settings **UI** (no REST endpoint); fallback = Templates repo Read grant |
| R4 | Evidence model | Live: feed lists `V`; foreign-token install exit 0 (no 103); `--productName` exit 0 (no 127); registry/issue/board closure; `29/29` confirmed by Templates |
| R5 | Registry delta (FR-008) | `fs-gg-ui-template` version/package-version/package-tag → `V`; productName note → released; coherence `resolved_by` → `vV` + readable |
