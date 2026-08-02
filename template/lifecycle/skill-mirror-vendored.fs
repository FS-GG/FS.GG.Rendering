// Feature 231 — VENDORED copy of the FS.GG.Contracts skill-mirror algorithm (ADR-0014
// §Decision 2, standalone lane). The module body below transliterates `Fsgg.SkillMirror`
// (FS.GG.Contracts 4.0.0) with only the namespace/module header, this banner, and the
// `SkillScope`/`agentSkillRoots` vendored definitions (which live in `Fsgg.Schemas`
// upstream) changed. DO NOT "improve" it: the Package.Tests parity gate
// (Feature231SkillManifestTests) asserts behavioral equality with the published library,
// and any intentional change must land upstream first (roadmap §6: two lanes, one algorithm).
namespace FsGg.Vendored

open System
open System.Security.Cryptography
open System.Text

module SkillMirror =

    /// Vendored `Fsgg.Schemas.SkillScope`.
    type SkillScope =
        | Process
        | Product

    /// Vendored `Fsgg.Schemas.agentSkillRoots` (AGENT_SKILL_ROOTS, ADR-0014 §Decision 5, narrowed by
    /// ADR-0067 §5 / ADR-0065's amendment, executed 2026-07-28 by `.github#1636`). `.codex/skills` is
    /// retired: `.agents/skills` is Codex CLI's own second native discovery root, so the third root
    /// carried no runtime the other two did not, and only produced a duplicate model-visible catalog
    /// entry. Issue #1121: this vendored copy must track the PINNED FS.GG.Contracts value exactly —
    /// the G-PARITY gate (Feature231SkillManifestTests) asserts equality against
    /// `Fsgg.Schemas.agentSkillRoots` from the package this repo pins
    /// (`Directory.Packages.local.props`), so a change here must land in the SAME commit as a pin
    /// bump to a published FS.GG.Contracts whose own constant already moved (7.5.2+).
    let agentSkillRoots: string list = [ ".claude"; ".agents" ]

    let providerSourceRoot = ".agents"

    // Normalize CRLF -> LF before hashing so a byte-for-byte-logically-identical skill body
    // hashes the same regardless of a checkout's line endings. This matches
    // FS.GG.SDD.Artifacts SchemaVersion.sha256Text, so the 057/058 per-skill sha256 manifest
    // does not spuriously flag drift on a CRLF checkout of LF-authored content.
    let sha256 (body: string) : string =
        (if String.IsNullOrEmpty body then
             ""
         else
             body.Replace("\r\n", "\n"))
        |> Encoding.UTF8.GetBytes
        |> SHA256.HashData
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let skillPath (root: string) (id: string) : string = root + "/skills/" + id + "/SKILL.md"

    // The `<id>` of a `<root>/skills/<id>/SKILL.md` path. Matches only the canonical skill-file
    // shape (a `skills` segment, then `<id>`, then a trailing `SKILL.md`); anything else is `None`.
    let skillIdOfPath (path: string) : string option =
        match path.Replace('\\', '/').Split('/') |> Array.toList |> List.rev with
        | "SKILL.md" :: id :: "skills" :: _ when id <> "" -> Some id
        | _ -> None

    let mirrorTargetRoots (roots: string list) : string list =
        roots |> List.filter (fun r -> r <> providerSourceRoot)

    let retargetSkillPath (targetRoot: string) (sourcePath: string) : string =
        let normalized = sourcePath.Replace('\\', '/')
        let prefix = providerSourceRoot + "/skills/"

        if normalized.StartsWith(prefix, StringComparison.Ordinal) then
            targetRoot + "/skills/" + normalized.Substring(prefix.Length)
        else
            normalized

    type MirrorWrite = { Path: string; Body: string }

    let mirror (roots: string list) (skills: (string * string) list) : MirrorWrite list =
        [ for (id, body) in skills |> List.sortBy fst do
              for root in roots -> { Path = skillPath root id; Body = body } ]

    type ExpectedSkill =
        { Id: string
          Scope: SkillScope
          Sha256: string }

    type ActualCopy =
        { Root: string
          Id: string
          Body: string option }

    type SkillDrift =
        { Id: string
          Scope: SkillScope
          MissingRoots: string list
          Divergent: bool
          HashMismatchRoots: string list }

    let verify (roots: string list) (expected: ExpectedSkill list) (actual: ActualCopy list) : SkillDrift list =
        let bodyAt =
            actual
            |> List.choose (fun copy -> copy.Body |> Option.map (fun body -> (copy.Root, copy.Id), body))
            |> Map.ofList

        expected
        |> List.sortBy (fun skill -> skill.Id)
        |> List.choose (fun skill ->
            let perRoot =
                roots
                |> List.map (fun root -> root, Map.tryFind (root, skill.Id) bodyAt)

            let missingRoots =
                perRoot
                |> List.choose (fun (root, body) -> if Option.isNone body then Some root else None)

            let presentBodies = perRoot |> List.choose snd

            // "byte-identical across roots": every present copy equal to the others.
            let divergent =
                match presentBodies with
                | [] -> false
                | first :: rest -> rest |> List.exists (fun body -> body <> first)

            // "matches the manifest hash": only when a reference digest is known.
            let hashMismatchRoots =
                if String.IsNullOrWhiteSpace skill.Sha256 then
                    []
                else
                    perRoot
                    |> List.choose (fun (root, body) ->
                        match body with
                        | Some content when sha256 content <> skill.Sha256 -> Some root
                        | _ -> None)

            if List.isEmpty missingRoots && not divergent && List.isEmpty hashMismatchRoots then
                None
            else
                Some
                    { Id = skill.Id
                      Scope = skill.Scope
                      MissingRoots = missingRoots
                      Divergent = divergent
                      HashMismatchRoots = hashMismatchRoots })
