# Quickstart: Validate the republished `game`-bearing template (Feature 222)

A runnable live-validation guide proving the feature end-to-end. **This is a release/feed/registry feature**,
so the "app" is the publish→feed→scaffold→registry path, not the Skia viewer. Replace `<V>` with the published
version (expected `0.1.54-preview.1`). See [contracts/fs-gg-ui-template-release.md](./contracts/fs-gg-ui-template-release.md)
and [data-model.md](./data-model.md) for the obligations and state transitions; this guide only runs them.

## Prerequisites

- `git`, `dotnet` (`10.0.x`), `gh` (authenticated).
- For the consumer probe: a token with **only** `packages: read` on the `FS-GG` org (no special private grant).
- Release-tag push rights on `FS-GG/FS.GG.Rendering` (for the publish step) and `FS-GG/.github` merge rights
  (for the registry flip). Steps requiring these are operator actions; if unavailable, **disclose and defer**
  (Principle V) — do not fake them.

## 0. Pre-publish probe — confirm the gap (Foundational; before any tag)

```bash
# The feed currently serves 0.1.53-preview.1 WITHOUT Feature 220.
gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'   # expect 0.1.53-preview.1 present, no V yet
git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.53-preview.1 && echo "BAD: 220 already on feed" || echo "OK: 0.1.53 lacks 220"
git merge-base --is-ancestor b78e72a main && echo "OK: 220 on main" || echo "BAD: 220 not on main"
```

Expected: `0.1.53-preview.1` lacks `b78e72a`; `main` contains it. This confirms the work is real before any
release tag is pushed.

## 1. Cut & publish the coherent set (US2, FR-001/002/010)

Done via the `speckit-merge` / release flow — **not** hand-rolled. It bumps both in-repo pins to `V` from a
`main` commit containing `b78e72a` and pushes the release tag-set so `release.yml` `publish-packages` packs +
pushes the whole set:

```bash
# Pins that must both move to V (verify before tagging):
grep -n 'FsGgUiVersion\|<Version>' template/base/Directory.Packages.props .template.package/FS.GG.UI.Template.fsproj
# After the release flow pushes fs-gg-ui-template/v<V> (+ sibling v* tags), publish-packages runs in CI.
```

## 2. Confirm the feed serves V and the contents carry Feature 220 (US2, SC-002)

```bash
gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name' | grep -F "<V>"   # V is served
git merge-base --is-ancestor b78e72a fs-gg-ui-template/v<V> && echo "OK: V carries Feature 220" || echo "FAIL: V lacks 220"
```

Expected: `V > 0.1.53-preview.1` listed; ancestry check **true** (content gate, not just the version string).

## 3. Consumer install + `game` scaffold (US1, SC-001) — from a `packages: read` token only

```bash
# Authenticated ONLY as an ordinary org consumer (packages: read):
dotnet new install FS.GG.UI.Template::<V>          # expect exit 0, NOT exit 103
dotnet new fs-gg-ui --profile game -o /tmp/game-probe   # game choice accepted; minimal Pong-style MVU starter generated
#   expect exit 0; NO missing-profile / unknown-choice error
```

Expected: install exit 0 (no 103), `game` accepted, minimal `Model`/`Msg`/`update`/`view` + tick skeleton
generated. (Confirm the exact profile-selection flag/syntax against the packed `template.json`.)

## 4. Generated `game` product builds + passes governance with ZERO edits (US1, SC-003, FR-004)

```bash
cd /tmp/game-probe
dotnet build                          # builds against the FS.GG.UI.* set at V
dotnet test                           # GovernanceTests pass — NO GovernanceTests edits (family-agnostic entrypoint)
# no-flag launch renders a live interactive game scene (no -- pong-style flag)
```

Expected: build + governance green with **zero** `GovernanceTests` edits.

## 5. Non-game profiles unaffected (FR-005, SC-003)

```bash
dotnet new fs-gg-ui --profile app -o /tmp/app-probe          # still the controls showcase
# headless-scene / governed / sample-pack output byte-identical to Feature 220's diff-verified baseline:
#   regenerate each and diff against the Feature-220 baseline (see Feature 220 FR-007 diff procedure).
```

Expected: `app` → controls showcase; the three non-game profiles diff-clean vs the Feature-220 baseline.

## 6. Registry flip — UNRELEASED → released (US3, FR-006/007) — ONLY after steps 2–4 are green

A `contract-change` PR on `FS-GG/.github`:

```bash
# In FS-GG/.github:
#  registry/dependencies.yml (fs-gg-ui-template): version/package-version/package-tag -> V;
#      game profile note UNRELEASED -> released @ V; flip the coherence entry (resolved_by -> fs-gg-ui-template/v<V>)
#  docs/registry/compatibility.md: regenerate the projection (no stale 0.1.53-preview.1 for this surface)
grep -n 'fs-gg-ui-template' registry/dependencies.yml docs/registry/compatibility.md   # post-merge: names V, reads released
```

Expected: entry + projection name `V`, `game` reads released, coherence flipped. **The PR lands only after
the feed is confirmed serving `V`** (publish-before-flip).

## 7. Board & downstream closure (US4, FR-008/009, SC-005)

```bash
gh issue view 33 --repo FS-GG/FS.GG.Rendering            # closed, carries V + registry PR link
# board item #33 -> Done; item #31 no longer "Blocked by: FS.GG.Rendering#33"
gh issue view 44 --repo FS-GG/FS.GG.SDD                  # notified of published V (app -> game default-flip can proceed)
```

Expected: #33 closed with `V` + registry PR; #31 unblocked; SDD#44 notified.

## Done When

- [ ] Feed serves exactly one new coherent-set version `V > 0.1.53-preview.1` whose contents carry `b78e72a` (steps 2). (SC-002)
- [ ] A `packages: read` consumer installs `V` (no 103) and scaffolds the `game` profile (step 3). (SC-001)
- [ ] The generated `game` product builds + passes governance with zero `GovernanceTests` edits; non-game profiles unaffected (steps 4–5). (SC-003)
- [ ] The registry + compatibility projection name `V` and read `game` released, after the feed listing (step 6). (SC-004)
- [ ] #33 closed (+`V`, +registry PR), board #33 `Done`, #31 unblocked, SDD#44 notified (step 7). (SC-005)
