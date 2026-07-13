module RepositoryRootSingleFinderTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

// #700 — the gate that makes `RepositoryRoot`'s central claim CHECKABLE rather than merely asserted.
//
// `tests/TestSupport/RepositoryRoot.fs` calls itself "the single shared repository-root finder for
// every test/harness project". That sentence was FALSE for as long as it existed. Until #699 gave it
// the ProjectReference, `tests/Build.Tests` could not even SEE TestSupport — so it hand-rolled the
// pre-refactor walk instead, and #699 landing the reference did not retract the five copies already
// written. All five were byte-identical, and all five disagreed with the shared finder on the marker
// set: `RepositoryRoot.find` accepts any `*.sln`, any `*.slnx`, or `build.fsx`; the hand-rolled walk
// accepted only the literal name `FS.GG.Rendering.slnx`. They resolved to the same directory purely
// because this repo owns exactly one solution file and it happens to carry that name.
//
// The failure that mattered was the QUIET one. A hand-rolled walk fails loud when it finds NOTHING
// (`failwith`), but it cannot fail loud about finding the WRONG root — it would simply resolve every
// repo-relative path against a different tree and judge a corpus that is not the one that ships. So
// this list guards BOTH halves of the claim, because either alone is worth little:
//
//   1. the finder lands on the RIGHT root  (`is the root it claims to be`), and
//   2. it is the ONLY finder               (`no other test source walks to the root itself`).
//
// TRAVERSAL IS THE SIGNAL, NOT THE SEED. The obvious rule — "only RepositoryRoot.fs may name
// `AppContext.BaseDirectory`" — is too weak, because the seed is interchangeable: this repo already
// roots walks at `__SOURCE_DIRECTORY__` too (tests/Elmish.Tests/*AdapterTests.fs, legitimately, via
// `RepositoryRoot.find`), so a hand-rolled walk seeded that way would sail straight past a seed-only
// check. What a walk CANNOT do without is the step upward. `Directory.GetParent`, and `.Parent` on a
// `DirectoryInfo`, are therefore the load-bearing tokens: they are rename-proof, seed-proof, and
// copy-paste-proof, and outside the finder itself no test source has any honest use for them.
// (`DirectoryInfo` alone is fine and stays legal — `DirectoryInfo(dir).Name` is how four tests read a
// directory's own name. It is the ascent that is forbidden, not the type.)
//
// SCOPE: `tests/`. Not `samples/**/*.Tests`, and that is deliberate rather than an oversight — the
// sample suites consume the PUBLISHED packages (`PackageReference FS.GG.UI.Controls`), not this repo's
// projects, so they structurally cannot reference TestSupport and cannot consume the shared finder.
// One of them does hand-roll an ancestor walk (samples/SampleApps/SampleApps.Tests), but it hunts a
// marker FILE rather than the repo root, so it is a different subject; it is filed separately.

let private repoRoot = RepositoryRoot.value

/// The one file allowed to find the repository root by walking to it.
let private theFinder = "tests/TestSupport/RepositoryRoot.fs"

/// This file, which names the forbidden tokens as DATA (below, and in the header) without ever walking.
/// The only exemption, and not a loophole worth losing sleep over: a hand-rolled walk smuggled into the
/// guard that exists to forbid hand-rolled walks would have to be written by someone reading this line.
let private thisFile = "tests/Build.Tests/RepositoryRootSingleFinderTests.fs"

/// The step upward. A walk can pick any seed it likes, but it cannot ascend without one of these.
let private traversalTokens = [ "Directory.GetParent"; "DirectoryInfo"; ".Parent" ]

/// Every F# source under `tests/`, repo-relative, excluding build output.
let private testSources =
    Directory.EnumerateFiles(Path.Combine(repoRoot, "tests"), "*.fs", SearchOption.AllDirectories)
    |> Seq.map (fun path -> Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
    |> Seq.filter (fun rel -> not (rel.Contains "/obj/" || rel.Contains "/bin/"))
    |> List.ofSeq

let private readSource (rel: string) =
    File.ReadAllText(Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar)))

/// Does this source ascend the directory tree? `Directory.GetParent` is unambiguous. A bare `.Parent`
/// is not — `mr.Parent` on a reflection-metadata MemberReference is not a filesystem step — so `.Parent`
/// only counts when the file also constructs the `DirectoryInfo` it would have to be walking.
let private ascendsTheTree (source: string) =
    source.Contains "Directory.GetParent"
    || (source.Contains "DirectoryInfo" && source.Contains ".Parent")

[<Tests>]
let tests =
    testList
        "#700 — RepositoryRoot is the single root finder"
        [
          // Half one of the claim: the finder lands where it says it does. Everything in `tests/` now
          // resolves its repo-relative paths through this one value, so if it were ever to stop at a
          // nearer marker — a `*.sln`/`*.slnx`/`build.fsx` materialized between a test binary and the
          // root — every one of those tests would silently judge the wrong tree. That is the exact quiet
          // failure the consolidation was for, and only an assertion makes it loud.
          test "the shared finder is the root it claims to be" {
              Expect.isTrue
                  (File.Exists(Path.Combine(repoRoot, "FS.GG.Rendering.slnx")))
                  (sprintf
                      "RepositoryRoot.value resolved to %s, which holds no FS.GG.Rendering.slnx. The shared finder stops at the NEAREST ancestor holding any *.sln/*.slnx/build.fsx, so a solution or build script materialized below the real root will silently capture it — and every test in the repo would then resolve its repo-relative paths against the wrong tree."
                      repoRoot)
          }

          // The next two are the anti-fails-open scaffolding (FS-GG/.github#266): an oracle that cannot
          // see its own subject reports green having verified nothing. They prove the probe works BEFORE
          // the rule below trusts the probe's silence about everybody else.
          test "the corpus reaches the finder itself" {
              Expect.contains
                  testSources
                  theFinder
                  (sprintf
                      "enumerating tests/**/*.fs did not reach %s (found %d file(s)). The probe is broken, so its silence about every other file means nothing."
                      theFinder
                      testSources.Length)
          }

          test "the finder still ascends the tree" {
              Expect.isTrue
                  (ascendsTheTree (readSource theFinder))
                  (sprintf
                      "%s no longer names any of %A. This guard detects a hand-rolled walk BY the step upward, so if the shared finder has stopped ascending that way, the guard is now watching for tokens nobody writes — it guards nothing and stays green forever. Re-derive the signal before changing how the finder walks."
                      theFinder
                      traversalTokens)
          }

          // A stale `thisFile` (renamed, or mistyped) does not weaken the guard — it un-exempts this
          // file, whose header names the tokens, and the test goes red pointing at it. The exemption
          // cannot rot quietly.
          test "no other test source walks to the repo root by itself" {
              let offenders =
                  testSources
                  |> List.filter (fun rel ->
                      rel <> theFinder && rel <> thisFile && ascendsTheTree (readSource rel))

              Expect.isEmpty
                  offenders
                  (sprintf
                      "these file(s) under tests/ ascend the directory tree themselves instead of consuming the shared finder:%s%sUse `RepositoryRoot.value` (or `RepositoryRoot.find <seed>`), with a ProjectReference to tests/TestSupport and `open FS.GG.TestSupport`. A second finder is a second marker set that can disagree with the first, and the disagreement is SILENT: it resolves repo-relative paths against the wrong tree rather than failing."
                      Environment.NewLine
                      (offenders
                       |> List.map (sprintf "  - %s")
                       |> String.concat Environment.NewLine
                       |> fun listing -> listing + Environment.NewLine))
          } ]
