module PackageTests

// =================================================================================================
// WHEN DOES A RULE YOU WRITE HERE ACTUALLY RUN? (#540 — read this before adding a check)
//
// This project has TWO tiers, and the difference is TIMING, not subject:
//
//   DEFAULT TIER — runs on EVERY PR (the `Deterministic gate`) and again in the release lane.
//     Package.Tests is a member of FS.GG.Rendering.slnx, and the gate's test loop is derived from the
//     slnx, so anything you add here runs pre-merge by default. It must therefore be HERMETIC: reads
//     of the working tree, no network, no `dotnet pack`, no published package. ~325 rules, ~4 seconds.
//
//   RELEASE TIER — runs ONLY in release.yml, because it needs something a PR does not have (a real
//     pack, a real feed, a published version). You get it by DEFERRING a test behind an env flag that
//     only release.yml sets — today that is FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE (see
//     `deferredPackageSmokeTests` below). "the release lane opts the package consumer smoke in" asserts
//     release.yml really does set it, because a check deferred behind a flag NOBODY sets is a check
//     that never runs.
//
// THE DEFAULT IS THE SAFE ONE, AND THAT IS THE POINT. This project used to be release-ONLY — excluded
// from the slnx so that release checks could not gate a PR. The effect was that EVERY rule in here fired
// after the merge that broke it, never on it: the capability catalog, the skill-manifest digests, R-CAT,
// R-PROF. Nothing told the author. That is how Renovate PR #233 reached 4/4 green while proposing a pin
// no local `dotnet pack` could produce — `Feature163PackageFeedValidationTests` had the rule, and the
// rule ran three days late (gate.yml's #300 step says so at length).
//
// So the polarity is inverted: a rule now runs pre-merge unless you SAY OUT LOUD that it cannot, by
// gating it. "I am writing a check that will not run on PRs" is a sentence you have to write, rather
// than a property you inherit from a workflow comment and discover afterwards (FS-GG/.github#266 — a
// check that reports green because it never ran).
//
// If your check needs a real feed or a real pack, defer it. If it can be broken by editing a file in
// this repo, LEAVE IT IN THE DEFAULT TIER — that is the whole reason it is here.
// =================================================================================================

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
// #670 — the harness that OWNS the pack path. `PackageFeed.discoverPackablePackages` is the function the
// real `package-feed` workflow uses to decide which packages the feed must contain; the pack guards below
// call it rather than re-deriving the rule, so there is one definition and no copy to drift (the #661
// lesson: two copies of one rule agree with each other, including when they are both wrong).
open Rendering.Harness

let repositoryRoot = RepositoryRoot.value

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

// #670 — WHAT ACTUALLY PACKS. These guards call the production discovery function, so they watch the
// code that packs rather than a document describing it.
//
// The real pack path is `dotnet pack FS.GG.Rendering.slnx` (PackageFeed.runPackIfRequested), and what
// it produces is decided by two things and no list: slnx membership, and each `src/**/*.fsproj`'s
// `<PackageId>FS.GG.UI.*` + `<IsPackable>true`. `PackageFeed.discoverPackablePackages` is the harness's
// own reading of that rule — the set it expects to find in the feed — so asking IT is asking the pack
// path itself.
//
// What these guards used to watch: `buildFrontEnd()`, which read every `.fs` under `build/Governance/`
// as TEXT. The only file there was `PackageSurface.fs`: two hardcoded lists stranded when feature 045's
// relocation of `build.fsx` into compiled build modules never completed. No project compiled it, nothing
// executed it, and `./fake.sh` is absent from the repo root — so `PackLocal` and `PackageSurfaceCheck`,
// the targets those lists fed, could not be run at all. `Expect.stringContains build
// "src/Scene/Scene.fsproj"` therefore asserted that an inert file mentioned a string the test itself also
// hardcoded: green forever, and blind in both directions. Add a package to the real pack path and nothing
// here moved; re-introduce the retired Charts package THROUGH the real pack path and the Charts guards
// would not have seen it. The list named five packages, its own comment said nine, and the repo ships
// seventeen — nobody noticed, because nothing read it.
//
// That is FS-GG/.github#266's "gate reports green on a missing subject" one level up: the subject was
// present, but it was not the subject that mattered.
let packablePackages () =
    // Reads the project files and nothing else — packs nothing, touches no network — so these stay in
    // the hermetic default tier (#540) and run pre-merge. The feed path only names the .nupkg each
    // package WOULD produce; discovery never looks for it.
    PackageFeed.discoverPackablePackages repositoryRoot (Path.Combine(Path.GetTempPath(), "fs-gg-packable-probe"))

