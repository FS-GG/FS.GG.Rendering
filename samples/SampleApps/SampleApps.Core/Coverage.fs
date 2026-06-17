/// Coverage + 22-spec backlog honesty artifact (contracts/coverage-backlog.md, research R7).
///
/// `coverageRows` is derived from `Registry.all` (single source of truth — it cannot drift from
/// the registry). `backlog` enumerates ALL 22 archived game/productivity specs, each adopted or
/// deferred with a reason. `check` runs the R-C1..3 / R-B1..4 honesty rules against the live
/// `Catalog.supportedControls` + the registry; `render` produces the committed report text. The
/// analogue of G1's catalog→page coverage check and the repo's matrix-honesty gates.
module SampleApps.Core.Coverage

open System.Text
open FS.GG.UI.Controls
open SampleApps.Core.Harness

/// Per-sample coverage: the catalog control ids a sample renders + the input modalities it
/// exercises. `Controls` ids are validated against the live `Catalog.supportedControls`.
type CoverageRow =
    { SampleId: string
      Family: string
      Controls: string list
      Inputs: string list }

/// One archived game/productivity spec, dispositioned `Adopted` (built in this slice) or
/// `Deferred` (backlog), each with a non-empty reason.
type BacklogEntry =
    { Spec: string
      Family: string
      Disposition: string
      Reason: string }

/// The honesty-check result. All four lists empty ⇒ pass; any non-empty ⇒ fail.
type CoverageBacklogResult =
    { DanglingControls: string list
      MissingInputs: string list
      UnaccountedSpecs: string list
      AdoptedMismatch: string list }

/// One coverage row per curated sample — derived from the registry so it cannot drift.
let coverageRows: CoverageRow list =
    Registry.all
    |> List.map (fun e -> { SampleId = e.Id; Family = e.Family; Controls = e.Controls; Inputs = e.Inputs })

/// The required input modalities the curated slice must collectively cover (R-C3 / SC-004).
let requiredInputs: string list = [ "keyboard"; "pointer"; "timing-step" ]

