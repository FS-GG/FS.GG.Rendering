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

        test "an API named only in a backslash-CONTINUED message does not exercise it" {
            // The repository's dominant message idiom: an Expecto message too long for one line, continued
            // with a trailing `\`. The literal spans lines, so a stripper that cannot span one reads its
            // prose as code — and `unexercised-api-symbol` then reports a seam EXERCISED that nothing calls
            // (#748). The single-line case has always stripped, which is exactly why this hid.
            let root = Feature168SkillParityFixtures.createTempRoot "issue748-continued-message"

            try
                let testsDir = Path.Combine(root, "tests")
                Directory.CreateDirectory testsDir |> ignore

                File.WriteAllText(
                    Path.Combine(testsDir, "SampleTests.fs"),
                    "module SampleTests\n"
                    + "let real = Widget.render \"x\"\n"
                    + "Expect.isTrue real\n"
                    + "    \"this message names Widget.hidden and must NOT count as \\\n"
                    + "     exercising it — it is prose, and Widget.alsoHidden is prose too\"\n"
                )

                let exercised = SkillParity.loadExercisedSymbols root |> Option.get

                Expect.isTrue (exercised |> Set.contains "Widget.render") "the real call still exercises the API"

                for mentioned in [ "Widget.hidden"; "Widget.alsoHidden" ] do
                    Expect.isFalse
                        (exercised |> Set.contains mentioned)
                        $"{mentioned} is named in a continued message, never called"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "a verbatim literal is read with ITS escape rules, not the regular literal's" {
            // `@"…"` is a different language: `\` is an ordinary character and `""` is the only escape, so
            // `@"C:\out\"` ends at its second quote. Reading it with the regular literal's rules breaks it
            // in BOTH directions, and this pins both — each line below fails if the verbatim literal is not
            // matched separately.
            //
            //   over-strip  `\"` looks like an escaped quote, so the match runs PAST the terminator and eats
            //               the code after it — reporting a genuinely-called API as unexercised.
            //   fail-open   a verbatim literal may span lines, and a pattern that cannot span one leaves its
            //               prose standing as code — the same defect as the continued message above (#748).
            let root = Feature168SkillParityFixtures.createTempRoot "issue748-verbatim"

            try
                let testsDir = Path.Combine(root, "tests")
                Directory.CreateDirectory testsDir |> ignore

                File.WriteAllText(
                    Path.Combine(testsDir, "SampleTests.fs"),
                    "module SampleTests\n"
                    + "let over = @\"C:\\out\\\" in Widget.render \"x\"\n"
                    + "let spanning = @\"a verbatim banner naming Widget.hidden\n"
                    + "   and still naming Widget.alsoHidden on a second line\"\n"
                )

                let exercised = SkillParity.loadExercisedSymbols root |> Option.get

                Expect.isTrue
                    (exercised |> Set.contains "Widget.render")
                    "the call AFTER a verbatim literal ending in `\\` is code — the literal ended at its quote"

                for mentioned in [ "Widget.hidden"; "Widget.alsoHidden" ] do
                    Expect.isFalse
                        (exercised |> Set.contains mentioned)
                        $"{mentioned} is prose inside a multi-line verbatim literal, never called"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "a symbol named only in a continued string inside a fence documents nothing" {
            // `codeOnly` strips a skill's fences too, so the same hole would have let a fence's prose
            // *document* an API it never demonstrates.
            let entry =
                Feature168SkillParityFixtures.entry
                    "src/Widget/skill/SKILL.md"
                    "fs-gg-widget"
                    "widget guidance"
                    ("```fsharp\n"
                     + "let value = Widget.render \"x\"\n"
                     + "printfn \"do not reach for Widget.missing here — it is \\\n"
                     + "         the wrong seam\"\n"
                     + "```\n")

            let symbols =
                SkillParity.evaluateApiSymbols surfaceMembers exercised [ entry ]
                |> List.map (fun item -> item.Symbol)

            Expect.equal symbols [ "Widget.render" ] "the continued literal's prose is not documentation"
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
