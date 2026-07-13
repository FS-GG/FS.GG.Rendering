module RepositoryRootSingleFinderTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

// #700 — the gate that makes `RepositoryRoot`'s central claim CHECKABLE rather than merely asserted.
// #734 — the gate that can actually SEE its subject.
//
// `tests/TestSupport/RepositoryRoot.fs` is the one file allowed to find the repository root by walking
// to it. A second finder is a second marker set that can disagree with the first, and the disagreement
// is SILENT: it resolves every repo-relative path against a different tree and judges a corpus that is
// not the one that ships. A hand-rolled walk fails loud when it finds NOTHING (`failwith`); it cannot
// fail loud about finding the WRONG root. So this list guards both halves of the claim, because either
// alone is worth little:
//
//   1. the finder lands on the RIGHT root  (`is the root it claims to be`), and
//   2. it is the ONLY finder               (`no other source walks to the repo root by itself`).
//
// TRAVERSAL IS THE SIGNAL, NOT THE SEED. The obvious rule — "only RepositoryRoot.fs may name
// `AppContext.BaseDirectory`" — is too weak, because the seed is interchangeable: this repo already
// roots walks at `__SOURCE_DIRECTORY__` too (tests/Elmish.Tests/*AdapterTests.fs, legitimately, via
// `RepositoryRoot.find`), so a hand-rolled walk seeded that way would sail straight past a seed-only
// check. What a walk CANNOT do without is the step upward.
//
// BUT THE TOKEN IS NOT THE TRAVERSAL, AND THAT IS WHAT #700 GOT WRONG. It detected a walk by naming
// the tokens a walk ascends WITH — `Directory.GetParent`, `DirectoryInfo` + `.Parent` — and argued
// that "outside the finder itself no test source has any honest use for them". The tokens were the
// right idea; the inventory was not exhaustive, and it could never be. `Path.GetDirectoryName` ascends
// just as well, and #700 was blind to it:
//
//     let rec private up (d: string) =                                    // sails straight past #700
//         if File.Exists(Path.Combine(d, "FS.GG.Rendering.slnx")) then d
//         else up (Path.GetDirectoryName d)
//
// That is the exact failure this gate exists to prevent, reported green. And it could NOT be fixed by
// adding the token to the list: `Path.GetDirectoryName` is the ordinary way to take a file's directory,
// and this repo takes one 19 times under `tests/` alone. Adding it turns 19 honest lines red.
//
// THE SIGNAL IS REPETITION. A walk ascends, and then ascends AGAIN — the ascent's own result is what
// the next step ascends from. Dirname never does that; it takes one step and stops. So the rule below
// is not "names an ascent token" but "applies an ascent to its own result", and the three `walkShapes`
// are the three ways F# closes that loop: a `let rec` that ascends and calls itself, an ascent inside a
// `while`, and a `x <- …ascend x…` rebinding. Every real walk this repo has ever had is one of those;
// no honest line in the corpus is any of them.
//
// That sharpening is what buys the two things #700 could not have:
//
//   * the ascent tokens no longer have to be unambiguous, so the list can be COMPLETE. It now carries
//     `Path.GetDirectoryName` (the evasion above) and a bare `.Parent` (which #700 had to conjoin with
//     `DirectoryInfo` to keep `mr.Parent` on a reflection MemberReference from tripping it — a
//     conjunction that let `FileInfo(p).Directory.Parent` through). Repetition does the disambiguating
//     that the token list used to have to do, and it does it without an inventory that can be outrun.
//
//   * the corpus can reach `samples/**/*.Tests`, which no `tests/`-scoped hygiene gate polices today.
//     #700 could not: `ascendsTheTree` would have fired on four HONEST single-step
//     `Directory.GetParent(file)` calls in SecondAntShowcase.Tests, which take the run directory of a
//     summary file they had already located. Those are not walks, and now they do not read as walks.
//
// THE SAMPLES ARE POLICED BUT CANNOT BE REMEDIED THE SAME WAY, and the offender message below says so.
// The sample suites are shadowing consumers — they reference the PUBLISHED packages, never this repo's
// projects (see the header of RepositoryRoot.fs) — so they structurally CANNOT consume the shared
// finder, and "use RepositoryRoot.value" is not advice they can take. Their remedy is the one #725
// applied when it removed the last walk out there: make the file locate itself (MSBuild copies it next
// to the test binary) so the sample needs no finder at all, rather than needing a second one. The gate
// can still POLICE them because it is a source-text scan and needs no ProjectReference — that asymmetry
// is the whole reason widening was worth doing rather than declaring `samples/` out of scope forever.
//
// WHAT THIS STILL CANNOT SEE, stated plainly so the next reader does not have to rediscover it. It is a
// textual scan, not a compiler. A walk built from mutual recursion (`let rec f … and g …`) or from
// `Seq.unfold` would slip past; neither shape exists anywhere in the repo today, and both are strange
// enough to write that a copy-paste will not produce one. And the recursion shape is a deliberate
// OVER-approximation in the other direction: a `let rec` that recurses over a list while taking a
// dirname would be flagged as a walk it is not. That trade is the right way round — an over-reach
// fails LOUD, on a line an author is looking at, and is one `widen`-the-exemption or one restructure
// away; #700's under-reach failed SILENT, and stayed silent for as long as nobody happened to look.
//
// AND THERE IS NO `thisFile` EXEMPTION ANY MORE. #700 had to exempt this very file, because its header
// named the forbidden tokens and a substring scan cannot tell a rule from a violation of it. That
// exemption was also the one hole nobody could check — a walk smuggled in HERE was the walk no test
// looked at. `codeOnly` below removes the need for it: comments and string literals are lexed out
// before any shape is matched, so this file may quote `Directory.GetParent` in prose and embed whole
// walks as `probeSubjects` data, and still be judged, unexempted, exactly like every other source.

