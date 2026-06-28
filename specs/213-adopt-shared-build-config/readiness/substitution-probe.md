# T021 — Deliberate-substitution probe (INV-4 / C-4) (Feature 213)

Date: 2026-06-28

## Procedure

1. Bumped one repo-owned pin WITHOUT regenerating the lockfile:
   `Directory.Packages.local.props`: `YamlDotNet 18.0.0` → `18.1.0`.
2. Ran `GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode`.
3. Reverted the bump.

## Result — restore FAILED (exit 1), as required

```
error NU1004: Mistmatch between the requestedVersion of a lock file dependency marked as
CentralTransitive and the version specified in the central package management file.
Lock file version [18.0.0, ), central package management version [18.1.0, ).
error NU1004: … The packages lock file is inconsistent with the project dependencies so restore
can't be run in locked mode. Disable RestoreLockedMode or pass --force-evaluate to update the lock file.
```

The locked-mode gate rejected the graph≠lockfile mismatch and pointed at the regenerate hint, exactly
like `gate.yml` would in CI. The `WarningsAsErrors` append rule (research R3) therefore did NOT
silently disable enforcement.

## Code reconciliation (plan expected NU1603/NU1605)

The actual diagnostic is **`NU1004`** (lock-file inconsistency under
`CentralPackageTransitivePinningEnabled`), which fires before NU1603 silent-substitution promotion is
even reached — a *stronger* failure than the plan's hypothesised NU1603/NU1605. Under transitive
pinning a `PackageVersion` change is a CentralTransitive mismatch caught directly by locked mode.

## Revert

`git diff --quiet -- Directory.Packages.local.props` → **REVERTED clean** (pin back at `18.0.0`).
