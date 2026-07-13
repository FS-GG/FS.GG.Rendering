module MarkdownFencesTests

open Expecto
open FS.GG.TestSupport

// THE INSTRUMENT, NOT THE CORPUS (#669, continuing #664).
//
// `MarkdownFences` is now the single reader behind three gates — S-DOC (`SurfaceDocCoverageTests`), and
// `skillFenceSymbols` / `skillProseSymbols` (`TemplateConsumesPinnedApiTests`). A gate is only ever as good as
// the extractor underneath it, and these tests are the only thing that can hold this one: the corpus cannot
// witness most of what is asserted here. All 61 shipped fences say `fsharp`, none is indented four spaces, none
// uses a tilde, and none nests. So every one of these holes is LATENT — the gates stay green with the bugs in
// place, and a fix pinned only by the corpus is a fix nothing holds.
//
// The cases below are the three readers' historical DISAGREEMENTS, each pinned as an agreement:
//
//   | question                    | S-DOC (old)            | skillFenceSymbols (old)  | skillProseSymbols (old) |
//   | --------------------------- | ---------------------- | ------------------------ | ----------------------- |
//   | is `~~~` a fence?           | yes                    | NO                       | NO                      |
//   | does ``` close a ````?      | no (run length)        | YES (toggle)             | YES (toggle)            |
//   | is ```fsx F#?               | yes                    | NO                       | n/a (any fence)         |
//   | is ```fsharpx F#?           | no                     | YES                      | n/a (any fence)         |
//
// Every row was a way for one block to be *documentation* to one gate and *not code* to another.

let private scan = MarkdownFences.scan

/// The F# a document SHOWS, as the fenced-code consumers see it.
let private fsharp (markdown: string) =
    scan markdown
    |> MarkdownFences.fsharpLines
    |> List.map (fun l -> l.Text)
    |> String.concat "\n"

/// The prose a document WRITES, as the prose consumer sees it.
let private prose (markdown: string) =
    scan markdown
    |> MarkdownFences.proseLines
    |> List.map (fun l -> l.Text)
    |> String.concat "\n"

