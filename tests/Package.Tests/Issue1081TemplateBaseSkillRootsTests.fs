module Issue1081TemplateBaseSkillRootsTests

// Issue #1081 — `template/base/` carries ALL THREE ADR-0011 agent-skill roots, byte-identical.
//
// WHY AN IN-REPO TEST WHEN A GATE EXISTS. `.github/workflows/template-base-skill-union.yml` is the
// item's answer to "nothing audits this tree", and it is the authority — it runs FS-GG/.github's
// reusable assertion over the real root set. This test is not a second copy of it: it is the half
// that runs inside the ALWAYS-ON required gate (`gate.yml` -> Package.Tests) rather than in a
// path-filtered workflow of its own, and it fails in the same place a contributor is already
// looking. The defect that motivated #1081 survived three commits precisely because the only thing
// asserting the invariant was a `comment` field in `.template.config/template.json` claiming
// "copyOnly keeps the fs-gg-project body byte-identical to the `.agents/` canonical copy
// (skill-manifest digest)" — while `.claude/` sat at 4cfdc0f8… and canonical at c9fac83f….
//
// DIRECTION MATTERS. `.agents/` is canonical: ADR-0011 §3 makes it the provider source root,
// `template/lifecycle/materialize-skill-roots.fsx` mirrors OUT of it, and the shipped
// `template/skill-manifest/skill-manifest.json` declares its digest. So the assertions below are
// "each other root equals `.agents/`", never "they equal each other" — a test written the loose way
// stays green when all three drift together, away from the manifest.
//
// SCOPE. Skill roots only. `template/base/.claude/settings.json` and `.claude/hooks/` are
// deliberately NOT triplicated (Claude Code's own configuration schema; no other runtime reads it)
// — see specs/229-drop-claude-skills-mirror/decision-1081-template-base-three-roots.md.

open System.IO
open System.Security.Cryptography
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private native (path: string) =
    Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar))

/// ADR-0011's three, in the order the assertion tool declares them.
let private agentSkillRoots = [ ".claude"; ".codex"; ".agents" ]

let private canonicalRoot = ".agents"

let private skillsDir (root: string) = native (sprintf "template/base/%s/skills" root)

/// Every skill id (a directory holding a SKILL.md) under one root of the base tree.
let private skillIds (root: string) =
    let dir = skillsDir root

    if not (Directory.Exists dir) then
        Set.empty
    else
        Directory.EnumerateDirectories dir
        |> Seq.filter (fun d -> File.Exists(Path.Combine(d, "SKILL.md")))
        |> Seq.map Path.GetFileName
        |> Set.ofSeq

/// Every file under one root, keyed by its path RELATIVE to that root — so the same key names the
/// corresponding file in every other root. Covers `references/**`, not just SKILL.md.
let private relativeFiles (root: string) =
    let dir = skillsDir root

    if not (Directory.Exists dir) then
        Map.empty
    else
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
        |> Seq.map (fun f -> Path.GetRelativePath(dir, f).Replace('\\', '/'), f)
        |> Map.ofSeq

let private sha256Of (path: string) =
    use stream = File.OpenRead path
    use sha = SHA256.Create()
    sha.ComputeHash stream |> Array.map (sprintf "%02x") |> String.concat ""

