# Quickstart: Validate the FS.GG.UI BOM / Metapackage

A runnable guide that proves the feature end-to-end: **pack** a coherent snapshot including the BOM,
**restore+build** a clean consumer against it, prove **deviation is loud**, confirm **reproducibility**,
then **record** the cross-repo registry entry. Details live in the linked contracts; this is the run
guide, not the implementation.

## Prerequisites

- .NET 10 SDK; the repo restored (`dotnet restore FS.GG.Rendering.slnx`).
- Local feed at `~/.local/share/nuget-local/` configured as a NuGet source.
- The `src/Meta` metapackage project added to `FS.GG.Rendering.slnx` (this feature).

## Scenario 1 — Pack the coherent snapshot (members + BOM at one version)

Pick the next coherent version `V` (e.g. `0.1.51-preview.1`; the pack fixes the exact value) and
pack the whole solution to a feed:

```sh
V=0.1.51-preview.1
dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=$V -o ~/.local/share/nuget-local
```

**Expected**: `FS.GG.UI.$V.nupkg` is produced alongside the 16 `FS.GG.UI.*.$V.nupkg` members.
Inspecting `FS.GG.UI.$V.nupkg` shows **no `lib/`** and **16** `<dependency … version="[$V]" />`
entries (contract [`bom-metapackage.md`](./contracts/bom-metapackage.md), BM-A).

## Scenario 2 — One reference ⇒ coherent set (US1)

In a clean throwaway consumer whose only FS.GG.UI declaration is:

```xml
<PackageReference Include="FS.GG.UI" Version="0.1.51-preview.1" />
```

```sh
dotnet restore && dotnet build
```

**Expected**: every resolved `FS.GG.UI.*` is at `$V`; **no** NU1101/NU1605/NU1608; it builds
(contract [`consumer-pinning-behavior.md`](./contracts/consumer-pinning-behavior.md), CP-A/CP-B;
SC-001). Enumerate the resolved graph (e.g. `dotnet list package --include-transitive`) to confirm
all members at `$V`.

## Scenario 3 — Deviation is loud (US2)

Add, to the same consumer, a conflicting pin for one member:

```xml
<PackageReference Include="FS.GG.UI.Scene" Version="0.1.50-preview.1" />  <!-- Y ≠ V -->
```

```sh
dotnet restore   # expected to FAIL
```

**Expected** (corrected against live evidence — see contract CP-3 / research R1 amendment): the exact
`[V]` bracket flags the deviation in both directions — `Y < V` ⇒ **NU1605** (downgrade), `Y > V` ⇒
**NU1608** (outside-constraint). These are NuGet warnings by default; restore them under
`-p:WarningsAsErrors=NU1605%3BNU1608` (the repo / governed-template posture) and restore **fails**
with **no** mixed-version graph (CP-D; SC-003). Repeat with a newer `Y > V` to confirm both
directions — the case a floating range would miss.

## Scenario 4 — Reproducibility & channel (US3 evidence)

```sh
dotnet nuget locals all --clear
dotnet restore   # run 1
# capture resolved set, clear cache, restore again
dotnet restore   # run 2
```

**Expected**: identical resolved member set across the two clean restores (SC-004); the
`FS.GG.UI@$V` channel matches the members (`-preview` ⇒ preview).

## Scenario 5 — Membership parity (always-on gate)

```sh
dotnet test tests/Package.Tests   # Feature207BomMembershipTests
```

**Expected**: green — the BOM nuspec's dependency-ID set equals the packable `FS.GG.UI.*` project
set, every dependency uses the single `[$version$]` token in exact-bracket form (CP-C). Removing a
member from the nuspec (or adding a 17th packable project) turns this **red**.

## Scenario 6 — Live consumer smoke + report (env-gated)

```sh
FS_GG_RUN_BOM_CONSUMER_SMOKE=1 dotnet test tests/Package.Tests   # Feature207BomConsumerTests
```

**Expected**: the test packs the snapshot, runs Scenarios 2–4 against a temp feed, and writes
`specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md` with
`bom-version: / resolved-members-at-version: / forced-mismatch: / result: pass`. The always-on gate
asserts that report.

## Scenario 7 — Record the cross-repo registry entry (US3 — gated)

**Only after** Scenarios 2 + 3 pass, record the BOM in `FS-GG/.github` via the
`cross-repo-coordination` skill / `gh` (contract [`cross-repo-record.md`](./contracts/cross-repo-record.md),
XR-A/XR-B). Do **not** record on a hypothesis.

## Scenario 8 — Optionality regression (FR-007)

```sh
dotnet test tests/Package.Tests   # GovernanceTests / FsSkiaUiVersion single-source invariant
```

**Expected**: green and **unchanged** — the `fs-gg-ui` template's `FsSkiaUiVersion`/CPM pinning is
untouched; the BOM is additive (CP-E; SC-002).
