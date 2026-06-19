// Feature 168 FSI authoring transcript.
// This records the intended public surface before the SkillParity.fs body was
// implemented. The checked surface remains in SkillParity.fsi and the baseline
// file under readiness/surface-baselines.

#r "../../../../../tests/Rendering.Harness/bin/Release/net10.0/Rendering.Harness.dll"

open Rendering.Harness

let request =
    SkillParity.defaultRequest "/home/developer/projects/FS.GG.Rendering"

let rules = SkillParity.defaultGuidanceRules ()
let surfaces = SkillParity.discoverDefaultSurfaces request.RepositoryRoot

printfn "rules=%i surfaces=%i" rules.Length surfaces.Length
