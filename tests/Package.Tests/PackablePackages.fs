module PackablePackages

// #674 — ONE reading of "what actually packs", for every test in this project that needs it.
//
// The rule is not ours to state. `dotnet pack FS.GG.Rendering.slnx` is the pack command, and what it
// produces is decided by slnx membership plus each `src/**/*.fsproj`'s `<PackageId>FS.GG.UI.*` +
// `<IsPackable>true` + `<Version>`. `PackageFeed.discoverPackablePackages` is the HARNESS's reading of
// that rule — the set the real `package-feed` workflow expects to find in the feed — so asking it is
// asking the pack path itself. This module just wraps that call.
//
// #670 is why the guards ask a FUNCTION and not a document. They used to scan `build/Governance/
// PackageSurface.fs` as text — two hardcoded lists in a file no project compiled and nothing executed,
// stranded when feature 045's relocation of `build.fsx` never completed, feeding `PackLocal` and
// `PackageSurfaceCheck` targets that cannot be run at all (there is no `./fake.sh`). So they asserted
// that an inert file mentioned strings the tests themselves also hardcoded: green forever, and blind in
// both directions. The list named five packages, its own comment said nine, and the repo ships sixteen.
// Nobody noticed, because nothing read it.
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
// NOT the only reading in the repo, and deliberately so. `Feature209VersionCoherenceTests` derives the
// packable set a third way — a regex scan of the project files — and that is left ALONE on purpose. It
// is an INDEPENDENT cross-check on this module, not a hand-mirrored copy of it, and it earned that
// standing during this very change: the first cut of #674 silently narrowed the set (see `assertNothing‐
// Dropped` below) and Feature209's separate derivation was the only thing that reddened. Two derivations
// that must agree are a safety net; two that merely happen to agree are the #661 hazard. The difference
// is whether anything CHECKS that they agree — which is what the tripwire below does.

open System.IO
open System.Text.RegularExpressions
open Rendering.Harness

let private repositoryRoot = FS.GG.TestSupport.RepositoryRoot.value

/// A project that LOOKS packable, read as text: `<PackageId>FS.GG.UI.…` + `<IsPackable>true`.
///
/// Deliberately an INDEPENDENT derivation, and deliberately text — it must be able to see a project that
/// `discoverPackablePackages` cannot, which is the entire point of the tripwire below.
let private looksPackable (projectText: string) =
    let packageId = Regex.Match(projectText, @"<PackageId>\s*(FS\.GG\.UI\.[^<\s]+)\s*</PackageId>")

    let packable =
        Regex.IsMatch(projectText, @"<IsPackable>\s*true\s*</IsPackable>", RegexOptions.IgnoreCase)

    if packageId.Success && packable then Some packageId.Groups.[1].Value else None

/// THE TRIPWIRE: discovery must not silently LOSE a package.
///
/// `discoverPackablePackages` is the right subject — it is what the pack path uses — but it is stricter
/// and more forgiving than it looks, in two ways that both fail OPEN:
///
///   * it additionally requires an inline `<Version>`, so a project that centralizes its version simply
///     is not discovered; and
///   * it wraps the whole per-project read in `try … with _ -> None`, so a malformed or unreadable
///     `.fsproj` is silently skipped rather than thrown.
///
/// Either way the package does not appear in the set — and almost everything built on this set is a
/// NEGATIVE assertion or a PARITY comparison, both of which get *easier* to satisfy as the set shrinks.
/// This is not theoretical. The first cut of #674 re-pointed `Feature207BomMembershipTests` (whose old
/// rule did not require `<Version>`) at this function, and a packable project with no inline `<Version>`
/// then vanished from the discovered set: the BOM parity gate — whose own header calls it "the drift
/// detector" — went GREEN over a shipping member the BOM omitted.
///
/// So do not trust the output. Derive the candidates independently, as text, and fail LOUDLY on anything
/// discovery dropped. A guard whose subject can silently shrink is the failure this whole issue chain is
/// about (FS-GG/.github#266).
let private assertNothingDropped (discovered: PackageFeed.PackablePackage list) =
    let discoveredIds = discovered |> List.map _.PackageId |> Set.ofList

    let dropped =
        Directory.GetFiles(Path.Combine(repositoryRoot, "src"), "*.fsproj", SearchOption.AllDirectories)
        |> Array.choose (fun project ->
            match looksPackable (File.ReadAllText project) with
            | Some packageId when not (Set.contains packageId discoveredIds) ->
                Some $"{packageId} ({Path.GetFileName project})"
            | _ -> None)
        |> Array.toList

    if not (List.isEmpty dropped) then
        failwith
            $"the pack path's discovery SILENTLY DROPPED a project that declares itself packable: %A{dropped}. \
              `PackageFeed.discoverPackablePackages` also requires an inline <Version> and swallows any \
              project it cannot parse, so the package vanishes from the packable set instead of failing — \
              and the negative/parity guards built on that set then pass over a smaller subject. Give the \
              project a <Version>, or fix the project file; do not let it disappear (#674)."

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

         // Empty is the total case; this is the partial one, and it is the one that actually happened.
         assertNothingDropped packages

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
