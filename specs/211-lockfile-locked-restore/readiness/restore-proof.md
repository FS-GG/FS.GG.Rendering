# Restore proof (T006 / T011) — empirical R3/R4 (real restore, not assumed)

Feature 211 · 2026-06-28 · SDK 10.0.301 · slnx `FS.GG.Rendering.slnx` (38 LOCKED projects).
This feature has no running app; per plan.md §Standing-assumption the locked-restore and
NU1603-as-error behaviors are proven by **running restore and observing**, scheduled here in
Foundational before any durable gate/test/docs wiring.

## (a) clean locked restore — quickstart A / SC-001
```
$ ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode
  Restored /home/developer/projects/FS.GG.Rendering/tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj (in 533 ms).
  Restored /home/developer/projects/FS.GG.Rendering/tests/Smoke.Tests/Smoke.Tests.fsproj (in 536 ms).
```

**Result: PASS** (exit 0) — clean locked restore succeeds against the committed lockfiles.

## (b) drift fail-closed — quickstart B / SC-002
Perturbation: `Yoga.Net` 3.2.3 → 3.2.0 in `Directory.Packages.props` **without** regenerating the lockfile, then re-run (a).
```
/home/developer/projects/FS.GG.Rendering/src/Layout/Layout.fsproj : error NU1004: The package reference Yoga.Net version has changed from [3.2.3, ) to [3.2.0, ).The packages lock file is inconsistent with the project dependencies so restore can't be run in locked mode. Disable the RestoreLockedMode MSBuild property or pass an explicit --force-evaluate option to run restore to update the lock file. [/home/developer/projects/FS.GG.Rendering/FS.GG.Rendering.slnx]
/home/developer/projects/FS.GG.Rendering/samples/SymbologyBoard/SymbologyBoard.fsproj : error NU1004: Mistmatch between the requestedVersion of a lock file dependency marked as CentralTransitive and the the version specified in the central package management file. Lock file version [3.2.3, ), central package management version [3.2.0, ). [/home/developer/projects/FS.GG.Rendering/FS.GG.Rendering.slnx]
/home/developer/projects/FS.GG.Rendering/samples/SymbologyBoard/SymbologyBoard.fsproj : error NU1004: The project references FS.GG.UI.Layout whose dependencies has changed.The packages lock file is inconsistent with the project dependencies so restore can't be run in locked mode. Disable the RestoreLockedMode MSBuild property or pass an explicit --force-evaluate option to run restore to update the lock file. [/home/developer/projects/FS.GG.Rendering/FS.GG.Rendering.slnx]
/home/developer/projects/FS.GG.Rendering/src/Controls/Controls.fsproj : error NU1004: Mistmatch between the requestedVersion of a lock file dependency marked as CentralTransitive and the the version specified in the central package management file. Lock file version [3.2.3, ), central package management version [3.2.0, ). [/home/developer/projects/FS.GG.Rendering/FS.GG.Rendering.slnx]
```
**Result: PASS** (exit 1, non-zero) — locked mode refuses the drifted graph; gate would block. Reverted.

## (c) silent version substitution as error — quickstart C / SC-003 (R3 finding)

The board item names **NU1603**. In practice, for this repo's **centrally-managed single-version
pins** the "silent higher-version substitution" NuGet emits is **NU1601** (the direct-reference analog
of the transitive NU1603), and an unavailable **exact** pin is the already-hard NU1102. All three are
the same fail-closed class. Both substitution paths were driven by real restore:

**Test 1 — central pin below what the feed carries (`Yoga.Net` 3.2.3 → 3.1.0 ⇒ 3.2.1 substituted):**
```
$ dotnet restore FS.GG.Rendering.slnx --force-evaluate
src/Layout/Layout.fsproj : error NU1601: Warning As Error: Dependency specified was Yoga.Net (>= 3.1.0) but ended up with Yoga.Net 3.2.1. [FS.GG.Rendering.slnx]
exit=1
```
The substitution is **promoted to an ERROR by the props alone** — note `Warning As Error`, i.e. the
repo's existing `TreatWarningsAsErrors=true` (Directory.Build.props) promotes the restore-phase NU16xx
warning. No `-warnaserror` flag was supplied to this invocation.

