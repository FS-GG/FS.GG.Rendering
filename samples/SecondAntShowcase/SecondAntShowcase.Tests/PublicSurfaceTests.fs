module SecondAntShowcase.Tests.PublicSurfaceTests

open System.IO
open System.Security.Cryptography
open System.Text
open Expecto

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private coreDir =
    Path.Combine(repoRoot, "samples", "SecondAntShowcase", "SecondAntShowcase.Core")

let private baselinePath =
    Path.Combine(repoRoot, "specs", "171-second-antshowcase-sample", "readiness", "surface-baselines", "SecondAntShowcase.Core.txt")

let private moduleOrder =
    [ "Model"
      "DemoState"
      "AntTheme"
      "PageRegistry"
      "CoverageMap"
      "InteractionContracts"
      "ReviewFindings"
      "VisualConfig"
      "VisualReadinessWorkflow"
      "ResponsivenessWorkflow"
      "Evidence"
      "Shell"
      "Pages"
      "Templates" ]

let private sha256 (text: string) =
    let bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n").TrimEnd())
    let hash = SHA256.HashData bytes
    hash |> Array.map (fun b -> b.ToString("x2")) |> String.concat ""

let private currentSurface () =
    moduleOrder
    |> List.map (fun moduleName ->
        let path = Path.Combine(coreDir, moduleName + ".fsi")
        sprintf "%s %s" moduleName (sha256 (File.ReadAllText path)))
    |> String.concat "\n"
    |> fun text -> text + "\n"

[<Tests>]
let publicSurfaceTests =
    testList "PublicSurface" [
        test "SecondAntShowcase.Core public .fsi surface matches the reviewed baseline" {
            Expect.isTrue (File.Exists baselinePath) "surface baseline exists"
            let expected = File.ReadAllText(baselinePath).Replace("\r\n", "\n").TrimEnd()
            let actual = currentSurface().Replace("\r\n", "\n").TrimEnd()
            Expect.equal actual expected "public Core surface drift requires reviewed baseline update"
        }
    ]
