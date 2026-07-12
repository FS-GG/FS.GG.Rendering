module SkillPackageReachTests

// Issue #430 — a product skill may never out-reach the packages it tells the author to `open`.
//
// THE BUG THIS CLOSES
// -------------------
// The scaffold shipped `fs-gg-symbology` on all five profiles, plus the authoritative
// `docs/api-surface/Symbology*` mirror — while `template/base/Directory.Packages.props` pinned neither
// FS.GG.UI.Symbology nor FS.GG.UI.Symbology.Render, on any profile. So a generated product carried a
// skill instructing the author to `open FS.GG.UI.Symbology` and rasterise boards with `Render.toPng`,
// and the first line they wrote did not compile. Every signal the scaffold gave said the API was there.
//
// It survived because every existing gate is one-directional:
//   * M-REF (ApiSurfaceMirrorTests) asserts package => mirror. The converse is not asserted.
//   * M-PTR asserts a mirrored .fsi names a SHIPPED skill — which symbology's did. Skill + surface were
//     perfectly consistent with each other; the package was simply absent from the pair.
//   * validate-version-coherence checks the symbology REFERENCE RECIPE's `#r` pins — and an .fsx
//     resolves its own packages from nuget, so the recipe worked while the product did not.
// Nothing anywhere asserted the direction that actually bites an author: skill => package.
//
// THE INVARIANT (asserted for EVERY product skill, not just symbology)
//   R-PINNED — every FS.GG.* namespace a product skill's body tells the author to `open`, whose name is
//              exactly a real package id, is pinned in the template's Directory.Packages.props.
//   R-REF    — ...and referenced by the project whose compile graph the skill's `open` lands in, so it is
//              actually reachable and not merely pinned. That is Product.fsproj for almost every skill —
//              but a TEST-SCOPED skill (#432: fs-gg-testing) is authored in the generated product's test
//              project, so its packages are looked for in Product.Tests.fsproj UNION Product.fsproj (the
//              test project project-references the product, so the product's references flow into it).
//              See `testScopedSkills` — the classification is itself asserted, not merely declared.
//   R-REACH  — ...and the profiles the SKILL materializes on are a SUBSET of the profiles that pin it.
//              A skill reaching a profile its package does not is exactly #430: silent, type-check-free,
//              and indistinguishable from working until an author types the `open`.
//   R-CAT    — and `template/capabilities.yml`'s `profiles:` is EXACTLY the profiles its package reaches
//              (#483). Equality, not subset: the catalog is the human-facing inventory of what a profile
//              gets, so over-claiming and under-claiming are both lies. This is the roster that had NO
//              gate at all, and six of its seven rows had drifted by the time one was noticed.
//
// Deliberately sound rather than exhaustive: an `open` is only held against a package when the namespace
// is EXACTLY a known package id, so a namespace that merely lives inside a differently-named package is
// never mis-attributed. That is enough to have caught #430 on the PR that introduced it.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private propsPath = repositoryPath "template/base/Directory.Packages.props"
let private projPath = repositoryPath "template/base/src/Product/Product.fsproj"
let private testsProjPath = repositoryPath "template/base/tests/Product.Tests/Product.Tests.fsproj"
let private manifestPath = repositoryPath "template/skill-manifest/skill-manifest.json"

/// TEST-SCOPED skills (#432). A skill's `open` lines have to land on SOME compile graph, but not always the
/// product's: `fs-gg-testing` is about authoring the generated product's TESTS, and its helpers are consumed
/// from `tests/Product.Tests` (BehaviorTests.fs already calls `GeneratedLayoutValidation.validate`). Nothing
/// in the product source uses them, so referencing FS.GG.UI.Testing from Product.fsproj would make every
/// generated product carry a test-only dependency to satisfy a skill about its tests.
///
/// The invariant is UNCHANGED — a skill may not out-reach the packages it says to open. Only the question
/// "which project must carry the reference" is answered per skill. Getting this wrong in the permissive
/// direction is the whole bug class this file exists for, so the set is explicit and small rather than a
/// heuristic: add a skill here only when its `open`s genuinely belong in the test project.
let private testScopedSkills = set [ "fs-gg-testing" ]

/// The projects whose `PackageReference`s can put a package on the compile graph `id`'s `open` lands in.
///
/// A UNION, not a replacement. `Product.Tests.fsproj` carries
/// `<ProjectReference Include="..\..\src\Product\Product.fsproj" />`, so the product's package references
/// flow into the test project transitively: a test-scoped skill that opens a package the PRODUCT already
/// carries (FS.GG.UI.Scene, say) is perfectly compilable, and modelling the redirect as a replacement would
/// red it and push someone to add a redundant reference to the template to shut the guard up.
let private referencingProjectsOf id =
    let product = projPath, "template/base/src/Product/Product.fsproj"

    if Set.contains id testScopedSkills then
        [ testsProjPath, "template/base/tests/Product.Tests/Product.Tests.fsproj"; product ]
    else
        [ product ]
let private skillistRel = "template/base/docs/skillist-reference.md"
let private skillistPath = repositoryPath skillistRel

let private capabilitiesRel = "template/capabilities.yml"
let private capabilitiesPath = repositoryPath capabilitiesRel

/// Every profile a scaffold can be generated on (.template.config/template.json `symbols.profile`).
let private allProfiles = set [ "app"; "headless-scene"; "governed"; "sample-pack"; "game" ]

