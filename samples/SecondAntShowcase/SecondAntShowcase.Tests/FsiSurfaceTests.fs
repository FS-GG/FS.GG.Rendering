module SecondAntShowcase.Tests.FsiSurfaceTests

open System.IO
open Expecto

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private featureDir =
    Path.Combine(repoRoot, "specs", "171-second-antshowcase-sample")

let private coreDir =
    Path.Combine(repoRoot, "samples", "SecondAntShowcase", "SecondAntShowcase.Core")

let private requiredModules =
    [ "Model"
      "DemoState"
      "AntTheme"
      "PageRegistry"
      "CoverageMap"
      "InteractionContracts"
      "ReviewFindings"
      "VisualConfig"
      "VisualReadinessWorkflow"
      "Evidence"
      "Shell"
      "Pages"
      "Templates" ]

[<Tests>]
let fsiSurfaceTests =
    testList "FsiSurface" [
        test "every planned public Core module has a curated .fsi file" {
            for moduleName in requiredModules do
                let path = Path.Combine(coreDir, moduleName + ".fsi")
                Expect.isTrue (File.Exists path) (sprintf "%s exists" path)
                Expect.stringContains (File.ReadAllText path) ("module SecondAntShowcase.Core." + moduleName) (sprintf "%s declares the expected module" moduleName)
        }

        test "FSI authoring transcript exists and exercises the public sample surface" {
            let readme = Path.Combine(featureDir, "readiness", "fsi", "README.md")
            let script = Path.Combine(featureDir, "readiness", "fsi", "second-ant-showcase-authoring.fsx")
            Expect.isTrue (File.Exists readme) "FSI README exists"
            Expect.isTrue (File.Exists script) "FSI authoring script exists"
            let text = File.ReadAllText script
            Expect.stringContains text "SecondAntShowcase.Core" "script loads Core"
            Expect.stringContains text "CoverageMap.check" "script exercises coverage"
            Expect.stringContains text "InteractionContracts.coverage" "script exercises interaction contracts"
            Expect.stringContains text "ReviewFindings.create" "script exercises review finding surface"
        }
    ]
