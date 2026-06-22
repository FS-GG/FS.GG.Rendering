// Semantic-diff two harness readiness artifact trees (Feature 185).
//
// Behavior-preserving refactor: the re-emitted tree must be SEMANTICALLY EQUIVALENT to the
// pre-refactor baseline. The only permitted differences are embedded wall-clock timestamps /
// run-ids (FR-007), which appear both in file CONTENT and in some FILE NAMES
// (e.g. `feature160-20260622040556-001.md`, `entry-feature161-20260622040557.md`,
// run identities `feature157-readiness-20260622040553`, proof ids `20260622-040544`).
//
// Strategy: normalize every timestamp token to `<TS>` in both paths and content, then compare the
// normalized path-set and the normalized content of each common path. Any other difference is a
// real semantic divergence and fails the run (non-zero exit).
//
// Usage:  dotnet fsi scripts/semantic-diff-artifacts.fsx <baseline-dir> <candidate-dir>

open System
open System.IO
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length < 2 then
    eprintfn "usage: dotnet fsi scripts/semantic-diff-artifacts.fsx <baseline-dir> <candidate-dir>"
    exit 2

let baselineDir = args.[0]
let candidateDir = args.[1]

// Timestamp / run-id token patterns, longest-first so they don't partially match each other.
// Longest-first so they don't partially match each other:
//   ISO 8601 ; yyyyMMddHHmmss (run-ids, entry/csv filenames) ; yyyyMMdd-HHmmss (proof ids) ; bare yyyyMMdd
let tsPatterns =
    [ @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?"
      @"\d{14}"
      @"\d{8}-\d{6}"
      @"\d{8}" ]

let normalize (s: string) =
    tsPatterns |> List.fold (fun (acc: string) p -> Regex.Replace(acc, p, "<TS>")) s

let isArtifact (p: string) =
    let n = Path.GetFileName p
    n <> ".stdout.txt" && n <> ".stderr.txt"

// relpath (normalized) -> (original relpath, normalized content)
let loadTree (root: string) =
    if not (Directory.Exists root) then Map.empty
    else
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        |> Seq.filter isArtifact
        |> Seq.map (fun full ->
            let rel = Path.GetRelativePath(root, full).Replace('\\', '/')
            let normRel = normalize rel
            let content =
                try File.ReadAllText full with _ -> "<unreadable>"
            // Feature 161 embeds the absolute --out path in its content; that is environmental,
            // not semantic, so fold the tree root to a placeholder before timestamp normalization.
            let rootNorm = (Path.GetFullPath root).Replace('\\', '/')
            let content = content.Replace(rootNorm, "<OUT>").Replace(root, "<OUT>")
            normRel, (rel, normalize content))
        |> Map.ofSeq

let baseline = loadTree baselineDir
let candidate = loadTree candidateDir

let mutable problems = 0
let report (msg: string) =
    problems <- problems + 1
    eprintfn "DIFF: %s" msg

// 1. path-set equivalence (normalized)
let baseKeys = baseline |> Map.toSeq |> Seq.map fst |> Set.ofSeq
let candKeys = candidate |> Map.toSeq |> Seq.map fst |> Set.ofSeq

for k in Set.difference baseKeys candKeys do
    let (orig, _) = baseline.[k]
    report (sprintf "missing in candidate: %s (baseline %s)" k orig)
for k in Set.difference candKeys baseKeys do
    let (orig, _) = candidate.[k]
    report (sprintf "unexpected in candidate: %s (candidate %s)" k orig)

// 2. content equivalence for common paths
for k in Set.intersect baseKeys candKeys do
    let (bOrig, bContent) = baseline.[k]
    let (cOrig, cContent) = candidate.[k]
    if bContent <> cContent then
        // show the first differing normalized line for triage
        let bLines = bContent.Replace("\r\n", "\n").Split('\n')
        let cLines = cContent.Replace("\r\n", "\n").Split('\n')
        let firstDiff =
            Seq.zip bLines cLines
            |> Seq.mapi (fun i (a, b) -> i, a, b)
            |> Seq.tryFind (fun (_, a, b) -> a <> b)
        match firstDiff with
        | Some (i, a, b) ->
            report (sprintf "content differs: %s (baseline %s vs candidate %s)\n    line %d:\n      base: %s\n      cand: %s"
                        k bOrig cOrig (i + 1) a b)
        | None ->
            report (sprintf "content differs (length %d vs %d): %s" bLines.Length cLines.Length k)

printfn "semantic-diff: baseline=%d files, candidate=%d files, problems=%d" baseline.Count candidate.Count problems
if problems > 0 then
    eprintfn "SEMANTIC DIVERGENCE: %d problem(s) — see DIFF lines above" problems
    exit 1
else
    printfn "OK: artifact trees are semantically equivalent (timestamps normalized)"
    exit 0