let packablePackageIds () = packablePackages () |> List.map _.PackageId |> Set.ofList

let runDotnetWithin (timeoutMilliseconds: int) (workingDirectory: string) (arguments: string) =
    let startInfo: ProcessStartInfo = ProcessStartInfo("dotnet", arguments)
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false

    match Process.Start(startInfo) |> Option.ofObj with
    | None -> failwithf "Could not start dotnet %s" arguments
    | Some proc ->
        use proc = proc
        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()

        if proc.WaitForExit(timeoutMilliseconds) then
            proc.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult()
        else
            proc.Kill(true)
            -1, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult()

/// Version the consumer smoke packs the coherent set at. It is not the repo's shipped version; it
/// only has to agree with the `PackageReference` the generated consumer restores, so pack and
/// reference both read it from here. Packing at an implicit default (1.0.0) while referencing a
/// literal here is what previously made the smoke resolve nothing and pass vacuously.
let packageVersion = "0.1.9-preview.1"

/// Packages the consumer smoke references directly.
let consumerSmokePackages =
    [ "FS.GG.UI.Scene"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Themes.Default" ]

/// Transitive project closure of `consumerSmokePackages`, in dependency order. The consumer restores
/// them all at `packageVersion`, a version on no public feed, so a partial feed cannot restore: every
/// FS.GG.UI.* package the four pull in has to be packed locally too. Ordered and packed one project
/// at a time rather than `pack FS.GG.Rendering.slnx`, which builds every package in parallel and
/// exhausts memory on smaller machines.
let consumerSmokeProjects =
    [ "src/Scene/Scene.fsproj"
      "src/Diagnostics/Diagnostics.fsproj"
      "src/Layout/Layout.fsproj"
      "src/KeyboardInput/KeyboardInput.fsproj"
      "src/DesignSystem/DesignSystem.fsproj"
      "src/Themes.Default/Themes.Default.fsproj"
      "src/Controls/Controls.fsproj" ]

/// A consumer that CALLS the packages rather than merely restoring them: it exports a scene through
/// SceneCodec, computes a real Yoga layout, and paints a Button through the default theme. Each call
/// is load-bearing — a package that restores but ships no native asset or no public entry point
/// fails here at build or at run, which a bare `dotnet restore` cannot detect.
let consumerSmokeProgram =
    """module PackageConsumerSmoke

open FS.GG.UI.Scene
open FS.GG.UI.Layout
open FS.GG.UI.Controls

type Msg = Clicked

[<EntryPoint>]
let main _ =
    let package = SceneCodec.export (Scene.rectangle (0.0, 0.0, 8.0, 8.0) Colors.white)

    if not (package.PackageIdentity.StartsWith "sha256:") then
        failwith "FS.GG.UI.Scene: SceneCodec.export produced no sha256 package identity"

    let layout = Layout.evaluate (Defaults.availableSpace 100.0 50.0) (Defaults.layoutNode "root")

    if List.isEmpty layout.Bounds then
        failwith "FS.GG.UI.Layout: Layout.evaluate computed no bounds"

    let theme = FS.GG.UI.Themes.Default.Theme.light
    let rendered = Control.render theme (Button.create [ Button.text "ok"; Button.onClick Clicked ])
    let renderedIdentity = (SceneCodec.export rendered.Scene).PackageIdentity

    if not (renderedIdentity.StartsWith "sha256:") then
        failwith "FS.GG.UI.Controls: Control.render produced no exportable scene"

    printfn "package consumer smoke: scene=%s layout=%d controls=%s" package.PackageIdentity layout.Bounds.Length renderedIdentity
    0
"""

// -------------------------------------------------------------------------------------------------
// R-PROF (#511) — `template/profiles/<p>.yml` is the TRANSPOSE of `template/capabilities.yml`.
//
// THE ROSTER PROBLEM. "Which capability reaches which profile" is written down in five places. Four are
// now gated: R-PINNED / R-REF / R-REACH / R-CAT (tests/Package.Tests/SkillPackageReachTests.fs, issues
// #430 and #483). `template/profiles/*.yml` was the fifth, and NOTHING held it — so it drifted, exactly
// as the others had: `sample-pack.yml` under-reported its own profile by FOUR capabilities, three files
// listed a `full-governance` capability that is not a row in any catalog, and `governed.yml` — one of the
// five real profiles — simply did not exist.
//
// WHY THIS GATE DOES NOT RECOMPUTE PACKAGE REACH. The obvious move is to assert these files against the
// scaffold's real `<!--#if -->` package gates, the way R-CAT does. That would be a SECOND copy of a subtle
// computation (`enclosingGate` / `profilesDeclaring` / `capabilityReach`), and a roster problem is not
// solved by adding a sixth roster's worth of parsing. capabilities.yml ALREADY carries that fact, and
// R-CAT already holds it to the real gates. So this gate asserts the one thing left: that the per-profile
// view and the per-capability view are the same table read two ways.
//
//     profiles/<p>.yml  --(R-PROF, here)-->  capabilities.yml  --(R-CAT, #483)-->  the template's gates
//
// Both links are equality, so the composition is: a profile file lists EXACTLY the capabilities a product
// on that profile can actually `open`. One source of truth, one derivation, no second parser.
//
// The files say so themselves ("DERIVED — do not hand-edit"), which is the other half of #511's fix: a
// directory named `profiles/` that silently disagrees with the scaffold is worse than no directory.
// -------------------------------------------------------------------------------------------------

let private capabilitiesYmlRel = "template/capabilities.yml"

/// Bound rather than inlined: F# forbids a string literal inside an interpolated expression in a
/// single-quoted interpolated string, and every failure message below renders a set.
let private commaSep (items: string seq) = String.Join(", ", items)

/// Every profile a scaffold can be generated on, read from `.template.config/template.json`'s
/// `symbols.profile` choices — the SAME list `dotnet new` offers, not a copy of it.
///
/// Derived rather than hardcoded on purpose. A hardcoded set is how `governed` went missing: it is a real
/// profile that no profile file described, and no list that a human maintains would have noticed. Read from
/// template.json, adding a sixth profile makes `every generatable profile has a profile file` fail until
/// somebody writes the file.
let private generatableProfiles =
    use doc = JsonDocument.Parse(File.ReadAllText(repositoryPath ".template.config/template.json"))

    doc.RootElement
        .GetProperty("symbols")
        .GetProperty("profile")
        .GetProperty("choices")
        .EnumerateArray()
    |> Seq.map (fun choice -> choice.GetProperty("choice").GetString())
    |> Seq.choose Option.ofObj
    |> Set.ofSeq

/// (capability id, the profiles that row claims) for EVERY row in the catalog.
///
/// Runtime AND non-runtime: R-CAT drops the non-runtime `samples` row (it pins no package, so there is no
/// reach to hold it to), but a profile file's `capabilities:` names `samples` all the same, so the transpose
/// has to carry it or R-PROF would demand its removal. The `samples` row's own `profiles:` is instead
/// cross-checked below, against each profile file's `samples:` flag.
///
/// Anchored on the two-space `  - id:` list indent, each row's body running to the next row, so a field is
/// never read out of a neighbouring row — the same boundary discipline SkillPackageReachTests uses.
let private catalogRows =
    let text = File.ReadAllText(repositoryPath capabilitiesYmlRel)

    Regex.Matches(text, @"^  - id: (?<id>\S+)(?<body>(?:\n(?!  - id: ).*)*)", RegexOptions.Multiline)
    |> Seq.map (fun m ->
        let profiles =
            Regex.Match(m.Groups.["body"].Value, @"^\s+profiles:\s*\[(?<p>[^\]]*)\]", RegexOptions.Multiline)
                .Groups.["p"].Value.Split(',')
            |> Seq.map (fun s -> s.Trim())
            |> Seq.filter (fun s -> s <> "")
            |> Set.ofSeq

        m.Groups.["id"].Value, profiles)
    |> List.ofSeq

/// The capabilities the CATALOG gives `profile` — the transpose, and the expected `capabilities:` list.
let private catalogCapabilitiesFor profile =
    catalogRows
    |> List.filter (fun (_, profiles) -> Set.contains profile profiles)
    |> List.map fst
    |> Set.ofList

let private profileFileRel profile = $"template/profiles/{profile}.yml"

/// A `key: [a, b, c]` inline list, or the empty set when the key is absent.
let private ymlList (text: string) (key: string) =
    let m = Regex.Match(text, $@"^{Regex.Escape key}:\s*\[(?<v>[^\]]*)\]", RegexOptions.Multiline)

    if not m.Success then
        Set.empty
    else
        m.Groups.["v"].Value.Split(',')
        |> Seq.map (fun s -> s.Trim())
        |> Seq.filter (fun s -> s <> "")
        |> Set.ofSeq

/// A `key: value` scalar, or None. Comment lines cannot match: the key is anchored at column 0.
let private ymlScalar (text: string) (key: string) =
    let m = Regex.Match(text, $@"^{Regex.Escape key}:\s*(?<v>.+?)\s*$", RegexOptions.Multiline)
    if m.Success then Some(m.Groups.["v"].Value) else None

[<Tests>]
let profileRosterTests =
    testList "Profile roster (R-PROF, #511)" [

        // The existence check `generatedProductInputs` could not make, because it filtered by File.Exists.
        // `governed` was a real, generatable profile with no profile file for the life of the directory.
        test "every generatable profile has a profile file" {
            Expect.isNonEmpty
                (Set.toList generatableProfiles)
                "template.json declares at least one profile — an empty set would make every assertion below \
                 vacuous, which is the fails-open shape this gate exists to close"

            let missing =
                generatableProfiles
                |> Set.filter (fun p -> not (File.Exists(repositoryPath (profileFileRel p))))

            Expect.isEmpty
                (Set.toList missing)
                $"every profile in .template.config/template.json symbols.profile has a template/profiles/<p>.yml. \
                  Missing: {commaSep missing}"
        }

        // The catalog must be readable, or every transpose below is silently empty and R-PROF passes by
        // checking nothing.
        test "the capability catalog parses (R-PROF is not vacuous)" {
            Expect.isNonEmpty catalogRows $"{capabilitiesYmlRel} declares capability rows"

            let rowsWithNoProfiles = catalogRows |> List.filter (snd >> Set.isEmpty) |> List.map fst

            Expect.isEmpty
                rowsWithNoProfiles
                $"every capability row declares a `profiles:` list — a row with none contributes to no \
                  profile's transpose and would silently narrow what R-PROF demands. Rows: \
                  {commaSep rowsWithNoProfiles}"
        }

        // R-PROF itself. Equality, not subset — over-claiming and under-claiming are both lies, and the
        // directory had one of each.
        for profile in Set.toList generatableProfiles do
            test $"R-PROF — {profile}.yml lists exactly the capabilities the catalog gives {profile}" {
                let rel = profileFileRel profile
                let text = File.ReadAllText(repositoryPath rel)

                let declared = ymlList text "capabilities"
                let expected = catalogCapabilitiesFor profile
                let catalogIds = catalogRows |> List.map fst |> Set.ofList

                // Reported separately from the equality below: "names a capability that does not exist" is a
                // different defect from "under-reports its profile", and `full-governance` — which three of
                // these files carried, and which is a row in NO catalog — deserves to be named as such
                // rather than buried in a set diff.
                let unknown = Set.difference declared catalogIds

                Expect.isEmpty
                    (Set.toList unknown)
                    $"{rel} names only capabilities that are rows in {capabilitiesYmlRel}. \
                      Unknown: {commaSep unknown}"

                let underReported = Set.difference expected declared
                let overClaimed = Set.difference declared expected

                Expect.isEmpty
                    (Set.toList underReported)
                    $"{rel} lists every capability the catalog gives '{profile}' — these reach the profile and \
                      the file does not say so: {commaSep underReported}"

                Expect.isEmpty
                    (Set.toList overClaimed)
                    $"{rel} claims no capability the catalog does NOT give '{profile}' — the catalog does not \
                      put these on this profile: {commaSep overClaimed}"
            }

        // The one link R-CAT cannot make. `samples` is non-runtime (it pins no package), so R-CAT drops it
        // and nothing holds its `profiles:` to anything. The profile files carry the same fact a second way,
        // in the `samples:` boolean — so hold the two against each other and the row stops being unasserted.
        for profile in Set.toList generatableProfiles do
            test $"the samples capability and {profile}.yml's `samples:` flag agree" {
                let rel = profileFileRel profile
                let text = File.ReadAllText(repositoryPath rel)

                let hasSamplesCapability = catalogCapabilitiesFor profile |> Set.contains "samples"
                let samplesFlag = ymlScalar text "samples" = Some "true"
                let catalogSays = if hasSamplesCapability then "gives" else "does NOT give"

                Expect.equal
                    samplesFlag
                    hasSamplesCapability
                    $"{rel}'s `samples: {samplesFlag}` agrees with the catalog, which {catalogSays} \
                      '{profile}' the samples capability"
            }

        // A file whose `name:` disagrees with its filename is a file the transpose above checked against the
        // WRONG profile's expectations — and it would pass, because the filename is what selects the row.
        for profile in Set.toList generatableProfiles do
            test $"{profile}.yml's `name:` field matches its filename" {
                let rel = profileFileRel profile
                let text = File.ReadAllText(repositoryPath rel)

                Expect.equal
                    (ymlScalar text "name")
                    (Some profile)
                    $"{rel} declares `name: {profile}`"
            }

        // The schema is CLOSED, and that is the actual cure for the disease #511 describes.
        //
        // Every assertion above reads a key it already knows about, so a key it does NOT know about is
        // invisible to all of them — which is exactly what `optionalCapabilities: [layout, controls, testing]`
        // was: a roster field, in the profiles directory, naming capabilities the scaffold cannot give that
        // profile, read by nothing and held to nothing. Pinning the key set means the next such field cannot
        // be added silently: it either gets a gate, or it does not get in.
        for profile in Set.toList generatableProfiles do
            test $"{profile}.yml declares no ungated field" {
                let rel = profileFileRel profile

                let known =
                    set [ "name"; "description"; "capabilities"; "governance"; "samples"; "sourceFrameworkMode"; "validationCommands" ]

                let declared =
                    File.ReadAllLines(repositoryPath rel)
                    |> Array.choose (fun line ->
                        let m = Regex.Match(line, @"^(?<k>[A-Za-z][\w-]*):")
                        if m.Success then Some m.Groups.["k"].Value else None)
                    |> Set.ofArray

                let ungated = Set.difference declared known

                Expect.isEmpty
                    (Set.toList ungated)
                    $"{rel} declares only fields this gate asserts. An unknown key is a roster nothing holds — \
                      which is what `optionalCapabilities` was. Either gate it here or drop it. \
                      Ungated: {commaSep ungated}"

                Expect.isEmpty
                    (Set.toList (Set.difference known declared))
                    $"{rel} declares every field a profile file is required to carry: {commaSep known}"
            }
    ]

[<Tests>]
let packageContractTests =
    let v1PackageTests = [
        // #670 — the ANCHOR half of the pack contract, and it is deliberately not redundant with
        // Feature207BomMembershipTests. That test asserts the BOM nuspec EQUALS the discovered packable
        // set — a parity test, and a parity test passes whenever BOTH sides move together. Flip Scene's
        // <IsPackable> to false and delete its nuspec <dependency> in one commit and it stays green,
        // because the two sides still agree; they just agree about a framework that no longer ships a
        // scene package. This names the packages the framework may not silently stop shipping, so
        // dropping one has to be argued for HERE, in words, instead of falling out of an edit elsewhere.
        test "the core packages really are packable by the real pack path" {
            let packable = packablePackageIds ()

            // Non-empty FIRST. Every Charts assertion in this list is negative, and a discovery that
            // returned the empty set would satisfy all of them vacuously. That is not hypothetical: it is
            // exactly what `buildFrontEnd()` did before #667 hardened it — its `else ""` made three
            // negative guards green over nothing, and one of them was demonstrated green over nothing.
            // Re-pointing at a real subject does not by itself retire that failure mode, so close it.
            Expect.isNonEmpty packable "the real pack path discovers at least one packable FS.GG.UI.* package"

            [ "FS.GG.UI.Scene"
              "FS.GG.UI.SkiaViewer"
              "FS.GG.UI.Layout"
              "FS.GG.UI.Controls.Elmish"
              "FS.GG.UI.Controls" ]
            |> List.iter (fun packageId ->
                Expect.isTrue
                    (Set.contains packageId packable)
                    $"{packageId} is packable, so `dotnet pack FS.GG.Rendering.slnx` ships it")

            Expect.isFalse (Set.contains "FS.GG.UI.Charts" packable) "the retired Charts package is not packable"
        }

        // #670 — `dotnet pack FS.GG.Rendering.slnx` is the pack COMMAND, but `discoverPackablePackages`
        // scans `src/**` and never reads the slnx. So the two can disagree, and the direction that hurts
        // is a packable project the slnx does not list: it never packs, yet the harness still expects it
        // in the feed and reds with MissingExpectedPackage — at RELEASE, after the merge that caused it.
        // Nothing else in the repo compares these two sets. Feature207 compares discovery to the nuspec;
        // Feature242 parses the slnx but only to demand the docs name what it finds. This is the join.
        test "every packable project is a member of the slnx the pack command actually packs" {
            let slnx = File.ReadAllText(repositoryPath "FS.GG.Rendering.slnx")

            let orphaned =
                packablePackages ()
                |> List.filter (fun package -> not (slnx.Contains(package.ProjectPath, StringComparison.Ordinal)))
                |> List.map _.PackageId

            Expect.isEmpty
                orphaned
                $"every packable project is listed in FS.GG.Rendering.slnx, or the pack command cannot \
                  produce it and the feed check reds a release later: {orphaned}"
        }

        test "controls boundary has no active Charts package capability or monolithic viewer coupling" {
            let packable = packablePackageIds ()
            let capabilities = File.ReadAllText(Path.Combine(repositoryRoot, "template", "capabilities.yml"))
            let controlsProject = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj"))

            // V3 Stage 5: the monolith project is retired; name it via parts so this guard
            // stays meaningful without re-introducing a literal monolith path reference.
            let monolithDir = "Lib"
            let monolithRef = $@"..\{monolithDir}\{monolithDir}.fsproj"

            // #670 — the anti-vacuity guard belongs with the negative it protects: this assertion is
            // "Charts is absent from the packable set", and an empty packable set satisfies it for free.
            Expect.isNonEmpty packable "the real pack path discovers at least one packable FS.GG.UI.* package"

            Expect.isFalse (File.Exists(Path.Combine(repositoryRoot, "src", "Charts", "Charts.fsproj"))) "legacy Charts project is removed or deactivated from source ownership"
            Expect.isFalse (Set.contains "FS.GG.UI.Charts" packable) "the real pack path does not produce a Charts package"
            Expect.isFalse (capabilities.Contains("id: charts", StringComparison.OrdinalIgnoreCase)) "generated capability catalog has no active charts capability"
            Expect.isFalse (controlsProject.Contains(monolithRef, StringComparison.Ordinal)) "Controls package does not depend on the retired monolithic viewer/runtime project"
            Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, "src", "Controls", "DataGrid.fsi"))) "DataGrid public contract is owned by Controls"
        }

        test "generated products and surface checks do not keep Charts as an active package" {
            let packable = packablePackageIds ()

            // Every profile, not four of five: `game` — the template's DEFAULT starter since Feature 220 —
            // was absent from this list, so the one profile most products actually generate was never
            // Charts-checked (#511). `governed.yml` WAS named here and did not exist, and the
            // `File.Exists` filter below turned that into a silent pass. See the existence assertion.
            let generatedProductInputs =
                [ "template/capabilities.yml"
                  "template/profiles/app.yml"
                  "template/profiles/game.yml"
                  "template/profiles/governed.yml"
                  "template/profiles/headless-scene.yml"
                  "template/profiles/sample-pack.yml"
                  "template/base/Directory.Packages.props"
                  "template/base/src/Product/Product.fsproj"
                  "template/base/.agents/skills/fs-gg-project/SKILL.md"
                  "scripts/refresh-surface-baselines.fsx" ]

            let forbiddenTokens =
                [ "PackageReference Include=\"FS.GG.UI.Charts\""
                  "src/Charts/Charts.fsproj"
                  "id: charts"
                  "template/fragments/charts"
                  ".agents/skills/fs-gg-charts/SKILL.md" ]

            // A REQUIRED-input list that skips what it cannot find is not a gate (#511). This used to read
            // `List.filter (repositoryPath >> File.Exists)`, which converted "this required input is
            // MISSING" into "nothing to check here" — and it was not hypothetical: `governed.yml` was on
            // the list, did not exist, and was silently dropped, so the guard reported green over a file it
            // never opened. Scanning zero inputs and scanning ten clean ones must not share a verdict
            // (the fails-open class of FS-GG/.github#266).
            let missing = generatedProductInputs |> List.filter (repositoryPath >> File.Exists >> not)

            Expect.isEmpty
                missing
                "every input this guard claims to scan EXISTS — a missing one means the guard is checking \
                 less than it says, not that there is nothing to check"

            let activeHits =
                generatedProductInputs
                |> List.collect (fun relative ->
                    let content = File.ReadAllText(repositoryPath relative)

                    forbiddenTokens
                    |> List.choose (fun token ->
                        if content.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 then
                            Some $"{relative}: {token}"
                        else
                            None))

            Expect.isEmpty activeHits "active generated product inputs do not select Charts package, capability, project, or chart-specific generated skill"
            Expect.isNonEmpty packable "the real pack path discovers at least one packable FS.GG.UI.* package"
            Expect.isFalse (Set.contains "FS.GG.UI.Charts" packable) "the real pack path does not produce a Charts package"
            Expect.isFalse (File.Exists(repositoryPath "readiness/surface-baselines/FS.GG.UI.Charts.txt")) "legacy Charts package has no active surface baseline"
            Expect.isFalse (File.Exists(repositoryPath "template/fragments/charts/skill/SKILL.md")) "template has no chart-specific generated skill fragment"
            Expect.isFalse (File.Exists(repositoryPath "template/base/.agents/skills/fs-gg-charts/SKILL.md")) "generated product base has no chart-specific generated skill"
        }

        // #670 — the "surface checks" half of the guard above, re-pointed at what a surface check IS.
        //
        // It used to assert that an inert text file MENTIONED three of the baseline paths
        // (`Expect.stringContains build "readiness/surface-baselines/FS.GG.UI.Controls.txt"`). Three of
        // sixteen, named by hand, checked against a file nobody runs — so it could not tell you the one
        // thing worth knowing: whether a package ships a public surface that nothing has baselined.
        //
        // And that hole is REACHABLE. `scripts/refresh-surface-baselines.fsx` — the generator gate.yml
        // runs, and the only thing that writes these files — enumerates its packages from a HARDCODED
        // list of sixteen. Add a packable package and the generator does not know about it, so it writes
        // no baseline, so gate.yml's regenerate-then-git-diff sees no drift and no untracked file, and
        // SurfaceAreaTests never names it either. Every gate stays green while a package's entire public
        // API goes unwatched. Deriving the expectation from the packable set is what closes it: the new
        // package has no baseline, and this test says so by name.
        test "every packable package has a committed public-surface baseline" {
            let packable = packablePackageIds ()
            Expect.isNonEmpty packable "the real pack path discovers at least one packable FS.GG.UI.* package"

            // The BOM metapackage is dependencies-only (IncludeBuildOutput=false — see src/Meta): it
            // carries no assembly, so it has no public surface to baseline. Every OTHER packable package
            // ships one, and must have both baselines: the type-name file, and the `members/` file that
            // Issue #200 added because the type-name file cannot see a member added to an existing type.
            let surfaceBearing = packable |> Set.remove "FS.GG.UI"

            let missing =
                surfaceBearing
                |> Set.toList
                |> List.collect (fun packageId ->
                    [ $"readiness/surface-baselines/{packageId}.txt"
                      $"readiness/surface-baselines/members/{packageId}.txt" ])
                |> List.filter (repositoryPath >> File.Exists >> not)

            Expect.isEmpty
                missing
                $"every packable package has a committed type-name AND member baseline — a missing one is a \
                  public API that no surface gate watches, because the generator's package list never \
                  learned about it: {missing}"
        }

        // The smoke is too slow for the push gate, so it stays opt-in for Dev/Verify/Ci. "Opt-in"
        // only means something if something opts in: assert the release lane sets the flag, or the
        // pack -> consume path is tested nowhere and its green is worth nothing.
        test "the release lane opts the package consumer smoke in" {
            let release = File.ReadAllText(repositoryPath ".github/workflows/release.yml")

            Expect.stringContains
                release
                "FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE: \"1\""
                "release.yml must enable the package consumer smoke; never-by-default is not a cadence"
        }
    ]

    let deferredPackageSmokeTests =
        if Environment.GetEnvironmentVariable("FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE") = "1" then
            [ test "explicit package consumer smoke builds and runs a consumer against the packed feed" {
                  let feed = Path.Combine(Path.GetTempPath(), "fs-gg-ui-package-feed-" + Guid.NewGuid().ToString("N"))
                  Directory.CreateDirectory feed |> ignore

                  consumerSmokeProjects
                  |> List.iter (fun project ->
                      let exitCode, stdout, stderr =
                          runDotnetWithin 600000 repositoryRoot $"pack {project} -c Release -m:1 -p:Version={packageVersion} --output {feed}"

                      Expect.equal exitCode 0 $"packing {project} to the local feed:{Environment.NewLine}{stdout}{stderr}")

                  let missing =
                      consumerSmokePackages
                      |> List.filter (fun packageId -> not (File.Exists(Path.Combine(feed, $"{packageId}.{packageVersion}.nupkg"))))

                  Expect.isEmpty missing $"every package the consumer references was packed to the local feed (feed: {feed})"

                  let consumerRoot = Path.Combine(Path.GetTempPath(), "fs-gg-ui-package-consumer-" + Guid.NewGuid().ToString("N"))
                  Directory.CreateDirectory consumerRoot |> ignore

                  File.WriteAllText(
                      Path.Combine(consumerRoot, "NuGet.config"),
                      $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="{feed}" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""
                  )

                  let references =
                      consumerSmokePackages
                      |> List.map (fun packageId -> $"""    <PackageReference Include="{packageId}" Version="{packageVersion}" />""")
                      |> String.concat Environment.NewLine

                  // Central package management is on repo-wide; the consumer lives outside the repo's
                  // Directory.Packages.props, so it pins versions on the PackageReference itself.
                  File.WriteAllText(
                      Path.Combine(consumerRoot, "PackageConsumerSmoke.fsproj"),
                      $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.fs" />
  </ItemGroup>
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
"""
                  )

                  File.WriteAllText(Path.Combine(consumerRoot, "Program.fs"), consumerSmokeProgram)

                  let buildExit, buildStdout, buildStderr = runDotnetWithin 600000 consumerRoot "build -c Release"
                  Expect.equal buildExit 0 (buildStdout + buildStderr)

                  // Building proves the public API compiles; running proves the packages' native and
                  // managed assets actually load. Restore alone proved neither.
                  let runExit, runStdout, runStderr = runDotnetWithin 300000 consumerRoot "run -c Release --no-build"
                  Expect.equal runExit 0 (runStdout + runStderr)
                  Expect.stringContains runStdout "package consumer smoke:" "the consumer executed its FS.GG.UI calls"
              } ]
        else
            []

    testList "Package contract" (v1PackageTests @ deferredPackageSmokeTests)