[<Tests>]
let templateBaseSkillRootTests =
    testList "Issue1081 template/base carries the three-root byte-identical skill union" [

        // Guards the guard: every assertion below quantifies over the canonical root's skills, so an
        // emptied `.agents/skills/` would satisfy all of them vacuously. That is the FS-GG/.github#266
        // shape this whole item is about, so it gets its own failing question. (The reusable
        // assertion fails closed the same way — "no skills found under any root", exit 2.)
        test "the canonical root is non-empty, so the assertions below are not vacuous" {
            let canonical = skillIds canonicalRoot

            Expect.isGreaterThan
                (Set.count canonical)
                0
                "template/base/.agents/skills/ holds at least one skill; if this fails every other test in this list passes over an empty set and proves nothing"
        }

        test "every ADR-0011 root exists and holds exactly the canonical skill set" {
            let canonical = skillIds canonicalRoot

            for root in agentSkillRoots do
                Expect.isTrue
                    (Directory.Exists(skillsDir root))
                    (sprintf
                        "template/base/%s/skills must exist — a missing root is a [partitioned] tree, and an absent .codex/ is exactly what issue #1081 was filed about"
                        root)

                Expect.equal
                    (skillIds root)
                    canonical
                    (sprintf
                        "template/base/%s/skills holds exactly the canonical .agents/skills set — a skill added to one root and not the others is [partitioned]"
                        root)
        }

        test "every file under every root is byte-identical to its canonical .agents counterpart" {
            let canonicalFiles = relativeFiles canonicalRoot

            for root in agentSkillRoots do
                let rootFiles = relativeFiles root

                Expect.equal
                    (rootFiles |> Map.toList |> List.map fst |> List.sort)
                    (canonicalFiles |> Map.toList |> List.map fst |> List.sort)
                    (sprintf
                        "template/base/%s/skills has the same file set as the canonical .agents/skills (references/** included, not only SKILL.md)"
                        root)

                for KeyValue(rel, canonicalPath) in canonicalFiles do
                    match Map.tryFind rel rootFiles with
                    | None -> ()
                    | Some actualPath ->
                        Expect.equal
                            (sha256Of actualPath)
                            (sha256Of canonicalPath)
                            (sprintf
                                "template/base/%s/skills/%s is byte-identical to the canonical .agents copy — [divergent] otherwise, which is how the .claude/ body silently sat three commits stale"
                                root
                                rel)
        }

        // The cross-root checks above would all stay green if every root drifted together. This is
        // the one that anchors them to the digest the template actually SHIPS.
        test "the canonical body matches the digest the shipped skill-manifest declares" {
            let manifestPath = native "template/skill-manifest/skill-manifest.json"
            use doc = JsonDocument.Parse(File.ReadAllText manifestPath)

            let declared =
                doc.RootElement.GetProperty("skills").EnumerateArray()
                |> Seq.map (fun e -> e.GetProperty("id").GetString(), e.GetProperty("sha256").GetString())
                |> Map.ofSeq

            let mutable checkedAny = false

            for id in skillIds canonicalRoot do
                match Map.tryFind id declared with
                | None -> ()
                | Some digest ->
                    checkedAny <- true

                    Expect.equal
                        (sha256Of (Path.Combine(skillsDir canonicalRoot, id, "SKILL.md")))
                        digest
                        (sprintf
                            "template/base/.agents/skills/%s/SKILL.md matches the sha256 template/skill-manifest/skill-manifest.json declares for it ([drifted] otherwise)"
                            id)

            Expect.isTrue
                checkedAny
                "at least one base skill is declared in the shipped manifest, or this test compares nothing"
        }

        // The base .claude/ tree also carries Claude Code's own configuration. #1081 decided
        // deliberately that it is NOT triplicated, and this pins the decision so a later "make the
        // roots symmetric" sweep has to read the rationale before undoing it.
        test "Claude-specific configuration stays in .claude/ only and is not triplicated" {
            Expect.isTrue
                (File.Exists(native "template/base/.claude/settings.json"))
                "the base .claude/ workspace tree still carries Claude Code's settings.json"

            for root in [ ".codex"; ".agents" ] do
                Expect.isFalse
                    (File.Exists(native (sprintf "template/base/%s/settings.json" root)))
                    (sprintf
                        "template/base/%s/settings.json must NOT exist — settings.json is Claude Code's own schema (permissions.allow, hooks.UserPromptSubmit, $CLAUDE_PROJECT_DIR); no other runtime reads it, so a copy there is an unread file and a drift source (#1081)"
                        root)

                Expect.isFalse
                    (Directory.Exists(native (sprintf "template/base/%s/hooks" root)))
                    (sprintf
                        "template/base/%s/hooks/ must NOT exist — the hook script exists only because .claude/settings.json points at it (#1081)"
                        root)
        }
    ]
