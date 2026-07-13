module PackablePackages

// #674 — ONE reading of "what actually packs", for every test in this project that needs it.
//
// The rule is not ours to state. `dotnet pack FS.GG.Rendering.slnx` is the pack command, and what it
// produces is decided by slnx membership plus each `src/**/*.fsproj`'s `<PackageId>FS.GG.UI.*` +
// `<IsPackable>true` + `<Version>`. `PackageFeed.discoverPackablePackages` is the HARNESS's reading of
// that rule — the set the real `package-feed` workflow expects to find in the feed — so asking it is
// asking the pack path itself. This module just wraps that call.
//
// It exists because three tests were each re-deriving the rule by hand:
//
//   * `Tests.fs` — re-pointed at the production function by #670.
//   * `Feature207BomMembershipTests.discoveredMembers()` — a hand-mirrored copy.
//   * `Feature163PackageFeedValidationTests.packageVersions()` — another hand-mirrored copy.
//
// All three agreed, which is exactly the state a shared-bug hazard is in right up until it isn't. This
// is the #661/#657 shape one directory over: the public-surface renderer was hand-mirrored into the
// generator and the gate, a bug landed in both copies, and the gate compared one copy against the other
// and stayed green while recording something that was not the public surface. Two copies of one rule
// agree with each other, *including when they are both wrong*.
//
// Feature207 is the sharpest case, because it is a PARITY test (`nuspec ids == discovered set`). Its
// whole value is that the two sides are derived independently. If its "discovered set" drifts from what
// actually packs, the parity it proves is between the BOM and a stale reading of the source tree — it
// would still be green, and it would no longer be about the pack path.
//
// One definition. No copy to drift.

open System.IO
open Rendering.Harness

let private repositoryRoot = FS.GG.TestSupport.RepositoryRoot.value

// FAIL LOUDLY ON EMPTY, here, so no caller has to remember.
//
// Much of what these guards assert is NEGATIVE — "the retired Charts package is not packable", "no
// packable project is missing from the slnx" — and an empty set satisfies every negative for free. That
// is not hypothetical: `buildFrontEnd()`, the text-scan #670 deleted, ended in `else ""`, and three
// negative Charts guards passed vacuously over the empty string until #667 hardened it.
//
// A non-emptiness assertion copy-pasted into each test only protects the tests that remembered to copy
// it, and the next negative guard someone writes is precisely the one that will not. Making vacuity
// impossible HERE protects all of them, including the ones not written yet.
//
// The set cannot change while the suite runs, and each call walks `src/` and parses every project file,
// so compute it once.
let private discovered =
    lazy
        // Reads the project files and nothing else — packs nothing, touches no network — so every
        // consumer stays in the hermetic default tier (#540) and runs pre-merge. The feed path only
        // names the .nupkg each package WOULD produce; discovery never looks for it.
        (let packages =
            PackageFeed.discoverPackablePackages repositoryRoot (Path.Combine(Path.GetTempPath(), "fs-gg-packable-probe"))

         if List.isEmpty packages then
             failwith
                 "the real pack path discovered NO packable FS.GG.UI.* package. Guards over this set are \
                  largely negative assertions, and every one of them would pass vacuously over an empty \
                  set — so this is a hard failure rather than a silent green (#670/#674)."

         packages)

/// Every package `dotnet pack FS.GG.Rendering.slnx` is expected to produce, as the harness sees it.
/// Note the BOM metapackage (`FS.GG.UI`) is absent by construction: the rule matches the DOTTED prefix
/// `FS.GG.UI.`, so the bare id does not qualify.
let packablePackages () : PackageFeed.PackablePackage list = discovered.Value

/// Their package ids.
let packablePackageIds () = packablePackages () |> List.map _.PackageId |> Set.ofList

/// id -> the `<Version>` its project declares.
let packablePackageVersions () =
    packablePackages () |> List.map (fun package -> package.PackageId, package.Version) |> Map.ofList

/// The project file behind a discovered package, as text.
let projectFileOf (package: PackageFeed.PackablePackage) =
    File.ReadAllText(Path.Combine(repositoryRoot, package.ProjectPath.Replace('/', Path.DirectorySeparatorChar)))
