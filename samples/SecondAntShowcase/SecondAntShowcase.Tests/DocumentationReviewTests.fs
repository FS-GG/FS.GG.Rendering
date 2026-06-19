module SecondAntShowcase.Tests.DocumentationReviewTests

open System.IO
open Expecto

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private samplePath file =
    Path.Combine(repoRoot, "samples", "SecondAntShowcase", file)

[<Tests>]
let documentationReviewTests =
    testList "DocumentationReview" [
        test "README explains purpose, AntShowcase relationship, commands, and visual review status" {
            let text = File.ReadAllText(samplePath "README.md")
            Expect.stringContains text "second Ant showcase" "purpose is identifiable"
            Expect.stringContains text "not a replacement" "relationship to existing AntShowcase is explicit"
            Expect.stringContains text "samples/AntShowcase" "existing sample is named"
            Expect.stringContains text "docs/product/ant-design/reference/ant-llms-sources.md" "local Ant source hub is cited"
            Expect.stringContains text "visual-readiness" "visual review command is documented"
            Expect.stringContains text "environment-limited" "limitations are disclosed"
            Expect.stringContains text "10-minute maintainer checklist" "documentation review checklist is present"
        }

        test "provenance cites local Ant guidance and package-consumer proof" {
            let text = File.ReadAllText(samplePath "PROVENANCE.md")
            Expect.stringContains text "docs/product/ant-design/reference/ant-llms-sources.md" "Ant hub cited"
            Expect.stringContains text "docs/product/ant-design/README.md" "Ant pattern index cited"
            Expect.stringContains text "~/.local/share/nuget-local" "local feed proof cited"
            Expect.stringContains text "FS.GG.UI.*" "package consumer surface cited"
        }
    ]
