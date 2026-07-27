module Feature1092SurfaceRootTests

// Issue #1092 — a surface's DECLARED roots must be the roots the resolver reads.
//
// The defect these tests pin is a published lie, not a wrong answer. `filesForSurface` matched on
// `SurfaceId` and re-stated each surface's path inline, so a surface globbed a hard-coded directory
// while `RootPath` said whatever it said.
//
// MEASURED on `e2d860bc` before the fix, by holding each declared surface identical and pointing only
// its root at an empty directory:
//
//     codex-local          real=30   redirected-to-empty=0     reads its declaration
//     claude               real=30   redirected-to-empty=0     reads its declaration
//     package-canonical    real=10   redirected-to-empty=10    IGNORES its declaration
//     template-canonical   real=18   redirected-to-empty=18    IGNORES its declaration
//     ant-canonical        real=1    redirected-to-empty=1     IGNORES its declaration
//     spec-kit-command     real=32   redirected-to-empty=32    IGNORES its declaration
//
// So it was FOUR of the six, not the five #1092's title claims: `codex-local` and `claude` did read
// `RootPath` and hard-coded only their FILTER. That distinction is why this fix declares BOTH halves —
// `Roots` and `Selector` — rather than only the roots the title complained about.
//
// `RootPath` is what gets PUBLISHED: `renderMarkdown` emits it as the `Root` column of the
// `Supported Surfaces` table in the committed `docs/reports/skills-parity.md`, and `renderSummaryJson`
// emits it too. A reader uses `Root` to know where a surface looks. It was a comment that happened to
// agree with the resolver, and nothing kept it agreeing — change `package-canonical`'s `RootPath` from
// `src` to `src/skills` and the report would say `src/skills` while the gate still scanned all of
// `src`: passing, and lying in a committed artifact.
//
// Two consequences, both pinned below:
//
//   * `spec-kit-command`'s root was not a path at all. It read
//     `.agents/skills/speckit-* and .claude/skills/speckit-*` — English prose for a human, published in
//     the `Root` column as though it were the surface's root.
//   * `--surface <id>=<path>` was a partly-inert flag. Documented in
//     `specs/168-skill-parity-evidence/contracts/skill-parity-cli.md` as "Add or override a skill
//     surface", it could only genuinely override an id that had no branch. Measured while writing
//     #1086's red-case test: `--surface ant-canonical=docs/product/ant-design/skill/NOPE.md` was
//     accepted, printed in the report, and silently ignored — the resolver read its hard-coded path,
//     found the real file, and reported green.
//
// The tests are written over the RULE and quantified over `discoverDefaultSurfaces`, never over the
// five ids that happened to be wrong. A test that named those five would pass on the day a sixth
// hard-coded branch is added, which is the failure mode this whole item is about (acceptance
// criterion 4). Every green-direction assertion carries a non-vacuity control: "resolves to nothing"
// is trivially true of a surface list that failed to load at all.

open System
open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

let private inventory (surfaces: SkillParity.SkillSurface list) =
    SkillParity.inventorySkills (Feature168SkillParityFixtures.repositoryRequest repositoryRoot) surfaces

/// A request whose report/summary paths land in a throwaway directory. The real
/// `docs/reports/skills-parity.md` is a committed artifact with a CI gate on its diff.
let private requestWithOverrides (outRoot: string) overrides =
    { SkillParity.defaultRequest repositoryRoot with
        OutDir = Path.Combine(outRoot, "out")
        ReportPath = Path.Combine(outRoot, "out", "report.md")
        SummaryJsonPath = Path.Combine(outRoot, "out", "summary.json")
        SurfaceOverrides = overrides }

let private normalize (path: string) = path.Replace('\\', '/')

let private isBeneath (root: string) (path: string) =
    let root = (normalize root).TrimEnd('/')
    let path = normalize path
    path = root || path.StartsWith(root + "/", StringComparison.Ordinal)