[<Tests>]
let markdownFenceTests =
    testList "Markdown fence reader (#669)" [

        test "a tilde fence is a fence" {
            // Row 1. S-DOC read `~~~`; both of TemplateConsumesPinnedApi's readers were blind to it — so a
            // `~~~fsharp` block's code was invisible to the pinned-API rule AND its contents were judged as
            // PROSE, where `Module.member` is read with prose's guards rather than F#'s. One block, three
            // readings, no gate wrong on its own terms.
            let markdown = "~~~fsharp\nlet m = Resolution.push model shot\n~~~\n"

            Expect.stringContains (fsharp markdown) "Resolution.push" "a ~~~fsharp block is F# code"
            Expect.isFalse ((prose markdown).Contains "Resolution.push") "...and is therefore NOT prose"
        }

        test "a fence closes only on its own character" {
            // A `~~~` cannot close a ``` block, and vice versa. The old toggles could not express this at all.
            let markdown = "```fsharp\nlet m = Resolution.push model shot\n~~~\nstill inside\n```\n"

            let code = fsharp markdown
            Expect.stringContains code "Resolution.push" "the block's code is code"
            Expect.stringContains code "still inside" "a ~~~ line does not close a ``` block"
            Expect.isFalse (scan markdown).UnclosedFence "...and the real ``` closer still closes it"
        }

        test "a fence closes only on a run at least as long as the one that opened it" {
            // Row 2, and the sharp end of it: a ``` INSIDE a ```` block is content, not a closer. Under the old
            // toggle the inner ``` closed the block, the following line re-opened one, and the reader's idea of
            // what was code inverted for the rest of the document.
            let markdown = "````fsharp\nlet m = Resolution.push model shot\n```\nlet n = Resolution.pop model\n````\n"

            let code = fsharp markdown
            Expect.stringContains code "Resolution.push" "the outer block opened"
            Expect.stringContains code "Resolution.pop" "the inner ``` is CONTENT — it cannot close a ```` block"
            Expect.stringContains code "```" "...and the inner fence line itself is part of the code it shows"
            Expect.isFalse (scan markdown).UnclosedFence "the ```` closer closes it"
        }

        test "a longer run DOES close a shorter one" {
            // The other half of the pairing rule, and the reason it is `StartsWith` and not equality.
            let markdown = "```fsharp\nlet m = Resolution.push model shot\n`````\n"

            Expect.isFalse (scan markdown).UnclosedFence "a ````` closes a ``` — at least as many, same char"
        }

        test "every spelling of F# is F#, and only F# is" {
            // Rows 3 and 4. A tag wrongly OMITTED costs a false accusation against a correct doc (its citations
            // vanish, and the author is sent to the ledger to excuse a surface they DID document); a tag wrongly
            // INCLUDED costs a homonym. `skillFenceSymbols`' old `StartsWith "fsharp"` managed BOTH: it missed
            // `fs`/`fsx`/`fsi`/`f#`, and it opened on `fsharpx`.
            for tag in [ "fsharp"; "fs"; "f#"; "fsx"; "fsi"; "FSharp"; "FSX" ] do
                let markdown = $"```{tag}\nlet m = Resolution.push model shot\n```\n"

                Expect.stringContains (fsharp markdown) "Resolution.push" $"a ```{tag} block is F#"
                Expect.equal (scan markdown).UntaggedFences 0 $"a ```{tag} block is tagged, not bare"

            for tag in [ "bash"; "json"; "text"; "console"; "fsharpx"; "fsharpy" ] do
                let markdown = $"```{tag}\ngit push origin main\n```\n"

                Expect.isFalse ((fsharp markdown).Contains "push") $"a ```{tag} block is NOT F#"
                Expect.isFalse ((prose markdown).Contains "push") $"...and a ```{tag} block is NOT prose either"
        }

        test "a fence indented inside a list item still opens a block" {
            // #664's hole 1, held at the scanner now that all three readers share it. CommonMark's `^ {0,3}`
            // allowance is a TOP-LEVEL rule; a fence nested in a list item is legitimately indented past it, and
            // a reader bounded by it never opens the block — so every line of the code lands in the prose buffer,
            // where it documents nothing, and the surface it cites is reported undeclared. That is the gate
            // inventing a violation against a correct doc.
            let markdown =
                "- To drive the program headlessly:\n\n      ```fsharp\n      let p = Program.program init update view subs\n      ```\n"

            Expect.stringContains (fsharp markdown) "Program.program" "a list-indented fence opens a block"
            Expect.isFalse ((prose markdown).Contains "Program.program") "...so its code is not prose"
            Expect.stringContains (prose markdown) "headlessly" "...and the prose around it is still prose"
        }

        test "the fence delimiter itself is neither code nor prose" {
            // It is markdown punctuation. Handing it to the F# reader invents call sites out of an info string;
            // handing it to the prose reader does the same. Both readers used to drop it by hand.
            let markdown = "```fsharp\nlet x = 1\n```\n"

            Expect.isFalse ((fsharp markdown).Contains "```") "the delimiter is not code"
            Expect.isFalse ((prose markdown).Contains "```") "the delimiter is not prose"
        }

        test "code and prose partition the document, and never overlap" {
            // The invariant the three readers depend on and none of them could state: every line is code, or
            // prose, or a delimiter — never two of those. `skillFenceSymbols` reads the first, `skillProseSymbols`
            // the second, and S-DOC both; if they could overlap, a symbol could be judged twice under two
            // different sets of rules.
            let markdown =
                "Call `respondsProofOf` to tell renders from responds.\n\n```fsharp\nlet p = Testing.respondsProofOf frame\n```\n\n```bash\ngit push origin main\n```\n\nAnd `Resolution.push` is a real surface.\n"

            let scanned = scan markdown
            let code = MarkdownFences.fsharpLines scanned |> List.map (fun l -> l.Number) |> Set.ofList
            let text = MarkdownFences.proseLines scanned |> List.map (fun l -> l.Number) |> Set.ofList

            Expect.isEmpty (Set.intersect code text) "no line is both code and prose"
            Expect.isNonEmpty code "the F# block is code"
            Expect.isNonEmpty text "the sentences are prose"

            // The bash block is neither — it cannot cite an F# API (#664), and it must not be read as English
            // either, or `git push origin main` becomes a mention of `Resolution.push`.
            Expect.isFalse ((fsharp markdown).Contains "git push") "a bash block is not F#"
            Expect.isFalse ((prose markdown).Contains "git push") "a bash block is not prose"
        }

        test "line numbers index the original document" {
            // `skillFenceSymbols` reports `Doc:Line` so a failure is clickable, and it indexes a line-count-
            // preserving F# erasure of the same text by these numbers. Off-by-one here is a wrong file:line in
            // every failure message the rule ever prints, and a misread line in the rule itself.
            let markdown = "one\n```fsharp\nlet x = 1\n```\nfive\n"
            let scanned = scan markdown

            Expect.equal (scanned.Lines |> List.map (fun l -> l.Number)) [ 1..6 ] "1-based, contiguous, no gaps"

            let code = MarkdownFences.fsharpLines scanned
            Expect.equal (code |> List.map (fun l -> l.Number)) [ 3 ] "only the block's content line is code"
            Expect.equal (code |> List.map (fun l -> l.Text)) [ "let x = 1" ] "...and it is the right line"
        }

        test "an unclosed fence is reported, at any indent and either character" {
            // The one failure that makes the documented count go UP rather than down, so no coverage floor can
            // catch it: the rest of the file is swallowed into a block. S-DOC reds on it; the pinned-API rule
            // reds on it (its prose reader would otherwise skip every line below the stray fence and report
            // green having read nothing — the fails-open shape .github#266).
            Expect.isFalse (scan "```fsharp\nlet x = 1\n```\n").UnclosedFence "a closed fence is closed"
            Expect.isTrue (scan "- like so:\n\n      ```fsharp\n      let x = 1\n").UnclosedFence "a list-indented fence that never closes is not"
            Expect.isTrue (scan "~~~fsharp\nlet x = 1\n").UnclosedFence "...and a tilde fence is held to the same rule"
            Expect.isTrue (scan "```fsharp\nlet x = 1\n~~~\n").UnclosedFence "a ~~~ does not close a ``` — so this file is still open"
        }

        test "an untagged fence is counted rather than guessed at" {
            // The price of the F#-only corpus (#664). An untagged block is ambiguous — credit it and the homonym
            // is back; drop it silently and a genuine F# example stops documenting its surfaces. So: drop it (the
            // safe direction) AND count it, so the drop can never be silent.
            let scanned = scan "```\nlet m = Resolution.push model shot\n```\n"

            Expect.equal scanned.UntaggedFences 1 "the bare fence is counted"
            Expect.isFalse ((fsharp "```\nlet m = Resolution.push model shot\n```\n").Contains "push")
                "an untagged block is not credited — it might not be F#"

            Expect.equal (scan "```fsharp\nlet x = 1\n```\n").UntaggedFences 0 "a tagged one is not counted"
        }

        test "the reader is not vacuous" {
            // A scanner that returns nothing makes every gate above it pass by checking nothing — which is the
            // failure (.github#266) this whole family exists to refuse.
            let scanned = scan "text\n```fsharp\nlet x = 1\n```\n"

            Expect.isNonEmpty scanned.Lines "the scan sees lines"
            Expect.isNonEmpty (MarkdownFences.fsharpLines scanned) "...and finds F# in an fsharp block"
            Expect.isNonEmpty (MarkdownFences.proseLines scanned) "...and prose outside it"
        }
    ]
