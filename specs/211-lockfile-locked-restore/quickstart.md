# Quickstart: Validate locked, reproducible restore

Runnable validation that the feature works end-to-end. Maps each User Story / Success Criterion to a
concrete command and expected outcome. Run from the repo root. These prove behavior against **real
restore**, not just that the props are present.

## Prerequisites
- .NET SDK `10.0.x` (`dotnet --info`).
- A clean working tree on branch `211-lockfile-locked-restore`.
- The local NuGet feed populated as usual (`~/.local/share/nuget-local/`) is **not** required for the
  locked set (it restores external packages from nuget.org); it matters only for the excluded
  local-feed lanes, which this feature does not touch.

## Scenario A — Locked CI restore succeeds against committed lockfiles (US1 / SC-001)

Simulate CI locally by setting the env var the gate keys off:

```bash
ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode
```

**Expected**: restore succeeds; the committed `packages.lock.json` graphs are used verbatim; no
re-resolution. (This is exactly what the gate's new "Restore (locked)" step runs.)

## Scenario B — Drift is fail-closed (US1 / SC-002)  *(perturbation)*

```bash
# Temporarily bump a centrally-managed version WITHOUT regenerating the lockfile:
#   edit Directory.Packages.props, e.g. Yoga.Net 3.2.3 -> 3.2.2
ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode
```

**Expected**: restore **fails** (locked-mode mismatch — the resolved graph no longer matches the
committed lockfile). Revert the edit afterwards.

## Scenario C — NU1603 substitution becomes an error (US2 / SC-003)  *(perturbation)*

```bash
# Point a centrally-managed version at one no feed provides EXACTLY so a higher one is substituted:
#   edit Directory.Packages.props to a bogus-but-close version, then:
dotnet restore FS.GG.Rendering.slnx --force-evaluate   # force re-resolution so NU1603 can occur
```

**Expected**: restore **fails** with `NU1603` treated as an error (not a warning that scrolls past).
> If it only warns, the props did not promote the restore-phase warning (research R3) — add
> `-warnaserror:NU1603` to the explicit gate restore step and re-run. Revert the edit afterwards.

## Scenario D — Fresh clone / local build is never blocked (US3 / SC-004)

```bash
# In a fresh clone (or just unset the CI flag — the default for local dev):
dotnet build FS.GG.Rendering.slnx -c Debug
```

**Expected**: builds successfully; locked mode is OFF (no `ContinuousIntegrationBuild`), so even a
stale-or-absent lockfile does not block local work. A brand-new project's first restore generates its
lockfile rather than failing.

## Scenario E — Regenerate after an intentional update is one command (US3 / SC-005)

```bash
# 1) bump a version in Directory.Packages.props on purpose
# 2) regenerate every lockfile:
dotnet restore FS.GG.Rendering.slnx --force-evaluate
# 3) inspect the reviewable diff:
git status --short '*packages.lock.json' Directory.Packages.props
```

**Expected**: the changed `packages.lock.json` files appear in the diff for review; committing them
together with `Directory.Packages.props` makes the subsequent locked CI restore (Scenario A) pass.

## Scenario F — Scope boundary holds (FR-006 / SC-006)

```bash
dotnet test tests/Build.Tests/Build.Tests.fsproj -c Debug
git ls-files '*packages.lock.json'   # inspect which projects are locked
```

**Expected**:
- The Build.Tests `RestoreLock` case passes: every slnx project has a committed lockfile; none of the
  excluded lanes do; the root props carry the policy.
- `git ls-files` lists lockfiles for the LOCKED set only — **no** lockfile under
  `samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}` or `tests/Package.Tests`.

## Scenario G — No regression to existing gate / release lanes (FR-009 / SC-006)

```bash
dotnet build FS.GG.Rendering.slnx -c Debug          # all projects build as before
dotnet fsi scripts/refresh-surface-baselines.fsx    # surface drift unaffected
dotnet fsi scripts/validate-version-coherence.fsx   # version-coherence guard unaffected
```

**Expected**: all pass exactly as before this feature. The samples/template/release pack lanes build
and pack unchanged (they never inherited the policy).

---

### Detail references
- Property/scope contract: [`contracts/restore-policy.md`](./contracts/restore-policy.md)
- Gate step contract: [`contracts/gate-restore.md`](./contracts/gate-restore.md)
- Scope sets & lifecycle: [`data-model.md`](./data-model.md)
- Decisions & the empirical-verification items: [`research.md`](./research.md)
