module Feature1081TemplateCanonicalRootsTests

// Issue #1081 — the `template-canonical` surface must treat EVERY ADR-0011 agent-skill root under
// `template/` as a MIRROR, never as a canonical source.
//
// The bug this pins: the filter enumerated `.agents/skills/` and `.claude/skills/` and stopped there.
// It was not wrong-looking, because those were the only two roots `template/base/` had — so the list
// was complete *for the tree that existed*, which is not the same fact as being correct. #1081 added
// `template/base/.codex/`, and the mirror was immediately counted as a 30th canonical source:
// `Canonical sources` inflated, a duplicate `fs-gg-project` row appeared in API Symbol Coverage, and
// two guarded-theme references were double-counted.
//
// It stayed `passed` throughout, which is the point. A two-of-three list fails OPEN — a root nobody
// has created yet is a root nobody excludes — and the failure is silent until the roots DISAGREE, at
// which point a divergent mirror is reported against the mirror's own path and sends the reader to
// fix the copy rather than the canonical body it drifted from.
//
// So this test is written over the RULE (all three roots, each proven independently) rather than over
// the one root that happened to be added, because a test that only covers `.codex/` would have passed
// on the very code that shipped the defect.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

/// ADR-0011's three roots. Written out here on purpose: this list is the ORACLE, so it must not be
/// imported from the code under test — that would make the assertion tautological the next time the
/// production list is edited, which is exactly the edit this test exists to catch.
let private adr0011Roots = [ ".claude"; ".codex"; ".agents" ]

let private writeSkill (root: string) (relativeDir: string) (name: string) =
    let dir = Path.Combine(root, relativeDir.Replace('/', Path.DirectorySeparatorChar))
    Directory.CreateDirectory dir |> ignore

    File.WriteAllText(
        Path.Combine(dir, "SKILL.md"),
        sprintf "---\nname: %s\ndescription: synthetic fixture body for issue 1081\n---\n\nBody.\n" name
    )

/// The `template-canonical` surface, as `discoverDefaultSurfaces` defines it for a given root.
let private templateCanonicalSurface (root: string) =
    SkillParity.discoverDefaultSurfaces root
    |> List.find (fun surface -> surface.SurfaceId = "template-canonical")

let private inventoriedPaths (root: string) =
    let surface = templateCanonicalSurface root

    SkillParity.inventorySkills (Feature168SkillParityFixtures.repositoryRequest root) [ surface ]
    |> List.map (fun entry -> entry.Path.Replace('\\', '/'))

[<Tests>]
let templateCanonicalRootsTests =
    testList "Feature1081 template-canonical treats every ADR-0011 root as a mirror" [

        test "a skill body under any ADR-0011 root inside template/ is NOT a canonical source" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1081-roots"

            try
                // One product skill that IS canonical, so the fixture cannot pass by inventorying
                // nothing at all — the vacuity this whole item is about.
                writeSkill root "template/product-skills/fs-gg-canonical" "fs-gg-canonical"

                for agentRoot in adr0011Roots do
                    writeSkill root (sprintf "template/base/%s/skills/fs-gg-mirrored" agentRoot) "fs-gg-mirrored"

                let paths = inventoriedPaths root

                Expect.isTrue
                    (paths |> List.exists (fun p -> p.Contains "template/product-skills/fs-gg-canonical"))
                    "the genuine canonical body under template/product-skills/ IS inventoried — if this fails the assertions below prove nothing, because an empty inventory excludes everything"

                for agentRoot in adr0011Roots do
                    let segment = sprintf "/%s/skills/" agentRoot

                    Expect.isFalse
                        (paths |> List.exists (fun p -> p.Contains segment))
                        (sprintf
                            "no body under template/base/%s/skills/ is counted as a canonical source — it is a byte-identical MIRROR of the .agents canonical body, and counting it double-counts the skill's API symbols and guarded-theme references (#1081)"
                            agentRoot)
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // Each root gets its own failing question. A single test covering all three passes as soon as
        // any ONE of them is excluded, which is the shape of the defect: two of three looked fine.
        for agentRoot in adr0011Roots do
            test (sprintf "the %s root alone is excluded, proven without the other two present" agentRoot) {
                let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1081-single"

                try
                    writeSkill root "template/product-skills/fs-gg-canonical" "fs-gg-canonical"
                    writeSkill root (sprintf "template/base/%s/skills/fs-gg-mirrored" agentRoot) "fs-gg-mirrored"

                    let paths = inventoriedPaths root

                    Expect.isTrue
                        (paths |> List.exists (fun p -> p.Contains "template/product-skills/fs-gg-canonical"))
                        "the canonical body is inventoried (non-vacuity)"

                    Expect.isFalse
                        (paths |> List.exists (fun p -> p.Contains (sprintf "/%s/skills/" agentRoot)))
                        (sprintf
                            "template/base/%s/skills/ is excluded on its own — a filter that only listed the roots a tree HAPPENS to carry today is how #1081's defect shipped"
                            agentRoot)
                finally
                    Feature168SkillParityFixtures.deleteTempRoot root
            }

        // The live tree, so the unit fixture above cannot drift away from the real repository.
        test "the real template/base ADR-0011 roots contribute no canonical sources" {
            let root = RepositoryRoot.value
            let paths = inventoriedPaths root

            Expect.isGreaterThan
                (List.length paths)
                0
                "this repository's template/ tree yields at least one canonical source (non-vacuity)"

            for agentRoot in adr0011Roots do
                let offenders =
                    paths |> List.filter (fun p -> p.Contains(sprintf "/%s/skills/" agentRoot))

                Expect.isEmpty
                    offenders
                    (sprintf
                        "template/base/%s/skills/ contributes no canonical source in the live tree; offenders: %A"
                        agentRoot
                        offenders)
        }
    ]