let private repoRoot = RepositoryRoot.value

/// The one file allowed to find the repository root by walking to it.
let private theFinder = "tests/TestSupport/RepositoryRoot.fs"

// ─── the lexer ────────────────────────────────────────────────────────────────────────────────────

/// A char literal, and ONLY a char literal. F# spends apostrophes on identifiers (`kept'`) and type
/// parameters (`'msg`) as well as on chars, so the lexer must not read every `'` as a quote — it opens
/// a char literal only where a complete one actually closes.
let private charLiteral = Regex(@"\G'(\\.|[^\\'\n])'", RegexOptions.Compiled)

let private startsAt (source: string) (index: int) (token: string) =
    index + token.Length <= source.Length
    && String.CompareOrdinal(source, index, token, 0, token.Length) = 0

/// `(*)` is F#'s multiplication OPERATOR as a function value (`List.reduce (*)`), not the start of a
/// block comment — F#'s own lexer special-cases it, and so must this one. Miss it and the "comment"
/// never closes: every line after it is blanked, the file reads as empty, and the gate reports green
/// over a source it did not read. That is the same fails-open shape (#266) this whole file refuses, so
/// it is guarded here rather than left to the fact that nobody happens to write `(*)` today.
let private opensBlockComment (source: string) (index: int) =
    startsAt source index "(*" && not (startsAt source index "(*)")