**Test 2 — exact pin not on any feed (`Yoga.Net` `[3.2.99]`):**
```
samples/CanvasDemo/CanvasDemo.fsproj : error NU1102: Unable to find package Yoga.Net with version (= 3.2.99) [ Nearest version: 3.2.3 ]
exit=1
```
Hard error by default — restore cannot silently substitute for an exact bracket. Both reverted.

### R3 finding (selects US2 enforcement mechanism)
**Mechanism needed: `props-alone`.** Restore-phase version-substitution warnings are promoted to errors
by the inherited `TreatWarningsAsErrors=true` (empirically shown via `NU1601: Warning As Error`), and
Feature 211 additionally lists `NU1603;NU1608` explicitly in `WarningsAsErrors` for the
transitive-substitution codes. **No `-warnaserror:NU1603;NU1608` is needed on the gate `Restore (locked)`
step** — so US2/T010 makes **no** change to `.github/workflows/gate.yml`; the props are the single
durable enforcement point. (Contract `gate-restore.md` GR2 fallback: not triggered.)

## (T011) US2 confirmation against the final durable wiring — SC-003

The R3 finding above is `props-alone`, so the durable enforcement point is the
`WarningsAsErrors;NU1603;NU1608` + inherited `TreatWarningsAsErrors=true` in `Directory.Build.props`
(T003) — **not** a gate-step flag. Re-running the substitution perturbation (Test 1) against the final
committed config reproduces `error NU1601: Warning As Error` (exit 1): a version NuGet can only satisfy
by substituting a higher one fails restore rather than warning-and-continuing. The gate `Restore
(locked)` step (`.github/workflows/gate.yml`) is therefore the **single** enforcement surface and needs
**no** `-warnaserror` addition (T010 = no-op edit to gate.yml). SC-003 closed against the durable
artifact, not just the T006 spike.

## Post-proof integrity
- Reverted `Directory.Packages.props` to the clean snapshot; `--force-evaluate` regenerate exit: 0
- LOCKED-set lockfiles present: 38 (expected 38)
- Yoga.Net restored to 3.2.3 in props and lockfiles ✓

_Logs: /tmp/claude-1000/-home-developer-projects-FS-GG-Rendering/8ceba034-7816-49fc-94e7-cfec92bcc855/scratchpad/_rp.{a,b,c1,c2,regen} (scratchpad)._

## (T013) US3 local & intentional-update guarantees — SC-004 / SC-005

### (a) fresh-clone local build is not blocked by locked mode (SC-004 / VR-4)
```
$ env -u ContinuousIntegrationBuild dotnet build FS.GG.Rendering.slnx -c Debug
Build succeeded.
```
**Result: PASS** (exit 0) — with `ContinuousIntegrationBuild` unset the RestoreLockedMode condition is false; local build restores open and succeeds.

### (b) intentional bump → single regenerate command → reviewable diff (SC-005)
Throwaway bump `Yoga.Net` 3.2.3 → 3.2.1, then `dotnet restore FS.GG.Rendering.slnx --force-evaluate`:
```
$ git status --short '*packages.lock.json' Directory.Packages.props | head
 M Directory.Packages.props
AM samples/CanvasDemo/packages.lock.json
AM samples/SymbologyBoard/packages.lock.json
A  src/Build/packages.lock.json
A  src/Canvas/packages.lock.json
A  src/ColorPolicy/packages.lock.json
AM src/Controls.Elmish/packages.lock.json
AM src/Controls/packages.lock.json

$ git diff -- src/Layout/packages.lock.json | grep -E '^[+-].*Yoga' | head
```
**Result: PASS** (regenerate exit 0) — one documented command rewrites the lockfiles into a reviewable diff.

Reverted to 3.2.3 and regenerated; LOCKED-set lockfiles: 38 (expected 38).
