module Feature541FrozenMirrorGuardTests

// #541 — the guard that stops a Rendering PR editing a frozen skill mirror it does not own.
//
// This file guards THE GUARD. `scripts/check-frozen-mirrors.fsx` is only a gate while a workflow runs
// it; delete the step and the check reports nothing, forever, and the next silent mirror break merges
// green exactly as the previous three did. A check nobody invokes is indistinguishable from a check
// that passes — which is the whole shape of #541 and of the .github#266 fail-open family.
//
// Deliberately NOT a re-implementation of the guard. The script does the real work (it reads the org
// registry, derives the mirrors, compares digests) and it cannot run here: it needs a GH token and the
// network. What CAN be asserted locally, and is worth more than a mock, is that the script exists and
// that the gate actually calls it.

open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relative: string) =
    Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

let private scriptRelative = "scripts/check-frozen-mirrors.fsx"

[<Tests>]
let feature541FrozenMirrorGuardTests =
    testList
        "Feature541 frozen skill-mirror guard"
        [ test "the frozen-mirror guard script exists" {
              Expect.isTrue
                  (File.Exists(repositoryPath scriptRelative))
                  $"`{scriptRelative}` is the only thing in this repo that knows the frozen mirrors exist (#541). Without it, a PR can edit a skill body FS.GG.Game owns and pass every required gate — which happened three times to fs-gg-persistence."
          }

          // The step is the gate. A guard the workflow does not call is a guard that reports green.
          test "the gate actually runs it — a check nobody invokes is a check that passes" {
              let gate = File.ReadAllText(repositoryPath ".github/workflows/gate.yml")

              Expect.stringContains
                  gate
                  "dotnet fsi scripts/check-frozen-mirrors.fsx"
                  "gate.yml must invoke the frozen-mirror guard. Deleting the step is how this class of check dies: it stops reporting, nothing goes red, and the next mirror break merges under a full set of green checks — which is exactly how the three previous ones did (#541, epic .github#266)."
          }

          // It must FAIL the job, not warn. All three previous breaks merged under six green checks; a
          // warning in that stream is a warning nobody reads, which #541 says in as many words.
          test "the guard fails the gate rather than warning" {
              let script = File.ReadAllText(repositoryPath scriptRelative)

              Expect.stringContains
                  script
                  "exit 1"
                  "the guard must exit non-zero on a drifted mirror. A warning would be worth nothing here: every previous break already merged under a full set of green checks."

              Expect.stringContains
                  script
                  "exit 2"
                  "a registry it cannot READ must be a hard failure too — a check that did not run has proved nothing, and reporting green for it is how a gate becomes decoration (the apicompat-check.sh rule)."
          }

          // The ids must be DERIVED from the org registry, never hand-listed — #541's own acceptance, and
          // the reason it is right: the issue says there are eight frozen mirrors. There are four. A
          // hand-list would have shipped the wrong number; the derivation gets it right for free, and a
          // ninth mirror cannot appear without the guard seeing it.
          test "the mirrored ids are derived from the org registry, not hand-listed" {
              let script = File.ReadAllText(repositoryPath scriptRelative)

              Expect.stringContains
                  script
                  "registry/skills.yml"
                  "the mirror set must come from FS-GG/.github's registry (the only place that records who OWNS each skill)"

              Expect.stringContains
                  script
                  "row.Owner <> \"fs-gg-rendering\""
                  "a frozen mirror is DERIVED — a product skill this repo ships but does not own. Hand-listing the ids is how a ninth mirror arrives unguarded (#541 acceptance)."
          } ]
