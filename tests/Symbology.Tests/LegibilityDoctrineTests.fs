module Symbology.Tests.LegibilityDoctrineTests

// #285 — the doctrine and the linter must quote ONE set of numbers.
//
// The symbology skill's §4 channel-grammar table used to hand-maintain a "Reliable levels" column that
// had already drifted from `Legibility.table`: the prose sold Size/Threat/Charge as `~4 ordered` while
// the linter marked them `Continuous`, capacity 0, and skipped them entirely. The doctrine's most
// salient numbers were the only ones nothing enforced.
//
// `Legibility.table` is now the single source. This gate parses the §4 table out of the skill body and
// fails when a `Kind` or `Capacity` cell disagrees with it — so the prose can only ever be a projection
// of the F# table, never a second opinion. `Motion` is whole-board and has no `table` row, so its
// budget is read out of the linter's own finding rather than restated here.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open FS.GG.UI.Symbology

let private skillPath =
    Path.Combine(RepositoryRoot.value, "src", "Symbology", "skill", "SKILL.md")

/// The generated-product variant. It carries no §4 table, but it restates the legibility rules — so the
/// vacuous per-symbol motion rule could regress there alone if only the library body were guarded.
let private productSkillPath =
    Path.Combine(RepositoryRoot.value, "template", "product-skills", "fs-gg-symbology", "SKILL.md")

/// The Token field each §4 row names, mapped to the linter channel it encodes. `R` is the `Size`
/// channel; `Motion` is whole-board. Any other backticked field in the table is a parse failure.
let private channelOfField field =
    match field with
    | "Faction" -> Some Legibility.Faction
    | "Klass" -> Some Legibility.Klass
    | "Sigil" -> Some Legibility.Sigil
    | "State" -> Some Legibility.State
    | "Shield" -> Some Legibility.Shield
    | "Speed" -> Some Legibility.Speed
    | "R" -> Some Legibility.Size
    | "Threat" -> Some Legibility.Threat
    | "Charge" -> Some Legibility.Charge
    | "Health" -> Some Legibility.Health
    | "Heading" -> Some Legibility.Heading
    | "SecondaryHeading" -> Some Legibility.SecondaryHeading
    | "Motion" -> Some Legibility.Motion
    | _ -> None

/// Markdown prose, flattened for phrase matching: emphasis markers dropped, every whitespace run (line
/// wraps included) collapsed to one space, lowercased. A rule that wraps across two lines is the same
/// rule, and a guard that misses it because of a line break guards nothing.
let private flattenProse (markdown: string) =
    Regex.Replace(markdown.Replace("*", ""), @"\s+", " ").ToLowerInvariant()

/// One parsed §4 row: the channel it names, and the raw `Kind` / `Capacity` cells as written.
type private DoctrineRow =
    { Channel: Legibility.Channel
      Kind: string
      Capacity: string }

/// The `| … |` rows of the first markdown table under the §4 heading, header and separator dropped.
let private doctrineRows () =
    let lines = File.ReadAllLines skillPath

    let heading =
        lines
        |> Array.tryFindIndex (fun l -> l.StartsWith "## The fixed channel grammar")
        |> Option.defaultWith (fun () -> failwithf "no '## The fixed channel grammar' heading in %s" skillPath)

    let body = lines |> Array.skip heading

    let start =
        body
        |> Array.tryFindIndex (fun l -> l.StartsWith "|---")
        |> Option.defaultWith (fun () -> failwithf "no table separator under the §4 heading in %s" skillPath)

    body
    |> Array.skip (start + 1)
    |> Array.takeWhile (fun l -> l.StartsWith "|")
    |> Array.map (fun line ->
        let cells = line.Split '|' |> Array.map (fun c -> c.Trim())
        // "| a | b |" splits to [""; "a"; "b"; ""] — six content cells, leading/trailing empties dropped.
        if cells.Length <> 8 then
            failwithf "§4 row is not 6 cells (Channel|Field|Primitive|Kind|Capacity|Salience): %s" line

        let field = Regex.Match(cells.[2], "`([A-Za-z]+)`")

        if not field.Success then
            failwithf "§4 row names no backticked Token field: %s" line

        let fieldName = field.Groups.[1].Value

        match channelOfField fieldName with
        | None -> failwithf "§4 row names unknown Token field `%s`: %s" fieldName line
        | Some channel ->
            { Channel = channel
              Kind = cells.[4]
              Capacity = cells.[5] })
    |> Array.toList

