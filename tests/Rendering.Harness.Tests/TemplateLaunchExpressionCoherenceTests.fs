module TemplateLaunchExpressionCoherenceTests

// FS-GG/FS.GG.Rendering#350 — a PR-VISIBLE lock on the generated product's per-family launch host.
//
// WHY THIS EXISTS. The 0.5.0 release was tagged but NEVER PUBLISHED. The release-only "Generated
// product (template instantiation)" job failed because #245's audio seam changed the game-family
// DEFAULT launch in `template/base/src/Product/Program.fs` from
//     Viewer.runApp viewerOptions generatedHost
// to
//     Viewer.runAppWithAudio viewerOptions audioSink generatedHost
// while the mirroring assertions in `template/base/tests/Product.Tests` still expected the pre-audio
// call. Those Product.Tests run ONLY when the template is instantiated in the release lane, so no PR
// gate ever exercised them — the drift sat latent until release, where it skipped the publish job
// (fixed after the fact by #352). This is the standing "PR-gated tests must be in the slnx" gap.
//
// WHAT IT LOCKS. This test reads the two template files STATICALLY (no instantiation, so it is cheap
// enough for the PR-gated slnx lane) and asserts the SET of default-branch launch expressions
// `Program.fs` emits equals the SET the `Product.Tests` assert. Change one without the other and this
// reds the PR instead of the release. It is deliberately the same coherence the release-only
// generated tests enforce, hoisted one gate earlier.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private programPath = repositoryPath "template/base/src/Product/Program.fs"
let private productTestsDir = repositoryPath "template/base/tests/Product.Tests"

// The generated product's per-family DEFAULT (no window flag) launch call. The window-flag overload
// threads `viewerOptions (AppRoot.WindowOptions.toViewerLaunchRequest windowBehavior) ...`, so
// requiring `viewerOptions` to be followed directly by an optional `audioSink` and the host value
// matches ONLY the default branch — never the flag overload — which is exactly what Product.Tests
// assert. Distinct full strings (`Viewer.runApp viewerOptions generatedHost` is NOT a substring of
// `Viewer.runAppWithAudio viewerOptions audioSink generatedHost`), so set membership is unambiguous.
let private launchExpression =
    Regex(@"(?:Viewer|ControlsElmish)\.run[A-Za-z]+ viewerOptions (?:audioSink )?(?:generatedHost|interactiveHost)")

let private expressionsIn (text: string) : Set<string> =
    launchExpression.Matches text |> Seq.map (fun m -> m.Value) |> Set.ofSeq

/// `Program.fs` carries every family's branch behind `//#if (profile == ...)` markers; the live
/// viewer entrypoint's default path begins at the LAST `| None ->` (an earlier one belongs to the
/// headless-scene entrypoint, which launches no viewer). Mirror the slice the Product.Tests take.
let private programDefaultBranch () =
    let source = File.ReadAllText programPath
    match source.LastIndexOf("| None ->", StringComparison.Ordinal) with
    | -1 -> failtest "Program.fs must contain a `| None ->` default launch branch"
    | index -> source.Substring index

let private productTestsText () =
    match Directory.GetFiles(productTestsDir, "*.fs", SearchOption.AllDirectories) with
    | [||] -> failtest "template/base/tests/Product.Tests must contain F# test sources"
    | files -> files |> Array.map File.ReadAllText |> String.concat "\n"

[<Tests>]
let templateLaunchExpressionCoherenceTests =
    testList
        "Template launch-expression coherence (FS.GG.Rendering#350)"
        [
          test "the generated Program.fs and its Product.Tests agree on the per-family default launch host" {
              let programExpressions = expressionsIn (programDefaultBranch ())
              let testExpressions = expressionsIn (productTestsText ())

              // Fail loud, never vacuous: a regex that silently matched nothing would satisfy the
              // set-equality below trivially while catching no drift at all.
              Expect.isNonEmpty
                  (Set.toList programExpressions)
                  "Program.fs must emit at least one recognizable default launch expression"
              Expect.isNonEmpty
                  (Set.toList testExpressions)
                  "Product.Tests must assert at least one default launch expression"

              let onlyInProgram = Set.difference programExpressions testExpressions |> Set.toList
              let onlyInTests = Set.difference testExpressions programExpressions |> Set.toList

              Expect.isEmpty
                  onlyInProgram
                  (sprintf
                      "Program.fs emits default launch host expression(s) the Product.Tests do not assert — the release-only generated-product tests would fail and skip the 0.5.x publish. Update template/base/tests/Product.Tests to expect: %A"
                      onlyInProgram)
              Expect.isEmpty
                  onlyInTests
                  (sprintf
                      "Product.Tests assert default launch host expression(s) Program.fs no longer emits — a stale expectation. Reconcile with template/base/src/Product/Program.fs: %A"
                      onlyInTests)
          }

          // Belt-and-braces on the regex intent: the three shipped families must each be present, so a
          // future rename of the host entry points cannot leave the lock quietly matching nothing.
          test "all three product families' default launch hosts are present and locked" {
              let programExpressions = expressionsIn (programDefaultBranch ())

              for expected in
                  [ "ControlsElmish.runInteractiveApp viewerOptions interactiveHost" // app / controls family
                    "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" // game family (#245 audio seam)
                    "Viewer.runApp viewerOptions generatedHost" ] do // headless / governed family
                  Expect.isTrue
                      (programExpressions.Contains expected)
                      (sprintf "Program.fs default branch must emit `%s`" expected)
          }
        ]
