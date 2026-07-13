// #706 — the template's `//#if` nesting balance gate.
//
// `template/base/**` is a `dotnet new` template. Its per-profile variation is expressed as
// COMMENT-WRAPPED directives — `//#if` / `//#elif` / `//#else` / `//#endif` in F# and shell, and
// `<!--#if -->` / `<!--#endif -->` in XML — which the template engine strips at scaffold time. The
// wrapping is what lets the template itself still compile: to F#, `//#if (profile == "game")` is a
// comment; to `dotnet new`, it is a conditional region.
//
// That property is also the hazard. An UNBALANCED directive nest is invisible to every compiler in
// this repo, because the compiler sees comments. Nothing here parses them.
//
// WHAT THAT COST (#680). A stray `//#endif` in `template/base/tests/Product.Tests/GovernanceTests.fs`
// closed the OUTER `//#if (profile == "governed" || profile == "headless-scene")` early, so the whole
// tail of the `#else` branch spilled out of its guard and the `governed` and `headless-scene` profiles
// HAD NEVER COMPILED. It arrived with #436 and rode green `main` for weeks.
//
// WHY THIS IS NOT #680'S LANE. `generated-product-gate` (#680) scaffolds all five profiles and compiles
// them, and it CANNOT be the answer here: it restores FSharp.Core / Expecto / FS.GG.Game.* / FS.GG.Audio.*
// from nuget.org, so its subject includes the feed. A feed outage is indistinguishable from a real break,
// which is why it is NOT-REQUIRED and must stay that way (cadence-map §4d/§5). A red advisory lane is
// evidence a reviewer can merge past, and #679 is the standing proof that this org does exactly that.
//
// This check is PURELY STRUCTURAL: no feed, no SDK, no restore, no network. So it has an elevation path
// a compile gate can never have — it runs in the REQUIRED tier, and a merge cannot walk past it.
//
// It also catches two things a compile gate structurally cannot:
//   * an imbalance that happens to emit VALID BUT WRONG F# compiles green — the right code for the wrong
//     profile, which no `dotnet build` of the scaffolded output will ever notice;
//   * imbalance in files NOTHING COMPILES — `build.fsx`, the `.md` docs, the `.sh`/`.cmd` scripts. They
//     carry no conditionals today, but they are emitted files and may; a balance check sees all of them.
//
// WHAT COUNTS AS A DIRECTIVE, AND WHY THE ANCHOR IS TIGHT. A directive is a line whose FIRST non-space
// content is `//#` or `<!--#` followed by the keyword. Prose that merely mentions one is not a directive,
// and the distinction is load-bearing rather than pedantic — `template/base/src/Product/Product.fsproj:44`
// reads "behind its own `#if`, rather than pretending one cue map fits both" inside an XML comment, and
// `template/product-skills/**` is full of markdown anchors like `(#if-you-own-the-backend)`. A loose
// `#if` match would red the repo on its own documentation. Matching the PREFIX, not the keyword, is what
// keeps a comment about a directive from being read as one.
//
// Exit codes (fails CLOSED, never green-by-absence):
//   0  balanced
//   1  imbalance — a named, located defect (file:line), expected-vs-actual
//   2  guard error — the root is unreadable, a file cannot be read, or ZERO directives were found.
//      The guard did not run; that is not a pass (FS-GG/.github#266). "No conditionals found" and
//      "conditionals are balanced" MUST NOT share an exit code — a `template/base` that has lost its
//      directives (a bad merge, a moved tree, a mis-pointed --root) is a guard with no subject, and
//      reporting BALANCED there is the precise fails-open lie this script exists to refuse.
//
// Usage:
//   dotnet fsi scripts/validate-template-conditionals.fsx                 # scans template/base
//   dotnet fsi scripts/validate-template-conditionals.fsx --root <dir>    # scans <dir> (the tests do this)

