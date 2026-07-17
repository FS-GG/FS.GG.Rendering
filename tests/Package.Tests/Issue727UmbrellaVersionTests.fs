module Issue727UmbrellaVersionTests

// #727 — the BOM must be COHERENT with the members it pins, and it must be VISIBLE to the guards that
// would notice when it is not. Those are two failures, and the repo had both; the second is why the first
// shipped.
//
// What shipped: `src/Meta/FS.GG.UI.fsproj` declared no `<Version>` of its own, so `$(Version)` fell back
// to the repo default in `Directory.Build.local.props` (0.1.0-preview.1) while the 16 members pinned
// themselves inline at 0.4.0-preview.1. The nuspec spends `$version$` TWICE — on the BOM's own version
// and on all 16 `[$version$]` EXACT dependency pins — so what packed was:
//
//     FS.GG.UI 0.1.0-preview.1
//       └─ FS.GG.UI.Canvas [0.1.0-preview.1]   <- an EXACT pin on a package that does not exist
//       └─ … x16
//
// No 0.1.0-preview.1 of any member has ever been published. `PackageReference Include="FS.GG.UI"` could
// not restore: NU1102, sixteen times. The single package a consumer is MEANT to reference — the front
// door — was the one that did not open.
//
// Why nothing caught it: every guard that could have was looking at a set the BOM was not in.
// `PackageFeed.discoverPackablePackages` selected on `id.StartsWith("FS.GG.UI.")` — a filter, not a
// prefix, because of that trailing DOT — and the umbrella's id is bare `FS.GG.UI`. `dotnet pack` emits
// 17 packages; discovery found 16. The discovered set IS the expected-feed set, so the seventeenth was
// not pin-checked, not in the source proof, and not covered by packaged-consumer: `MissingExpectedPackage`
// cannot fire for a package nobody expects. The package whose version was wrong was precisely the package
// every version guard was blind to.
//
// And it was blind twice over. `PackablePackages.assertNothingDropped` — the TRIPWIRE whose entire job is
// to catch a package discovery drops, derived independently as a text scan — used the same dotted regex.
// The check and its independent cross-check shared one one-character bug, so on the umbrella they could
// never disagree. That is the #661 hazard `PackablePackages`' own header warns about, reproduced inside
// the file that warns about it.
//
// These tests therefore assert BOTH halves, because fixing only the version would leave the next drift
// just as invisible:
//
//   VISIBILITY — the pack path can SEE the umbrella (and the identity rule still admits the bare id).
//   COHERENCE  — the umbrella's version equals the members', so its EXACT pins name packages that exist.
//
// #866 then removed the CAUSE the two halves were guarding against. The drift needed TWO definitions of
// the version — the repo-wide default the BOM inherited, and the inline literals the members declared —
// so the version is now defined ONCE, in `Directory.Build.local.props`, and all 17 inherit it. The
// coherence assertions are unchanged (they compare RESOLVED versions, so they hold however the version is
// spelled); the last test below flipped from "the BOM declares it inline" to "nothing declares it inline",
// which is the same invariant guarded at its new source. See that test's own note.
//
// Deterministic and hermetic: reads project files and the nuspec, packs nothing, touches no network.

open System
open System.IO
open System.Xml.Linq
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private nuspecPath = repo "src/Meta/FS.GG.UI.nuspec"

/// The `<dependency>` ids the BOM pins.
let private nuspecDependencyIds () : Set<string> =
    XDocument.Load(nuspecPath).Descendants()
    |> Seq.filter (fun e -> e.Name.LocalName = "dependency")
    |> Seq.choose (fun e ->
        e.Attributes()
        |> Seq.tryFind (fun a -> a.Name.LocalName = "id")
        |> Option.map _.Value)
    |> Set.ofSeq

