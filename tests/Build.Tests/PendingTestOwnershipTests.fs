module PendingTestOwnershipTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private pendingCall =
    Regex(@"\bptest(?:List)?\b", RegexOptions.Compiled)

let private ownership =
    Regex(
        @"^\s*//\s*PendingTest:\s+owner=(?<owner>FS-GG/FS\.GG\.Rendering#\d+)\s+review-by=(?<date>\d{4}-\d{2}-\d{2})\s*$",
        RegexOptions.Compiled
    )

let private testSources () =
    Directory.EnumerateFiles(Path.Combine(repositoryRoot, "tests"), "*.fs", SearchOption.AllDirectories)
    |> Seq.filter (fun path ->
        not (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        && not (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))

let private pendingTests () =
    seq {
        for path in testSources () do
            let lines = File.ReadAllLines path

            for index in 0 .. lines.Length - 1 do
                let code = lines[index].Split("//", 2, StringSplitOptions.None)[0]

                if pendingCall.IsMatch code then
                    let marker =
                        lines[max 0 (index - 2) .. index - 1]
                        |> Array.tryPick (fun line ->
                            let matched = ownership.Match line
                            if matched.Success then Some matched else None)

                    yield path, index + 1, marker
    }
    |> Seq.toList

[<Tests>]
let tests =
    testList "Pending test ownership" [
        test "every unconditional pending test names an owning issue and a future review date" {
            let today = DateOnly.FromDateTime DateTime.UtcNow
            let pending = pendingTests ()

            Expect.isGreaterThan pending.Length 0 "the guard exercises the repository's pending-test declarations"

            for path, line, marker in pending do
                let relative = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')
                Expect.isSome marker $"{relative}:{line} has a nearby PendingTest owner/review marker"

                let marker = marker |> Option.get
                let reviewBy = DateOnly.ParseExact(marker.Groups["date"].Value, "yyyy-MM-dd")
                let owner = marker.Groups["owner"].Value

                Expect.isGreaterThanOrEqual
                    reviewBy
                    today
                    $"{relative}:{line} pending ownership review has not expired ({owner})"
        }
    ]