[<Tests>]
let surfaceRootTests =
    testList "Feature1092 a surface's declared roots are the roots the resolver reads" [

        // ---------- Acceptance criterion 4: the rule, quantified over every declared surface ----------

        test "redirecting any declared surface's roots to an empty tree empties that surface" {
            // THE load-bearing test. It is the one experiment that separates "the resolver derives from
            // the declaration" from "the declaration happens to agree with a hard-coded path": hold the
            // surface identical and move only `Roots`. Against the code before this item it FAILS for
            // the four surfaces measured in the header, which keep resolving their hard-coded
            // directories and return the live tree's bodies out of a root that is provably empty.
            //
            // Quantified over `discoverDefaultSurfaces`, so a seventh surface added tomorrow with a new
            // hard-coded branch fails here without anyone remembering to extend this file.
            let empty = Feature168SkillParityFixtures.createTempRoot "fsgg-1092-empty-root"

            try
                for surface in declaredSurfaces () do
                    // Non-vacuity, per surface: this surface really does resolve to bodies in the live
                    // tree, so the emptiness below is a fact about the redirect and not about a surface
                    // that reads nothing either way.
                    Expect.isNonEmpty
                        (inventory [ surface ])
                        (sprintf "non-vacuity: surface '%s' resolves to at least one body in the live tree" surface.SurfaceId)

                    let redirected = inventory [ { surface with Roots = [ empty ] } ]

                    Expect.isEmpty
                        redirected
                        (sprintf
                            "surface '%s' must read its DECLARED roots — pointed at an empty tree it resolved to %d body/bodies, so it is reading a path it does not publish"
                            surface.SurfaceId
                            (List.length redirected))
            finally
                Feature168SkillParityFixtures.deleteTempRoot empty
        }

        // ---------- Acceptance criterion 1: the published `Root` column cannot disagree ----------

        test "every body a surface inventories lives beneath a root that surface publishes" {
            let surfaces = declaredSurfaces ()

            Expect.isGreaterThanOrEqual
                (List.length surfaces)
                6
                "non-vacuity: the repository declares at least the six surfaces this item enumerated"

            for surface in surfaces do
                let entries = inventory [ surface ]

                Expect.isNonEmpty
                    entries
                    (sprintf "non-vacuity: surface '%s' inventories at least one body" surface.SurfaceId)

                for entry in entries do
                    Expect.isTrue
                        (surface.Roots |> List.exists (fun root -> isBeneath root entry.Path))
                        (sprintf
                            "surface '%s' inventoried '%s', which is beneath none of the roots it publishes (%s) — the report's `Root` column would be asserting a path the gate does not read"
                            surface.SurfaceId
                            (normalize entry.Path)
                            (String.concat ", " surface.Roots))
        }

        // ---------- Acceptance criterion 2: a root is a checkable declaration, not prose ----------

        test "every declared root is a real repository path, with no prose and no glob metacharacters" {
            // `spec-kit-command` used to declare `.agents/skills/speckit-* and .claude/skills/speckit-*`.
            // The rule is expressed generically — no whitespace, no glob characters, and it must resolve
            // — rather than as "spec-kit-command must not say `and`", so the next surface tempted to put
            // a sentence in the field the report publishes fails here too.
            for surface in declaredSurfaces () do
                Expect.isNonEmpty
                    surface.Roots
                    (sprintf "surface '%s' must declare at least one root" surface.SurfaceId)

                for root in surface.Roots do
                    Expect.isFalse
                        (root |> Seq.exists Char.IsWhiteSpace)
                        (sprintf "surface '%s' declares root '%s', which contains whitespace — prose belongs in Notes" surface.SurfaceId root)

                    Expect.isFalse
                        (root.Contains '*' || root.Contains '?')
                        (sprintf
                            "surface '%s' declares root '%s', which contains a glob metacharacter — a root is a directory or a SKILL.md file, and narrowing is the surface's Selector"
                            surface.SurfaceId
                            root)

                    let absolute = Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar))

                    Expect.isTrue
                        (File.Exists absolute || Directory.Exists absolute)
                        (sprintf
                            "surface '%s' declares root '%s', which resolves to no file or directory in the repository"
                            surface.SurfaceId
                            root)
        }

        test "spec-kit-command declares both of the roots it reads" {
            // The multi-root surface is the one a single-string field could not express honestly, so it
            // gets a named assertion in addition to the generic rules above.
            let specKit =
                declaredSurfaces ()
                |> List.find (fun surface -> surface.SurfaceId = "spec-kit-command")

            let roots = specKit.Roots |> List.map normalize |> Set.ofList

            Expect.equal
                roots
                (Set.ofList [ ".agents/skills"; ".claude/skills" ])
                "spec-kit-command reads both agent-skill roots, so it must declare both"

            Expect.equal
                specKit.Selector
                SkillParity.CommandWrappers
                "and the `speckit-*` narrowing is a declared Selector, not a sentence in the Root column"
        }

        // ---------- Acceptance criterion 3: `--surface id=path` genuinely overrides ANY id ----------

        test "an operator override of a surface id the resolver has a rule for is genuinely applied" {
            // The issue's demonstration, verbatim, as a paired experiment. Before this item BOTH halves
            // reported one canonical source, because the override was read out of the report and out of
            // nothing else.
            let outRoot = Feature168SkillParityFixtures.createTempRoot "fsgg-1092-override"

            try
                let real = "docs/product/ant-design/skill/SKILL.md"
                let bogus = "docs/product/ant-design/skill/NOPE.md"

                let control = SkillParity.runCheck (requestWithOverrides outRoot [ "ant-canonical", real ])

                // Non-vacuity: the override MECHANISM reaches this surface at all. Without this, the
                // zero below is equally consistent with `--surface` having been dropped on the floor.
                Expect.equal
                    control.CanonicalSourceCount
                    1
                    "control: overriding ant-canonical at its real body inventories exactly that body"

                let overridden = SkillParity.runCheck (requestWithOverrides outRoot [ "ant-canonical", bogus ])

                Expect.equal
                    (overridden.SupportedSurfaces |> List.collect (fun surface -> surface.Roots))
                    [ bogus ]
                    "the override is what the report publishes as the surface's root"

                Expect.equal
                    overridden.CanonicalSourceCount
                    0
                    "and it is what the resolver READS: a surface pointed at a nonexistent body resolves to nothing, rather than quietly reading the real file"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        test "an override is honoured for every surface id the repository declares" {
            // Generalises the Ant case over the whole declared set, so no id keeps a private branch.
            let outRoot = Feature168SkillParityFixtures.createTempRoot "fsgg-1092-override-all"

            try
                let missing = "docs/product/ant-design/skill/NOPE.md"

                for surface in declaredSurfaces () do
                    let report = SkillParity.runCheck (requestWithOverrides outRoot [ surface.SurfaceId, missing ])

                    // Non-vacuity, per iteration: "zero bodies" is equally true of a run that produced
                    // no surfaces at all — the exact fail-open shape this whole item is about. Pin that
                    // the override really did become THIS surface first.
                    Expect.equal
                        (report.SupportedSurfaces |> List.map (fun s -> s.SurfaceId, s.Roots))
                        [ surface.SurfaceId, [ missing ] ]
                        (sprintf "non-vacuity: the run for '%s' is a run over the overridden surface" surface.SurfaceId)

                    Expect.equal
                        (report.CanonicalSourceCount + report.WrapperCount)
                        0
                        (sprintf
                            "overriding surface '%s' onto a nonexistent path must empty it; it inventoried %d canonical and %d wrapper bodies, so the override was ignored"
                            surface.SurfaceId
                            report.CanonicalSourceCount
                            report.WrapperCount)
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }
    ]
