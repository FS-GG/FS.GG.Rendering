# Phase 0 Research: Adopt org-shared .NET build config

All decisions below are grounded in the verified state of this repo and the sibling
`../.github` checkout (`dist/dotnet/`, `scripts/sync-build-config.sh`, `docs/build/README.md`,
ADR-0006). No NEEDS CLARIFICATION remained from the spec.

## R1 — Adoption mechanism: sync-not-fork via `--adopt`

**Decision**: Adopt with `../.github/scripts/sync-build-config.sh --adopt .`, then prune the
generated `*.local.props` (see R2). Re-verify pristineness with `--check`.

**Rationale**: The canonical files carry a "Source of truth: FS-GG/.github" marker and import a
repo-owned `*.local.props` last, so repo settings survive a takeover. `--adopt` renames an existing
hand-authored `*.props` (no marker) to `*.local.props` before writing the canonical file, then copies
`.config/dotnet-tools.json` (no pre-existing file here → straight copy). Hand-copying would lose the
marker and make the `--check` drift gate meaningless.

**Alternatives considered**: Hand-authoring the canonical files (rejected — drift-prone, no marker);
vendoring the sync script into this repo (rejected — duplicates org tooling; the script is reachable
from a sibling checkout and, in CI, via the reusable workflow `.github#18`).

## R2 — What `--adopt` produces, and the mandatory prune of `*.local.props`

`--adopt` moves the **entire** current root `Directory.Build.props` / `Directory.Packages.props` into
the matching `*.local.props`. Because the canonical file imports the local file **last** (MSBuild
last-write-wins), any property the local file still sets will **override** the canonical one. So the
local files MUST be pruned of everything the canonical now owns:

**`Directory.Build.local.props` — remove (now owned by canonical):**
- the entire "Restore (211)" group: `RestorePackagesWithLockFile`, the `RestoreLockedMode` **CIB**
  gate, and the `NU1603;NU1608` promotion. **Leaving the old CIB `RestoreLockedMode` here would
  silently re-defeat the gate migration** (local wins).

**`Directory.Build.local.props` — keep (repo-specific):**
- `TargetFramework` (`net10.0`), `LangVersion`, `Nullable`, `AllowUnsafeBlocks`
- `TreatWarningsAsErrors` and the F# warning promotions `FS0025;FS0026;FS0052;FS0064;FS0078`
- the `Package` metadata group (`Version`, `Authors`, repository URLs, license, `PackageReadmeFile`)
- the `FsDocs` group
- the `FSharp.Core` **PackageReference** item group (no version → resolves from CPM) and the README
  `None Include … Pack=true` group

**`Directory.Packages.local.props` — remove:** the redundant `ManagePackageVersionsCentrally`
property group (canonical sets it, plus transitive pinning) **and the `FSharp.Core` `PackageVersion`**
(now in the org baseline; a duplicate raises CPM `NU1504`/`NU1011`).

**`Directory.Packages.local.props` — keep:** every other `PackageVersion` (Fable.Elmish, SkiaSharp*,
HarfBuzz*, Silk.NET*, Yoga.Net, YamlDotNet; the test-only Expecto/Test.Sdk/YoloDev; the build-tooling
Fake.*/FSharp.SystemTextJson/XParsec/etc.). Versions are unchanged.

## R3 — `WarningsAsErrors` ordering hazard (append, do not clobber)

**Decision**: In `Directory.Build.local.props`, the first F# warning-promotion line MUST **append**:
`<WarningsAsErrors>$(WarningsAsErrors);FS0025;FS0026;FS0052;FS0064</WarningsAsErrors>` (then
`;FS0078`), not assign an absolute list.

**Rationale**: The canonical `Directory.Build.props` sets `WarningsAsErrors` to include `NU1603;NU1608`
**before** importing the local file. If the local file *assigns* `WarningsAsErrors=FS0025;…` it
clobbers `NU1603;NU1608`, silently disabling silent-substitution enforcement. The original root file
already used the append idiom for FS0078 and the NU pins; preserving append-form for the first line
keeps the canonical NU promotions intact. This is verified by the deliberate-substitution probe (R7),
not by the `RestoreLockTests` string check (which reads the canonical root file and would not catch a
clobber that happens only in the effective, post-import property value).

## R4 — Transitive pinning regenerates all 39 lockfiles