/// Blank every comment and string literal to spaces, preserving length and line structure so the shapes
/// below match CODE and nothing but code.
///
/// This is load-bearing, not hygiene. F# prose is full of the word "while"; this file's header quotes
/// `Directory.GetParent`; and `probeSubjects` embeds entire walks as test data. Match those and the
/// gate convicts its own documentation.
let private codeOnly (source: string) : string =
    let out = source.ToCharArray()

    let blank (start: int) (finish: int) =
        for k in start .. finish - 1 do
            if out[k] <> '\n' then out[k] <- ' '

    let endOf (needle: string) (from: int) =
        match source.IndexOf(needle, from, StringComparison.Ordinal) with
        | -1 -> source.Length
        | at -> at + needle.Length

    let mutable i = 0
    let mutable blockDepth = 0

    while i < source.Length do
        // `(* … *)` nests in F#, so this counts rather than scanning for the first `*)`.
        if blockDepth > 0 then
            let step =
                if opensBlockComment source i then
                    blockDepth <- blockDepth + 1
                    2
                elif startsAt source i "*)" then
                    blockDepth <- blockDepth - 1
                    2
                else
                    1

            blank i (i + step)
            i <- i + step
        elif opensBlockComment source i then
            blockDepth <- 1
            blank i (i + 2)
            i <- i + 2
        elif startsAt source i "//" then
            let finish =
                match source.IndexOf('\n', i) with
                | -1 -> source.Length
                | at -> at

            blank i finish
            i <- finish
        elif startsAt source i "\"\"\"" then
            let finish = endOf "\"\"\"" (i + 3)
            blank i finish
            i <- finish
        elif startsAt source i "@\"" then
            // Verbatim: no escapes, and `""` is how a quote is written.
            let mutable j = i + 2
            let mutable closed = false

            while not closed && j < source.Length do
                if source[j] <> '"' then j <- j + 1
                elif startsAt source j "\"\"" then j <- j + 2
                else
                    j <- j + 1
                    closed <- true

            blank i j
            i <- j
        elif source[i] = '"' then
            let mutable j = i + 1
            let mutable closed = false

            while not closed && j < source.Length do
                if source[j] = '\\' then j <- min source.Length (j + 2)
                elif source[j] = '\n' then closed <- true
                elif source[j] = '"' then
                    j <- j + 1
                    closed <- true
                else
                    j <- j + 1

            blank i j
            i <- j
        else
            let literal = charLiteral.Match(source, i)

            // `\G` should pin the match to `i`; the index check makes that a fact rather than an
            // assumption about how .NET anchors a `startat` match, because a match found FORWARD of
            // `i` would blank the wrong span.
            if literal.Success && literal.Index = i then
                blank i (i + literal.Length)
                i <- i + literal.Length
            else
                i <- i + 1

    String(out)

// ─── the detector ─────────────────────────────────────────────────────────────────────────────────

/// The step upward, in every spelling. NOT evidence on its own — every one of these has an honest
/// single-step use somewhere in the corpus, which is exactly why #700's token match could not be
/// completed without turning honest lines red. A token only convicts inside one of the shapes below.
let private ascentTokens = [ "Directory.GetParent"; "Path.GetDirectoryName"; ".Parent" ]

let private namesAnAscent (code: string) =
    ascentTokens |> List.exists (fun token -> code.Contains(token, StringComparison.Ordinal))

let private indentOf (line: string) = line.Length - line.TrimStart().Length

/// The offside block a construct opens: its own line, plus every following line indented deeper. A
/// blank line does not close a block.
let private blockAt (lines: string[]) (start: int) =
    let head = indentOf lines[start]

    let body =
        lines[start + 1 ..]
        |> Array.takeWhile (fun line -> String.IsNullOrWhiteSpace line || indentOf line > head)

    String.Join("\n", Array.append [| lines[start] |] body)

let private recBinding =
    Regex(@"\blet\s+rec\s+(?:(?:private|internal|public|inline)\s+)*([A-Za-z_][A-Za-z0-9_']*)", RegexOptions.Compiled)

let private whileLoop = Regex(@"\bwhile\b", RegexOptions.Compiled)

let private assignment = Regex(@"([A-Za-z_][A-Za-z0-9_']*)\s*<-(.*)$", RegexOptions.Compiled)

let private mentions (name: string) (code: string) =
    Regex.IsMatch(code, @"\b" + Regex.Escape name + @"\b")

type private WalkShape = { Shape: string; Line: int; Text: string }

