# Quickstart / Validation Guide: FS.GG.UI Simulation Primitives

How to prove the three helpers work end-to-end. Prerequisites: .NET `net10.0` SDK; repo builds green on `main`.

## 1. Build the packages

```sh
dotnet build src/Scene/Scene.fsproj -c Debug
dotnet build src/Canvas/Canvas.Lib.fsproj -c Debug
```

Expected: both build clean (the new `.fsi`/`.fs` pairs compile; no access-modifier gate violations).

## 2. Regenerate & check surface baselines

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff readiness/surface-baselines/FS.GG.UI.Scene.txt readiness/surface-baselines/FS.GG.UI.Canvas.txt
```

Expected new lines (and nothing else): `FS.GG.UI.Scene.Geometry`; `FS.GG.UI.Canvas.Rng`, `FS.GG.UI.Canvas.FixedStep`, and the `Rng` state type entry. Commit the updated baselines.

## 3. FSI transcript — exercise the public surface as a consumer would (Principle I)

```fsharp
// dotnet fsi, referencing the built assemblies (or the *-prelude.fsx)
open FS.GG.UI.Scene
open FS.GG.UI.Canvas

// Geometry — hit detection + containment + centering
Geometry.intersects { X=0.; Y=0.; Width=10.; Height=10. } { X=5.; Y=5.; Width=10.; Height=10. }   // true
Geometry.intersects { X=0.; Y=0.; Width=10.; Height=10. } { X=10.; Y=0.; Width=10.; Height=10. }  // false (edge touch)
Geometry.containsPoint { X=0.; Y=0.; Width=10.; Height=10. } { X=10.; Y=10. }                      // true (inclusive edge)
let c = Geometry.center (Geometry.ofCenter { X=100.; Y=50. } 20. 8.)                                // { X=100.; Y=50. } (round-trip)
// swept: a fast bullet tunneling through a thin wall in one step
Geometry.sweptIntersects { X=0.; Y=0.; Width=2.; Height=2. } { X=100.; Y=0. } { X=50.; Y=0.; Width=1.; Height=10. } // true

// Rng — deterministic, value-type, replayable
let r0 = Rng.ofSeed 42UL
let struct(a, r1) = Rng.nextInt 1 6 r0
let struct(b, _)  = Rng.nextInt 1 6 r0     // b = a  (r0 unchanged; pure)
let struct(a', _) = Rng.nextInt 1 6 (Rng.ofSeed 42UL)  // a' = a  (same seed reproduces)

// FixedStep — pace a sim at 60 Hz from a variable frame delta
FixedStep.drain (1.0/60.0) (1.0/30.0) 0.0    // struct(2, ~0.0)  two steps for a 33ms frame
FixedStep.drain (1.0/60.0) 10.0 0.0          // clamped: struct(15, r)  not 600 steps
FixedStep.drain (1.0/60.0) 0.0 0.0           // struct(0, 0.0)  no time, no steps
```

Each line's expected result is in the comment; a mismatch is a real defect (no success-shaped stubs — Feature 237).

## 4. Run the semantic + property tests

```sh
dotnet test tests/Scene.Tests/Scene.Tests.fsproj     # includes GeometryTests
dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj   # includes RngTests, FixedStepTests
dotnet test tests/Package.Tests/Package.Tests.fsproj # SurfaceAreaTests: baselines match assemblies
```

Expected: all green. Key property assertions (see `data-model.md` for the invariants):
- **Geometry**: `intersects` is symmetric; `center ∘ ofCenter = id`; `sweptIntersects` ⊇ `intersects` at both endpoints; NaN inputs never throw.
- **Rng**: same seed ⇒ identical sequence; input state unchanged by a draw; `nextInt lo hi` stays within `[lo,hi]`; `nextFloat` in `[0,1)`; split streams differ.
- **FixedStep**: `stepCount >= 0`; conservation `newAcc = (acc + clamp dt) - steps*interval` with `0 <= newAcc < interval`; clamp bound holds for huge `dt`; degenerate interval/`dt` return `struct(0, acc)`.

## 5. Confirm additivity (SC-005) & docs currency (FR-012)

```sh
dotnet build FS.GG.Rendering.slnx    # or the repo's full build target — every existing consumer still compiles
```

- No existing test changed or regressed (surface additions only).
- `template/base/docs/product.md` collision/RNG/fixed-step guidance now names the real `Geometry`/`Rng`/`FixedStep` API; `src/Scene/skill/SKILL.md` advertises `Geometry`; the stale `FS.GG.UI.SkillSupport.Random` reference in `src/Elmish/skill/SKILL.md` is corrected.

## 6. (Optional) Real-reuse smoke — SC-004

Re-point one sample game's hand-rolled PRNG at `FS.GG.UI.Canvas.Rng` (e.g. `samples/SampleApps/.../Games/*.fs`) and confirm `samples/SampleApps/SampleApps.Tests/DeterminismTests.fs` still passes — demonstrates a consumer consuming the shipped primitive instead of re-rolling it.
