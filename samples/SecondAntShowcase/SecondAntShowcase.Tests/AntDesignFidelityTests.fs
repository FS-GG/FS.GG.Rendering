module SecondAntShowcase.Tests.AntDesignFidelityTests

open System.IO
open Expecto

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private samplePath file =
    Path.Combine(repoRoot, "samples", "SecondAntShowcase", file)

let private patternDocs =
    [ "display"
      "input"
      "selection"
      "layout"
      "navigation"
      "overlay"
      "feedback"
      "data"
      "chart"
      "graph"
      "custom" ]

[<Tests>]
let antDesignFidelityTests =
    testList "AntDesignFidelity" [
        test "sample docs cite local Ant guidance instead of raw upstream Ant URLs" {
            let docs = File.ReadAllText(samplePath "README.md") + "\n" + File.ReadAllText(samplePath "PROVENANCE.md")
            Expect.stringContains docs "docs/product/ant-design/reference/ant-llms-sources.md" "central source hub cited"
            Expect.stringContains docs "docs/product/ant-design/README.md" "pattern index cited"
            Expect.isFalse (docs.Contains("https://ant.design/")) "sample docs avoid raw upstream Ant URLs"
        }

        test "all local Ant pattern family docs exist for visual review traceability" {
            for name in patternDocs do
                let path = Path.Combine(repoRoot, "docs", "product", "ant-design", "patterns", name + ".md")
                Expect.isTrue (File.Exists path) (sprintf "pattern doc exists: %s" name)
        }

        test "catalog pages and templates are styled through the single semantic control set" {
            let provenance = File.ReadAllText(samplePath "PROVENANCE.md")
            Expect.stringContains provenance "one semantic control set" "no per-theme control fork statement exists"
            Expect.stringContains provenance "AntTheme.antLight" "light theme source is named"
            Expect.stringContains provenance "AntTheme.antDark" "dark theme source is named"
            Expect.isFalse (provenance.Contains("AntButton")) "no Ant-specific behavior fork is documented"
        }
    ]
