# Contract: Gate locked-restore step (`gate.yml`)

How the required pre-merge gate enforces locked restore (FR-002) and surfaces NU1603 (FR-004).

## Change to `gate.yml`

Insert a named restore step **before** the existing "Build solution (net10.0)" step, and add
`--no-restore` to the build step so restore happens exactly once.

```yaml
# 211 (FR-002/FR-004) — locked restore against committed packages.lock.json. On a graph that differs
# from the lockfile (or an un-pinnable version, NU1603) restore fails and the gate blocks. The
# RestoreLockedMode property (Directory.Build.props) makes the repo behave like generated products;
# --locked-mode here forces it explicitly and gives one clear place to point at the regenerate command.
- name: Restore (locked)
  run: |
    set -euo pipefail
    if ! dotnet restore FS.GG.Rendering.slnx --locked-mode; then
      echo "::error::locked restore failed — the resolved graph does not match the committed packages.lock.json (or a version was substituted, NU1603). If this is an intentional dependency change, run: dotnet restore FS.GG.Rendering.slnx --force-evaluate  and commit the updated lockfiles."
      exit 1
    fi

# existing step, now with --no-restore:
- name: Build solution (net10.0)
  run: dotnet build FS.GG.Rendering.slnx -c Debug --no-restore
```

## Guarantees
- **GR1 (FR-002, SC-001/SC-002)**: a clean run restores the slnx in locked mode; a graph differing
  from any committed lockfile fails the step → PR blocked. Two runners resolve identically (no
  re-resolution under `--locked-mode`).
- **GR2 (FR-004, SC-003)**: if NU1603/NU1608 is not promoted by the props alone (research R3), add
  `-warnaserror:NU1603` to this `dotnet restore` invocation so substitution fails here. The step is
  the single enforcement point.
- **GR3 (FR-009, SC-006)**: every other gate step (probe, default tier, surface-baseline drift,
  version-coherence guard, docs-strict, harness offscreen, GL local checks) is unchanged and keeps
  passing. The only edit to an existing step is `--no-restore` on the build (restore already done).
- **GR4**: `release.yml` and `capability.yml` are unchanged; their restores become locked
  automatically via the property only for projects that have a committed lockfile (the LOCKED set).
  `Package.Tests` (release-only) is opted out, so `release.yml` is unaffected (SC-006).

## Regenerate command (FR-008, the single documented command)

```bash
dotnet restore FS.GG.Rendering.slnx --force-evaluate
```

Re-resolves from `Directory.Packages.props` and rewrites all lockfiles → reviewable diff (SC-005).
Run it after any intentional version change; commit `Directory.Packages.props` + the changed
lockfiles together.

## Negative / perturbation contract (the proof, not the assumption)
- **NC1**: point a `Directory.Packages.props` version at one not covered by the committed lockfile →
  the gate's "Restore (locked)" step MUST fail (not silently substitute).
- **NC2**: request a version no configured feed provides exactly (forces a higher substitution) →
  NU1603 MUST fail the step.
- **NC3**: a fresh clone local `dotnet build` (no `ContinuousIntegrationBuild`) MUST succeed and MUST
  NOT be blocked by locked mode.
