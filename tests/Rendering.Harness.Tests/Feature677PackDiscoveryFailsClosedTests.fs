module Feature677PackDiscoveryFailsClosedTests

// #677 — `PackageFeed.discoverPackablePackages` decides which packages the real feed must contain, so
// every project that quietly leaves the discovered set is a package the feed check STOPS LOOKING FOR:
// `MissingExpectedPackage` cannot fire for a package that is no longer expected, and
// `package-feed --mode check` reports `Passed` over a package that never packed.
//
// It used to leave that set in three ways, all of them silent, and all of them making the set SMALLER:
// a packable project with no inline `<Version>` (identity conflated with version resolution), an
// unparseable `.fsproj` (`try … with _ -> None` swallowed everything), and a repository root with no
// `src/` (`[]`, which every downstream check then confirms).
//
// These tests are the negative half — they assert on what discovery REFUSES. That is deliberate: the
// #670/#667 lesson is that a guard which only ever asserts what a clean tree contains is green on a
// tree it could not read, so the reading failures need their own red. Each test below writes an input
// that USED to be dropped and asserts a loud `PackageDiscoveryError` (or, for the resolvable case, that
// the project is discovered rather than omitted).

open System.IO
open Expecto
open Rendering.Harness

let private feedOf (root: string) = Path.Combine(root, "feed")

let private packableProject (packageId: string) =
    $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>true</IsPackable>
    <PackageId>{packageId}</PackageId>
  </PropertyGroup>
</Project>
"""

let private propsWithVersion (version: string) =
    $"""<Project>
  <PropertyGroup Label="Package">
    <Version>{version}</Version>
  </PropertyGroup>