/// The whole-board rhythm budget, read from the linter's own overload message so the number lives in
/// exactly one place (`Legibility.motionBudget`, private) and is never restated by this test.
///
/// Probed with EVERY non-`Idle` rhythm the grammar has, not with two: a two-rhythm board only overloads
/// while the budget is 1, so probing with two would make this helper — the one thing here that is meant
/// to survive a budget change — the thing that breaks on one. Overloads for any budget below five.
let private motionBudgetFromLinter () =
    let allRhythms = [ Pulse; Spin; Blink; Damage; Moving ]

    let report =
        Legibility.scoreAnimated [ for m in allRhythms -> (m, Symbology.defaultToken) ]

    let message =
        report.Findings
        |> List.tryFind (fun f -> f.Channel = Legibility.Motion)
        |> Option.map (fun f -> f.Message)
        |> Option.defaultWith (fun () ->
            failwithf "every rhythm at once (%d) produced no Motion overload — is the budget now unbounded?" allRhythms.Length)

    let m = Regex.Match(message, @"budget (\d+)")

    if not m.Success then
        failwithf "the Motion overload message no longer states its budget: %s" message

    int m.Groups.[1].Value

[<Tests>]
let doctrineDrift =
    testList
        "Legibility doctrine (#285)"
        [ test "the §4 table names every per-unit channel exactly once, in Legibility.table order" {
              let prose = doctrineRows () |> List.filter (fun r -> r.Channel <> Legibility.Motion)

              Expect.equal
                  (prose |> List.map (fun r -> r.Channel))
                  (Legibility.table |> List.map (fun s -> s.Channel))
                  "the §4 rows are Legibility.table's channels, in its order — add/reorder the F# table first"
          }

          test "every §4 Kind and Capacity cell equals Legibility.table (the single source)" {
              let prose =
                  doctrineRows ()
                  |> List.filter (fun r -> r.Channel <> Legibility.Motion)
                  |> List.map (fun r -> r.Channel, r)
                  |> Map.ofList

              for spec in Legibility.table do
                  let row = prose.[spec.Channel]

                  let expectedKind =
                      match spec.Kind with
                      | Legibility.Categorical -> "Categorical"
                      | Legibility.Ordered -> "Ordered"
                      | Legibility.Continuous -> "Continuous"

                  // An em-dash, not "0": a Continuous channel has no capacity, and printing 0 would read
                  // as "zero levels are legible" rather than "this channel is not ranked".
                  let expectedCapacity =
                      match spec.Kind with
                      | Legibility.Continuous -> "—"
                      | _ -> string spec.Capacity

                  Expect.equal row.Kind expectedKind (sprintf "§4 Kind cell for %A" spec.Channel)

                  Expect.equal
                      row.Capacity
                      expectedCapacity
                      (sprintf "§4 Capacity cell for %A — change Legibility.table, then this prose" spec.Channel)
          }

          test "the §4 Motion row states the linter's whole-board rhythm budget" {
              let motion =
                  doctrineRows ()
                  |> List.tryFind (fun r -> r.Channel = Legibility.Motion)
                  |> Option.defaultWith (fun () -> failwith "the §4 table has no Motion row")

              Expect.equal motion.Kind "whole board" "Motion is scored per board, and the table must say so"

              Expect.equal
                  motion.Capacity
                  (sprintf "budget %d" (motionBudgetFromLinter ()))
                  "the §4 Motion row must quote the budget the linter actually enforces"
          }

          test "no channel carrying a capacity is overload-exempt, and no exempt channel carries one" {
              // The invariant the drift produced: a channel the doctrine assigns a level count to, that
              // `overloadFindings` then skips. Continuous ⇒ exempt ⇒ must not advertise a capacity.
              for spec in Legibility.table do
                  match spec.Kind with
                  | Legibility.Continuous ->
                      Expect.equal spec.Capacity 0 (sprintf "%A is overload-exempt, so it must not claim a capacity" spec.Channel)
                  | Legibility.Categorical
                  | Legibility.Ordered ->
                      Expect.isGreaterThan
                          spec.Capacity
                          0
                          (sprintf "%A is enforced, so it must carry the capacity the doctrine quotes" spec.Channel)
          }

          test "the §4 table cites Legibility.table as the source, so a reader knows which to edit" {
              let body = flattenProse (File.ReadAllText skillPath)
              Expect.stringContains body "`legibility.table` is the single source" "the §4 preamble names the source"
          }

          test "the vacuous per-symbol motion rule is gone from BOTH skill variants; the board rule is written down" {
              // `animate : Motion -> Token -> float -> Scene` takes ONE Motion, so "never stack rhythms on
              // one symbol" cannot be violated and taught nothing. The rule that can be violated is the
              // board-wide budget — it was enforced and undocumented.
              for path in [ skillPath; productSkillPath ] do
                  let body = flattenProse (File.ReadAllText path)
                  let name = Path.GetFileName(Path.GetDirectoryName path)

                  Expect.isFalse
                      (body.Contains "one active motion at a time")
                      (sprintf "%s: the per-symbol rule is a type-level guarantee, not a legibility rule" name)

                  Expect.stringContains
                      body
                      "one active rhythm per board"
                      (sprintf "%s: the board-wide rule is stated as a rule" name)
          } ]
