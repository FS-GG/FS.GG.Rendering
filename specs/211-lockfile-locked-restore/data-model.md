# Phase 1 Data Model: Locked, reproducible dependency restore

This feature has no runtime domain entities. Its "data model" is the set of **build-configuration
artifacts** and the **scope sets** they govern. Documenting them precisely is what makes the scope
boundary auditable.

## Configuration entities

### 1. Restore policy (root `Directory.Build.props`)
The single host expressing locked-restore for the gate solution.

| Property | Value | Condition | Requirement |
|----------|-------|-----------|-------------|
| `RestorePackagesWithLockFile` | `true` | (unconditional at root) | FR-001 — every reached project records a lockfile |
| `RestoreLockedMode` | `true` | `ContinuousIntegrationBuild == true` **AND** `Exists(packages.lock.json)` | FR-002 (CI locked), FR-003 (local/bootstrap never blocked) |
| `WarningsAsErrors` (append) | `…;NU1603;NU1608` | (root) | FR-004 — silent substitution becomes an error |

Inheritance rule: applies to every project that does **not** shadow the root with its own
`Directory.Build.props`. (Validity verified empirically in R3/R4 — see research.)

### 2. Per-project opt-out (`tests/Package.Tests/Package.Tests.fsproj`)
| Property | Value | Why |
|----------|-------|-----|
| `RestorePackagesWithLockFile` | `false` | FR-006 — release-only lane, excluded |
| `RestoreLockedMode` | `false` | defensive; no lockfile exists anyway |

Set in the `.fsproj` body (after the implicit `Directory.Build.props` import) so it overrides the
inherited `true`.

### 3. Lockfile (`packages.lock.json`)
The per-project resolved-graph record (direct + transitive, with content hashes) compared against
during locked restore. **One per locked project**; committed to source control (not gitignored — only
`*.fsx.lock` is ignored). It is the *projection* of `Directory.Packages.props`, never a second place
to edit versions (Assumptions).

### 4. Central package versions (`Directory.Packages.props`)
Unchanged by this feature. Remains the single source of truth for requested versions; the lockfiles
are regenerated from it via `--force-evaluate` (FR-008).

## Scope sets (the boundary, enumerated)

**LOCKED set** — gets a committed `packages.lock.json` (FR-001):
- `src/**` — all 18 projects
- local-tier `tests/**` — all 17 projects **except** `Package.Tests`
- `tools/Rendering.Harness`
- `samples/CanvasDemo`, `samples/SymbologyBoard` (in slnx; ProjectReference-only)

**EXCLUDED set** — no lockfile, never locked (FR-006):
- `samples/AntShowcase`, `samples/SampleApps`, `samples/SecondAntShowcase`,
  `samples/ControlsGallery` — shadow root `Directory.Build.props`; not in slnx
- `tests/Package.Tests` — explicit `.fsproj` opt-out; not in slnx
- template-instantiated products — out of this repo's tree entirely; already covered by the
  template's own lockfile mechanism (Feature 204)

Invariant (asserted by the Build.Tests case, R6): `LOCKED set == FS.GG.Rendering.slnx membership`
and `EXCLUDED set ∩ {has committed lockfile} == ∅`.

## State transitions (lockfile lifecycle)

```
(no lockfile) --first restore (RestorePackagesWithLockFile=true)--> (lockfile generated)
   [bootstrap; never blocked even in CI because Exists() gate is false on that first restore]

(lockfile committed) + CI restore + graph matches --> SUCCESS (graph used verbatim)        [FR-002, SC-001]
(lockfile committed) + CI restore + graph differs --> FAIL (locked-mode mismatch, gate blocks) [FR-002, SC-002]

(Directory.Packages.props version changed) --dotnet restore --force-evaluate--> (lockfile rewritten, reviewable diff) [FR-008, SC-005]
(version changed but lockfile NOT regenerated) + CI --> FAIL (forces the update into the same change) [FR-005, Edge]

(requested version not on feeds → higher substituted) --> FAIL with NU1603-as-error [FR-004, SC-003]
```

## Validation rules

- VR-1 (FR-001): a project in the slnx without a committed lockfile is a defect (Build.Tests fails).
- VR-2 (FR-006): an excluded-lane project *with* a committed lockfile is a defect (Build.Tests fails).
- VR-3 (FR-004): a restore that substitutes a version (NU1603) MUST fail — proven by induced
  perturbation, not assumed (research R3).
- VR-4 (FR-003/SC-004): a non-CI local build, and a first restore with no lockfile, MUST NOT be
  blocked.
- VR-5 (FR-009/SC-006): all pre-existing gate steps and the samples/template/release lanes build and
  pack exactly as before.