</Project>
"""

/// The message of the `PackageDiscoveryError` `f` raises. Returning a result at all is the failure this
/// whole module exists to catch, so it is a test failure here rather than an assertion someone forgot.
let private discoveryErrorFrom (f: unit -> 'a) : string =
    try
        f () |> ignore

        failtest "expected a PackageDiscoveryError, but discovery returned a result — the input was silently dropped, which is the #677 fails-open bug"
    with PackageFeed.PackageDiscoveryError message ->
        message

[<Tests>]
let tests =
    testList "Feature677 pack-path discovery fails closed" [

        // THE DEMONSTRATED BUG. Centralizing a version is an ordinary refactor — `Directory.Build.props`
        // already exists — and it used to make the project vanish from the discovered set entirely, which
        // is how `Feature207BomMembershipTests`, the BOM membership DRIFT DETECTOR, went green while the
        // BOM omitted a shipping member. Identity (`PackageId` + `IsPackable`) and version RESOLUTION are
        // separate concerns; a failure of the second must not silently erase the first.
        test "a packable project with no inline <Version> is discovered at the version it inherits" {
            let root = Feature163TestFixtures.createTempRoot "feature677-inherited-version"

            try
                Feature163TestFixtures.writeFile root "Directory.Build.props" (propsWithVersion "9.9.9") |> ignore
                Feature163TestFixtures.writeFile root "src/Scene/Scene.fsproj" (packableProject "FS.GG.UI.Scene") |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)

                Expect.equal
                    (packages |> List.map (fun p -> p.PackageId, p.Version))
                    [ "FS.GG.UI.Scene", "9.9.9" ]
                    "a packable project that centralizes its version is DISCOVERED at the inherited version, not dropped"

                Expect.equal
                    packages[0].PackageFilePath
                    (Path.Combine(feedOf root, "FS.GG.UI.Scene.9.9.9.nupkg"))
                    "and the feed check looks for the nupkg the inherited version names"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // MSBuild last-write-wins: the canonical `Directory.Build.props` imports `.local.props` LAST, so
        // the repo's own package metadata overrides the org defaults. Resolution has to agree, or it
        // expects a nupkg at a version nothing packs.
        test "Directory.Build.local.props wins over Directory.Build.props (it is imported last)" {
            let root = Feature163TestFixtures.createTempRoot "feature677-local-props-wins"

            try
                Feature163TestFixtures.writeFile root "Directory.Build.props" (propsWithVersion "1.0.0-org") |> ignore
                Feature163TestFixtures.writeFile root "Directory.Build.local.props" (propsWithVersion "2.0.0-repo") |> ignore
                Feature163TestFixtures.writeFile root "src/Scene/Scene.fsproj" (packableProject "FS.GG.UI.Scene") |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)
                Expect.equal packages[0].Version "2.0.0-repo" "the repo's local.props overrides the org default"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // An inline `<Version>` still beats an inherited one — the project body is evaluated after the
        // implicit props import. This is every real member in this repo today, so it is the case a
        // regression here would break first.
        test "an inline <Version> still wins over the inherited one" {
            let root = Feature163TestFixtures.createTempRoot "feature677-inline-wins"

            try
                Feature163TestFixtures.writeFile root "Directory.Build.props" (propsWithVersion "9.9.9") |> ignore
                Feature163TestFixtures.writePackageProject root "src/Scene/Scene.fsproj" "FS.GG.UI.Scene" "1.2.3" |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)
                Expect.equal packages[0].Version "1.2.3" "the project's own <Version> wins"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // REFUSE, do not guess. There is a tempting wrong answer here — MSBuild defaults an unset
        // `Version` to `1.0.0` — and taking it would expect `FS.GG.UI.Scene.1.0.0.nupkg`, a package
        // nothing packs, turning a resolution failure into a confusing missing-package red. Name the
        // project and say what is wrong with it.
        test "a packable project whose version resolves to nothing is a hard error, not an omission" {
            let root = Feature163TestFixtures.createTempRoot "feature677-unresolvable-version"

            try
                Feature163TestFixtures.writeFile root "src/Scene/Scene.fsproj" (packableProject "FS.GG.UI.Scene") |> ignore

                let message = discoveryErrorFrom (fun () -> PackageFeed.discoverPackablePackages root (feedOf root))
                Expect.stringContains message "FS.GG.UI.Scene" "the error names the package"
                Expect.stringContains message "Scene.fsproj" "and the project file to go and fix"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // `try … with _ -> None` made "this project is not one of ours" and "I could not read this
        // project" the same answer. They are opposites: the first is a fact about the tree, the second is
        // a fact about the scanner.
        test "an unparseable .fsproj under src/ fails the run and names the file" {
            let root = Feature163TestFixtures.createTempRoot "feature677-unparseable-project"

            try
                Feature163TestFixtures.writePackageProject root "src/Scene/Scene.fsproj" "FS.GG.UI.Scene" "1.2.3" |> ignore
                Feature163TestFixtures.writeFile root "src/Broken/Broken.fsproj" "<Project><PropertyGroup>" |> ignore

                let message = discoveryErrorFrom (fun () -> PackageFeed.discoverPackablePackages root (feedOf root))
                Expect.stringContains message "Broken.fsproj" "the error names the file it could not read"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // IDENTITY FIRST. A project that never claimed to be ours is not our problem, and resolving a
        // version for it is work we must not do — otherwise every unversioned non-packable project in the
        // tree (ColorPolicy, the tests, the tools) becomes a hard error and the guard is unusable.
        test "a project that does not declare itself a packable FS.GG.UI.* is skipped, version or no version" {
            let root = Feature163TestFixtures.createTempRoot "feature677-not-ours"

            try
                Feature163TestFixtures.writePackageProject root "src/Scene/Scene.fsproj" "FS.GG.UI.Scene" "1.2.3" |> ignore
                // IsPackable=false, no <Version> — ColorPolicy's shape.
                Feature163TestFixtures.writeFile
                    root
                    "src/ColorPolicy/ColorPolicy.fsproj"
                    """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
