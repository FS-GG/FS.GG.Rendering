module TemplateLaunchExpressionCoherenceTests

// FS-GG/FS.GG.Rendering#350 — a PR-VISIBLE lock on the generated product's per-family launch host,
// MOVED TO A BEHAVIOR CONTRACT by FS-GG/FS.GG.Governance#297 (adjudicated 2026-07-22), routed via
// FS.GG.Rendering#981.
//
// WHAT #350 WAS. The 0.5.0 release was tagged but NEVER PUBLISHED: the release-only "Generated product
// (template instantiation)" job failed because #245's audio seam changed the game-family DEFAULT launch
// in `template/base/src/Product/Program.fs` from
//     Viewer.runApp viewerOptions generatedHost
// to
//     Viewer.runAppWithAudio viewerOptions audioSink generatedHost
// while the mirroring assertions in `template/base/tests/Product.Tests` still expected the pre-audio
// call. That per-family drift is now caught AT ITS SOURCE by gate.yml's `generated-product-gate` (#680,
// PR #704): every profile is scaffolded and `dotnet build`+`dotnet test`'d on every PR, so the
// instantiated Product.Tests run pre-merge. `GeneratedProductGateCoverageTests` locks that job's
// existence and per-profile coverage.
//
// WHY THIS FILE PINNED A LITERAL — AND WHY IT NO LONGER MAY. This file used to assert set-EQUALITY
// between the literal launch expressions `Program.fs` emits and the literal expressions the Product.Tests
// assert (`Expect.stringContains defaultBranch "Viewer.runAppWithAudio viewerOptions audioSink
// generatedHost"`). Governance#297 retired that coupling: a suite whose purpose is to survive a
// scaffold-model swap must ALSO survive a SANCTIONED LAUNCHER UPGRADE (a persistence/pointer-capable
// runner rename), and pinning the exact launch-overload as a literal source substring breaks the moment
// the lifecycle mandates such an upgrade. Two products paid that pin — Rougue1 M9 edited 4 assertions
// across 2 files on a runner upgrade; TowerDefense1 M8's persistence-sink upgrade forced edits to a suite
// meant to survive model swaps. The emitted Product.Tests (`GovernanceTests.fs`) now assert the launch
// BEHAVIOR (the effect reaches its sink), not the expression, so the old set-equality's other direction
// has no literal to compare against — and should not.
//
// WHAT IT LOCKS NOW — the one residue that was never redundant, stated as a behavior contract on the
// template TEXT (read STATICALLY — no instantiation — so it is cheap enough for the PR-gated slnx lane):
//
//   * The default launch HONOURS the launch effects: the audio sink (PlayAudio -> its sink) and the
//     persistent host (Persist honoured) both reach whatever launcher runs. Matched by the EFFECT
//     ARGUMENTS the launcher receives, never the launcher NAME — so a sanctioned launcher upgrade passes
//     this file WITHOUT editing it.
//
//   * NO family launches through a sink-discarding overload (#436). `sample-pack` once sat on the
//     sinkless `Viewer.runApp viewerOptions generatedHost` — referencing all four FS.GG.Audio packages
//     and wiring none — while every positive `stringContains` stayed green, because a presence check
//     cannot see a silent twin standing beside what it asserts. The silent-launch shape is matched by
//     ARGUMENT SHAPE (`viewerOptions <host>` with no `audioSink` between them), launcher-name-agnostic,
//     so a rename cannot smuggle one past. This — not the retired set-equality — is what earned this
//     file its place, and it is preserved.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private programPath = repositoryPath "template/base/src/Product/Program.fs"

/// The persistent host VALUE each family threads into its launcher (feature 086, FR-006): the controls
/// family the pointer-aware `interactiveHost`, the game/sample-pack family the keyboard-only
/// `generatedHost`. `Program.fs` carries every family's branch behind `//#if (profile == ...)` markers,
/// so reading it as raw text sees both host values at once. A sanctioned launcher upgrade renames the
/// RUNNER, not the host it is handed — so the host value is the stable anchor, and the launcher name is
/// deliberately unpinned everywhere below.
let private persistentHost = @"(?:generatedHost|interactiveHost)"

/// A launch that HONOURS both effects: SOME launcher (ANY overload name) applied to `viewerOptions` and,
/// as its terminal arguments, the audio sink (PlayAudio -> its sink) then the persistent host (Persist
/// honoured). Only the effect arguments are matched. `[^\n]*` tolerates the window-behavior request the
/// `--window-*` overload threads between `viewerOptions` and the sink. This is exactly what a sanctioned
/// launcher upgrade must keep satisfying.
let private effectHonouringLaunch =
    Regex($@"[A-Za-z_][\w.]*\s+viewerOptions\b[^\n]*\baudioSink\s+{persistentHost}\b")

