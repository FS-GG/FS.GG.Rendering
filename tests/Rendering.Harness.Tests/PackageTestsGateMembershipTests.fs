module PackageTestsGateMembershipTests

// FS-GG/FS.GG.Rendering#613 — the guard that pays for retiring the coherence twins.
//
// WHY THIS EXISTS. Sixteen `*CoherenceTests.fs` twins used to hoist a Package.Tests rule into this
// slnx-resident project, because Package.Tests was NOT a slnx member and so ran only in the release
// lane. #540 made it a member, which put all ~325 of its working-tree rules on the PR gate — and left
// fifteen of those twins as byte-identical copies of rules that already run on every PR. #613 deleted
// them.
//
// That deletion is only sound while Package.Tests STAYS on the PR gate, and nothing asserted that.
// Build.Tests' `CadenceCoverageTests` proves the gate's test list EQUALS the slnx test set — so
// membership implies PR-gated — but it says nothing about which projects are members. Drop
// `Package.Tests.fsproj` from `FS.GG.Rendering.slnx` and CadenceCoverageTests stays green (the set
// simply shrinks), while every rule in the project silently leaves the PR gate. Before #613 the twins
// were the accidental backstop for that; now this file is the deliberate one.
//
// WHAT IT LOCKS.
//   G-MEMBER   `tests/Package.Tests/Package.Tests.fsproj` is a member of `FS.GG.Rendering.slnx`
//              (and the project it names actually exists). Membership + CadenceCoverageTests ⇒ every
//              Package.Tests rule runs on the PR gate.
//   G-UNMIRRORED  the fifteen rule files whose PR-time twins #613 retired still exist AND are still
//              compiled into Package.Tests. They have no twin left to catch their loss: delete or
//              unwire one and the rule leaves the gate entirely, with nothing else to red. The old
//              `ReleaseOnlyTwinLockstepTests` L-EXISTS check covered exactly this, via the twin
//              registry it kept; this preserves it now that the registry is gone.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private packageTestsProject = "tests/Package.Tests/Package.Tests.fsproj"

/// Every `<Project Path="…" />` the solution declares.
let private slnxProjects =
    let slnx = File.ReadAllText(repositoryPath "FS.GG.Rendering.slnx")

    Regex.Matches(slnx, "Path=\"([^\"]+\\.fsproj)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value.Replace('\\', '/'))
    |> Set.ofSeq

/// The rule files that lost their PR-time twin in #613. Each one now reaches the PR gate ONLY through
/// Package.Tests' slnx membership, so its continued presence is load-bearing.
let private unmirroredRules =
    [ "ApiSurfaceMirrorTests.fs"
      "AudioProfileWiringTests.fs"
      "AudioSkillSurfaceTests.fs"
      "Feature207BomMembershipTests.fs"
      "Feature238SkillMaterializesWhenTests.fs"
      "Feature240GameCoreSkillTests.fs"
      "Feature246CollisionSkillTests.fs"
      "Feature247VisibilitySkillTests.fs"
      "Feature248LineDrawingSkillTests.fs"
      "Feature249GridsSkillTests.fs"
      "Feature250CollisionSafeVec2Tests.fs"
      "Feature264FragmentProseTests.fs"
      "Feature282EmbeddedTokenGuardTests.fs"
      "ScaffoldIdentifierLeakGuardTests.fs"
      "SwapChecklistTemplateTests.fs" ]

[<Tests>]
let packageTestsGateMembershipTests =
    testList
        "#613 — Package.Tests is on the PR gate (the premise the retired twins rest on)"
        [
          // G-MEMBER — the membership #540 established, asserted rather than assumed.
          test "Package.Tests is a member of FS.GG.Rendering.slnx" {
              // Fail loud, never vacuous: an extraction that matched nothing would satisfy the
              // membership check below only by accident of an empty set.
              Expect.isNonEmpty
                  (Set.toList slnxProjects)
                  "the slnx must declare at least one project — the parse above matched nothing, so this guard is not reading the solution it thinks it is"

              Expect.isTrue
                  (slnxProjects |> Set.contains packageTestsProject)
                  (sprintf
                      "%s must be a member of FS.GG.Rendering.slnx (#540). The gate derives its test list from the slnx, so dropping this entry takes every Package.Tests rule off the PR gate — including the fifteen whose PR-time twins #613 retired on the strength of this membership. If you are removing it deliberately, restore those twins in the same PR."
                      packageTestsProject)

              Expect.isTrue
                  (File.Exists(repositoryPath packageTestsProject))
                  (sprintf "%s is named by the slnx but does not exist on disk" packageTestsProject)
          }

          // G-UNMIRRORED — the fifteen rules that no longer have a twin to fall back on.
          test "every rule whose twin #613 retired still exists and is compiled into Package.Tests" {
              // The files the project actually COMPILES — not merely the strings it happens to contain.
              // A substring scan of the raw project text would also match a name left behind in a
              // comment or an <None Include>, and would then pass for a rule that had been dropped from
              // the build: green on a missing subject, the very thing this file exists to prevent.
              let compiled =
                  let projectText = File.ReadAllText(repositoryPath packageTestsProject)

                  Regex.Matches(projectText, "<Compile\\s+Include=\"([^\"]+)\"")
                  |> Seq.map (fun m -> Path.GetFileName(m.Groups.[1].Value.Replace('\\', '/')))
                  |> Set.ofSeq

              Expect.isNonEmpty
                  (Set.toList compiled)
                  "the parse above matched no <Compile Include=…> items — this guard is not reading the project it thinks it is"

              for rule in unmirroredRules do
                  let rulePath = sprintf "tests/Package.Tests/%s" rule

                  Expect.isTrue
                      (File.Exists(repositoryPath rulePath))
                      (sprintf
                          "%s must still exist: #613 deleted its PR-time twin because this rule already runs on the PR gate. There is no twin left to catch its loss — remove it and the rule is simply gone."
                          rulePath)

                  Expect.isTrue
                      (compiled |> Set.contains rule)
                      (sprintf
                          "%s must still be compiled into Package.Tests: a file present on disk but absent from the project's <Compile> items runs in NO cadence, which is the exact silent-drop #540 and #613 exist to prevent."
                          rule)
          }
        ]