"""
                |> ignore
                // Packable, but not one of ours, and unversioned.
                Feature163TestFixtures.writeFile root "src/Other/Other.fsproj" (packableProject "Contoso.Widgets") |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)
                Expect.equal (packages |> List.map _.PackageId) [ "FS.GG.UI.Scene" ] "only the project that declares itself ours is discovered — and the others raise nothing"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // Version resolution reads MSBuild PROPERTIES, so it must not mistake the element form of a pin
        // (`<PackageReference><Version>…`) for the project's own version.
        test "a <Version> inside a <PackageReference> is a pin, not the project's version" {
            let root = Feature163TestFixtures.createTempRoot "feature677-pin-is-not-a-version"

            try
                Feature163TestFixtures.writeFile root "Directory.Build.props" (propsWithVersion "9.9.9") |> ignore

                Feature163TestFixtures.writeFile
                    root
                    "src/Scene/Scene.fsproj"
                    """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>true</IsPackable>
    <PackageId>FS.GG.UI.Scene</PackageId>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FSharp.Core">
      <Version>10.1.301</Version>
    </PackageReference>
  </ItemGroup>
</Project>
"""
                |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)
                Expect.equal packages[0].Version "9.9.9" "the inherited PROPERTY, not the dependency's pin"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // THE WALK STOPS AT THE REPOSITORY ROOT — and the boundary is tested from BOTH sides, because
        // the first cut of this fix got it wrong in a way all the tests above missed: `stopAt` was
        // compared untrimmed, `Path.GetDirectoryName` never emits a trailing separator, so a
        // `repositoryRoot` given as "…/repo/" never matched "…/repo", the walk left the checkout, and a
        // `Directory.Build.props` sitting ABOVE the repository resolved the version. A fails-open
        // introduced while fixing a fails-open — which is what a boundary you never test buys you.
        for trailing in [ ""; string Path.DirectorySeparatorChar ] do
            let label = if trailing = "" then "" else " (even when the root is given with a trailing separator)"

            test $"version resolution never walks above the repository root{label}" {
                let parent = Feature163TestFixtures.createTempRoot "feature677-root-boundary"

                try
                    let root = Path.Combine(parent, "repo")
                    // A props file OUTSIDE the repository. Resolution must not see it.
                    Feature163TestFixtures.writeFile parent "Directory.Build.props" (propsWithVersion "6.6.6-outside-the-repo") |> ignore
                    Feature163TestFixtures.writeFile root "src/Scene/Scene.fsproj" (packableProject "FS.GG.UI.Scene") |> ignore

                    let message =
                        discoveryErrorFrom (fun () -> PackageFeed.discoverPackablePackages (root + trailing) (feedOf root))

                    Expect.stringContains message "FS.GG.UI.Scene" "the version is UNRESOLVED — a props file above the checkout is not this repository's"
                finally
                    Feature163TestFixtures.deleteTempRoot parent
            }

        // `[]` said "this repository expects no packages", and `checkLocalFeed` over an empty set is
        // `Passed`. A root with no `src/` is a broken input, not a repository with nothing to pack — the
        // same rule the `package-feed` CLI already states for its sample set ("no samples selected" and
        // "all samples pass" must not share an exit code).
        test "a repository root with no src/ is a broken input, not an empty expected-feed set" {
            let root = Feature163TestFixtures.createTempRoot "feature677-no-src"

            try
                let message = discoveryErrorFrom (fun () -> PackageFeed.discoverPackablePackages root (feedOf root))
                Expect.stringContains message "src" "the error says what is missing"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // The same swallow, one function down: an unreadable SAMPLE contributed no pins, and a sample
        // with no pins has nothing that can be stale. The pin gate reported green over exactly the input
        // it could not read.
        test "an unparseable sample project fails the pin read rather than contributing no pins" {
            let root = Feature163TestFixtures.createTempRoot "feature677-unparseable-sample"

            try
                Feature163TestFixtures.writePackageProject root "src/Scene/Scene.fsproj" "FS.GG.UI.Scene" "1.2.3" |> ignore
                Feature163TestFixtures.writeFile root "samples/Demo/Demo.fsproj" "<Project><ItemGroup>" |> ignore

                let packages = PackageFeed.discoverPackablePackages root (feedOf root)

                let message =
                    discoveryErrorFrom (fun () -> PackageFeed.readSelectedPackagePins root [ "samples/Demo" ] packages Set.empty [])

                Expect.stringContains message "Demo.fsproj" "the error names the sample it could not read"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }
    ]