open System
open System.IO
open System.Text.RegularExpressions

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let repo (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

/// Raised for an unreadable input / an empty subject ⇒ exit 2 (fail closed).
exception GuardError of string

/// A named, located imbalance ⇒ exit 1.
type Failure =
    { Rule: string
      Location: string
      Expected: string
      Actual: string
      Fix: string }

// ---- the directive grammar --------------------------------------------------------------------
type Directive =
    | If
    | Elif
    | Else
    | Endif

/// The FIRST non-space content on the line must be the comment prefix immediately followed by `#` and
/// the keyword. `endif` precedes `elif`/`else`/`if` in the alternation so the longest keyword wins;
/// `\b` then stops `#if` from matching inside `#ifdef`.
///
/// Both prefixes are the template engine's own: `//#` for the C-comment family (F#, `.fsx`, `.sh`,
/// `.cmd`) and `<!--#` for the XML family (`.fsproj`, `.props`). See the header for why this anchors on
/// the prefix rather than on the keyword.
let private directiveRegex = Regex(@"^\s*(?://|<!--)#(?<kw>endif|elif|else|if)\b", RegexOptions.Compiled)

let private parseDirective (line: string) : Directive option =
    let m = directiveRegex.Match line
    if not m.Success then
        None
    else
        match m.Groups.["kw"].Value with
        | "if" -> Some If
        | "elif" -> Some Elif
        | "else" -> Some Else
        | "endif" -> Some Endif
        | kw -> raise (GuardError(sprintf "directive regex matched `%s`, which parseDirective does not handle — the two have drifted apart" kw))

// ---- the scan ---------------------------------------------------------------------------------
/// Trees that are build output rather than template payload. A scaffolded product's `obj/` can contain
/// generated F# that is none of this guard's business.
let private skipDirs = set [ "obj"; "bin"; ".git"; ".vs"; "node_modules" ]

/// Binary payload a text scan has no business reading. The template is text today; this keeps a future
/// icon or font from being decoded as source and blowing up the guard.
let private skipExtensions =
    set [ ".png"; ".jpg"; ".jpeg"; ".gif"; ".ico"; ".dll"; ".exe"; ".pdf"; ".zip"; ".woff"; ".woff2"; ".ttf" ]

let rec private filesUnder (dir: string) : string seq =
    seq {
        for f in Directory.EnumerateFiles dir do
            if not (skipExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())) then
                yield f

        for d in Directory.EnumerateDirectories dir do
            if not (skipDirs.Contains(Path.GetFileName d)) then
                yield! filesUnder d
    }

/// One open `//#if` region: where it was opened, and whether its `#else` has already been seen.
type private Region =
    { Line: int
      mutable SawElse: bool }

/// Walk one file's directive nesting. `rel` is the repo-relative path used in every message.
///
/// Nesting is PER-FILE by construction: a region cannot span files, so the stack starts empty and any
/// residue at EOF is an unclosed `#if`.
let private scanFile (rel: string) (lines: string[]) : Failure list * int =
    let stack = Collections.Generic.Stack<Region>()
    let failures = ResizeArray<Failure>()
    let mutable directives = 0

    let at line = sprintf "%s:%d" rel line

    lines
    |> Array.iteri (fun idx line ->
        let lineNo = idx + 1

        match parseDirective line with
        | None -> ()
        | Some d ->
            directives <- directives + 1

            match d with
            | If -> stack.Push { Line = lineNo; SawElse = false }

            | Endif ->
                if stack.Count = 0 then
                    failures.Add
                        { Rule = "orphan-endif"
                          Location = at lineNo
                          Expected = "an `#endif` closes an open `#if`"
                          Actual = "`#endif` with no open `#if`"
                          Fix =
                            "delete it, or add the `#if` it was meant to close. A STRAY `#endif` is the #680 defect exactly: it closes an OUTER region early, and the rest of that region's body silently escapes its guard — valid F#, wrong profile, green build." }
                else
                    stack.Pop() |> ignore

            | Else ->
                if stack.Count = 0 then
                    failures.Add
                        { Rule = "orphan-else"
                          Location = at lineNo
                          Expected = "an `#else` sits inside an open `#if`"
                          Actual = "`#else` with no open `#if`"
                          Fix = "an `#else` outside a region guards nothing — its body is emitted for EVERY profile" }
                else
                    let region = stack.Peek()

                    if region.SawElse then
                        failures.Add
                            { Rule = "duplicate-else"
                              Location = at lineNo
                              Expected = sprintf "at most one `#else` per region (this one opened at %s)" (at region.Line)
                              Actual = "a second `#else` in the same region"
                              Fix = "two `#else` branches in one `#if` — one of them is dead, and which one is silently up to the template engine" }
                    else
                        region.SawElse <- true

            | Elif ->
                if stack.Count = 0 then
                    failures.Add
                        { Rule = "orphan-elif"
                          Location = at lineNo
                          Expected = "an `#elif` sits inside an open `#if`"
                          Actual = "`#elif` with no open `#if`"
                          Fix = "an `#elif` outside a region guards nothing — its body is emitted for EVERY profile" }
                else
                    let region = stack.Peek()

                    if region.SawElse then
                        failures.Add
                            { Rule = "elif-after-else"
                              Location = at lineNo
                              Expected = sprintf "every `#elif` precedes the region's `#else` (region opened at %s)" (at region.Line)
                              Actual = "`#elif` after `#else`"
                              Fix = "an `#elif` after the catch-all `#else` is unreachable — the condition can never be consulted" })

    // Anything still open at EOF was never closed. Reported OLDEST-FIRST: the outermost unclosed `#if`
    // is the one to fix, and an inner one is usually its consequence rather than a second defect.
    for region in stack |> Seq.rev do
        failures.Add
            { Rule = "unclosed-if"
              Location = at region.Line
              Expected = "every `#if` is closed by an `#endif`"
              Actual = "`#if` still open at end of file"
              Fix =
                "add the missing `#endif`. Everything from here to EOF is inside the region, so a profile the condition excludes silently loses all of it." }

    List.ofSeq failures, directives