/// All 22 archived specs (12 games + 10 productivity), source: plan §10 / contracts. The six
/// `Adopted` are the curated slice; the other 16 are the disclosed `Deferred` backlog.
let backlog: BacklogEntry list =
    [ { Spec = "Tetris"; Family = "game"; Disposition = "Adopted"; Reason = "curated slice — grid + gravity loop + keyboard" }
      { Spec = "Snake"; Family = "game"; Disposition = "Adopted"; Reason = "curated slice — grid + directional + step loop" }
      { Spec = "Pong"; Family = "game"; Disposition = "Adopted"; Reason = "curated slice — continuous motion + paddle" }
      { Spec = "Asteroids"; Family = "game"; Disposition = "Deferred"; Reason = "backlog — coverage already met by Tetris/Snake/Pong" }
      { Spec = "Breakout"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Lunar Lander"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Sokoban"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Space Invaders"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Tower Defense"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Top-down Racer"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Bomberman-lite"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Platformer"; Family = "game"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Kanban board"; Family = "productivity"; Disposition = "Adopted"; Reason = "curated slice — data grid + pointer move + inline edit" }
      { Spec = "Todo/task manager"; Family = "productivity"; Disposition = "Adopted"; Reason = "curated slice — forms + validation + list + inline edit" }
      { Spec = "Calendar scheduler"; Family = "productivity"; Disposition = "Adopted"; Reason = "curated slice — date grid + forms" }
      { Spec = "Contact manager"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog — patterns already met by Kanban/Todo/Calendar" }
      { Spec = "Expense tracker"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "File manager"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Invoice builder"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Markdown notes"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Pomodoro timer"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" }
      { Spec = "Spreadsheet editor"; Family = "productivity"; Disposition = "Deferred"; Reason = "backlog" } ]

/// The six adopted spec names ↔ their registry sample ids (R-B3 mapping).
let private adoptedSpecToId: Map<string, string> =
    Map.ofList
        [ "Tetris", "tetris"
          "Snake", "snake"
          "Pong", "pong"
          "Kanban board", "kanban"
          "Todo/task manager", "todo"
          "Calendar scheduler", "calendar" ]

/// Run every honesty rule (R-C1..3 / R-B1..4). All-empty result ⇒ pass.
let check (): CoverageBacklogResult =
    let catalogIds = Catalog.supportedControls |> List.map (fun d -> d.Id) |> Set.ofList

    // R-C2: no dangling control id.
    let dangling =
        coverageRows
        |> List.collect (fun r -> r.Controls)
        |> List.filter (fun c -> not (catalogIds.Contains c))
        |> List.distinct

    // R-C3: the input union spans keyboard + pointer + timing-step.
    let inputUnion = coverageRows |> List.collect (fun r -> r.Inputs) |> Set.ofList
    let missingInputs = requiredInputs |> List.filter (fun i -> not (inputUnion.Contains i))

    // R-B1/B2/B4: 22 entries, no dup spec, valid disposition + non-empty reason, 12 game/10 prod.
    let specs = backlog |> List.map (fun b -> b.Spec)
    let dupes =
        specs
        |> List.countBy id
        |> List.choose (fun (s, n) -> if n > 1 then Some(sprintf "duplicate-spec:%s" s) else None)
    let badEntries =
        backlog
        |> List.filter (fun b -> not (b.Disposition = "Adopted" || b.Disposition = "Deferred") || b.Reason.Trim() = "")
        |> List.map (fun b -> sprintf "bad-entry:%s" b.Spec)
    let games = backlog |> List.filter (fun b -> b.Family = "game") |> List.length
    let prod = backlog |> List.filter (fun b -> b.Family = "productivity") |> List.length
    let countIssues =
        [ if List.length backlog <> 22 then sprintf "total:%d<>22" (List.length backlog)
          if games <> 12 then sprintf "games:%d<>12" games
          if prod <> 10 then sprintf "productivity:%d<>10" prod ]
    let unaccounted = dupes @ badEntries @ countIssues

    // R-C1 + R-B3: every curated sample appears once in coverageRows, and the 6 adopted map 1:1
    // to the registry ids.
    let coverageIds = coverageRows |> List.map (fun r -> r.SampleId) |> Set.ofList
    let registryIds = Registry.all |> List.map (fun e -> e.Id) |> Set.ofList
    let adopted = backlog |> List.filter (fun b -> b.Disposition = "Adopted")
    let adoptedIds = adopted |> List.choose (fun b -> Map.tryFind b.Spec adoptedSpecToId) |> Set.ofList
    let unmappedAdopted =
        adopted
        |> List.filter (fun b -> not (Map.containsKey b.Spec adoptedSpecToId))
        |> List.map (fun b -> sprintf "unmapped-adopted:%s" b.Spec)
    let adoptedMismatch =
        unmappedAdopted
        @ (Set.difference adoptedIds registryIds |> Set.toList |> List.map (sprintf "adopted-not-in-registry:%s"))
        @ (Set.difference registryIds adoptedIds |> Set.toList |> List.map (sprintf "registry-not-adopted:%s"))
        @ (Set.difference coverageIds registryIds |> Set.toList |> List.map (sprintf "coverage-row-not-in-registry:%s"))
        @ (Set.difference registryIds coverageIds |> Set.toList |> List.map (sprintf "registry-not-in-coverage:%s"))

    { DanglingControls = dangling
      MissingInputs = missingInputs
      UnaccountedSpecs = unaccounted
      AdoptedMismatch = adoptedMismatch }

/// True when the report is honest (all checks empty).
let isClean (result: CoverageBacklogResult): bool =
    List.isEmpty result.DanglingControls
    && List.isEmpty result.MissingInputs
    && List.isEmpty result.UnaccountedSpecs
    && List.isEmpty result.AdoptedMismatch

/// The committed report text — deterministic (fixed order), drift-checked against the committed
/// `coverage-backlog.md` (T035 / CoverageBacklogTests).
let render (): string =
    let sb = StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line "# Sample Apps — coverage + backlog"
    line ""
    line "Generated from `Coverage.render ()` (research R7). Do not hand-edit — regenerate via the"
    line "`coverage` CLI. Validated by `Coverage.check` / `CoverageBacklogTests`."
    line ""
    line "## Part A — per-sample coverage"
    line ""
    line "| Sample | Family | Inputs | Controls |"
    line "|---|---|---|---|"
    for r in coverageRows do
        line (sprintf "| %s | %s | %s | %s |" r.SampleId r.Family (String.concat ", " r.Inputs) (String.concat ", " r.Controls))
    line ""
    line (sprintf "Input union spans: %s." (String.concat ", " requiredInputs))
    line ""
    line "## Part B — backlog (all 22 archived specs)"
    line ""
    line "| Spec | Family | Disposition | Reason |"
    line "|---|---|---|---|"
    for b in backlog do
        line (sprintf "| %s | %s | %s | %s |" b.Spec b.Family b.Disposition b.Reason)
    line ""
    let adoptedCount = backlog |> List.filter (fun b -> b.Disposition = "Adopted") |> List.length
    let deferredCount = backlog |> List.filter (fun b -> b.Disposition = "Deferred") |> List.length
    line (sprintf "%d adopted (the curated slice) · %d deferred · %d total." adoptedCount deferredCount (List.length backlog))
    sb.ToString()
