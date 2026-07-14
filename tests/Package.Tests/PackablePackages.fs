module PackablePackages

// #674 — ONE reading of "what actually packs", for every test in this project that needs it.
//
// The rule is not ours to state. `dotnet pack FS.GG.Rendering.slnx` is the pack command, and what it
// produces is decided by slnx membership plus each `src/**/*.fsproj`'s `<PackageId>` in the FS.GG.UI set
// (the `FS.GG.UI` umbrella, or a dotted `FS.GG.UI.*` member) + `<IsPackable>true` + a resolvable
// `<Version>`. `PackageFeed.discoverPackablePackages` is the HARNESS's reading of that rule — the set the
// real `package-feed` workflow expects to find in the feed — so asking it is asking the pack path itself.
// This module just wraps that call.
//
// #727 — and it is SEVENTEEN, not sixteen. That `FS.GG.UI.*` had a trailing dot, in the harness and in
// this module's own independent cross-check, and the umbrella's id is bare. See `memberPackages` below
// for the distinction the dot was silently making.
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

/// A project that LOOKS packable, read as text: `<PackageId>FS.GG.UI…` + `<IsPackable>true`.
///
/// Deliberately an INDEPENDENT derivation, and deliberately text — it must be able to see a project that
/// `discoverPackablePackages` cannot, which is the entire point of the tripwire below.
///
/// #727 — AND IT COULD NOT. This regex required the DOTTED prefix (`FS\.GG\.UI\.`), the same trailing dot
/// `discoverPackablePackages` required, so the umbrella (`FS.GG.UI`, bare) was invisible to the check AND
/// to the check's independent cross-check. The tripwire's whole job is to catch a package discovery
/// drops; it could not catch the one package discovery was dropping, because both copies of the rule had
/// the same one-character bug.
///
/// That is the #661 hazard this module's own header warns about — "two copies of one rule agree with each
/// other, INCLUDING when they are both wrong" — reproduced inside the file that warns about it. Two
/// derivations that must agree are a safety net only where they can actually DISAGREE, and on the
/// umbrella these two never could. The dot is now optional, so the text scan sees all 17 and the tripwire
/// can finally fire on the package it was written to protect.
let private looksPackable (projectText: string) =
    let packageId = Regex.Match(projectText, @"<PackageId>\s*(FS\.GG\.UI(?:\.[^<\s]+)?)\s*</PackageId>")

    let packable =
        Regex.IsMatch(projectText, @"<IsPackable>\s*true\s*</IsPackable>", RegexOptions.IgnoreCase)

    if packageId.Success && packable then Some packageId.Groups.[1].Value else None

/// THE TRIPWIRE: discovery must not silently LOSE a package.
///
/// `discoverPackablePackages` is the right subject — it is what the pack path uses — but a package can
/// leave its result silently, and the ways it can are not obvious from the call site. Two are now closed,
/// and both were fixed only after they had already hidden a live bug:
///
///   * it used to additionally require an INLINE `<Version>`, so a project that centralized its version
///     simply was not discovered (#677 — it now resolves through `Directory.Build[.local].props`, and
///     raises rather than dropping when it cannot);
///   * it used to wrap the whole per-project read in `try … with _ -> None`, so a malformed or unreadable
///     `.fsproj` was silently skipped rather than thrown (#677 — it now raises `PackageDiscoveryError`);
///   * and its IDENTITY test used to demand a dotted `FS.GG.UI.` prefix, so the bare-id umbrella never
///     matched at all (#727 — the one that actually shipped, see the module header).
///
/// Any of them and the package does not appear in the set — and almost everything built on this set is a
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
              A package that leaves the discovered set does not fail — it stops being EXPECTED, and the \
              negative/parity guards built on that set then pass over a smaller subject. Check \
              `PackageFeed.isFsGgUiPackageId` (an id the identity test does not match is dropped whole — \
              #727's umbrella) and the project's `<Version>` (#677). Fix the project or the rule; do not \
              let the package disappear (#674)."

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

/// Every package `dotnet pack FS.GG.Rendering.slnx` is expected to produce, as the harness sees it —
/// all 17: the 16 dotted members AND the `FS.GG.UI` umbrella/BOM.
///
/// #727: the umbrella used to be absent from this set "by construction", and this comment used to SAY so
/// — one line under a summary that called the set "every package `dotnet pack` is expected to produce".
/// Both sentences cannot be true, and it was the summary that was right: `dotnet pack` emits 17. The BOM
/// is a real package, it ships, and the feed must contain it, so it belongs in the set every feed guard
/// is built on. Excluding it is what made it the one package no version guard could see — and therefore
/// the one that shipped broken.
let packablePackages () : PackageFeed.PackablePackage list = discovered.Value

/// Their package ids (all 17).
let packablePackageIds () = packablePackages () |> List.map _.PackageId |> Set.ofList

/// id -> the `<Version>` its project declares (all 17).
let packablePackageVersions () =
    packablePackages () |> List.map (fun package -> package.PackageId, package.Version) |> Map.ofList

/// The BOM's MEMBERS: the 16 dotted packages, WITHOUT the umbrella itself.
///
/// #727 — the distinction this module used to collapse. Two different sets are in play and they differ by
/// exactly one element:
///
///   * the PACKABLE set (17) — what `dotnet pack` produces, so what the feed must contain. Every feed and
///     version guard wants this one, and the umbrella's absence from it is precisely what let the BOM
///     ship with EXACT pins on a version that did not exist.
///   * the MEMBER set (16) — what the BOM may list as `<dependency>`. A BOM must not depend on itself, so
///     the umbrella is correctly absent HERE, and only here.
///
/// Collapsing them into one set forces a choice between a BOM that lists itself and a feed guard that
/// cannot see the BOM. The repo chose the second, silently, in a `StartsWith` filter.
let memberPackages () : PackageFeed.PackablePackage list =
    packablePackages ()
    |> List.filter (fun package -> package.PackageId <> PackageFeed.UmbrellaPackageId)

/// Their package ids (the 16).
let memberPackageIds () = memberPackages () |> List.map _.PackageId |> Set.ofList

/// The umbrella / BOM package itself, as the pack path sees it.
///
/// Fails loudly rather than returning `None`: after #727 the umbrella IS discovered, and a run in which
/// it is not is a discovery regression — the exact one that hid the unrestorable BOM — not a repository
/// that happens to have no BOM. A test that skipped quietly here would go green on the bug.
let umbrellaPackage () : PackageFeed.PackablePackage =
    match packablePackages () |> List.tryFind (fun p -> p.PackageId = PackageFeed.UmbrellaPackageId) with
    | Some package -> package
    | None ->
        failwith
            $"the pack path's discovery did not find the '{PackageFeed.UmbrellaPackageId}' umbrella/BOM, which \
              `dotnet pack FS.GG.Rendering.slnx` demonstrably produces. Discovery's id test has regressed to a \
              DOTTED prefix that the bare umbrella id cannot match — the #727 blind spot, which shipped a BOM \
              whose 16 exact pins named a version no member ever published."

/// The project file behind a discovered package, as text.
let projectFileOf (package: PackageFeed.PackablePackage) =
    File.ReadAllText(Path.Combine(repositoryRoot, package.ProjectPath.Replace('/', Path.DirectorySeparatorChar)))