/// Package ids the scaffold COULD pin: everything packable in this repo, plus everything the template
/// already pins (which is how the external FS.GG.Game.* / FS.GG.Audio.* components enter the set).
/// A package that is packable but UNPINNED is precisely the #430 shape, so the repo side must be in
/// here — deriving the candidate set from the props file alone would make the bug invisible to its own
/// guard.
let private candidatePackages =
    let packable =
        Directory.GetFiles(repositoryPath "src", "*.fsproj", SearchOption.AllDirectories)
        |> Seq.map File.ReadAllText
        |> Seq.filter (fun t -> t.Contains "<IsPackable>true</IsPackable>")
        |> Seq.choose (fun t ->
            let m = Regex.Match(t, "<PackageId>([^<]+)</PackageId>")
            if m.Success then Some(m.Groups.[1].Value.Trim()) else None)
    let pinned =
        Regex.Matches(File.ReadAllText propsPath, "<PackageVersion\\s+Include=\"([^\"]+)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value)
        |> Seq.filter (fun id -> id.StartsWith "FS.GG.")
    Set.union (Set.ofSeq packable) (Set.ofSeq pinned)

/// The `dotnet new` gate opening the region a line sits in, or None when the line is ungated.
/// Walks upward to the nearest `<!--#if -->` not already closed by an intervening `<!--#endif -->`
/// (the same walk AudioProfileWiringTests and validate-template-payload-pins.fsx use).
let private enclosingGate (lines: string[]) (lineIndex: int) =
    let ifRegex = Regex(@"<!--#if\s+\((?<cond>.*?)\)\s*-->")
    let rec walk i depth =
        if i < 0 then None
        elif lines.[i].Contains "<!--#endif" then walk (i - 1) (depth + 1)
        else
            let m = ifRegex.Match lines.[i]
            if m.Success then
                if depth = 0 then Some(m.Groups.["cond"].Value.Trim()) else walk (i - 1) (depth - 1)
            else walk (i - 1) depth
    walk lineIndex 0

/// The profiles on which `packageId` is declared in `text` (a props or fsproj file), or None when it is
/// not declared there at all. An ungated declaration reaches every profile.
///
/// UNIONS every declaration, rather than reading the first: the props file already carries two separate
/// `(app || sample-pack || game)` regions, so a package legitimately CAN be declared more than once, and
/// taking only the first would under-report its reach and fail R-REACH on profiles that are in fact
/// covered. The exact `Include="<id>"` match (closing quote included) keeps `FS.GG.UI.Symbology` from
/// matching the `FS.GG.UI.Symbology.Render` line — a prefix collision that would silently merge the two.
let private profilesDeclaring (text: string) (packageId: string) =
    let lines = text.Replace("\r\n", "\n").Split('\n')
    let needle = $"Include=\"{packageId}\""

    let declared =
        lines
        |> Array.indexed
        |> Array.filter (fun (_, l) -> l.Contains needle)
        |> Array.map (fun (idx, _) ->
            match enclosingGate lines idx with
            | None -> allProfiles
            | Some cond ->
                Regex.Matches(cond, "\"([^\"]+)\"")
                |> Seq.map (fun m -> m.Groups.[1].Value)
                |> Set.ofSeq)

    if Array.isEmpty declared then None else Some(Set.unionMany declared)

/// The profiles on which `pkg` reaches the compile graph `id`'s `open` lands in — the UNION over every
/// project that can carry it (see `referencingProjectsOf`). None when NO such project declares it at all,
/// which is R-REF's failure to report rather than R-REACH's.
let private refProfilesFor id pkg =
    let declared =
        referencingProjectsOf id
        |> List.choose (fun (path, _) -> profilesDeclaring (File.ReadAllText path) pkg)

    if List.isEmpty declared then None else Some(Set.unionMany declared)

/// The profile set of a manifest `materializes-when` clause (ADR-0017 grammar: `profile in [a, b]`,
/// optionally `and`-ed with non-profile clauses such as `lifecycle == spec-kit`, which we ignore — they
/// narrow WHEN a skill ships, never onto a profile its `profile in [..]` clause excluded).
/// A row with no profile clause constrains no profile, so it reaches all of them.
let private profilesOf (materializesWhen: string) =
    let m = Regex.Match(materializesWhen, @"profile\s+in\s+\[([^\]]+)\]")
    if m.Success then
        m.Groups.[1].Value.Split(',') |> Seq.map (fun s -> s.Trim()) |> Set.ofSeq
    else
        let eq = Regex.Match(materializesWhen, @"profile\s*==\s*(\w[\w-]*)")
        if eq.Success then set [ eq.Groups.[1].Value ] else allProfiles

/// (capability id, packageId, the profiles the row CLAIMS — None when it declares none) for every RUNTIME
/// capability in the catalog. `non-runtime` rows (samples) are dropped: they pin no package, so there is no
/// reach to hold them to.
///
/// A missing `profiles:` is carried as None rather than dropping the row, because dropping it would fail
/// OPEN: add a capability, forget the field, and R-CAT would assert nothing about it while still reading
/// green. The row that started #483 was wrong for years precisely because no gate looked at it — a gate
/// that quietly skips the rows it cannot parse reintroduces that, one refactor later.
let private capabilityRows =
    let text = File.ReadAllText capabilitiesPath
    // Anchored on the two-space `  - id:` list indent, and each row's body runs to the next row — so a field
    // is never read out of a neighbouring row (the same boundary discipline `productSkills` uses below).
    Regex.Matches(text, @"^  - id: (?<id>\S+)(?<body>(?:\n(?!  - id: ).*)*)", RegexOptions.Multiline)
    |> Seq.choose (fun m ->
        let body = m.Groups.["body"].Value

        let field name =
            let f = Regex.Match(body, $@"^\s+{name}:\s*(?<v>.+?)\s*$", RegexOptions.Multiline)
            if f.Success then Some f.Groups.["v"].Value else None

        match field "packageId" with
        | Some pkg when pkg <> "non-runtime" ->
            let claimed =
                field "profiles"
                |> Option.map (fun profiles ->
                    Regex.Match(profiles, @"\[(?<p>[^\]]*)\]").Groups.["p"].Value.Split(',')
                    |> Seq.map (fun s -> s.Trim())
                    |> Seq.filter (fun s -> s <> "")
                    |> Set.ofSeq)

            Some(m.Groups.["id"].Value, pkg, claimed)
        | _ -> None)
    |> List.ofSeq

/// R-FRAG's two inputs.
///
/// EVERY capability row — including the `non-runtime` ones `capabilityRows` drops, because `samples` is a
/// non-runtime row and is the ONE row whose fragment actually ships. A parser that skipped it would leave the
/// only true `materializes:` in the catalog unasserted, which is the exact shape of the hole #510 reports.
let private capabilityFragments =
    let text = File.ReadAllText capabilitiesPath

    Regex.Matches(text, @"^  - id: (?<id>\S+)(?<body>(?:\n(?!  - id: ).*)*)", RegexOptions.Multiline)
    |> Seq.map (fun m ->
        let body = m.Groups.["body"].Value

        let field name =
            let f = Regex.Match(body, $@"^\s+{name}:\s*(?<v>.+?)\s*$", RegexOptions.Multiline)
            if f.Success then Some f.Groups.["v"].Value else None

        let materializes =
            field "materializes"
            |> Option.map (fun v ->
                if v.Trim() = "none" then
                    Set.empty
                else
                    Regex.Match(v, @"\[(?<p>[^\]]*)\]").Groups.["p"].Value.Split(',')
                    |> Seq.map (fun s -> s.Trim().TrimEnd '/')
                    |> Seq.filter (fun s -> s <> "")
                    |> Set.ofSeq)

        m.Groups.["id"].Value, field "templateFragment", materializes)
    |> List.ofSeq

/// R-SKILL's two inputs (#564).

/// (capability id, its `skill:` pointer) for EVERY capability row — non-runtime included, for the same reason
/// `capabilityFragments` includes them: `samples` carries a `skill:` too, and a rule that quietly skipped the
/// one row that is shaped differently is how these catalogs drift in the first place.
let private capabilitySkills =
    let text = File.ReadAllText capabilitiesPath

    Regex.Matches(text, @"^  - id: (?<id>\S+)(?<body>(?:\n(?!  - id: ).*)*)", RegexOptions.Multiline)
    |> Seq.map (fun m ->
        let body = m.Groups.["body"].Value

        let skill =
            Regex.Match(body, @"^\s+skill:\s*(?<v>.+?)\s*$", RegexOptions.Multiline)
            |> fun f -> if f.Success then Some(f.Groups.["v"].Value) else None

        m.Groups.["id"].Value, skill)
    |> List.ofSeq

/// Bound rather than inlined: F# forbids a string literal inside an interpolated expression in a
/// single-quoted interpolated string, and R-SKILL's failure message renders a set.
let private commaSorted (items: string seq) = String.Join(", ", Seq.sort items)

/// Every directory the MANIFEST names as some skill's `supplied-by`.
///
/// The manifest is the authority on which SKILL.md a generated product actually receives — capabilities.yml's
/// own header concedes as much ("the manifest's `materializes-when` is the authority for that"). So it is the
/// side R-SKILL holds the catalog TO, rather than the other way round.
let private manifestSuppliedByDirs =
    Regex.Matches(File.ReadAllText manifestPath, "\"supplied-by\":\\s*\"(?<dir>[^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.["dir"].Value.Replace('\\', '/').TrimEnd '/')
    |> Set.ofSeq

/// What the scaffold REALLY copies: the `sources[].source` roots in .template.config/template.json that live
/// under `template/fragments/`. template.json is the only thing `dotnet new` reads, so it is the authority.
///
/// Parsed as JSON and normalized with `TrimEnd '/'` — the same normalization tests/TestSupport/ScaffoldSources.fs
/// applies to this exact field, for the same reason. `"template/fragments/scene"` and `"template/fragments/scene/"`
/// name the SAME tree and `dotnet new` treats them identically, so a comparison that only handles the trailing-slash
/// spelling would find nothing under the slash-less one, report the fragment as shipping nothing, and bless a
/// `materializes: none` row for a fragment a product receives IN FULL. That is the precise direction this rule
/// exists to catch, so it is normalized on BOTH sides rather than trusted to a house style in a JSON file.
let private templateJsonFragmentSources =
    use document = JsonDocument.Parse(File.ReadAllText(repositoryPath ".template.config/template.json"))

    document.RootElement.GetProperty("sources").EnumerateArray()
    |> Seq.choose (fun source ->
        match source.TryGetProperty "source" with
        | true, value ->
            match value.GetString() with
            | null -> None
            | raw ->
                let root = raw.TrimEnd '/'
                if root.StartsWith "template/fragments/" then Some root else None
        | _ -> None)
    |> Set.ofSeq

/// The profiles a capability's package actually reaches: pinned AND referenced by a project the generated
/// product compiles.
///
/// The PRODUCT's references when the product carries the package, falling back to the test project's only
/// when it does not — which is what "test-scoped" MEANS for a package, and is how FS.GG.UI.Testing (#432,
/// referenced from Product.Tests.fsproj alone) enters the catalog at all.
///
/// Deliberately NOT a blind union of the two. Unioning the test project into every capability would let a
/// package added to Product.Tests.fsproj widen the reach R-CAT blesses for a capability whose author consumes
/// it from PRODUCT source: the catalog could then claim `layout` on a profile where `open FS.GG.UI.Layout` in
/// Program.fs does not compile, and this guard would bless it. That is the permissive direction `testScopedSkills`
/// above exists to police, and the whole bug class (#430) this file was written for. Erring the other way merely
/// under-reports, which R-CAT reports as a loud RED rather than a silent green.
let private capabilityReach pkg =
    let pin = profilesDeclaring (File.ReadAllText propsPath) pkg

    let refs =
        match profilesDeclaring (File.ReadAllText projPath) pkg with
        | Some product -> Some product
        | None -> profilesDeclaring (File.ReadAllText testsProjPath) pkg

    match pin, refs with
    | Some pins, Some refs -> Some(Set.intersect pins refs)
    | _ -> None

/// KNOWN, FILED violations of R-REACH that predate this guard — named, never silent.
///
/// `fs-gg-project` (issue #431) is FIXED. The product-orientation umbrella ships on all five profiles and
/// its `## Usage` example opened FS.GG.UI.SkiaViewer + called `Viewer.runApp`, while `headless-scene` and
/// `governed` pin no viewer. Markdown cannot be profile-gated (template.json's skillist-reference row
/// records that inline `#if` in markdown is unproven), so the example had to become true on EVERY profile:
/// it is now lane-neutral and opens only FS.GG.UI.Scene, the one package all five pin. Nothing was lost —
/// the viewer wiring it dropped was a verbatim duplicate of `fs-gg-skiaviewer`'s, and THAT skill is already
/// gated to exactly the three profiles that pin a viewer. An umbrella that ships everywhere may only teach
/// what holds everywhere; the profile-specific entry point belongs to the profile-specific skill.
///
/// `fs-gg-testing` (issue #432) is FIXED and its exemption is therefore GONE — it is now held to R-REACH
/// like every other skill. #90 had widened the skill's materializes-when to all five profiles and never
/// widened the pin with it, so four of the five shipped a testing skill whose first `open` did not compile.
/// #432 finished #90: the pin is ungated, and the REFERENCE moved to `Product.Tests.fsproj` (see
/// `testScopedSkills` above) rather than being widened on `Product.fsproj` — the product source never used
/// the package, and spreading a test-only dependency across all five profiles would have traded one defect
/// for a worse one.
///
/// **THE MAP IS NOW EMPTY, AND THAT IS THE POINT.** This guard shipped with two exemptions because it
/// DISCOVERED two pre-existing defects on the day it was written — which was always the argument for it.
/// Both are now fixed (#431, #432), so every product skill is held to R-REACH with no exceptions, and the
/// class #430 opened is closed.
///
/// The machinery stays. An exemption is a debt with a number on it, not a pass: R-PINNED/R-REF still hold
/// for an exempted skill (the package must exist and be referenced SOMEWHERE), only the profile-subset
/// check is waived, and the "still violating" test below deletes the row the moment its defect is fixed.
/// Adding a row requires a filed issue; the guard is worthless the moment it becomes a place to put things
/// that are merely inconvenient. Re-opening this map should feel like taking on debt, because it is.
let private reachExemptions : Map<string, string> = Map.empty

/// (skill id, profiles it materializes on, the FS.GG.* packages its body says to `open`).
let private productSkills =
    let manifest = File.ReadAllText manifestPath
    // Each row: id, materializes-when, supplied-by (the skill's source directory). `[^{}]*?` rather than
    // `[\s\S]*?` so a match can never span a JSON object boundary — a row missing one of the three fields
    // would otherwise pair THIS skill's id with the NEXT skill's materializes-when, and the guard would
    // hold a skill to a gate that is not its own.
    Regex.Matches(
        manifest,
        "\"id\":\\s*\"(?<id>[^\"]+)\"[^{}]*?\"materializes-when\":\\s*\"(?<when>[^\"]+)\"[^{}]*?\"supplied-by\":\\s*\"(?<dir>[^\"]+)\""
    )
    |> Seq.map (fun m ->
        let id = m.Groups.["id"].Value
        let profiles = profilesOf m.Groups.["when"].Value
        let dir = repositoryPath (m.Groups.["dir"].Value)
        // The instructional body only — an author copies `open` lines out of SKILL.md into product
        // source. A reference .fsx is NOT read: it is an FSI script that `#r`s its own packages from
        // nuget, so its opens prove nothing about the compiled product's reference set.
        let skillMd = Path.Combine(dir, "SKILL.md")
        let opens =
            if File.Exists skillMd then
                Regex.Matches(File.ReadAllText skillMd, @"open\s+(FS\.GG\.[A-Za-z0-9_.]+)")
                |> Seq.map (fun o -> o.Groups.[1].Value)
                |> Seq.filter candidatePackages.Contains
                |> Set.ofSeq
            else
                Set.empty
        id, profiles, opens)
    |> Seq.filter (fun (_, _, opens) -> not opens.IsEmpty)
    |> List.ofSeq

// ---------------------------------------------------------------------------------------------
// R-INST (#624) — A SKILL MAY NOT MANDATE A RULE WHOSE INSTRUMENT THE PRODUCT NEVER RECEIVES.
//
// A product skill may DELEGATE: state a rule in its own Evidence Rules and point at another skill for
// the instrument that satisfies it. #507 does exactly that — `fs-gg-ui-widgets` demands responsiveness
// evidence and points at `[[fs-gg-elmish]]` for `captureRespondsProof` / the `OnFrameMetrics`
// projection. Nothing compared the two `materializes-when` sets, so narrowing `fs-gg-elmish` to `[app]`
// would hand a `game` product a MANDATE WITH NO INSTRUMENT, with every test green.
//
// THE OBVIOUS RULE IS WRONG, and that is the whole difficulty. "A skill's `[[link]]` target must
// materialize wherever the linking skill does" would go RED on correct, deliberate guidance:
// `fs-gg-testing` ships to `headless-scene` and `governed`, `fs-gg-elmish` does not, and it links there
// ANYWAY — on purpose, and it says why (a headless product has no controls to click, so the mandate is
// vacuous for it). A gate with a wrong oracle is worse than no gate: the first thing it does is force an
// exemption for correct work, and the exemption is what the next real bug hides behind.
//
// SO THE INVARIANT IS ABOUT THE MANDATE, NOT THE LINK. Containment is required only where the linking
// skill DECLARES that the target supplies the instrument for a rule it states. That declaration is a
// frontmatter block — structural, not prose:
//
//     ---
//     name: fs-gg-ui-widgets
//     instruments:
//       - rule: responsiveness evidence
//         skill: fs-gg-elmish
//     ---
//
// Frontmatter rather than a marker inside the prose sentence, deliberately: a prose marker is lost by
// the next person who rewords the paragraph, and losing it is SILENT — the pointer stops being judged
// and the gate goes on reporting green. That is the exact rot this rule exists to catch, so the
// declaration is kept where a rewording cannot reach it.
//
// It fails OPEN by construction, and that is a known, bounded cost: an author who delegates and does not
// declare it is not judged. The alternative — judging every `[[link]]` — is the wrong oracle above. The
// shape is documented in `template/product-skills/README.md`, where the next skill author is looking.
// ---------------------------------------------------------------------------------------------

/// (id, profiles, skill-body path) for EVERY product skill — not just the ones that `open` a package.
/// `productSkills` above filters to skills with FS.GG opens, which is right for R-REACH and wrong here:
/// a skill that opens nothing can still mandate a rule and delegate its instrument.
let private allProductSkills =
    let manifest = File.ReadAllText manifestPath

    Regex.Matches(
        manifest,
        "\"id\":\\s*\"(?<id>[^\"]+)\"[^{}]*?\"materializes-when\":\\s*\"(?<when>[^\"]+)\"[^{}]*?\"supplied-by\":\\s*\"(?<dir>[^\"]+)\""
    )
    |> Seq.map (fun m ->
        m.Groups.["id"].Value,
        profilesOf m.Groups.["when"].Value,
        Path.Combine(repositoryPath (m.Groups.["dir"].Value), "SKILL.md"))
    |> List.ofSeq

/// A declared delegation: "I state RULE, and SKILL supplies the instrument for it."
type InstrumentDeclaration =
    { Linker: string
      LinkerProfiles: Set<string>
      LinkerBody: string
      Rule: string
      Target: string }

/// The YAML frontmatter block — the leading `---` … `---`. Read as its own region so an `instruments:`
/// word occurring in the PROSE cannot be mistaken for the declaration.
let private frontmatterOf (path: string) =
    if not (File.Exists path) then
        ""
    else
        let text = File.ReadAllText path
        let m = Regex.Match(text, @"\A---\s*\n(?<body>[\s\S]*?)\n---\s*\n")
        if m.Success then m.Groups.["body"].Value else ""

let private instrumentEntryRegex =
    Regex(@"-\s*rule:\s*(?<rule>[^\n]+?)\s*\n\s*skill:\s*(?<skill>[\w-]+)", RegexOptions.Compiled)

let private instrumentDeclarations =
    allProductSkills
    |> List.collect (fun (id, profiles, bodyPath) ->
        let front = frontmatterOf bodyPath

        // Only the `instruments:` block, so a `skill:` key elsewhere in the frontmatter cannot be read
        // as a declaration.
        let block =
            let m = Regex.Match(front, @"(?m)^instruments:\s*\n(?<body>(?:[ \t]+.*\n?)+)")
            if m.Success then m.Groups.["body"].Value else ""

        let body = if File.Exists bodyPath then File.ReadAllText bodyPath else ""

        instrumentEntryRegex.Matches block
        |> Seq.map (fun m ->
            { Linker = id
              LinkerProfiles = profiles
              LinkerBody = body
              Rule = m.Groups.["rule"].Value.Trim()
              Target = m.Groups.["skill"].Value })
        |> List.ofSeq)


[<Tests>]
let skillPackageReachTests =
    testList
        "Skill package reach (#430) — a skill may not out-reach the packages it says to open"
        [
          // ---- R-INST (#624) -----------------------------------------------------------------

          // The floor. A gate whose subject set is empty asserts nothing and reports green — the shape
          // this whole file is written against — and R-INST is unusually exposed to it, because its
          // subjects are OPT-IN. If the frontmatter parser breaks, or the last declaration is deleted,
          // every assertion below passes vacuously.
          test "R-INST has instrument declarations under test (the parser still reads frontmatter)" {
              Expect.isNonEmpty
                  instrumentDeclarations
                  "NO skill declares an `instruments:` block, so R-INST judges nothing and every \
                   assertion below is vacuous. Either the frontmatter parser broke, or the last \
                   declaration was deleted — both are defects, and neither is a pass. The shape is in \
                   template/product-skills/README.md."

              let widgets =
                  instrumentDeclarations
                  |> List.tryFind (fun d -> d.Linker = "fs-gg-ui-widgets" && d.Target = "fs-gg-elmish")

              Expect.isSome
                  widgets
                  "fs-gg-ui-widgets -> fs-gg-elmish is the delegation #507 created and #624 was filed \
                   for; it must be one of the declarations under test."
          }

          // The rule. Containment, and only for DECLARED instruments.
          test "R-INST — a declared instrument skill materializes wherever the skill that mandates it does" {
              let known = allProductSkills |> List.map (fun (id, _, _) -> id) |> Set.ofList

              // A target that is not a skill at all. Fail CLOSED: a typo'd id would otherwise resolve to
              // no profiles and be silently excused, which turns the declaration into a decoration.
              let unknown =
                  instrumentDeclarations
                  |> List.filter (fun d -> not (known.Contains d.Target))
                  |> List.map (fun d -> $"{d.Linker} -> {d.Target}")

              Expect.isEmpty
                  unknown
                  $"an `instruments:` entry names a skill that does not exist in the manifest — so it \
                    resolves to no profiles and would be excused by every check below. Fix the id.\n\n\
                    Unknown: {commaSorted unknown}"

              // The declaration must still match the PROSE. A frontmatter entry whose `[[link]]` has been
              // deleted from the body is a mandate whose pointer is gone — the reader is told to produce
              // evidence and never told where the instrument is.
              let unlinked =
                  instrumentDeclarations
                  |> List.filter (fun d -> not (d.LinkerBody.Contains $"[[{d.Target}]]"))
                  |> List.map (fun d -> $"{d.Linker} -> {d.Target}")

              Expect.isEmpty
                  unlinked
                  $"a skill DECLARES that another supplies the instrument for one of its rules, and its \
                    body no longer links `[[that skill]]` anywhere. The declaration and the prose have \
                    drifted apart: a reader is handed the mandate and never handed the pointer.\n\n\
                    Declared but not linked: {commaSorted unlinked}"

              // The invariant itself.
              let starved =
                  instrumentDeclarations
                  |> List.filter (fun d -> known.Contains d.Target)
                  |> List.choose (fun d ->
                      let targetProfiles =
                          allProductSkills
                          |> List.tryPick (fun (id, profiles, _) -> if id = d.Target then Some profiles else None)
                          |> Option.defaultValue Set.empty

                      let missing = Set.difference d.LinkerProfiles targetProfiles

                      if missing.IsEmpty then
                          None
                      else
                          Some
                              $"{d.Linker} mandates \"{d.Rule}\" on [{commaSorted d.LinkerProfiles}] and \
                                delegates its instrument to {d.Target}, which does not materialize on \
                                [{commaSorted missing}]")

              let renderedStarved = String.Join("\n", starved)

              Expect.isEmpty
                  starved
                  $"a product on these profiles is handed a RULE and never handed the INSTRUMENT for it. \
                    That is #507's contradiction, one level up: the skill tells the author to produce \
                    evidence, and the skill that teaches how to produce it is not in their scaffold.\n\n\
                    Either widen the instrument skill's `materializes-when`, narrow the mandating \
                    skill's, or move the rule.\n\n{renderedStarved}"
          }

          // THE ORACLE, ANCHORED — and this is the test that earns R-INST its shape. `fs-gg-testing`
          // links `[[fs-gg-elmish]]` and ships to two profiles fs-gg-elmish does NOT reach. A gate that
          // judged every `[[link]]` would redden it, and it is CORRECT: a headless-scene product has no
          // controls to click, so the mandate is vacuous for it, and the skill says so.
          //
          // R-INST is silent on it because it is UNDECLARED, not because the numbers happen to work out.
          // This test proves that distinction is real: the containment it would fail is asserted here
          // explicitly, so if someone ever "helpfully" marks that link, R-INST reddens — correctly, and
          // loudly — rather than the anchor rotting into a tautology.
          test "R-INST does not fire on fs-gg-testing -> fs-gg-elmish (the deliberate cross-profile reference)" {
              let profilesFor id =
                  allProductSkills
                  |> List.tryPick (fun (skill, profiles, _) -> if skill = id then Some profiles else None)

              let testing = profilesFor "fs-gg-testing"
              let elmish = profilesFor "fs-gg-elmish"

              Expect.isSome testing "fs-gg-testing is in the manifest"
              Expect.isSome elmish "fs-gg-elmish is in the manifest"

              let body =
                  allProductSkills
                  |> List.tryPick (fun (id, _, path) ->
                      if id = "fs-gg-testing" && File.Exists path then Some(File.ReadAllText path) else None)
                  |> Option.defaultValue ""

              Expect.stringContains
                  body
                  "[[fs-gg-elmish]]"
                  "fs-gg-testing really does link fs-gg-elmish — if this ever stops being true, this \
                   anchor is testing nothing and R-INST's hardest case has quietly left the repo."

              let gap = Set.difference testing.Value elmish.Value

              Expect.isNonEmpty
                  gap
                  "fs-gg-testing is supposed to reach profiles fs-gg-elmish does not (headless-scene, \
                   governed) — that gap is the whole reason R-INST judges declarations and not links. If \
                   the gap has closed, this anchor no longer proves anything and R-INST's oracle needs \
                   re-examining, not re-blessing."

              let declared =
                  instrumentDeclarations
                  |> List.exists (fun d -> d.Linker = "fs-gg-testing" && d.Target = "fs-gg-elmish")

              Expect.isFalse
                  declared
                  $"fs-gg-testing now DECLARES fs-gg-elmish as an instrument, and fs-gg-elmish does not \
                    materialize on [{commaSorted gap}]. If that delegation is real, the rule it supplies \
                    must not be mandated on those profiles. If it is a cross-reference — which is what it \
                    has always been — it must not be declared as an instrument."
          }

          // A floor: if the manifest regex ever stops matching, every assertion below vacuously passes
          // and the guard reads green while asserting nothing. Symbology is named because it is the bug
          // this file exists for — it must always be one of the rows under test.
          test "the guard actually has product skills (with FS.GG package opens) under test" {
              Expect.isGreaterThan (List.length productSkills) 3 "several product skills open FS.GG packages"

              let symbology = productSkills |> List.tryFind (fun (id, _, _) -> id = "fs-gg-symbology")
              Expect.isSome symbology "fs-gg-symbology is under test — it is the skill #430 was filed for"

              let _, _, opens = symbology.Value
              Expect.contains opens "FS.GG.UI.Symbology" "the symbology skill's body opens the pure vocabulary"
              Expect.contains opens "FS.GG.UI.Symbology.Render" "...and the render bridge its design loop calls"
          }

          // R-PINNED / R-REF — the package an author is told to `open` is on the compile graph at all.
          // Which compile graph is per-skill: a test-scoped skill (#432) must be referenced by the generated
          // product's TEST project, not its product source. Central pinning without a reference is #430's bug.
          test "every package a product skill says to open is pinned AND referenced by the project that opens it" {
              let props = File.ReadAllText propsPath

              for id, _, opens in productSkills do
                  let refRels = referencingProjectsOf id |> List.map snd |> String.concat " or "

                  for pkg in opens do
                      Expect.isSome
                          (profilesDeclaring props pkg)
                          $"the {id} skill tells the author to `open {pkg}`, so {pkg} must be pinned in template/base/Directory.Packages.props — a skill that names a package the scaffold does not pin does not compile (#430)"

                      Expect.isSome
                          (refProfilesFor id pkg)
                          $"the {id} skill tells the author to `open {pkg}`, so {refRels} must reference {pkg} — pinning it centrally without referencing it leaves it off the compile graph (#430)"
          }

          // The test-scoped redirect (#432) is the one place this guard is TOLD something rather than
          // checking it, and a redirect that is merely declared is a fail-open hatch: drop a skill into
          // `testScopedSkills`, move its package onto the test project, and R-REF/R-REACH stop consulting
          // the product at all — while the author's `open` still sits in Program.fs and still does not
          // compile. That is #430 shipped past its own guard, by the guard's own author. So the
          // classification is ASSERTED, not assumed: a test-scoped skill's packages must be absent from
          // every product source and present in at least one test source.
          test "every test-scoped skill really is test-scoped (the redirect is checked, not declared)" {
              let sourcesUnder relative =
                  Directory.EnumerateFiles(repositoryPath relative, "*.fs", SearchOption.AllDirectories)
                  |> Seq.map (fun path -> path, File.ReadAllText path)
                  |> List.ofSeq

              // Source files only: Product.fsproj carries a comment NAMING the package to explain its
              // absence, and a substring scan over project files would read that tombstone as a use.
              let productSources = sourcesUnder "template/base/src"
              let testSources = sourcesUnder "template/base/tests"

              for id in testScopedSkills do
                  let row = productSkills |> List.tryFind (fun (sid, _, _) -> sid = id)
                  Expect.isSome row $"test-scoped skill {id} is a product skill that opens FS.GG packages"

                  let _, _, opens = row.Value

                  for pkg in opens do
                      let usedIn sources =
                          sources
                          |> List.filter (fun (_, text: string) -> text.Contains pkg)
                          |> List.map (fun (path: string, _) -> Path.GetFileName path)

                      Expect.isEmpty
                          (usedIn productSources)
                          $"{id} is classified test-scoped, so R-REF/R-REACH no longer require {pkg} on Product.fsproj — but product source USES it ({usedIn productSources}). The redirect is wrong: the package belongs on the product, and holding this skill to the test project instead would let a real #430 violation walk straight through."

                      Expect.isNonEmpty
                          (usedIn testSources)
                          $"{id} is classified test-scoped, but nothing under template/base/tests uses {pkg} — no evidence supports the claim that its `open` lands in the test project. Remove it from testScopedSkills rather than let an unfounded redirect weaken the guard."
          }

          // An exemption that outlives its defect is a lie of a quieter kind: it keeps the guard green
          // over a skill that no longer needs the waiver, and the next real violation of that skill
          // passes unseen. So every exemption must STILL be violating — fix #431 and this test fails
          // until the row is deleted, which is exactly when it should be.
          test "every R-REACH exemption is still violating (a stale waiver must be deleted, not kept)" {
              let props = File.ReadAllText propsPath

              for KeyValue(id, issue) in reachExemptions do
                  let row = productSkills |> List.tryFind (fun (sid, _, _) -> sid = id)
                  Expect.isSome row $"exempted skill {id} ({issue}) still exists and still opens FS.GG packages"

                  let _, skillProfiles, opens = row.Value
                  // `refProfilesFor`, NOT a hard-coded Product.fsproj read: if a skill were ever both
                  // test-scoped AND exempted, reading only the product would report None for a test-only
                  // package, fall to the `| _ -> true` arm, and leave this test unconditionally green —
                  // a stale waiver surviving inside the very guard that exists to kill stale waivers.
                  let stillOrphaned =
                      opens
                      |> Set.exists (fun pkg ->
                          match profilesDeclaring props pkg, refProfilesFor id pkg with
                          | Some pins, Some refs -> not (Set.isEmpty (Set.difference skillProfiles (Set.intersect pins refs)))
                          | _ -> true)

                  Expect.isTrue
                      stillOrphaned
                      $"the {id} exemption ({issue}) no longer violates R-REACH — the defect it waives is fixed, so DELETE the row from reachExemptions and let the guard hold {id} to the invariant like every other skill"
          }

          // R-DOC — `docs/skillist-reference.md` SHIPS TO THE GENERATED PRODUCT and its "Profiles" column
          // is what an author reads to learn which skills their scaffold vendors. It is hand-maintained,
          // and nothing asserted it against the manifest — so narrowing a skill's materializes-when left
          // the shipped doc claiming the old reach, silently. (That is not hypothetical: #430's own first
          // pass did exactly this, and only a manual read caught it.) A skill roster that lies to the
          // author is the same failure as a skill with no package: it looks wired.
          test "the shipped skillist-reference profile column matches the manifest's materializes-when" {
              let skillist = File.ReadAllText skillistPath
              let manifest = File.ReadAllText manifestPath

              // | `fs-gg-x` | .agents/skills/fs-gg-x/SKILL.md | app, game |
              let rows =
                  Regex.Matches(skillist, @"\|\s*`(?<id>fs-gg-[\w-]+)`\s*\|[^|]*\|\s*(?<profiles>[^|]+?)\s*\|")
                  |> Seq.map (fun m ->
                      m.Groups.["id"].Value,
                      m.Groups.["profiles"].Value.Split(',') |> Seq.map (fun s -> s.Trim()) |> Set.ofSeq)
                  |> List.ofSeq

              Expect.isNonEmpty rows $"{skillistRel} has a parseable skill roster (if this fails the guard is vacuous)"

              for id, documented in rows do
                  let m = Regex.Match(manifest, $"\"id\":\\s*\"{Regex.Escape id}\"[\\s\\S]*?\"materializes-when\":\\s*\"(?<when>[^\"]+)\"")

                  // An exempted skill's row cannot be settled while its issue is open: which side is wrong
                  // depends on how that issue resolves, so correcting it here would prejudge the answer.
                  // `fs-gg-testing` WAS the live case; #432 settled it by widening the package rather than
                  // narrowing the skill, so its roster row now reads all five and it is held to R-DOC like
                  // every other skill. `fs-gg-project` (#431) remains open.
                  if m.Success && not (reachExemptions.ContainsKey id) then
                      let actual = profilesOf m.Groups.["when"].Value

                      Expect.equal
                          documented
                          actual
                          $"{skillistRel} tells the product author that `{id}` vendors on {Set.toList documented}, but the manifest materializes it on {Set.toList actual} — the shipped skill roster is lying to the author. Update the row when you change a skill's gate."
          }

          // R-CAT — `template/capabilities.yml` is the fourth roster of "what reaches which profile", and
          // until #483 it was the one nothing held against anything. So it drifted, and not subtly: SIX of
          // its seven runtime rows were wrong. Five over-claimed `governed` (a profile that pins no viewer,
          // no elmish, no keyboard-input, no layout, no controls); `testing` under-claimed by three, still
          // reading `[governed, sample-pack]` from before #432 ungated the pin — the row that prompted this
          // issue was simply the one someone happened to read.
          //
          // The catalog is a PACKAGE catalog, so its `profiles:` is held to package reach (pin ∩ reference),
          // NOT to the manifest's `materializes-when` the way R-DOC holds skillist-reference. Those two are
          // genuinely different sets and conflating them would force a lie: `fs-gg-ui-widgets` materializes on
          // [app, game] while FS.GG.UI.Controls is pinned and referenced on sample-pack as well. Asserting the
          // catalog against the manifest would make it under-report its own packages to match a skill gate —
          // trading a stale row for a wrong one.
          test "every capability's profiles are exactly the profiles its package reaches (#483)" {
              Expect.isGreaterThan
                  (List.length capabilityRows)
                  3
                  $"{capabilitiesRel} has a parseable runtime capability roster (if this fails the guard is vacuous)"

              for id, pkg, claimed in capabilityRows do
                  match claimed, capabilityReach pkg with
                  | Some claimed, Some reach ->
                      Expect.equal
                          claimed
                          reach
                          $"{capabilitiesRel} says the `{id}` capability is on {Set.toList claimed}, but {pkg} is pinned-and-referenced on {Set.toList reach}. The catalog is read as authoritative and asserted by nothing else — a row that over-claims sends an author to a profile where the package is absent, and one that under-claims hides a capability their scaffold really has (#483)."
                  | None, _ ->
                      failtestf
                          "%s declares the runtime capability `%s` (packageId %s) with no `profiles:` line, so R-CAT has nothing to hold it to and the row can say anything. Declare the profiles its package reaches."
                          capabilitiesRel
                          id
                          pkg
                  | _, None ->
                      failtestf
                          "%s declares the `%s` capability with packageId %s, but %s is not both pinned in template/base/Directory.Packages.props and referenced by Product.fsproj or Product.Tests.fsproj — the catalog names a package no generated product can compile against (#430)."
                          capabilitiesRel
                          id
                          pkg
                          pkg
          }

          // R-FRAG — `templateFragment:` was read as "where this capability's scaffold content lives" while
          // resolving to shipped content for exactly ONE row in eight (`samples`). The other seven name
          // directories .template.config/template.json does not source, so nothing in them is ever copied into a
          // generated product — and the READMEs in them read like shipped product guidance, which is what makes
          // them a trap rather than merely dead (#510).
          //
          // The field is settled as a SOURCE pointer (see the capabilities.yml header): it says where the
          // fragment's sources live in this repo, and never that they ship. `materializes:` carries the shipping
          // fact per row, and this rule holds it EXACTLY to template.json — the same duplicate-then-assert
          // discipline R-CAT applies to `profiles:`, and for the same reason. #483's lesson, restated by #510:
          // fixing the values without adding the assertion buys correct rows and zero protection.
          //
          // Asserted in BOTH directions deliberately. A row over-claiming sends a reader looking for shipped
          // content that does not exist; a fragment wired into template.json whose row still says `none` ships
          // content the catalog denies — and that direction is the one no human notices, because the scaffold
          // just quietly works.
          test "every capability's materializes: is exactly what template.json sources from its fragment (#510)" {
              Expect.isGreaterThan
                  (List.length capabilityFragments)
                  3
                  $"{capabilitiesRel} has a parseable capability roster (if this fails the guard is vacuous)"

              Expect.isNonEmpty
                  templateJsonFragmentSources
                  ".template.config/template.json sources at least one template/fragments/ path (if this fails the guard is vacuous — it would bless every row as `none`)"

              for id, fragment, materializes in capabilityFragments do
                  match fragment with
                  | None ->
                      failtestf
                          "%s declares the `%s` capability with no `templateFragment:` line — R-FRAG has nothing to hold it to."
                          capabilitiesRel
                          id
                  | Some fragment ->
                      let dir = repositoryPath fragment

                      Expect.isTrue
                          (Directory.Exists dir)
                          $"{capabilitiesRel} points the `{id}` capability's templateFragment at `{fragment}`, which does not exist. The pointer is the only thing telling a reader where the fragment's sources live."

                      // Everything template.json actually takes from THIS fragment directory. Both sides are
                      // already TrimEnd'd, and the root itself counts: a fragment sourced whole is spelled as
                      // the directory, not as something strictly beneath it.
                      let frag = fragment.TrimEnd '/'

                      let actual =
                          templateJsonFragmentSources
                          |> Set.filter (fun s -> s = frag || s.StartsWith(frag + "/"))

                      match materializes with
                      | None ->
                          failtestf
                              "%s declares the `%s` capability with no `materializes:` line, so nothing says whether its fragment ships. Declare the template.json sources it materializes, or `none` (#510)."
                              capabilitiesRel
                              id
                      | Some declared ->
                          Expect.equal
                              declared
                              actual
                              $"{capabilitiesRel} says the `{id}` capability materializes {Set.toList declared}, but .template.config/template.json sources {Set.toList actual} from `{fragment}`. template.json is the only thing `dotnet new` reads, so it is the authority — a row that over-claims sends a reader hunting for scaffold content that never ships, and one that under-claims hides content a generated product really receives (#510)."
          }

          // R-FRAG-ALL — the converse of R-FRAG, and without it the rule above is half a guard.
          //
          // R-FRAG walks the CATALOG's rows, so it can only see fragments a row already points at. Ten
          // `template/fragments/` sources exist in template.json and only two of them (`samples/`, `samples/skill/`)
          // belong to a capability row — so `mkdir template/fragments/foo`, wire it into template.json, add no row,
          // and R-FRAG stays green while the scaffold ships it. That is the #510 shape again, one level out: the
          // catalog is silent about content a generated product really receives.
          //
          // So every fragment source must be accounted for: claimed by a row's `materializes:`, or named in the
          // waiver below. The waiver is not a loophole — a stale entry is a RED (the discipline R-REACH's
          // exemption test already applies), so a fragment that stops shipping cannot rot here unnoticed.
          test "every fragment template.json sources is claimed by a capability row or an explicit waiver (#510)" {
              // Fragments that genuinely have NO capability row: product-OWNED source helpers the author edits
              // (they back no package, so there is no reach to catalog) and the per-profile swap checklists.
              // Explicit, so that an eleventh fragment source is a decision somebody makes rather than one nobody
              // notices.
              let nonCapabilityFragments =
                  set
                      [ "template/fragments/swap-checklist/game"
                        "template/fragments/swap-checklist/app"
                        "template/fragments/swap-checklist/governed"
                        "template/fragments/vec2/src"
                        "template/fragments/collision/src"
                        "template/fragments/visibility/src"
                        "template/fragments/grids/src"
                        "template/fragments/line-drawing/src" ]

              let claimed =
                  capabilityFragments
                  |> List.choose (fun (_, _, materializes) -> materializes)
                  |> List.fold Set.union Set.empty

              let unaccounted =
                  Set.difference templateJsonFragmentSources (Set.union claimed nonCapabilityFragments)

              Expect.isEmpty
                  unaccounted
                  $".template.config/template.json sources {Set.toList unaccounted} from template/fragments/, and NO capability row's `materializes:` claims it and no waiver names it. The scaffold ships it into a generated product while {capabilitiesRel} — the human-facing inventory of what a profile gets — says nothing about it (#510). Claim it on the owning row, or add it to nonCapabilityFragments with a reason."

              let staleWaivers = Set.difference nonCapabilityFragments templateJsonFragmentSources

              Expect.isEmpty
                  staleWaivers
                  $"nonCapabilityFragments waives {Set.toList staleWaivers}, but .template.config/template.json no longer sources it. A waiver for a fragment that does not ship is a lie that hides the next one: delete the entry."
          }

          // R-REACH — the heart of #430. Pinning is not enough: the skill must not reach a profile the
          // package does not. A `game`-gated package under an all-profile skill is the same silent
          // failure, one profile over.
          test "every product skill's profiles are a subset of the profiles pinning the packages it opens" {
              let props = File.ReadAllText propsPath

              for id, skillProfiles, opens in productSkills do
                  for pkg in opens do
                      match profilesDeclaring props pkg, refProfilesFor id pkg with
                      | _ when reachExemptions.ContainsKey id -> ()
                      | Some pinProfiles, Some refProfiles ->
                          let reachable = Set.intersect pinProfiles refProfiles
                          let orphaned = Set.difference skillProfiles reachable

                          Expect.isEmpty
                              orphaned
                              $"the {id} skill materializes on {Set.toList skillProfiles} and tells the author to `open {pkg}`, but {pkg} only reaches {Set.toList reachable} — on {Set.toList orphaned} the scaffold ships the skill with no package, so the author's first line fails to compile with nothing in the type system objecting (#430). Either gate the skill to the package's profiles, or pin the package on the skill's."
                      | _ ->
                          // Absence is R-PINNED/R-REF's failure to report, not this test's.
                          ()
          }

          // R-SKILL (#564) — a capability's `skill:` names the PRODUCT-skill the manifest supplies, and
          // nothing else.
          //
          // `skill:` is meant to answer "where is the guidance for consuming this capability?", and seven of
          // the eight rows answered it with a `template/product-skills/…` path — the SKILL.md a generated
          // product actually receives. `layout` answered with `src/Layout/skill/SKILL.md`, the FRAMEWORK
          // skill: guidance for working on `src/Layout/` IN THIS REPO. A reader following the catalog for
          // `layout` was sent to instructions for building the framework when they wanted instructions for
          // consuming it — the same "two answers, no signal which one ships" failure #510 found in
          // `templateFragment:`, one field over. Nothing asserted it, so it drifted, exactly as `profiles:`
          // had (#483) and `templateFragment:` had (#510).
          //
          // The MANIFEST is the side to hold the catalog to, not the reverse: it is what `dotnet new`
          // materializes, and capabilities.yml's own header already concedes it is the authority. So the rule
          // is `skill:`'s directory ∈ the manifest's `supplied-by` set — which makes the framework skill
          // unnameable here by construction, since the manifest supplies no such directory.
          //
          // No row is exempt. A `skill:` line that is MISSING fails too: a pointer nobody wrote is a pointer
          // nothing can hold, and "the row said nothing" and "the row said the right thing" must not share a
          // verdict (FS-GG/.github#266).
          test "R-SKILL — every capability's `skill:` is the product-skill the manifest supplies (#564)" {
              Expect.isNonEmpty capabilitySkills $"{capabilitiesRel} declares capability rows to check"
              Expect.isNonEmpty (Set.toList manifestSuppliedByDirs) "the skill manifest declares `supplied-by` directories"

              for id, skill in capabilitySkills do
                  match skill with
                  | None ->
                      failtestf
                          "%s: the `%s` capability declares no `skill:` — R-SKILL has nothing to hold it to, and a reader has nowhere to go."
                          capabilitiesRel
                          id

                  | Some path ->
                      let normalized = path.Replace('\\', '/')

                      let dir =
                          match normalized.LastIndexOf '/' with
                          | -1 -> ""
                          | i -> normalized.Substring(0, i)

                      Expect.isTrue
                          (manifestSuppliedByDirs.Contains dir)
                          $"{capabilitiesRel}: the `{id}` capability points `skill:` at `{normalized}`, whose directory `{dir}` is NOT one the skill manifest supplies. `skill:` names the PRODUCT-skill a generated product receives — the manifest's `supplied-by` is the authority on that, and it offers: {commaSorted manifestSuppliedByDirs}. Pointing at a framework skill (e.g. `src/<Pkg>/skill/`) sends a reader to guidance for building the framework when they wanted guidance for consuming it (#564)."

                      // ...and it has to actually be there. A pointer at a supplied directory that ships no
                      // SKILL.md is a dangling reference the manifest cannot catch on the catalog's behalf.
                      Expect.isTrue
                          (File.Exists(repositoryPath normalized))
                          $"{capabilitiesRel}: the `{id}` capability points `skill:` at `{normalized}`, which does not exist."
          }
        ]
