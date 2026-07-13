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
// (fixed after the fact by #352).
//
// WHY THIS TWIN SURVIVED #613 — AND WHY THAT REASON IS NOW DEAD (#719). #613 retired fifteen sibling
// twins and kept this one, on the stated ground that "no PR job instantiates the template": its
// counterpart is `template/base/tests/Product.Tests`, which exists only once the template has been
// scaffolded, and only release.yml scaffolded it. #680 (PR #704) FALSIFIED that sentence. gate.yml's
// `generated-product-gate` now scaffolds EVERY profile and runs `dotnet build` + `dotnet test` on each,
// on every PR. The instantiated Product.Tests run pre-merge, at their source. Do not reason from the
// old premise; it is gone.
//
// WHY IT SURVIVES ANYWAY, ON A DIFFERENT AND NARROWER GROUND. Every launch assertion the Product.Tests
// make is EXISTENTIAL — `Expect.stringContains defaultBranch "Viewer.runAppWithAudio …"`. Every
// assertion below is UNIVERSAL: the SET of launch expressions `Program.fs` emits equals the SET the
// Product.Tests assert, and EVERY member of that set carries `audioSink`. You cannot build a universal
// out of a pile of existentials, and the gap between them is not academic:
//
//   - A branch that emits `Viewer.runAppWithAudio … generatedHost` AND, beside it, a sinkless
//     `Viewer.runApp … generatedHost` satisfies every `stringContains` in Product.Tests. It reds here.
//   - That is #436, exactly. `sample-pack` sat on the sink-discarding overload — referencing all four
//     FS.GG.Audio packages and wiring none — while the positive assertions stayed green, because a
//     presence check cannot see a silent twin standing next to what it asserts. The universal loop at
//     the bottom of this file is what ended that, and it is the only thing that did.
//   - The gate scaffolds a HARDCODED profile list. A family branch added to `Program.fs` but not to
//     that list is never instantiated, so no Product.Test of any profile reads it. This file reads the
//     template TEXT with every `//#if` branch present at once, so it sees the family the gate does not.
//
// So: the per-family drift this was written for (#350 — a family's launch changed, its Product.Tests
// not) IS now caught at its source by the gate, and that half of the set equality (`onlyInTests`, a
// stale expectation) is genuinely redundant. It stays because it is one direction of a single set
// comparison and costs nothing to keep. What is NOT redundant, and what earns this file its place, is
// the closure: an expression nobody asserts, and the absence of a silent launch. Delete this and that
// class goes uncovered — do not "finish the job" of #613 on the strength of a premise #680 already
// retired. See ReleaseOnlyTwinLockstepTests before adding another twin.
//
// AND THE NEW PREMISE IS ASSERTED, NOT ASSUMED. Both residues above are stated against a job that has
// to keep existing and keep covering every profile. `GeneratedProductGateCoverageTests` locks exactly
// that (#613's `PackageTestsGateMembershipTests` pattern), so this header cannot rot into a lie a
// second time the way the last one did.
//
// WHAT IT LOCKS. This test reads the two template files STATICALLY (no instantiation, so it is cheap
// enough for the PR-gated slnx lane) and asserts the SET of default-branch launch expressions
// `Program.fs` emits equals the SET the `Product.Tests` assert, and that every one of them carries the
// audio sink. Change one without the other and this reds.

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

/// Drop whole-line `//` comments before matching.
///
/// Both sides of this comparison are PROSE-RICH: Program.fs explains each family's launch in comments,
/// and the Product.Tests explain what they assert. A comment that merely NAMES a launch expression —
/// "sample-pack used to land on the sinkless `Viewer.runApp viewerOptions generatedHost`" — is not an
/// assertion, and reading it as one makes this gate report drift that does not exist (it did exactly
/// that while #436 was being written). The launch expressions this test cares about live in code, or
/// inside a string literal on a code line; neither is touched here.
let private withoutComments (text: string) =
    text.Replace("\r\n", "\n").Split('\n')
    |> Array.filter (fun line -> not ((line.TrimStart()).StartsWith "//"))
    |> String.concat "\n"

let private expressionsIn (text: string) : Set<string> =
    launchExpression.Matches(withoutComments text) |> Seq.map (fun m -> m.Value) |> Set.ofSeq

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

          // Belt-and-braces on the regex intent: every shipped family's launch must be present, so a
          // future rename of the host entry points cannot leave the lock quietly matching nothing.
          //
          // There are TWO default launch expressions, not three (#436). The third used to be
          // `Viewer.runApp viewerOptions generatedHost`, labelled here as the "headless / governed
          // family" — which was never true, and the mislabel is a fair part of why the defect #436
          // fixed went unseen for so long. `governed`/`headless-scene` return from an EARLIER
          // entrypoint and never reach this branch at all; the profile actually landing on that
          // sinkless call was `sample-pack`, which had referenced all four FS.GG.Audio packages since
          // ADR-0024 and wired none of them. #436 routes it through `Viewer.runAppWithAudio` with the
          // `game` family it already shared a host (`generatedHost`) with, so the two collapse into
          // one expression — and the silent launch is gone from the template entirely.
          test "every product family's default launch host is present, locked, and carries the audio sink" {
              let programExpressions = expressionsIn (programDefaultBranch ())

              for expected in
                  [ "ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost" // app / controls family (#429 seam, reached by #436)
                    "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" ] do // game + sample-pack (#245 audio seam)
                  Expect.isTrue
                      (programExpressions.Contains expected)
                      (sprintf "Program.fs default branch must emit `%s`" expected)

              // The point of #436: no viewer profile launches through a sink-discarding overload. Every
              // default launch expression the template emits carries `audioSink`. Without this, a
              // future edit could drop one family back to the silent overload and only the two
              // positive assertions above would notice — and they would not, because they only check
              // presence, not absence of a silent twin.
              for expression in programExpressions do
                  Expect.stringContains
                      expression
                      "audioSink"
                      (sprintf
                          "every viewer profile's default launch must carry the audio sink (#436) — `%s` discards it, which is the exact defect that left `app` and `sample-pack` silent"
                          expression)
          }
        ]
