# Feature Specification: Locked, reproducible dependency restore (repo lockfiles + locked-mode CI)

**Feature Branch**: `211-lockfile-locked-restore`

**Created**: 2026-06-28

**Status**: Draft

**Change Classification**: **Tier 2 (internal change)** — no public F# API surface is added or changed, no new shipped dependency is introduced, and no observable library behavior changes. The feature pins the *resolution* of the already-pinned dependency graph and adds a CI restore guard; `.fsi` files and surface-area baselines remain untouched.

**Input**: User description: "next Rendering item on the project coordination board" → resolved to the Rendering slice of the P5 Versioning board item *"Commit packages.lock.json + CI --locked-mode in consumer repos; NU1603 → error"* (Coordination board #1, repo scope `cross-repo`, the only Backlog item; the one Rendering-scoped item — the P1 lifecycle-agnostic template epic — is already In review/closed by Feature 210).

## Why this, why now

The FS.GG.Rendering repo consumes external dependencies (SkiaSharp/HarfBuzz/Silk.NET previews, Yoga, Expecto, etc.) that are version-pinned in `Directory.Packages.props` via Central Package Management, **but the transitive graph is not locked**: there is no committed `packages.lock.json` anywhere in the repo, restore is not run in locked mode, and the gate does not treat a "requested version not found, substituted a different one" outcome (NU1603) as an error. A transitive dependency can therefore float between restores and a drifted resolution can merge silently.

The fix already exists — for *generated products*. `template/base/Directory.Build.props` (Feature 204, SC-002) gives every scaffolded product `RestorePackagesWithLockFile` plus a CI/lockfile-gated `RestoreLockedMode`. This feature brings the **Rendering repo's own restore** up to the same bar the repo already hands its consumers, closing the last unlocked consumer in the org's P5 versioning hardening line (cf. Feature 207 BOM metapackage, Feature 209 staleness guard).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Locked, reproducible CI restore (Priority: P1)

A maintainer merges to `main`. The gate (`gate.yml`) restores the gate solution against committed lockfiles in locked mode. If the resolved dependency graph matches the committed lockfiles, the build proceeds; if anything resolves differently than what is recorded, restore fails and the PR is blocked.

**Why this priority**: This is the core value — it makes "what built and was tested" reproducible run-to-run and prevents a silently drifted transitive graph from reaching `main`. It is the MVP: lockfiles committed + locked-mode CI is independently shippable and delivers the reproducibility guarantee on its own.

**Independent Test**: On a clean checkout, run the gate's restore/build in CI mode; it succeeds against the committed lockfiles. Then perturb the resolved graph (e.g. point a centrally-managed version at one not covered by the lockfile) and confirm the CI restore fails rather than silently resolving a substitute.

**Acceptance Scenarios**:

1. **Given** committed lockfiles that match the current `Directory.Packages.props`, **When** the gate runs a CI restore in locked mode, **Then** restore succeeds and the recorded graph is used verbatim.
2. **Given** a dependency change that is NOT reflected in the committed lockfiles, **When** the gate runs in CI mode, **Then** locked-mode restore fails and the gate blocks the merge.
3. **Given** the gate solution is built in CI, **When** restore runs twice on separate runners, **Then** both resolve an identical dependency graph (zero drift).

---

### User Story 2 - Silent version drift becomes a hard error (Priority: P2)

A dependency change requests a version that the configured feeds cannot satisfy exactly, and NuGet substitutes the nearest higher version (NU1603). Today that is a warning that scrolls past; after this feature it fails the gate.

**Why this priority**: Locked-mode catches drift against the lockfile, but NU1603 also signals an *un-pinnable* request the moment it is introduced — promoting it to an error stops the drift at its source and complements US1. Valuable but secondary to having locked restore at all.

**Independent Test**: Introduce a centrally-managed version that no feed provides exactly so a higher one is substituted; confirm the gate fails with NU1603 treated as an error instead of warning-and-continuing.

**Acceptance Scenarios**:

1. **Given** a requested package version not available exactly on the configured feeds, **When** restore substitutes a higher version (NU1603), **Then** the gate fails the build.
2. **Given** all centrally-managed versions resolve exactly, **When** the gate runs, **Then** no NU1603 is raised and the build proceeds.

---

### User Story 3 - Frictionless local and intentional-update flow (Priority: P3)

A developer clones fresh and builds locally without being blocked by locked mode; and when they intentionally change a dependency, a single documented command regenerates and re-commits the lockfiles so the diff is reviewable.

**Why this priority**: Reproducibility must not tax day-to-day local work or make intentional upgrades painful. Important for adoption, but the guarantee in US1/US2 is what the board item is about, so this is P3.

**Independent Test**: On a fresh clone with no local restore state, run a normal local build and confirm it is not blocked by locked mode. Then bump a centrally-managed version, run the documented regenerate command, and confirm the lockfile diff appears and CI then accepts it.

**Acceptance Scenarios**:

1. **Given** a fresh clone, **When** a developer runs a normal local build (not in CI mode), **Then** restore is not blocked even before any lockfile exists locally.
2. **Given** an intentional dependency version change, **When** the developer runs the documented lockfile-regeneration command, **Then** the updated lockfiles are produced and show up in the change for review, and the subsequent CI run passes.

---

### Edge Cases

- **First restore, no lockfile yet** (e.g. brand-new project added to the solution): locked mode must not block it; the lockfile is generated on that restore and committed.
- **Volatile local-feed previews**: projects that restore the repo-owned FS.GG.UI.* preview packages from the local feed (samples, template-instantiated products, release-only pack / `Package.Tests`) change version every merge; they are explicitly out of scope so their churn does not destabilize the locked set.
- **Intentional dependency bump without regenerating lockfiles**: CI must fail (locked-mode mismatch), forcing the lockfile update into the same change.
- **New transitive dependency pulled in by an upgraded direct dependency**: surfaces as a locked-mode mismatch until the lockfile is regenerated.
- **Lockfile present locally but stale relative to `Directory.Packages.props`**: a local CI-mode/verify build reproduces the gate failure so drift is catchable before push.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every project in the gate solution (`FS.GG.Rendering.slnx`: `src/**`, local-tier `tests/**`, the harness, and build-tooling restored as part of the gate) MUST restore against a committed lockfile that records the exact transitive dependency graph.
- **FR-002**: CI MUST restore the gate solution in locked mode, so any resolution that differs from the committed lockfile fails the restore/build and blocks the merge.
- **FR-003**: A local build that is not running in CI MUST NOT be blocked by locked mode, and a first restore when no lockfile yet exists MUST succeed and produce the lockfile (bootstrap is never blocked) — mirroring the repo's existing template behavior.
- **FR-004**: The gate MUST treat NU1603 (requested version not found; a different/higher version substituted) as an error rather than a warning.
- **FR-005**: Lockfiles MUST be committed to source control; an intentional dependency change MUST update them within the same change set, enforced by FR-002 (an un-updated lockfile fails CI).
- **FR-006**: Scope MUST be bounded to the externally sourced, centrally-managed dependencies of the gate solution. Projects that restore repo-owned preview packages from the volatile local feed — samples, template-instantiated products, and the release-only pack/`Package.Tests` lane — are OUT of scope and MUST NOT be destabilized by this feature.
- **FR-007**: The repo's locked-restore mechanism MUST be consistent with the proven template pattern (CI-and-lockfile-gated locked mode) so the repo and the products it generates behave the same way.
- **FR-008**: There MUST be a single documented command to regenerate the lockfiles after an intentional dependency update, producing a reviewable diff.
- **FR-009**: Existing gate behavior unrelated to restore (build, tests, surface baselines, version-coherence, docs-strict) MUST continue to pass unchanged; this feature only adds the locked-restore guarantee.

### Key Entities *(include if feature involves data)*

- **Lockfile (`packages.lock.json`)**: the per-project record of the exact resolved dependency graph (direct + transitive, with content hashes); the artifact compared against during locked-mode restore.
- **Gate solution (`FS.GG.Rendering.slnx`)**: the set of projects whose dependency graph is locked and verified by the gate; the unit of scope for this feature.
- **Central package versions (`Directory.Packages.props`)**: the single source of truth for requested versions under Central Package Management; lockfiles are the resolved projection of it.
- **Restore policy (`Directory.Build.props`)**: where `RestorePackagesWithLockFile` and the CI/lockfile-gated `RestoreLockedMode` (plus NU1603-as-error) are expressed for the repo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A clean CI restore of the gate solution resolves an identical dependency graph on every run (zero drift), verified by locked-mode restore succeeding only when the committed lockfiles match.
- **SC-002**: A deliberately introduced, unrecorded dependency-graph change causes the gate to fail and block the PR 100% of the time (no silent pass).
- **SC-003**: A package that resolves to a version other than requested (NU1603) fails the gate instead of emitting a warning, in 100% of such cases.
- **SC-004**: A fresh-clone local build succeeds with no manual lockfile steps and is never blocked by locked mode.
- **SC-005**: Regenerating lockfiles after an intentional dependency change is a single documented command, and the change is visible in the diff for review.
- **SC-006**: No regression to the samples, template-instantiation, or release/pack lanes — they build and pack exactly as before this feature.

## Assumptions

- CI runs with `ContinuousIntegrationBuild=true` (GitHub Actions sets this), which the gated locked-mode condition keys off — matching the template's Feature 204 pattern; local builds do not, so they are not locked.
- Central Package Management (`Directory.Packages.props`) remains the version source of truth; lockfiles are its resolved projection, not a second place to edit versions.
- The locked unit is the gate solution (`FS.GG.Rendering.slnx`). Samples (own `nuget.config` → local feed), template-instantiated products (already covered by the template's own lockfile mechanism), and the release-only pack/`Package.Tests` lane are excluded because they consume repo-owned preview packages whose versions change every merge; locking them would produce constant churn without reproducibility value.
- NU1603 is the firm drift code named by the board item; downgrade (NU1605) is already an error by default in the SDK, and out-of-range (NU1608) may be promoted alongside NU1603 at implementation discretion without changing this spec's intent.
- This is the **Rendering slice** of the org-level P5 cross-repo item; the SDD, Governance, and Templates consumer repos own their own equivalent slices and are out of scope here (tracked separately on the Coordination board).
