# Version coherence — forced-drift fixtures & quickstart scenarios (A1 canonical ledger)

The **authoritative** forced-drift fixtures (tasks T010/T011/T017–T019/T026) are the documented shell
scenarios below — the source of truth (A1). The auto-regenerated verdict snapshot lives next to this
file in `version-coherence.md` (the script overwrites it every run); the optional xUnit wrapper
`tests/Package.Tests/Feature209VersionCoherenceTests.fs` (T033) mirrors these scenarios and must stay
in parity — it never replaces them.

All runs executed 2026-06-28 on branch `209-version-staleness-guard` with
`dotnet fsi scripts/validate-version-coherence.fsx` (structural verdict-core, env-free) unless noted.

## Real pre-existing drift caught (fail-before evidence — stronger than synthetic)

The guard, run against the tree as committed, caught a **real** Feature-204 staleness condition: the
pin (`0.1.50-preview.1`) lagged the latest published coherent snapshot tag
(`fs-gg-ui/v0.1.51-preview.1`, 16 members + BOM in the feed). This is the headline bug class, present
in the live tree, not a fabricated fixture:

```
DRIFT [pin-lags-tag] template/base/Directory.Packages.props:9 <FsGgUiVersion>
  expected: >= 0.1.51-preview.1 (latest fs-gg-ui/v* tag)
  actual:   0.1.50-preview.1
```
→ exit `1`. **Remediation applied** (the coherent bump, D2/D3): `<FsGgUiVersion>` → `0.1.51-preview.1`
(tag exists; all 16 members + the `FS.GG.UI` BOM published at that version in the local feed). After
the bump the structural verdict-core and the live restore both pass.

## Scenario A — coherent tree passes (US1 #3, T016)

`dotnet fsi scripts/validate-version-coherence.fsx` → exit `0`, report `result: pass`,
`provenance: verdict-core`. ✅

## Scenario B — pin-lags-tag, the 204 lag (US1 #2, SC-001, T010/T013)

Set `<FsGgUiVersion>` to `0.1.0-preview.1`:
```
DRIFT [pin-lags-tag] template/base/Directory.Packages.props:9 <FsGgUiVersion>
```
→ exit `1`; restore → A passes. ✅

## Scenario E — phantom version, no snapshot tag (US1 #1, FR-009, T011/T012)

Set `<FsGgUiVersion>` to `0.1.99-preview.1` (no tag, and not lagging the latest):
```
DRIFT [pin-no-tag] template/base/Directory.Packages.props:9 <FsGgUiVersion>
```
→ exit `1`. ✅ (Confirms the lag/phantom split: a pin *ahead* of every tag is `pin-no-tag`, a pin
*behind* the latest is `pin-lags-tag`.)

## Scenario C — BOM half-bump, policy-independent (US2 #2, FR-004, SC-002, T017/T020)

Flip `src/Meta/FS.GG.UI.nuspec` `FS.GG.UI.Scene` from `[$version$]` to `[0.1.50-preview.1]`, **no**
`WarningsAsErrors` set:
```
DRIFT [bom-pin-not-token] src/Meta/FS.GG.UI.nuspec FS.GG.UI.Scene
```
→ exit `1`. ✅ Fails by direct structural compare, independent of NU1605/NU1608 loudness (FR-004).

## Scenario D — unwired member skew (US2 #3, SC-004, T018/T021)

Remove the `FS.GG.UI.Scene` `<dependency>` from the BOM nuspec:
```
DRIFT [bom-member-skew] src/Meta/FS.GG.UI.nuspec
  expected: a <dependency> for every packable FS.GG.UI.* member (16)
  actual:   missing FS.GG.UI.Scene
```
→ exit `1`. ✅

## Hardcoded template pin (US2, FR-005, T019/T022)

Change `FS.GG.UI.Build` `Version="$(FsGgUiVersion)"` to a literal `0.1.51-preview.1`:
```
DRIFT [template-pin-hardcoded] template/base/Directory.Packages.props FS.GG.UI.Build
```
→ exit `1`. ✅

## Aggregation (T024) — no early-exit hides a second drift

Scenarios C/D/hardcoded, run while the pin was momentarily stale, reported **two** `DRIFT` lines each
(e.g. `pin-lags-tag` + `bom-pin-not-token`). All failures across US1+US2 are collected and printed
together. ✅

## Scenario F — restore-grounded proof, complete set (US3, FR-008, T030)

`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx`:
packs the 16 members + BOM from source at the pinned `V` to a throwaway feed, restores `FS.GG.UI@V`
in a clean consumer → **`resolved-members-at-version: 16/16 at 0.1.51-preview.1`**,
`clean-consumer-build: pass`, exit `0`, report `provenance: live`. ✅ (See `version-coherence.md`.)

## restore-partial negative path (US3, T026)

The positive (16/16 @V) is proved by the real live restore above. The negative branch — a member
resolving to a *different* version (mixed graph) — is exercised deterministically by a **Synthetic**
(disclosed) logic check of `liveProof`'s partial-detection predicate: a fabricated resolved-set with
`FS.GG.UI.Scene @0.1.50-preview.1` (rest @V) yields
`DRIFT [restore-partial] FS.GG.UI.Scene @0.1.50-preview.1`. A genuinely *absent* member fails restore
as exit `2` (guard error, fails closed) rather than this mixed-graph branch — disclosed per
Constitution Principle V; not reported as a real restore.

## Scenario G — the gate blocks merge (US1 #4, FR-006)

`.github/workflows/gate.yml` runs the **Version coherence guard** step (structural verdict-core +
scoped restore-grounded proof, both merge-blocking) with `actions/checkout` `fetch-depth: 0` so
`git tag` sees `fs-gg-ui/v*`. Non-zero exit fails the required gate ⇒ PR cannot merge to `main`. The
script echoes the `DRIFT […]` lines to `$GITHUB_STEP_SUMMARY` on failure (SC-006).

## Done-when (quickstart) — status

- A–E + hardcoded each go red on the named location; A passes clean after each restore — ✅
- F resolves the full 16-member set to `V` — ✅
- G blocks a drifting PR at the gate (wired; enforced by branch protection requiring `gate`) — ✅