/// Every repeated ascent in a source: an ascent applied to its OWN RESULT, which is what makes a walk a
/// walk and a dirname merely a dirname. Three shapes, because F# has three ways to close that loop:
///
///   * RECURSIVE — a `let rec f` whose body both ascends and calls `f` again. This is what the shared
///     finder is, what both walks #700 killed were, and what #734's evasion is.
///   * LOOPING — an ascent anywhere inside a `while` body. A `while` runs its body many times, so an
///     ascent in one is repeated by construction. (`for` is deliberately NOT a trigger: `for file in
///     files do … Path.GetDirectoryName file` is honest and common, and a `for`-hosted walk has to
///     carry its state in a mutable — which the third shape catches.)
///   * MUTATING — `x <- …ascend x…`. Rebinding a directory to its own parent IS a step of a walk;
///     nothing honest ever writes it.
let private walkShapes (source: string) : WalkShape list =
    let normalized = source.Replace("\r\n", "\n")
    let lines = (codeOnly normalized).Split('\n')
    // Match on the LEXED lines, but quote the ORIGINAL one back at the author: `codeOnly` blanks string
    // literals, and an offender told its walk reads `Path.Combine(dir,        )` will go looking for a
    // bug in the report rather than in the walk.
    let asWritten = normalized.Split('\n')

    [ for index in 0 .. lines.Length - 1 do
          let line = lines[index]
          let written = asWritten[index].Trim()
          let recursive = recBinding.Match line

          if recursive.Success then
              let name = recursive.Groups[1].Value
              let block = blockAt lines index
              // Drop the binding's own header, so `let rec walk` is not itself read as a call to `walk`.
              let body =
                  block.Substring(block.IndexOf(recursive.Value, StringComparison.Ordinal) + recursive.Value.Length)

              if namesAnAscent body && mentions name body then
                  { Shape = "recursive ascent"
                    Line = index + 1
                    Text = written }

          if whileLoop.IsMatch line && namesAnAscent (blockAt lines index) then
              { Shape = "looping ascent"
                Line = index + 1
                Text = written }

          let assigned = assignment.Match line

          if assigned.Success then
              let target = assigned.Groups[1].Value
              let value = assigned.Groups[2].Value

              if namesAnAscent value && mentions target value then
                  { Shape = "mutating ascent"
                    Line = index + 1
                    Text = written } ]

let private walksTheTree (source: string) = walkShapes source |> List.isEmpty |> not

// ─── the corpus ───────────────────────────────────────────────────────────────────────────────────

let private relativeTo (path: string) =
    Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

let private isBuildOutput (rel: string) = rel.Contains "/obj/" || rel.Contains "/bin/"

/// The sample TEST suites, discovered from disk rather than listed here by hand, so a suite that is
/// added or renamed joins the corpus without anybody having to remember this file exists.
///
/// A missing `samples/` yields an empty list rather than an exception, so the loss surfaces as the
/// `reaches every sample test suite` assertion below — which explains what was lost — instead of as a
/// DirectoryNotFoundException thrown while the test assembly is still loading, which explains nothing.
let private sampleTestSuites =
    let samples = Path.Combine(repoRoot, "samples")

    if not (Directory.Exists samples) then
        []
    else
        Directory.EnumerateFiles(samples, "*.Tests.fsproj", SearchOption.AllDirectories)
        |> Seq.choose (fun project -> Path.GetDirectoryName project |> Option.ofObj)
        |> Seq.map relativeTo
        |> Seq.sort
        |> List.ofSeq

/// Every F# source this gate polices: all of `tests/`, and every sample test suite. Build output is
/// excluded; nothing else is.
let private policedSources =
    "tests" :: sampleTestSuites
    |> List.collect (fun root ->
        Directory.EnumerateFiles(Path.Combine(repoRoot, root), "*.fs", SearchOption.AllDirectories)
        |> Seq.map relativeTo
        |> List.ofSeq)
    |> List.filter (isBuildOutput >> not)
    |> List.sort

let private readSource (rel: string) =
    File.ReadAllText(Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar)))

// ─── the probe's own subjects ─────────────────────────────────────────────────────────────────────