[<Tests>]
let issue727UmbrellaVersionTests =
    testList "Issue727 umbrella BOM version coherence" [

        // ---- VISIBILITY: the guards can see the package they are guarding -------------------------

        // The identity rule, at unit level. This is the one-character regression, isolated: if the
        // trailing dot ever comes back, this reds first and names why, rather than the failure surfacing
        // as an unrestorable package three releases later.
        test "the packable identity rule admits the BARE umbrella id, not just the dotted members" {
            Expect.isTrue
                (PackageFeed.isFsGgUiPackageId PackageFeed.UmbrellaPackageId)
                "'FS.GG.UI' (the BOM's own id) must satisfy the packable identity rule — it packs, so the \
                 feed must contain it. A rule requiring the DOTTED prefix 'FS.GG.UI.' excludes it, which is \
                 exactly how the BOM shipped with exact pins on a version that did not exist (#727)."

            Expect.isTrue
                (PackageFeed.isFsGgUiPackageId "FS.GG.UI.Canvas")
                "a dotted member must still satisfy the rule"

            // The rule is about OUR package set, so it must not swallow a foreign id that merely shares a
            // prefix. `FS.GG.UIX` is not ours; without the dot-or-exact test it would look like it is.
            Expect.isFalse
                (PackageFeed.isFsGgUiPackageId "FS.GG.UIX")
                "'FS.GG.UIX' is not in the FS.GG.UI package set — widening the rule to a bare StartsWith \
                 'FS.GG.UI' would claim any package whose id merely begins with those characters"

            Expect.isFalse (PackageFeed.isFsGgUiPackageId "FS.GG.Audio.Core") "a sibling repo's package is not ours"
        }

        // The rule above is only worth something if the real pack path actually applies it. This asks
        // discovery — over the real repository — for the umbrella, and `umbrellaPackage()` fails loudly
        // rather than returning None, so a discovery regression cannot pass here quietly.
        test "the real pack path DISCOVERS the umbrella — 17 packages, not 16" {
            let umbrella = PackablePackages.umbrellaPackage ()
            let packable = PackablePackages.packablePackages ()
            let members = PackablePackages.memberPackages ()

            Expect.equal umbrella.PackageId PackageFeed.UmbrellaPackageId "the discovered umbrella is the BOM"

            Expect.equal
                umbrella.ProjectPath
                "src/Meta/FS.GG.UI.fsproj"
                "the umbrella is discovered from the metapackage project"

            // Counted as a set relation, never as a magic number: the packable set is the members plus the
            // one BOM, and both sides are derived, so adding a 17th member cannot silently break this.
            Expect.equal
                packable.Length
                (members.Length + 1)
                "the packable set is exactly the BOM's members PLUS the BOM itself — `dotnet pack` produces \
                 all of them, so the feed must expect all of them"

            Expect.isFalse
                (Set.contains PackageFeed.UmbrellaPackageId (PackablePackages.memberPackageIds ()))
                "the BOM is NOT one of its own members — a BOM must not depend on itself. This is the one \
                 place the umbrella is correctly subtracted, and it is a choice made here rather than a \
                 side effect of a filter bug in the harness (#727)."
        }

        // ---- COHERENCE: the pins name packages that exist ------------------------------------------

        // THE REGRESSION GUARD. The nuspec spends one `$version$` on the BOM's own version and on every
        // exact pin, so "the BOM's version equals the members' version" is precisely "the BOM's exact pins
        // resolve". Bump the 16 and forget the 17th and this reds — which is the mistake that shipped.
        test "the umbrella's version equals the single version its members are published at" {
            let members = PackablePackages.memberPackages ()
            let umbrella = PackablePackages.umbrellaPackage ()

            let memberVersions = members |> List.map _.Version |> Set.ofList

            // Coherence among the members is the premise of the assertion below; if they disagree with each
            // other, "the members' version" is not a thing and the real failure is here, not on the BOM.
            Expect.equal
                memberVersions.Count
                1
                (sprintf
                    "the 16 members must all publish at ONE version for the BOM's single [$version$] token to \
                     pin a coherent set; found %A"
                    memberVersions)

            let memberVersion = Set.minElement memberVersions

            Expect.equal
                umbrella.Version
                memberVersion
                (sprintf
                    "the BOM packs at %s while its members publish at %s. The nuspec pins every member at the \
                     EXACT version [$version$], and $version$ is the BOM's OWN version — so every one of those \
                     16 pins names a package that does not exist, and `PackageReference Include=\"FS.GG.UI\"` \
                     fails to restore with NU1102 x16. Give src/Meta/FS.GG.UI.fsproj the same <Version> as the \
                     members (#727)."
                    umbrella.Version
                    memberVersion)
        }

        // The above compares the BOM against the members as DISCOVERED. This closes the loop on the other
        // side: every id the nuspec actually pins is a real discovered member sitting at that same version.
        // Together they say the packed graph resolves — without packing, restoring, or hitting the network.
        test "every EXACT pin in the nuspec names a discovered member at the umbrella's own version" {
            let umbrella = PackablePackages.umbrellaPackage ()
            let versionOf = PackablePackages.packablePackageVersions ()

            let unresolvable =
                nuspecDependencyIds ()
                |> Set.toList
                |> List.choose (fun id ->
                    match Map.tryFind id versionOf with
                    | None -> Some $"{id} — pinned by the BOM but not a discovered packable package at all"
                    | Some version when version <> umbrella.Version ->
                        Some
                            $"{id} — the BOM pins [{umbrella.Version}] (its own version, via $version$) but the \
                              package publishes at {version}"
                    | Some _ -> None)

            Expect.isEmpty
                unresolvable
                (sprintf
                    "the BOM's EXACT [$version$] pins must every one name a package that is actually published at \
                     that version, or `PackageReference Include=\"FS.GG.UI\"` cannot restore (NU1102). \
                     Unresolvable pins: %A"
                    unresolvable)
        }

        // The mechanism, guarded at its source — and #866 MOVED that source, so this test now asserts the
        // opposite spelling of the same invariant. Read this before "restoring" it.
        //
        // The bug was never "the version is wrong". It was that the version had TWO definitions — a
        // repo-wide default in Directory.Build.local.props that the BOM inherited, and inline literals the
        // 16 members declared — so the two could drift apart without anyone writing a wrong number
        // anywhere. #727 closed the GAP by giving the BOM an inline literal too, in lockstep with the
        // members, and asserted that here.
        //
        // Lockstep across 17 copies is a rule that has already failed once per reader: #815 read one of
        // those literals as a package's PUBLISHED version and scoped a Diagnostics-only major against it,
        // which does not exist (#866). So #866 deleted the second definition instead of testing for the gap
        // between them: ONE <Version>, in Directory.Build.local.props, inherited by all 17. The BOM cannot
        // drift from its members because there is no longer a second value to drift from — bump the one
        // definition and the BOM and all 16 move together, which is exactly what the EXACT [$version$] pins
        // require.
        //
        // The coherence tests above are unchanged and remain the real guard: they compare RESOLVED versions
        // (discovery honours inheritance — PackageFeed.resolveProjectVersion, #677), so they catch drift
        // however it is spelled. This test guards the property that makes drift unrepresentable: that the
        // second definition has not come back.
        test "the umbrella and its members resolve ONE version from ONE definition — none declares it inline" {
            let inlineVersionOf (projectPath: string) =
                XDocument.Load(repo projectPath).Descendants()
                |> Seq.tryFind (fun e ->
                    e.Name.LocalName = "Version"
                    && (match e.Parent with
                        | null -> false
                        | parent -> parent.Name.LocalName = "PropertyGroup"))
                |> Option.map (fun e -> e.Value.Trim())
                |> Option.filter (String.IsNullOrWhiteSpace >> not)

            // Derived from discovery, never a hardcoded list: a 17th packable project added tomorrow is
            // covered by this the day it is added, with no edit here. And it covers ONLY the src/**
            // packable set — .template.package/FS.GG.UI.Template.fsproj is not a slnx member, so
            // discovery never returns it, and its legitimate inline <Version> (the release-tag axis, not
            // this one) is correctly outside this assertion rather than a false OFFENDER.
            let redeclared =
                PackablePackages.packablePackages ()
                |> List.choose (fun p ->
                    inlineVersionOf p.ProjectPath
                    |> Option.map (fun v -> $"{p.ProjectPath} declares <Version>{v}</Version> inline"))

            Expect.isEmpty
                redeclared
                (sprintf
                    "a packable project under src/ has re-declared <Version> inline, which re-creates the SECOND \
                     definition #866 deleted. The version of all 17 is defined once, in \
                     Directory.Build.local.props; an inline literal here overrides it for THIS project only, so \
                     the BOM and its members can once again drift to different versions — and the BOM's 16 EXACT \
                     [$version$] pins then name packages that do not exist (NU1102 x16, #727). Delete the inline \
                     <Version> and let it inherit; to change what the 17 pack at, edit the one definition. (A \
                     command-line -p:Version=V is a global property and overrides the inherited value, so the \
                     coherent release pack is unaffected.) Offenders: %A"
                    redeclared)

            // And the one definition actually resolves — the inverse failure. If the sole <Version> were
            // deleted outright, discovery would refuse (PackageDiscoveryError, #677) rather than reach here;
            // this pins the positive fact that the BOM's resolved version is non-empty and shared.
            let umbrella = PackablePackages.umbrellaPackage ()

            Expect.isFalse
                (String.IsNullOrWhiteSpace umbrella.Version)
                "the BOM must resolve a non-empty version from the single Directory.Build.local.props \
                 definition — $version$ stamps its own version and all 16 EXACT pins from it"
        }
    ]
