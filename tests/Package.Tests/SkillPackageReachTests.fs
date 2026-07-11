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
//
// Deliberately sound rather than exhaustive: an `open` is only held against a package when the namespace
// is EXACTLY a known package id, so a namespace that merely lives inside a differently-named package is
// never mis-attributed. That is enough to have caught #430 on the PR that introduced it.

open System.IO
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

/// KNOWN, FILED violations of R-REACH that predate this guard — named, never silent.
///
/// `fs-gg-project` (issue #431): the product-orientation umbrella ships on all five profiles, and its
/// `## Usage` example opens FS.GG.UI.SkiaViewer + calls `Viewer.runApp` — but `headless-scene` and
/// `governed` pin no viewer. It is the same defect this file exists for, one skill over, and #430 does
/// NOT fix it: those two profiles have no host entry point at all, so no single example is true on all
/// five, and markdown cannot be profile-gated (template.json's skillist-reference row records that
/// inline `#if` in markdown is unproven). Making the umbrella lane-neutral is a content decision, taken
/// in #431 rather than smuggled into #430's touch-set.
///
/// `fs-gg-testing` (issue #432) is FIXED and its exemption is therefore GONE — it is now held to R-REACH
/// like every other skill. #90 had widened the skill's materializes-when to all five profiles and never
/// widened the pin with it, so four of the five shipped a testing skill whose first `open` did not compile.
/// #432 finished #90: the pin is ungated, and the REFERENCE moved to `Product.Tests.fsproj` (see
/// `testScopedSkills` above) rather than being widened on `Product.fsproj` — the product source never used
/// the package, and spreading a test-only dependency across all five profiles would have traded one defect
/// for a worse one.
///
/// An exemption is a debt with a number on it, not a pass: R-PINNED/R-REF still hold for these skills
/// (the package must exist and be referenced SOMEWHERE), only the profile-subset check is waived. Adding
/// a row here requires a filed issue; the guard is worthless the moment it becomes a place to put things
/// that are merely inconvenient. The row below is a PRE-EXISTING defect this guard DISCOVERED — it found
/// it on the day it was written, which is the argument for it.
let private reachExemptions = Map.ofList [ "fs-gg-project", "#431" ]

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

[<Tests>]
let skillPackageReachTests =
    testList
        "Skill package reach (#430) — a skill may not out-reach the packages it says to open"
        [
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
        ]