// ---- reporting --------------------------------------------------------------------------------
let private printDrift (failures: Failure list) =
    for f in failures do
        eprintfn "IMBALANCE [%s] %s" f.Rule f.Location
        eprintfn "  expected: %s" f.Expected
        eprintfn "  actual:   %s" f.Actual
        eprintfn "  fix:      %s" f.Fix

let private writeStepSummary (title: string) (body: string seq) =
    match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
    | null
    | "" -> ()
    | path ->
        let sb = Text.StringBuilder()
        sb.AppendLine(sprintf "### %s" title) |> ignore
        sb.AppendLine "" |> ignore
        for l in body do sb.AppendLine l |> ignore
        sb.AppendLine "" |> ignore
        File.AppendAllText(path, sb.ToString())

// ---- main -------------------------------------------------------------------------------------
let private parseArgs (argv: string list) =
    let rec loop args root =
        match args with
        | [] -> root
        | "--root" :: value :: rest -> loop rest value
        | "--root" :: [] -> raise (GuardError "--root needs a directory")
        | unknown :: _ -> raise (GuardError(sprintf "unknown argument `%s` — usage: [--root <dir>]" unknown))

    loop argv (repo "template/base")

let private main (argv: string list) =
    let root = parseArgs argv

    if not (Directory.Exists root) then
        raise (GuardError(sprintf "root does not exist: %s" root))

    let files = filesUnder root |> Seq.sort |> List.ofSeq

    let scanned =
        [ for file in files do
            let lines =
                try
                    File.ReadAllLines file
                with ex ->
                    raise (GuardError(sprintf "cannot read %s: %s" file ex.Message))

            // Repo-relative when the subject is in the repo (the real run, and what a reader wants to
            // paste into an editor); root-relative when it is not (a test fixture in a temp dir, where
            // a repo-relative path would be a wall of `../..`).
            let rel =
                let fromRepo = Path.GetRelativePath(repoRoot, file).Replace('\\', '/')

                if fromRepo.StartsWith ".." then
                    Path.GetRelativePath(root, file).Replace('\\', '/')
                else
                    fromRepo

            let failures, directives = scanFile rel lines
            if directives > 0 then yield rel, failures, directives ]

    let totalDirectives = scanned |> List.sumBy (fun (_, _, d) -> d)
    let failures = scanned |> List.collect (fun (_, f, _) -> f)

    // FAIL CLOSED. A subject with no directives in it is a guard that checked nothing, and saying
    // BALANCED here would be the #266 lie the header forbids. See the exit-code note.
    if totalDirectives = 0 then
        raise (
            GuardError(
                sprintf
                    "found ZERO `//#if` directives under %s — %d file(s) scanned. `dotnet new` conditionals are how this template varies by profile, so a template with none of them is not a balanced template: it is the wrong subject (a bad merge, a moved tree, a mis-pointed --root). 'Nothing to check' and 'checked, and it's fine' must not share an exit code (FS-GG/.github#266)."
                    root
                    files.Length
            )
        )

    printfn "template conditionals: %d directive(s) across %d file(s) under %s" totalDirectives scanned.Length (Path.GetRelativePath(repoRoot, root).Replace('\\', '/'))

    // The scanned set is printed on BOTH verdicts, not just the happy one. On a red gate this is the
    // evidence that says WHICH files were in scope — the first question anyone asks when a guard fires,
    // and the one a success-only listing answers only when nobody needs it.
    for rel, fs, directives in scanned do
        printfn "  %-58s %2d%s" rel directives (if fs.IsEmpty then "" else "  <-- IMBALANCE")

    if failures.IsEmpty then
        printfn "template conditionals: BALANCED."

        writeStepSummary
            "Template conditionals — balanced"
            [ sprintf "- **%d** directives across **%d** files, all regions balanced." totalDirectives scanned.Length ]

        0
    else
        printDrift failures

        writeStepSummary
            "Template conditionals — IMBALANCE"
            [ for f in failures ->
                sprintf "- `IMBALANCE [%s]` %s — expected `%s`; actual `%s` — fix: %s" f.Rule f.Location f.Expected f.Actual f.Fix ]

        // Count the files that are BROKEN, not the files that were LOOKED AT. `scanned` is every file
        // carrying a directive (11 today), and reporting it here read as "11 files are broken" when one
        // stray `#endif` in one file was the whole defect. This is the first line a human sees on a red
        // gate; it has to be about their bug, not about the size of the subject.
        let brokenFiles = scanned |> List.filter (fun (_, f, _) -> not f.IsEmpty) |> List.length

        eprintfn
            "template conditionals: IMBALANCE — %d defect(s) in %d of %d file(s) carrying directives."
            failures.Length
            brokenFiles
            scanned.Length

        1

let exitCode =
    try
        // `fsi.CommandLineArgs.[0]` is the script path; the rest are ours.
        main (fsi.CommandLineArgs |> Array.skip 1 |> List.ofArray)
    with
    | GuardError msg ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" msg
        2
    | ex ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" ex.Message
        2

exit exitCode
