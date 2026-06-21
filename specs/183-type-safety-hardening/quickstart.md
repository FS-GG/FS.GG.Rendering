# Quickstart — Validating Type-Safety Hardening (Feature 183)

A run/validation guide. The binding invariants live in [contracts/](./contracts/); design in
[data-model.md](./data-model.md) and [research.md](./research.md). This is **Tier 1**: behavior stays
byte-stable, but `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer` intentionally change surface and bump.

## Prerequisites

- Linux desktop with a display for GL: prefix GL-touching runs with `DISPLAY=:1`.
- .NET `net10.0` SDK; `dotnet fsi` for the scripts.
- Local feed at `~/.local/share/nuget-local` (used by samples/template via PackageReference).

## 0. Capture the baseline (Foundational — before any edit)

```bash
cd /home/developer/projects/FS.GG.Rendering
mkdir -p specs/183-type-safety-hardening/readiness/{baseline,post-change}

dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx          # 12 baselines; `git status` must be clean after
git diff --quiet -- readiness/surface-baselines && echo "baselines clean ✓"

DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/183-type-safety-hardening/readiness/baseline/test-baseline.md
# Expect the known reds: Package.Tests (8 fail) + ControlsGallery (2 fail); 14 greens.
```

Also snapshot the **behavior corpus** into `baseline/`: `SceneNode` codec bytes for one value of every
case, scene hashes/fingerprints for the control corpus, and `damageRegion`/`validateDamage`/
`classifyWindowObservation` outputs for fixed inputs. These are the §A byte-diff inputs.

## 1. US1 — Control `Kind` registry (Tier 2, no bump)

```bash
# After adding src/Controls/ControlKindRegistry.fs and migrating the ~13 dispatch sites:
dotnet build FS.GG.Rendering.slnx -c Debug
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --quiet -- readiness/surface-baselines && echo "Controls surface UNCHANGED ✓"   # MUST be clean
```
**Pass when:** Controls scene-hash/fingerprint/inspection/a11y/virtualization outputs are byte-identical
to baseline; the catalog↔registry completeness test is green; **no** baseline or `.fsi` changed.

## 2. US2 — `SceneNode` codec symmetry + DU naming (Tier 1, bump Scene)

```bash
# After naming the 19 bare-tuple cases (arity/types preserved) and table-driving the codec:
dotnet build FS.GG.Rendering.slnx -c Debug
DISPLAY=:1 dotnet test tests/Scene.Tests/Scene.Tests.fsproj -c Release   # incl. every-case round-trip
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines src/Scene/Scene.fsi              # ONLY Scene field names / planned
```
**Pass when:** every-case codec round-trip is byte-identical to baseline bytes; the table has 25 rows /
tags 0–24; the whole solution still compiles (source-compatible DU); `Scene.fsi` diff is only field
names; `Scene.fsproj` `<Version>` bumped.

## 3. US3 — named flag records (Tier 1, bump Scene + SkiaViewer)

```bash
# After converting the 6 functions and updating call sites (incl. Controls/Inspection.fs:460):
dotnet build FS.GG.Rendering.slnx -c Debug
DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines 'src/**/*.fsi'                   # ONLY Scene + SkiaViewer, planned
```
**Pass when:** damage/diagnostic/promotion outputs byte-identical for fixed inputs; `Scene.txt` +
`SkiaViewer.txt` show only the new record types; `Controls.txt` unchanged; SkiaViewer (and Scene if not
already) bumped.

## 4. Polish — full sweep, feed + sample/template alignment

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/183-type-safety-hardening/readiness/post-change/test-baseline.md
# Same red/green as baseline (Package.Tests + ControlsGallery only).

# Repack the bumped packages and realign the actively-maintained sample:
dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase
DISPLAY=:1 dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
```
**Done when:** SC-001…SC-008 hold — full solution builds + tests at baseline red/green; only
Scene/SkiaViewer surfaces changed (and only as planned); bumps applied + feed/samples/template aligned;
behavior byte-stable across all three stories; every FR-010 retention recorded with rationale.

## Quick reference

| Need | Command |
|---|---|
| Regenerate + diff surface baselines | `dotnet fsi scripts/refresh-surface-baselines.fsx && git diff -- readiness/surface-baselines` |
| Full Release test sweep | `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out <path>` |
| Repack + retarget a sample | `dotnet fsi scripts/dev-repack.fsx --sample samples/<Name>` |
| Known reds (baseline-not-regression) | `specs/182-god-module-splits/readiness/baseline/known-reds.md` |
