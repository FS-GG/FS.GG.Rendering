module Feature223SymbologyParityTests

// Feature 223 (US3 / FR-004 / SC-003 / SC-004) — the skill-parity harness must fail HONESTLY when a
// product-skill's `fs-gg-product-*` wrapper is missing, even when a bare same-named framework wrapper
// (`fs-gg-symbology`) is present. Before the fix, the bare name satisfied the requirement for ALL
// canonical kinds, so a missing product wrapper was masked (parity green over a real hole).
//
// GL-free and self-contained: each case builds a tiny on-disk repo tree shaped like the real default
// surfaces (`template/product-skills/<id>/SKILL.md` canonical; `.claude/skills/`, `.agents/skills/`
// wrappers; `src/<id>/skill/SKILL.md` package canonical) and drives the real public `SkillParity.runCheck`
// over it (FixtureMode = None ⇒ discoverDefaultSurfaces), then inspects the MissingWrapper findings.

open System.IO
open Expecto
open Rendering.Harness

let private createRoot () =
    let root = Path.Combine(Path.GetTempPath(), "feature223-parity-" + System.Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory root |> ignore
    root

let private deleteRoot root =
    if Directory.Exists root then
        Directory.Delete(root, true)

let private writeFile (path: string) (content: string) =
    match Path.GetDirectoryName path with
    | null | "" -> ()
    | dir -> Directory.CreateDirectory dir |> ignore
    File.WriteAllText(path, content)

/// A canonical product-skill at template/product-skills/<id>/SKILL.md (Path contains
/// "template/product-skills", so the harness classifies it as a product skill).
let private writeProductCanonical root (id: string) (description: string) =
    writeFile
        (Path.Combine(root, "template", "product-skills", id, "SKILL.md"))
        (sprintf "---\nname: %s\ndescription: %s\n---\n\n# %s\n\nCanonical product-skill content.\n" id description id)

/// A non-product canonical at src/<id>/skill/SKILL.md (package-canonical surface).
let private writePackageCanonical root (id: string) (description: string) =
    writeFile
        (Path.Combine(root, "src", id, "skill", "SKILL.md"))
        (sprintf "---\nname: %s\ndescription: %s\n---\n\n# %s\n\nCanonical package-skill content.\n" id description id)

/// A wrapper SKILL.md under `<surfaceDir>/<wrapperName>/` routing to a canonical (relative `route`).
let private writeWrapper root (surfaceDir: string) (wrapperName: string) (description: string) (route: string) =
    let dir = surfaceDir.Replace("/", string Path.DirectorySeparatorChar)
    writeFile
        (Path.Combine(root, dir, wrapperName, "SKILL.md"))
        (sprintf
            "---\nname: %s\ndescription: %s\n---\n\n# %s\n\nWrapper.\n\nBefore acting, read the canonical instructions in:\n\n`%s`\n"
            wrapperName description wrapperName route)

let private productRoute (id: string) = sprintf "../../../template/product-skills/%s/SKILL.md" id
let private packageRoute (id: string) = sprintf "../../../src/%s/skill/SKILL.md" id

let private runParity root =
    let req =
        { SkillParity.defaultRequest root with
            OutDir = Path.Combine(root, "out", "parity")
            ReportPath = Path.Combine(root, "docs", "reports", "skills-parity.md")
            SummaryJsonPath = Path.Combine(root, "readiness", "skill-parity-summary.json") }
    (SkillParity.runCheck req).Findings

let private missingWrapperFor (name: string) findings =
    findings
    |> List.filter (fun (f: SkillParity.ParityFinding) ->
        f.Category = SkillParity.MissingWrapper && f.SkillName = name)

[<Tests>]
let feature223SymbologyParityTests =
    testList
        "Feature223 symbology parity blind-spot"
        [
          // SC-003: a product-skill canonical whose product alias is ABSENT but whose bare framework
          // wrapper is PRESENT must still yield a MissingWrapper finding on both surfaces. (This is the
          // assertion that FAILS before the SkillParity.fs narrowing and PASSES after.)
          test "blind-spot closed: bare wrapper does NOT satisfy a product skill" {
              let root = createRoot ()
              try
                  let desc = "Author legible unit-symbology in a generated FS.GG.UI product."
                  writeProductCanonical root "fs-gg-symbology" desc
                  // bare framework wrapper present on both surfaces; product alias absent.
                  writeWrapper root ".agents/skills" "fs-gg-symbology" desc (productRoute "fs-gg-symbology")
                  writeWrapper root ".claude/skills" "fs-gg-symbology" desc (productRoute "fs-gg-symbology")

                  let missing = runParity root |> missingWrapperFor "fs-gg-symbology"
                  let surfaces = missing |> List.map (fun f -> f.SurfaceId) |> Set.ofList
                  Expect.equal
                      surfaces
                      (set [ "claude"; "codex-local" ])
                      "a product skill with only its bare wrapper present is MissingWrapper on both surfaces"
              finally
                  deleteRoot root
          }

          // The product alias DOES satisfy the requirement.
          test "product alias satisfies: fs-gg-product-symbology present ⇒ no finding" {
              let root = createRoot ()
              try
                  let desc = "Author legible unit-symbology in a generated FS.GG.UI product."
                  writeProductCanonical root "fs-gg-symbology" desc
                  writeWrapper root ".agents/skills" "fs-gg-product-symbology" desc (productRoute "fs-gg-symbology")
                  writeWrapper root ".claude/skills" "fs-gg-product-symbology" desc (productRoute "fs-gg-symbology")

                  let missing = runParity root |> missingWrapperFor "fs-gg-symbology"
                  Expect.isEmpty missing "product alias on both surfaces satisfies the product-skill wrapper requirement"
              finally
                  deleteRoot root
          }

          // SC-004 / FR-006: the six delivered product skills (alias present) stay green, and a
          // non-product canonical satisfied by its bare name keeps the package/ant self-exposure path
          // intact — no MissingWrapper regression from the narrowing.
          test "regression: delivered product alias + bare-named package canonical ⇒ no MissingWrapper" {
              let root = createRoot ()
              try
                  let sceneDesc = "Build pure scene descriptions in a generated FS.GG.UI product."
                  writeProductCanonical root "fs-gg-scene" sceneDesc
                  writeWrapper root ".agents/skills" "fs-gg-product-scene" sceneDesc (productRoute "fs-gg-scene")
                  writeWrapper root ".claude/skills" "fs-gg-product-scene" sceneDesc (productRoute "fs-gg-scene")

                  // a non-product (package) canonical satisfied by its bare same-name wrapper.
                  let pkgDesc = "A package-owned canonical skill."
                  writePackageCanonical root "fs-gg-foo" pkgDesc
                  writeWrapper root ".agents/skills" "fs-gg-foo" pkgDesc (packageRoute "fs-gg-foo")
                  writeWrapper root ".claude/skills" "fs-gg-foo" pkgDesc (packageRoute "fs-gg-foo")

                  let findings = runParity root
                  Expect.isEmpty (missingWrapperFor "fs-gg-scene" findings) "delivered product alias keeps the six green"
                  Expect.isEmpty (missingWrapperFor "fs-gg-foo" findings) "non-product canonical is still satisfied by its bare name"
              finally
                  deleteRoot root
          }
        ]