/// #700 proved its probe on ONE positive (the finder) and no negatives. That was enough while the rule
/// was a substring match. It is not enough now: "does this ascend repeatedly?" is a JUDGEMENT, and a
/// judgement fails in two directions — a walk that reads as clean is #734's hole reopened, and an
/// honest line that reads as a walk is a gate nobody can keep green. So the probe is driven against
/// both, on the shapes that actually exist rather than the ones that are easy to assert.
///
/// The sources sit at column 0 inside their quotes on purpose: the shapes are offside-sensitive, and
/// what the detector sees should be what the reader sees, with no enclosing indentation to discount.
let private probeSubjects: (bool * string * string) list =
    [ true,
      "#734's evasion — a let rec walk on Path.GetDirectoryName, invisible to #700's token list",
      """
let rec private up (d: string) =
    if File.Exists(Path.Combine(d, "FS.GG.Rendering.slnx")) then d
    else up (Path.GetDirectoryName d)
"""

      true,
      "the shared finder's own shape — a let rec walk on Directory.GetParent",
      """
let find (start: string) =
    let rec walk (directory: string) =
        if Directory.GetFiles(directory, "*.slnx").Length > 0 then directory
        else
            match Directory.GetParent directory |> Option.ofObj with
            | Some parent -> walk parent.FullName
            | None -> failwith "no root"
    walk start
"""

      true,
      "a while loop rebinding a DirectoryInfo to its own .Parent",
      """
let root () =
    let mutable current = DirectoryInfo(AppContext.BaseDirectory)
    while current <> null && not (File.Exists(Path.Combine(current.FullName, "x.slnx"))) do
        current <- current.Parent
    current.FullName
"""

      true,
      "a while loop whose assignment does NOT name the ascent — only the loop shape catches this one",
      """
let root () =
    let mutable dir = AppContext.BaseDirectory
    while not (File.Exists(Path.Combine(dir, "x.slnx"))) do
        let parent = Path.GetDirectoryName dir
        dir <- parent
    dir
"""

      true,
      "a .Parent chain that never names DirectoryInfo — the conjunction #700 needed would have let this through",
      """
let rec climb (f: FileInfo) =
    if File.Exists(Path.Combine(f.Directory.FullName, "x.slnx")) then f.Directory.FullName
    else climb (FileInfo(f.Directory.Parent.FullName))
"""

      false,
      "an honest single-step dirname — the 19 lines under tests/ that made the token unaddable",
      """
let dirOf (p: string) = match Path.GetDirectoryName p with | null -> "" | d -> d
"""

      false,
      "an honest single-step Directory.GetParent — the four lines in SecondAntShowcase.Tests that blocked the widening",
      """
let runRoot outDir =
    match Directory.GetParent(summaryFile outDir) with
    | null -> failwith "no run dir"
    | parent -> parent.FullName
"""

      false,
      "an honest dirname inside a for loop — repeated, but never fed back, which is why `for` is not a shape",
      """
let check files =
    for file in files do
        printfn "%s" (Path.GetDirectoryName file)
"""

      false,
      "an honest while loop that does not ascend at all",
      """
let scan (lines: string[]) =
    let mutable i = 0
    while i < lines.Length do
        i <- i + 1
    i
"""

      true,
      "a walk BELOW a `(*)` operator — the lexer must not read `(*)` as an unterminated block comment and blank the rest of the file",
      """
let product = List.reduce (*) [ 1; 2; 3 ]

let rec private up (d: string) =
    if File.Exists(Path.Combine(d, "x.slnx")) then d
    else up (Path.GetDirectoryName d)
"""

      false,
      "a walk quoted in a COMMENT is not a walk — this file's own header depends on that",
      """
// let rec up (d: string) =
//     if File.Exists(Path.Combine(d, "x.slnx")) then d else up (Path.GetDirectoryName d)
let harmless () = 1
"""

      false,
      "a walk quoted in a STRING is not a walk — this list itself depends on that",
      "let sample = \"\"\"\nlet rec up d = if File.Exists(Path.Combine(d, \"x.slnx\")) then d else up (Path.GetDirectoryName d)\n\"\"\"\n" ]