**Decision**: After adoption, run `dotnet restore FS.GG.Rendering.slnx --force-evaluate` to regenerate
every `packages.lock.json`, then commit the (large, mechanical) diff. Also regenerate the lockfiles for
the lanes outside the slnx that still carry one (the two samples under `samples/CanvasDemo`,
`samples/SymbologyBoard`, and any other committed lockfile) so no lockfile is left stale.

**Rationale**: The canonical `Directory.Packages.props` enables `CentralPackageTransitivePinningEnabled`
(currently unset here). Transitive pinning records resolved transitive versions into the lockfiles, so
their content changes even though no top-level version changes. `gate.yml` enforces locked restore with
an explicit `--locked-mode`, so a stale lockfile would fail CI. The regenerated set must be committed.

**Risk to verify empirically**: transitive pinning can surface a previously-floating transitive version
conflict. The restore→build green run (R7) is the gate; if a conflict appears it is resolved by an
explicit `PackageVersion` in `Directory.Packages.local.props` (documented at the use site).

## R5 — Gate migration does not weaken CI enforcement

**Decision**: Migrate the property gate to `GITHUB_ACTIONS` and rely on the existing explicit
`dotnet restore … --locked-mode` step in `gate.yml` as the enforcement point.

**Rationale**: `gate.yml` already runs `dotnet restore --locked-mode` explicitly (the single restore
enforcement point; the build step uses `--no-restore`). The `RestoreLockedMode` MSBuild property is
belt-and-suspenders. `GITHUB_ACTIONS` is set automatically in Actions and never locally, so CI still
fails closed on drift while a fresh local clone is never blocked (the `And Exists(lockfile)` guard
additionally lets a first restore bootstrap). The repo never forces `ContinuousIntegrationBuild=true`
in a way the local gate depended on, so nothing else needs rewiring.

## R6 — CI drift-check wiring is deferred (bounded follow-up)

**Decision**: Do **not** add a bespoke per-repo CI step that runs `sync-build-config.sh --check` in
this feature. Use `--check` as **acceptance evidence** at adoption time; rely on the existing
`--locked-mode` gate for ongoing dependency enforcement.

**Rationale**: The intended ongoing drift gate is the org reusable workflow `contract-coherence.yml`
(`.github#18`), which is Backlog/blocked on the Coordination board. Adding a one-off step that shells
to a sibling checkout would be divergent, fragile tooling (Principle III) and would be replaced when
`.github#18` lands. This deferral is explicit and bounded (constitution Development Workflow), tracked
by `.github#18`.

## R7 — Verification strategy (the build-level "live" proof)

**Decision**: The Foundational verification, run before marking adoption done:
1. `dotnet restore FS.GG.Rendering.slnx --force-evaluate` → regenerate + commit lockfiles (R4).
2. `dotnet restore FS.GG.Rendering.slnx --locked-mode` → second restore is byte-reproducible (SC-004).
3. `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore` → green (SC-003).
4. `dotnet test` (Build.Tests + SkiaViewer.Tests at minimum) → updated policy tests pass (US-evidence).
5. `../.github/scripts/sync-build-config.sh --check .` → zero drift (SC-001).
6. Gate probes: with `GITHUB_ACTIONS=true` + a lockfile, locked restore engages; with the variable
   unset, restore is not blocked (SC-002 / US2).
7. Deliberate-substitution probe: temporarily perturb a pin/lockfile and confirm `--locked-mode`
   restore fails with NU1603/NU1605 (confirms R3 did not clobber enforcement); then revert.

**Rationale**: Deterministic string tests can pass while the effective build property differs
(Feature 175 lesson); the live restore proof and the substitution probe are the honest checks.

## R8 — Test updates required (verified breakages)

Two tests read the **root** config files and break on adoption:
- `tests/Build.Tests/RestoreLockTests.fs` (the props-policy test): asserts the root
  `Directory.Build.props` contains `ContinuousIntegrationBuild`. Update the assertion (and its message)
  to `GITHUB_ACTIONS`. The other assertions (`RestorePackagesWithLockFile`, `<RestoreLockedMode`,
  `NU1603` in `WarningsAsErrors`) still hold against the canonical file.
- `tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs`: reads the root
  `Directory.Packages.props` asserting it contains `SkiaSharp.HarfBuzz`. After adoption that pin lives
  in `Directory.Packages.local.props`; update the path to read the local file (the place the repo now
  owns its package versions).

No other test reads the root config for a moved value (`tests/Package.Tests/*` and `Tests.fs` read
`template/base/…`, which is out of scope). The 38-project slnx-membership / lockfile-coverage tests in
`RestoreLockTests` are unaffected (membership unchanged; every member keeps a regenerated lockfile).
