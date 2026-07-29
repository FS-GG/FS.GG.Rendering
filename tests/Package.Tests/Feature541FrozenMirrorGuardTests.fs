module Feature541FrozenMirrorGuardTests

// #541 / #1147 — Rendering no longer carries frozen copies of Game-owned skills. Keep the original
// safety property without preserving a second owner list: every body physically delivered from
// template/product-skills must be declared by Rendering's generated manifest, and every such manifest
// declaration must have a body. A copied foreign body therefore fails as an undeclared extra.

open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private normalizePath (path: string) =
    path.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')

let private manifestProductSkillBodies () =
    use document =
        repositoryPath "template/skill-manifest/skill-manifest.json"
        |> File.ReadAllText
        |> JsonDocument.Parse

    document.RootElement.GetProperty("skills").EnumerateArray()
    |> Seq.choose (fun entry ->
        let suppliedBy = entry.GetProperty("supplied-by").GetString()

        match suppliedBy with
        | null -> None
        | path ->
            let normalized = normalizePath path

            if normalized.StartsWith("template/product-skills/") then
                Some(normalized + "/SKILL.md")
            else
                None
        )
    |> Set.ofSeq

let private deliveredProductSkillBodies () =
    repositoryPath "template/product-skills"
    |> fun root -> Directory.EnumerateFiles(root, "SKILL.md", SearchOption.AllDirectories)
    |> Seq.map (fun path -> Path.GetRelativePath(repositoryRoot, path) |> normalizePath)
    |> Set.ofSeq

[<Tests>]
let issue541NoUndeclaredProductSkillBodies =
    testList
        "Issue541 product-skill ownership boundary"
        [
          test "every delivered product-skill body is declared by Rendering's manifest" {
              Expect.equal
                  (deliveredProductSkillBodies ())
                  (manifestProductSkillBodies ())
                  "template/product-skills must exactly match Rendering's generated producer manifest; a copied foreign body may not silently reappear"
          } ]