/// The SINKLESS launch shape #436 forbids: the persistent host threaded DIRECTLY after `viewerOptions`
/// with NO audio sink between them — the sink-discarding overload that left `app`/`sample-pack` silent.
/// Launcher name unpinned, so a rename cannot disguise a silent launch. (The sink-carrying forms never
/// match: `viewerOptions audioSink <host>` puts `audioSink` where this expects the host, and the
/// `--window-*` overload puts a `(request)` there.)
let private sinklessLaunch =
    Regex($@"[A-Za-z_][\w.]*\s+viewerOptions\s+{persistentHost}\b")

/// Drop whole-line `//` comments before matching. Both `Program.fs`'s launch comments and this file's
/// own prose NAME launch expressions without being one; the launch calls this test cares about live in
/// code, so the comment lines are removed first (the #436 write reported phantom drift until they were).
let private withoutComments (text: string) =
    text.Replace("\r\n", "\n").Split('\n')
    |> Array.filter (fun line -> not ((line.TrimStart()).StartsWith "//"))
    |> String.concat "\n"

/// `Program.fs` carries every family's branch behind `//#if (profile == ...)` markers; the live viewer
/// entrypoint's default path begins at the LAST `| None ->` (an earlier one belongs to the headless-scene
/// entrypoint, which launches no viewer). Mirror the slice the Product.Tests take.
let private programDefaultBranch () =
    let source = File.ReadAllText programPath

    match source.LastIndexOf("| None ->", StringComparison.Ordinal) with
    | -1 -> failtest "Program.fs must contain a `| None ->` default launch branch"
    | index -> withoutComments (source.Substring index)

[<Tests>]
let templateLaunchExpressionCoherenceTests =
    testList
        "Template launch-expression coherence (FS.GG.Rendering#350, behavior contract per Governance#297)"
        [
          // The behavior residue of #350, launcher-name-agnostic. Non-vacuous by construction: the
          // default branch MUST contain an effect-honouring launch, or a refactor that stopped emitting
          // one would reduce this to a green check of nothing.
          test "the generated Program.fs default launch honours the launch effects (the audio sink and the persistent host reach the launcher)" {
              let defaultBranch = programDefaultBranch ()

              Expect.isTrue
                  (effectHonouringLaunch.IsMatch defaultBranch)
                  "Program.fs default branch must thread `viewerOptions`, the audio sink, and the \
                   persistent host into a launcher — the host honours PlayAudio (the effect reaches its \
                   sink) and Persist (the persistent interactive host) — regardless of the launcher \
                   overload name"
          }

          // The #436 invariant, stated where it can be enforced against the template text and by ARGUMENT
          // SHAPE, so a launcher rename cannot smuggle a silent launch past it.
          test "no product family launches through a sink-discarding overload (#436), whatever the launcher is named" {
              let defaultBranch = programDefaultBranch ()

              Expect.isFalse
                  (sinklessLaunch.IsMatch defaultBranch)
                  "no family may thread the persistent host DIRECTLY after `viewerOptions` with no audio \
                   sink between them — that is the silent, sink-discarding launch #436 removed (it left \
                   `app`/`sample-pack` mute while every positive check stayed green). The sink must reach \
                   every launcher; matched by argument shape, so a launcher rename cannot disguise one"
          }

          // The survival proof Governance#297/#981 asks for: the behavior contract holds across a
          // sanctioned launcher upgrade (a persistence/pointer-capable runner rename) WITHOUT editing the
          // suite, and the retired literal-substring pin demonstrably would NOT — which is the exact
          // brittleness that forced edits on Rougue1 M9 and TowerDefense1 M8.
          test "the behavior contract survives a sanctioned launcher upgrade (a persistence/pointer-capable runner rename) without editing the suite" {
              // A sanctioned upgrade renames the RUNNER and keeps handing it the same effect arguments.
              let upgraded =
                  "            Viewer.runPersistentPointerAppWithAudio viewerOptions audioSink generatedHost"

              Expect.isTrue
                  (effectHonouringLaunch.IsMatch upgraded)
                  "a renamed persistence/pointer-capable runner still threads the audio sink and the \
                   persistent host — the behavior contract holds with no edit to this suite"

              Expect.isFalse
                  (sinklessLaunch.IsMatch upgraded)
                  "the upgraded launch still carries the sink between `viewerOptions` and the host, so it \
                   is not the sinkless shape"

              // The retired literal-substring pin does NOT match the renamed launcher — this is the
              // brittleness Governance#297 removed, made visible so a future edit cannot quietly restore
              // it (guard the guard, mirroring the #111 pattern in GovernanceTests.fs).
              Expect.isFalse
                  (upgraded.Contains "Viewer.runAppWithAudio viewerOptions audioSink generatedHost")
                  "the retired literal launch-overload substring pin does NOT match a renamed launcher — \
                   demonstrating exactly the brittleness (Rougue1 M9's 4 edits, TowerDefense1 M8's suite) \
                   that Governance#297 replaced with the behavior contract above"
          }
        ]
