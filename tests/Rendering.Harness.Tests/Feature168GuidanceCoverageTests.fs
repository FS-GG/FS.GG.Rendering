module Feature168GuidanceCoverageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature168 Guidance" [
        test "required rule catalog exposes the seven retrospective themes" {
            let ids =
                SkillParity.defaultGuidanceRules ()
                |> List.map (fun rule -> rule.RuleId)
                |> Set.ofList

            Expect.equal ids.Count 7 "seven rules"
            Expect.contains ids "package-pin-drift" "package rule"
            Expect.contains ids "readiness-allowlisting" "readiness rule"
            Expect.contains ids "validation-output-isolation" "validation isolation rule"
            Expect.contains ids "visual-readiness" "visual rule"
            Expect.contains ids "responsiveness-diagnostics" "responsiveness rule"
            Expect.contains ids "post-merge-package-bump" "merge package rule"
            Expect.contains ids "evidence-honesty" "evidence honesty rule"
        }

        test "covered implementation guidance satisfies package, readiness, validation, and evidence rules" {
            let entry =
                Feature168SkillParityFixtures.entry
                    ".agents/skills/speckit-implement/SKILL.md"
                    "speckit-implement"
                    "Execute the implementation plan"
                    Feature168SkillParityFixtures.coveredBody

            let coverage = SkillParity.evaluateGuidanceCoverage (SkillParity.defaultGuidanceRules ()) [ entry ]

            for ruleId in [ "package-pin-drift"; "readiness-allowlisting"; "validation-output-isolation"; "evidence-honesty" ] do
                let item = coverage |> List.find (fun item -> item.RuleId = ruleId)
                Expect.equal item.Status SkillParity.Covered ruleId
        }

        test "visual and responsiveness evidence honesty cases are covered independently" {
            let entry =
                Feature168SkillParityFixtures.entry
                    "src/SkiaViewer/skill/SKILL.md"
                    "fs-gg-skiaviewer"
                    "viewer guidance"
                    Feature168SkillParityFixtures.coveredBody

            let coverage = SkillParity.evaluateGuidanceCoverage (SkillParity.defaultGuidanceRules ()) [ entry ]

            let visual = coverage |> List.find (fun item -> item.RuleId = "visual-readiness")
            let responsiveness = coverage |> List.find (fun item -> item.RuleId = "responsiveness-diagnostics")

            Expect.equal visual.Status SkillParity.Covered "visual evidence caveats"
            Expect.equal responsiveness.Status SkillParity.Covered "responsiveness diagnostics"
        }

        test "guidance gaps stay visible as missing or partial coverage" {
            let entry =
                Feature168SkillParityFixtures.entry
                    "src/Testing/skill/SKILL.md"
                    "fs-gg-testing"
                    "testing guidance"
                    "This intentionally omits the required package-feed, readiness, validation, visual, responsiveness, and caveat tokens."

            let coverage = SkillParity.evaluateGuidanceCoverage (SkillParity.defaultGuidanceRules ()) [ entry ]

            Expect.isTrue
                (coverage |> List.exists (fun item -> item.Status = SkillParity.Missing || item.Status = SkillParity.Partial))
                "gaps are visible"
        }
    ]