[<Tests>]
let tests =
    testList
        "#700/#734 — RepositoryRoot is the single root finder"
        [
          // Half one of the claim: the finder lands where it says it does. Everything under `tests/`
          // resolves its repo-relative paths through this one value, so if it were ever to stop at a
          // nearer marker — a `*.sln`/`*.slnx`/`build.fsx` materialized between a test binary and the
          // root — every one of those tests would silently judge the wrong tree. That is the exact quiet
          // failure the consolidation was for, and only an assertion makes it loud.
          test "the shared finder is the root it claims to be" {
              Expect.isTrue
                  (File.Exists(Path.Combine(repoRoot, "FS.GG.Rendering.slnx")))
                  (sprintf
                      "RepositoryRoot.value resolved to %s, which holds no FS.GG.Rendering.slnx. The shared finder stops at the NEAREST ancestor holding any *.sln/*.slnx/build.fsx, so a solution or build script materialized below the real root will silently capture it — and every test in the repo would then resolve its repo-relative paths against the wrong tree."
                      repoRoot)
          }

          // The next three are the anti-fails-open scaffolding (FS-GG/.github#266): an oracle that
          // cannot see its own subject reports green having verified nothing. They prove the corpus
          // reaches both trees and that the probe judges both ways, BEFORE the rule below trusts the
          // probe's silence about everybody else.
          test "the corpus reaches the finder itself" {
              Expect.contains
                  policedSources
                  theFinder
                  (sprintf
                      "enumerating the policed trees did not reach %s (found %d file(s)). The probe is broken, so its silence about every other file means nothing."
                      theFinder
                      policedSources.Length)
          }

          test "the corpus reaches every sample test suite" {
              Expect.isNonEmpty
                  sampleTestSuites
                  "no samples/**/*.Tests.fsproj was found. #734 widened this gate to the sample suites BECAUSE no tests/-scoped hygiene gate reaches them; if they have moved or been renamed, this gate silently went back to policing only tests/ while staying green."

              let unreached =
                  sampleTestSuites
                  |> List.filter (fun suite ->
                      policedSources
                      |> List.exists (fun rel -> rel.StartsWith(suite + "/", StringComparison.Ordinal))
                      |> not)

              Expect.isEmpty
                  unreached
                  (sprintf
                      "these sample test suite(s) contributed NO source to the corpus:%s%sThe gate reports green over them having read nothing."
                      Environment.NewLine
                      (unreached
                       |> List.map (sprintf "  - %s")
                       |> String.concat Environment.NewLine
                       |> fun listing -> listing + Environment.NewLine))
          }

          test "the probe tells a repeated ascent from an honest single step" {
              let misjudged =
                  probeSubjects
                  |> List.filter (fun (isWalk, _, source) -> walksTheTree source <> isWalk)
                  |> List.map (fun (isWalk, name, _) ->
                      sprintf
                          "  - expected %s, read it as %s — %s"
                          (if isWalk then "WALK" else "clean")
                          (if isWalk then "clean" else "WALK")
                          name)

              Expect.isEmpty
                  misjudged
                  (sprintf
                      "the probe misjudged these subject(s):%s%s%s"
                      Environment.NewLine
                      (misjudged |> String.concat Environment.NewLine |> fun listing -> listing + Environment.NewLine)
                      "A subject it reads as clean when it is a WALK is #734's hole, reopened: the rule below would report green over a second finder. A subject it reads as a WALK when it is clean is a gate nobody can keep green. Fix `walkShapes`, not the subject — these rows are the shapes this repo actually writes.")
          }

          test "the finder still walks the tree" {
              Expect.isTrue
                  (walksTheTree (readSource theFinder))
                  (sprintf
                      "%s no longer reads as a repeated ascent. This guard detects a hand-rolled walk BY the ascent that feeds itself, so if the shared finder has stopped walking that way, the guard is now watching for a shape nobody writes — it guards nothing and stays green forever. Re-derive the signal in `walkShapes` before changing how the finder walks."
                      theFinder)
          }

          test "no other source walks to the repo root by itself" {
              let offenders =
                  policedSources
                  |> List.filter (fun rel -> rel <> theFinder)
                  |> List.collect (fun rel -> walkShapes (readSource rel) |> List.map (fun shape -> rel, shape))

              Expect.isEmpty
                  offenders
                  (sprintf
                      "these source(s) ascend the directory tree REPEATEDLY — they walk to a root themselves instead of consuming the shared finder:%s%s%s"
                      Environment.NewLine
                      (offenders
                       |> List.map (fun (rel, shape) -> sprintf "  - %s:%d — %s: %s" rel shape.Line shape.Shape shape.Text)
                       |> String.concat Environment.NewLine
                       |> fun listing -> listing + Environment.NewLine)
                      "A second finder is a second marker set that can disagree with the first, and the disagreement is SILENT: it resolves repo-relative paths against the wrong tree rather than failing.\n\nUnder tests/: use `RepositoryRoot.value` (or `RepositoryRoot.find <seed>`), with a ProjectReference to tests/TestSupport and `open FS.GG.TestSupport`.\n\nUnder samples/: you CANNOT — the sample suites reference the published packages, not this repo's projects, and a ProjectReference to TestSupport would puncture the shadowing isolation they exist to prove (see the header of tests/TestSupport/RepositoryRoot.fs). Do what #725 did instead: make the file locate itself. Have MSBuild copy what the test needs next to the test binary, so the sample needs no finder at all rather than needing a second one.")
          } ]
