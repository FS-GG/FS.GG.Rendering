module Feature168ApiSymbolTests

open System.IO
open Expecto
open Rendering.Harness

/// A two-member synthetic surface: `render` is exercised by the corpus below, `hidden` is not.
let private surfaceMembers = Map [ "Widget", Set [ "render"; "hidden" ] ]

let private exercised = Set [ "Widget.render" ]

let private documenting symbol =
    Feature168SkillParityFixtures.entry
        "src/Widget/skill/SKILL.md"
        "fs-gg-widget"
        "widget guidance"
        $"Prose that names Widget.missing outside a fence.\n\n```fsharp\nlet value = {symbol} \"x\"\n```\n"

let private statusOf symbol =
    SkillParity.evaluateApiSymbols surfaceMembers exercised [ documenting symbol ]
    |> List.map (fun item -> item.Symbol, item.Status)

[<Tests>]
let tests =
    testList "Feature168 ApiSymbols" [
        test "a documented symbol that the surface baseline carries and a test names is exercised" {
            Expect.equal (statusOf "Widget.render") [ "Widget.render", SkillParity.Exercised ] "resolved and exercised"
        }

        test "a documented symbol absent from the surface baseline is unresolved" {
            Expect.equal (statusOf "Widget.missing") [ "Widget.missing", SkillParity.Unresolved ] "the API does not exist"
        }

        test "a documented symbol that exists but no test names is unexercised" {
            Expect.equal (statusOf "Widget.hidden") [ "Widget.hidden", SkillParity.Unexercised ] "the seam may be dead"
        }

        test "only F# code fences are read, so prose naming an API is not a claim about it" {
            let symbols = SkillParity.evaluateApiSymbols surfaceMembers exercised [ documenting "Widget.render" ]

            Expect.equal (symbols |> List.map (fun item -> item.Symbol)) [ "Widget.render" ] "the prose `Widget.missing` is ignored"
        }

        test "a symbol named only in a fence comment or string literal documents nothing" {
            let entry =
                Feature168SkillParityFixtures.entry
                    "src/Widget/skill/SKILL.md"
                    "fs-gg-widget"
                    "widget guidance"
                    ("```fsharp\n"
                     + "// never call Widget.missing — see Program.fs\n"
                     + "(* Widget.alsoMissing was removed *)\n"
                     + "let value = Widget.render \"Widget.stillMissing\"\n"
                     + "```\n")

            let symbols =
                SkillParity.evaluateApiSymbols surfaceMembers exercised [ entry ]
                |> List.map (fun item -> item.Symbol)

            Expect.equal symbols [ "Widget.render" ] "comments and string literals in a fence are not documentation"
        }

        test "a test source only mentioning an API in a comment or string does not exercise it" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-exercised"

            try
                let testsDir = Path.Combine(root, "tests")
                Directory.CreateDirectory testsDir |> ignore

                File.WriteAllText(
                    Path.Combine(testsDir, "SampleTests.fs"),
                    "module SampleTests\n"
                    + "// Widget.commented is only named here\n"
                    + "(* Widget.blockCommented too *)\n"
                    + "let url = \"https://example.com/Widget.quoted\"\n"
                    + "let real = Widget.render \"x\" // trailing Widget.trailing\n"
                )

                let exercised = SkillParity.loadExercisedSymbols root |> Option.get

                Expect.isTrue (exercised |> Set.contains "Widget.render") "a real call exercises the API"

                for mentioned in [ "Widget.commented"; "Widget.blockCommented"; "Widget.quoted"; "Widget.trailing" ] do
                    Expect.isFalse (exercised |> Set.contains mentioned) $"{mentioned} is only mentioned, never called"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "an unknown option is a configuration error, not a silent full check" {
            // `--list-rules` died with the guidance layer. Ignoring it would run a full check and
            // rewrite the committed report under an operator who asked only to list something.
            Expect.equal (SkillParity.runCli [ "--list-rules" ]) 2 "removed flag is rejected"
            Expect.equal (SkillParity.runCli [ "--not-a-flag" ]) 2 "unknown flag is rejected"
        }

        test "a missing surface baseline or test corpus is reported, never silently passed" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-missing-inputs"

            try
                Expect.isNone (SkillParity.loadSurfaceMembers root) "no surface baseline"
                Expect.isNone (SkillParity.loadExercisedSymbols root) "no test corpus"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "symbols outside the surface baseline's modules are product-local and not judged" {
            let entry =
                Feature168SkillParityFixtures.entry
                    "template/product-skills/fs-gg-widget/SKILL.md"
                    "fs-gg-widget"
                    "product guidance"
                    "```fsharp\nlet view model = Stack.create [ Button.text model.Name ]\n```\n"

            Expect.isEmpty (SkillParity.evaluateApiSymbols surfaceMembers exercised [ entry ]) "no closed-world module matches"
        }

        test "wrapper entries are not a documentation surface" {
            let wrapper =
                { documenting "Widget.missing" with EntryKind = SkillParity.WrapperEntry }

            Expect.isEmpty (SkillParity.evaluateApiSymbols surfaceMembers exercised [ wrapper ]) "only canonical and command skills document APIs"
        }

        test "the repository's own surface baseline and test corpus both load" {
            let root = FS.GG.TestSupport.RepositoryRoot.value

            Expect.isSome (SkillParity.loadSurfaceMembers root) "member-granular surface baseline is present"
            Expect.isSome (SkillParity.loadExercisedSymbols root) "test corpus is present"
        }

        test "a repository skill documenting a real control API resolves it as exercised" {
            let root = FS.GG.TestSupport.RepositoryRoot.value
            let surfaceMembers = SkillParity.loadSurfaceMembers root |> Option.get
            let exercised = SkillParity.loadExercisedSymbols root |> Option.get

            let members = surfaceMembers |> Map.find "DataGrid"
            Expect.contains members "visibleRange" "DataGrid.visibleRange is public"
            Expect.isTrue (exercised |> Set.contains "DataGrid.visibleRange") "and a test names it"
        }
    ]
