module SpecLifecycleTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto

// Feature 187 — `specs/` is a build journal that never pruned and never re-stated itself: 150 of
// 155 spec.md files read `**Status**: Draft`, including specs whose implementation had shipped and
// whose packages had been released (251 shipped under #139 and still said Draft). Nothing
// distinguished a spec describing current behaviour from one describing behaviour replaced eighteen
// features ago. This test is the gate that keeps the lifecycle vocabulary honest.
//
// Canonical doc: docs/product/spec-lifecycle.md.

let private repoRoot =
    let rec up (dir: DirectoryInfo | null) =
        match dir with
        | null -> failwith "could not locate repo root (FS.GG.Rendering.slnx) walking up from test base dir"
        | d ->
            if File.Exists(Path.Combine(d.FullName, "FS.GG.Rendering.slnx")) then d.FullName
            else up d.Parent
    up (DirectoryInfo(AppContext.BaseDirectory))

let private repoPath (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

/// One feature's spec directory: the `<n>-<slug>` name, its spec.md, and its tasks.md if it has one.
type private Spec =
    { Name: string
      SpecMd: string
      TasksMd: string option }

let private specs =
    Directory.EnumerateDirectories(repoPath "specs")
    |> Seq.choose (fun dir ->
        let specMd = Path.Combine(dir, "spec.md")
        if not (File.Exists specMd) then None
        else
            let tasksMd = Path.Combine(dir, "tasks.md")
            Some
                { Name = DirectoryInfo(dir).Name
                  SpecMd = specMd
                  TasksMd = if File.Exists tasksMd then Some tasksMd else None })
    |> List.ofSeq
    |> List.sortBy (fun s -> s.Name)

// The terminal + in-flight vocabulary. `Final` was an earlier undocumented spelling of `Shipped` on
// four specs; it is retired, and a re-introduction must fail rather than rot.
let private statusLine = Regex(@"^\*\*Status\*\*:[ \t]*(?<value>.+?)[ \t]*$", RegexOptions.Multiline)
let private supersededBy = Regex(@"^Superseded by #(?<issue>\d+)$")
let private knownStatuses = set [ "Draft"; "Shipped"; "Abandoned" ]

/// Every `**Status**:` value declared by a spec.md (more than one is itself a defect).
let private statusesOf (spec: Spec) =
    statusLine.Matches(File.ReadAllText spec.SpecMd)
    |> Seq.map (fun m -> m.Groups.["value"].Value)
    |> List.ofSeq

let private isKnownStatus (value: string) =
    Set.contains value knownStatuses || supersededBy.IsMatch value

// A task list that finished: at least one ticked box and no unticked ones. Sufficient evidence that
// the implementation merged — NOT necessary (a spec can ship with boxes left unticked, so this
// gate is deliberately one-directional and never flags a spec that did not ship).
let private unchecked = Regex(@"^[ \t]*- \[ \]", RegexOptions.Multiline)
let private ticked = Regex(@"^[ \t]*- \[[xX]\]", RegexOptions.Multiline)

let private tasksFullyChecked (spec: Spec) =
    match spec.TasksMd with
    | None -> false
    | Some path ->
        let text = File.ReadAllText path
        ticked.IsMatch text && not (unchecked.IsMatch text)

[<Tests>]
let specLifecycleTests =
    testList "Feature 187 — spec lifecycle currency" [

        test "the spec set is non-empty (sanity on the specs/ enumeration)" {
            Expect.isNonEmpty specs "no specs/<feature>/spec.md found"
        }

        test "every spec.md declares exactly one **Status** line" {
            let bad = specs |> List.map (fun s -> s.Name, List.length (statusesOf s)) |> List.filter (fun (_, n) -> n <> 1)
            Expect.isEmpty bad (sprintf "spec(s) with a missing or duplicated **Status** line (name, count): %A" bad)
        }

        test "every **Status** value is from the documented vocabulary" {
            let bad =
                specs
                |> List.collect (fun s -> statusesOf s |> List.map (fun v -> s.Name, v))
                |> List.filter (fun (_, v) -> not (isKnownStatus v))
            Expect.isEmpty
                bad
                (sprintf
                    "unknown **Status** value(s) %A — use Draft | Shipped | Abandoned | 'Superseded by #N' (docs/product/spec-lifecycle.md)"
                    bad)
        }

        // The core gate: `Draft` outliving its merged implementation.
        test "no spec with a fully-checked tasks.md is still Draft" {
            let stale =
                specs
                |> List.filter tasksFullyChecked
                |> List.filter (fun s -> statusesOf s |> List.contains "Draft")
                |> List.map (fun s -> s.Name)
            Expect.isEmpty
                stale
                (sprintf
                    "spec(s) %A tick every task yet still say **Status**: Draft — the implementation merged, so set Shipped (or 'Superseded by #N'). See docs/product/spec-lifecycle.md"
                    stale)
        }

        test "'Superseded by #N' names a positive issue number" {
            let bad =
                specs
                |> List.collect (fun s -> statusesOf s |> List.map (fun v -> s.Name, v))
                |> List.filter (fun (_, v) -> v.StartsWith "Superseded")
                |> List.filter (fun (_, v) ->
                    let m = supersededBy.Match v
                    not m.Success || int m.Groups.["issue"].Value <= 0)
            Expect.isEmpty bad (sprintf "malformed 'Superseded by #N' status(es): %A" bad)
        }

        // Feature 187 retired `Final` in favour of `Shipped`; the four specs that used it (091, 093,
        // 095, 096) were converted. Guard the spelling so it cannot creep back via copy-paste.
        test "the retired 'Final' status does not reappear" {
            let offenders =
                specs |> List.filter (fun s -> statusesOf s |> List.contains "Final") |> List.map (fun s -> s.Name)
            Expect.isEmpty offenders (sprintf "spec(s) %A use the retired 'Final' status; use 'Shipped'" offenders)
        }
    ]
