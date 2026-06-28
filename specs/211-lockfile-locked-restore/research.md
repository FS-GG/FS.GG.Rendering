# Phase 0 Research: Locked, reproducible dependency restore

All decisions below are grounded in the actual repo state inspected during planning (the gate
`gate.yml`, the root `Directory.Build.props`, the template `template/base/Directory.Build.props`, the
`FS.GG.Rendering.slnx` membership, and the per-project `nuget.config`/`Directory.Build.props`
inventory). There are **no open NEEDS CLARIFICATION** items; the two empirical questions (R3, R4) are
resolved by *running restore* during implementation, not by guessing — and the plan schedules that.

---

## R1 — Where to express the restore policy

**Decision**: Add the policy to the repo-root `Directory.Build.props`, copying the template's
two-property idiom verbatim:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode Condition="'$(ContinuousIntegrationBuild)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>
```

**Rationale**:
- FR-007 requires the repo to behave like the products it generates. `template/base/Directory.Build.props`
  (Feature 204, SC-002) uses exactly this. Reusing it byte-for-byte is the strongest possible
  consistency guarantee and the plainest code (Principle III).
- The CI-and-lockfile gate is the whole trick: `ContinuousIntegrationBuild=true` (set by GitHub
  Actions) AND a committed `packages.lock.json` must both hold before locked mode engages. That gives:
  - **FR-002** locked restore in CI,
  - **FR-003** fresh-clone/first-restore is never blocked locally (no `ContinuousIntegrationBuild`,
    and on a brand-new project no lockfile yet → both gates false),
  - **Edge "first restore, no lockfile"** — the bootstrap restore generates the lockfile instead of
    failing.

**Alternatives considered**:
- *Per-project props in each `.fsproj`* — rejected: 38× duplication, drift-prone, contradicts the
  single-host template pattern.
- *A repo `nuget.config` with `<RestoreLockedMode>`* — rejected: `nuget.config` cannot express the
  CI/lockfile MSBuild gate; the property must live where `$(ContinuousIntegrationBuild)` is visible.

---

## R2 — Scope boundary: which projects get locked (FR-001 vs FR-006)

**Decision**: Lock exactly the `FS.GG.Rendering.slnx` membership. Achieve the exclusion of the
local-feed lanes **structurally**, not with a scattered opt-out list:

| Lane | In slnx? | Own `nuget.config`? | Own `Directory.Build.props`? | Locked? | Mechanism |
|------|----------|---------------------|------------------------------|---------|-----------|
| `src/**` (18) | yes | no | no | **yes** | inherits root |
| local-tier `tests/**` (17) | yes | no | no | **yes** | inherits root |
| `tools/Rendering.Harness` | yes | no | no | **yes** | inherits root |
| `samples/CanvasDemo`, `samples/SymbologyBoard` | yes | no | no | **yes** | inherits root; ProjectReference-only graph (stable) |
| `samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}` | no | **yes** | **yes (shadows root)** | **no** | their `Directory.Build.props` shadows the root → policy never reaches them |
| `tests/Package.Tests` | no | no | no | **no** | **explicit one-line opt-out** in its `.fsproj` |

**Rationale**:
- The apparent tension between FR-001 ("every project in the gate solution") and FR-006 ("samples …
  out of scope") dissolves once you see that the 4 churny samples are **not in the slnx** and already
  **shadow** the root `Directory.Build.props` (verified: each contains the comment "This file SHADOWS
  the repository-root Directory.Build.props"). So locking "everything the root reaches" == locking
  "the slnx (minus the one opt-out)" — the two requirements coincide.
- `CanvasDemo`/`SymbologyBoard` live under `samples/` but are genuinely in the slnx and consume only
  ProjectReferences + the same external packages as `src/` (verified). Their lockfiles are stable, so
  locking them is correct and adds coverage — they are *not* the "volatile local-feed samples" FR-006
  excludes.
- `Package.Tests` is the one project the root *does* reach but that the spec excludes (release-only
  lane). It builds against ProjectReferences and only touches packed FS.GG.UI.* at **test runtime**
  (the tests restore packages into temp dirs), so its own compile-time graph is actually stable — but
  the spec is explicit (FR-006) and it is restored in `release.yml` under
  `ContinuousIntegrationBuild=true`, so we honor the exclusion to avoid any future surprise.

**Package.Tests exclusion mechanism — why one line in the `.fsproj` works**: `Directory.Build.props`
is imported at the *top* of the project, so a property set in the `.fsproj` body is a *later*
assignment and wins. Setting `<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>` in
`Package.Tests.fsproj` overrides the inherited `true`, so no lockfile is ever written there.
`RestoreLockedMode` also stays false because no lockfile exists. (We add a matching
`<RestoreLockedMode>false</RestoreLockedMode>` defensively with an FR-006 comment.)

**Discovery flagged for the record** (planning honesty): the spec's stated rationale for excluding
`Package.Tests` — "consume repo-owned preview packages" — is only true at **test runtime**, not at
its own restore. We still exclude it (spec-compliant, low-risk), but note the rationale is
runtime-, not restore-scoped. No spec change required.

**Alternatives considered**:
- *`!Exists($(MSBuildProjectDirectory)/nuget.config)` condition on the root property* — elegant for
  the 4 samples (their `nuget.config` would auto-exclude them) but (a) redundant since they already
  shadow root, and (b) **fails for `Package.Tests`** (no `nuget.config`) — so it does not actually
  reduce work and adds an implicit, fragile coupling ("a `src` project that ever gains a
  `nuget.config` silently loses locking"). Rejected in favor of the explicit, commented opt-out.
- *A nested `Directory.Build.props` under `tests/Package.Tests`* — rejected: heavier than one
  property line and would have to re-import the root to keep other inherited settings.

---

## R3 — Promoting NU1603 to an error (FR-004) — VERIFY, don't assume

**Decision**: Add `NU1603` (and `NU1608`, per the spec's discretion) to the root's existing
`WarningsAsErrors`. **Empirically confirm** during implementation that this actually fails restore —
do not trust the property alone.

**Rationale / the trap**:
- The root already sets `TreatWarningsAsErrors=true`, yet the spec states NU1603 "is a warning that
  scrolls past" today. That is the known NuGet behavior: **restore-phase** warnings (NU16xx) are not
  reliably promoted by the compiler-oriented `TreatWarningsAsErrors`; they need an explicit
  `WarningsAsErrors` entry that the restore honors, and sometimes the property must be present at the
  *restore* invocation (not only at build).
- Therefore the implementation MUST prove NU1603 fails by inducing it (quickstart: point a
  centrally-managed version at one no feed provides exactly, so a higher one is substituted) and
  observing a **failed** restore — both via the explicit gate restore step and locally. If the
  property alone does not promote it, fall back to passing `-warnaserror:NU1603` (or
  `-p:WarningsAsErrors=...`) on the explicit `dotnet restore` step in `gate.yml` so the guarantee is
  not dependent on cross-phase property plumbing.
- `NU1605` (downgrade) is already an error by default in the SDK (spec Assumptions), so no action.
  `NU1608` (out-of-range) is promoted alongside NU1603 — same class of silent drift, costs nothing.

**Scope note**: promotion lives in the root props, so it applies to the locked set only (the 4
samples shadow root; `Package.Tests` keeps the root's warnings but its volatile resolution happens at
runtime, not restore, so no spurious NU1603 at its own restore). This keeps the churny lanes from
being tripped by a legitimate preview substitution.

---

## R4 — How CI restores in locked mode (FR-002): property-driven vs explicit step

**Decision**: Keep the **property** (`RestoreLockedMode`) as the *contract* (it is what makes the
repo identical to generated products, FR-007), AND add a thin **explicit named step** to `gate.yml`:

```yaml
- name: Restore (locked)
  run: dotnet restore FS.GG.Rendering.slnx --locked-mode
# then add --no-restore to the existing "Build solution" step
```

**Rationale**:
- The existing gate step `dotnet build FS.GG.Rendering.slnx -c Debug` already does an *implicit*
  restore that, once the props + lockfiles are in place, becomes locked automatically in CI. So the
  pure-property approach would technically satisfy FR-002 with **zero `gate.yml` change**.
- But the gate names every other check (build, default tier, surface drift, version coherence, docs).
  A named, debuggable "Restore (locked)" step makes the new guarantee visible, gives one obvious
  place to surface a clear `::error::` pointing at the regenerate command, and is where an explicit
  `-warnaserror:NU1603` lands if R3 needs it. `--locked-mode` on the CLI also forces locked behavior
  independent of `ContinuousIntegrationBuild` detection — belt and suspenders.
- Adding `--no-restore` to the build step avoids a redundant second restore.

**Alternatives considered**:
- *Property-only, no `gate.yml` change* — most minimal and most template-consistent, but less legible
  and no clean error-annotation hook. Kept as the fallback if the explicit step proves redundant.
- *`dotnet restore` without `--locked-mode`, relying solely on the property* — rejected: the explicit
  flag is clearer and removes any dependence on the env-var gate being set on the runner.

---

## R5 — The single regenerate command (FR-008)

**Decision**: Document exactly one command for intentional dependency updates:

```bash
dotnet restore FS.GG.Rendering.slnx --force-evaluate
```

**Rationale**:
- `--force-evaluate` re-resolves the graph from `Directory.Packages.props` and **rewrites** every
  `packages.lock.json` in the solution, producing a reviewable diff (SC-005). A normal `dotnet
  restore` would honor an existing lockfile and not pick up the version change; `--force-evaluate` is
  the canonical "I changed versions on purpose, update the lock" command.
- One command for the whole solution keeps it frictionless (US3). It is placed in `quickstart.md` and
  a short CONTRIBUTING/docs note so the diff-for-review workflow is discoverable.
- Workflow: bump a version in `Directory.Packages.props` → run the command → commit the changed
  `Directory.Packages.props` **and** the changed lockfiles in the same change → CI's locked restore
  then accepts it (FR-005, Edge "intentional bump without regenerating → CI fails").

---

## R6 — Test evidence shape (Principle V)

**Decision**: Add one deterministic Expecto test in `tests/Build.Tests`
(`RestoreLockTests.fs`) asserting, against the real working tree:
1. every `FS.GG.Rendering.slnx` member has a committed `packages.lock.json`;
2. the excluded lanes (`Package.Tests`, the 4 shadowing samples) do **not** have one;
3. the root `Directory.Build.props` contains `RestorePackagesWithLockFile` + the gated
   `RestoreLockedMode` + NU1603 in `WarningsAsErrors`.

**Rationale**: This is real (not synthetic) filesystem evidence that the policy holds and fences the
scope, and it regresses loudly if someone adds a slnx project without a lockfile or accidentally locks
a churny lane. The *behavioral* proof (locked restore fails on drift / NU1603) is the live gate
restore step + the quickstart perturbation — also real evidence. No synthetic disclosure needed.

**Alternatives considered**: relying only on CI behavior (no unit test) — rejected: a committed test
catches scope regressions at PR time without needing to induce a restore failure, and satisfies
Principle V with deterministic local evidence.
