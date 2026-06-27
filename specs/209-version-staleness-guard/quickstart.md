# Quickstart — Version Coherence Guard

Validation/run guide. Proves the guard catches the Feature-204 drift class locally before a PR can
merge. Implementation details live in `tasks.md`; design rationale in `research.md`.

## Prerequisites
- .NET SDK `net10.0` (`dotnet --version`).
- Repo checkout with tags fetched: `git fetch --tags` (the guard reads `fs-gg-ui/v*`).
- Current state: `FsGgUiVersion = 0.1.50-preview.1`; tags `fs-gg-ui/v0.1.50-preview.1`,
  `fs-gg-ui/v0.1.51-preview.1`; 16 published members + BOM; 11 consumed template pins.

## Scenario A — coherent tree passes (US1 #3)
```bash
dotnet fsi scripts/validate-version-coherence.fsx        # structural verdict-core
echo $?                                                  # => 0
```
Expected: exit `0`; `specs/209-version-staleness-guard/readiness/version-coherence.md` shows
`result: pass`, `provenance: verdict-core`.

## Scenario B — reintroduce the Feature-204 drift (US1 #2, SC-001)
Set the pin behind the latest published tag, then run the guard:
```bash
sed -i 's#<FsGgUiVersion>[^<]*#<FsGgUiVersion>0.1.0-preview.1#' template/base/Directory.Packages.props
dotnet fsi scripts/validate-version-coherence.fsx ; echo $?   # => 1
git checkout -- template/base/Directory.Packages.props        # restore
```
Expected: exit `1` with
```
DRIFT [pin-lags-tag] template/base/Directory.Packages.props:9 <FsGgUiVersion>
  expected: >= 0.1.51-preview.1 (latest fs-gg-ui/v* tag)
  actual:   0.1.0-preview.1
```
Restoring the value → Scenario A passes again. This is the exact 204 condition, now caught here.

## Scenario C — half-bump: one BOM pin lags, policy-independent (US2 #2, SC-002)
Perturb a single BOM dependency off the single `[$version$]` token:
```bash
sed -i 's#id="FS.GG.UI.Scene" version="\[\$version\$\]"#id="FS.GG.UI.Scene" version="[0.1.50-preview.1]"#' src/Meta/FS.GG.UI.nuspec
dotnet fsi scripts/validate-version-coherence.fsx ; echo $?   # => 1  (no WarningsAsErrors needed)
git checkout -- src/Meta/FS.GG.UI.nuspec
```
Expected: `DRIFT [bom-pin-not-token] … FS.GG.UI.Scene` — fails **without** any
`WarningsAsErrors=NU1605;NU1608` policy (FR-004): the in-repo gate does not depend on consumer build
posture.

## Scenario D — unwired new member (US2 #3, SC-004)
Drop a member from the BOM nuspec (simulating a new `src/**` package not wired in):
```bash
# remove one <dependency .../> line from src/Meta/FS.GG.UI.nuspec, then:
dotnet fsi scripts/validate-version-coherence.fsx ; echo $?   # => 1
git checkout -- src/Meta/FS.GG.UI.nuspec
```
Expected: `DRIFT [bom-member-skew] … missing FS.GG.UI.<X>`.

## Scenario E — phantom version, no snapshot tag (Edge: pin with no tag)
```bash
sed -i 's#<FsGgUiVersion>[^<]*#<FsGgUiVersion>0.1.99-preview.1#' template/base/Directory.Packages.props
dotnet fsi scripts/validate-version-coherence.fsx ; echo $?   # => 1
git checkout -- template/base/Directory.Packages.props
```
Expected: `DRIFT [pin-no-tag] … expected a tag fs-gg-ui/v0.1.99-preview.1; actual none`.

## Scenario F — restore-grounded proof (FR-008, US3)
```bash
FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx ; echo $?
```
Expected: packs framework + BOM from source at the pinned `V` to a throwaway feed, restores
`FS.GG.UI@V` in a clean consumer, asserts the **complete** member set resolves to exactly `V`, and
the report gains `provenance: live`, `resolved-members-at-version: 16/16`. A pin that cannot resolve
to the full set fails loudly (`restore-partial`), never a silent partial graph.

## Scenario G — the gate blocks merge (US1 #4, FR-006)
Open a PR carrying any of B–E. The `gate` workflow's **Version coherence guard** step exits non-zero;
branch protection (gate required) blocks merge to `main`. The `DRIFT […]` lines appear in the job
summary (SC-006) — fix the named location, push, gate goes green.

## Tag-fetch note (CI)
The gate's `actions/checkout@v4` must use `fetch-depth: 0` (or `fetch-tags: true`); otherwise `git tag`
is empty and the guard fails closed (exit `2`) rather than passing by absence.

## Done when
- A–E each go red on the named location; A passes clean after each restore.
- F resolves the full 16-member set to `V`.
- G blocks a drifting PR at the gate.
