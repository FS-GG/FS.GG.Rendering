namespace FS.GG.UI.Symbology

open System

[<RequireQualifiedAccess>]
module Legibility =

    type Channel =
        | Faction
        | Klass
        | Sigil
        | Size
        | Heading
        | Threat
        | Charge
        | Health
        | Speed
        | State
        | Shield
        | Motion

    type ChannelKind =
        | Categorical
        | Ordered
        | Continuous

    type Severity =
        | Warning
        | Error

    type Finding =
        { Channel: Channel
          Severity: Severity
          Message: string
          Units: int list }

    type ChannelSpec =
        { Channel: Channel
          Kind: ChannelKind
          Capacity: int }

    type ChannelUsage =
        { Channel: Channel
          Kind: ChannelKind
          DistinctLevels: int
          Capacity: int }

    type Verdict =
        | Clean
        | HasWarnings

    type Report =
        { Findings: Finding list
          Usage: ChannelUsage list
          Verdict: Verdict }

    /// The fixed capacity table (research D2), §4 grammar order. One row per per-unit channel (11);
    /// `Motion` is whole-board and has no `ChannelKind`, so it is NOT a row here (Capacity is unused
    /// for the Continuous rows — they are overload-exempt, FR-009).
    let table: ChannelSpec list =
        [ { Channel = Faction; Kind = Categorical; Capacity = 7 }
          { Channel = Klass; Kind = Categorical; Capacity = 6 }
          { Channel = Sigil; Kind = Categorical; Capacity = 12 }
          { Channel = State; Kind = Categorical; Capacity = 3 }
          { Channel = Shield; Kind = Categorical; Capacity = 3 }
          { Channel = Speed; Kind = Ordered; Capacity = 4 }
          { Channel = Size; Kind = Continuous; Capacity = 0 }
          { Channel = Threat; Kind = Continuous; Capacity = 0 }
          { Channel = Charge; Kind = Continuous; Capacity = 0 }
          { Channel = Health; Kind = Continuous; Capacity = 0 }
          { Channel = Heading; Kind = Continuous; Capacity = 0 } ]

    /// Deterministic channel order: the §4 table order, with whole-board `Motion` sorting last.
    /// Drives the stable finding/usage ordering the determinism contract relies on (FR-001/SC-001).
    let private channelOrder channel =
        match channel with
        | Faction -> 0
        | Klass -> 1
        | Sigil -> 2
        | State -> 3
        | Shield -> 4
        | Speed -> 5
        | Size -> 6
        | Threat -> 7
        | Charge -> 8
        | Health -> 9
        | Heading -> 10
        | Motion -> 11

    let private isFiniteF (v: float) = not (Double.IsNaN v || Double.IsInfinity v)

    /// Distinct-level count for a per-unit channel via structural equality (each distinct `Custom`
    /// colour and each distinct `Mark` path counted separately). For Continuous channels this is the
    /// informational count of distinct raw values (never drives an overload finding — research D3).
    let private distinctLevels channel (tokens: Token list) =
        let count projection =
            tokens |> List.map projection |> List.distinct |> List.length

        match channel with
        | Faction -> count (fun t -> box t.Faction)
        | Klass -> count (fun t -> box t.Klass)
        | Sigil -> count (fun t -> box t.Sigil)
        | State -> count (fun t -> box t.State)
        | Shield -> count (fun t -> box t.Shield)
        | Speed -> count (fun t -> box t.Speed)
        | Size -> count (fun t -> box t.R)
        | Threat -> count (fun t -> box t.Threat)
        | Charge -> count (fun t -> box t.Charge)
        | Health -> count (fun t -> box t.Health)
        | Heading -> count (fun t -> box t.Heading)
        | Motion -> 0

    /// Level-overload check (FR-003): one `Warning` per Categorical/Ordered channel whose distinct
    /// levels exceed capacity. Every unit contributes to the distinct-level count, so `Units` names
    /// the whole scored set. Continuous channels are exempt (FR-009).
    let private overloadFindings (tokens: Token list) =
        let allUnits = [ 0 .. List.length tokens - 1 ]

        [ for spec in table do
              match spec.Kind with
              | Continuous -> ()
              | Categorical
              | Ordered ->
                  let used = distinctLevels spec.Channel tokens

                  if used > spec.Capacity then
                      let finding =
                          { Channel = spec.Channel
                            Severity = Warning
                            Message =
                                sprintf
                                    "%A overloaded: %d distinct levels used, capacity %d"
                                    spec.Channel
                                    used
                                    spec.Capacity
                            Units = allUnits }

                      yield (channelOrder spec.Channel, -1), finding ]

    /// Per-unit out-of-domain / degenerate / non-finite check (FR-004/FR-005). Each produces an
    /// `Error` naming the channel and the offending unit; the scan always continues (FR-008).
    let private unitFindings (i: int) (t: Token) =
        let band channel (v: float) =
            if not (isFiniteF v) then
                Some
                    { Channel = channel
                      Severity = Error
                      Message = sprintf "%A non-finite: %g" channel v
                      Units = [ i ] }
            elif v < 0.0 || v > 1.0 then
                Some
                    { Channel = channel
                      Severity = Error
                      Message = sprintf "%A out of domain: %g (expected 0..1)" channel v
                      Units = [ i ] }
            else
                None

        [ // Size: non-finite, then degenerate R <= 0 (research D6).
          match (if not (isFiniteF t.R) then
                     Some(sprintf "Size non-finite: %g" t.R)
                 elif t.R <= 0.0 then
                     Some(sprintf "Size degenerate: R = %g (expected R > 0)" t.R)
                 else
                     None)
              with
          | Some msg ->
              yield
                  (channelOrder Size, i),
                  { Channel = Size
                    Severity = Error
                    Message = msg
                    Units = [ i ] }
          | None -> ()

          // Speed: discrete count, must fall inside the legible bead range [0,6].
          if t.Speed < 0 || t.Speed > 6 then
              yield
                  (channelOrder Speed, i),
                  { Channel = Speed
                    Severity = Error
                    Message = sprintf "Speed out of domain: %d (expected 0..6)" t.Speed
                    Units = [ i ] }

          // Continuous magnitude bands (non-finite first, then [0,1]).
          for channel, v in [ Threat, t.Threat; Charge, t.Charge; Health, t.Health ] do
              match band channel v with
              | Some f -> yield (channelOrder channel, i), f
              | None -> ()

          // Heading: any finite angle is valid (angles wrap); only non-finite is an error.
          if not (isFiniteF t.Heading) then
              yield
                  (channelOrder Heading, i),
                  { Channel = Heading
                    Severity = Error
                    Message = sprintf "Heading non-finite: %g" t.Heading
                    Units = [ i ] } ]

    /// One `ChannelUsage` per per-unit channel (11), in table order (FR-007). Motion excluded.
    let private usageOf (tokens: Token list) =
        table
        |> List.map (fun spec ->
            { Channel = spec.Channel
              Kind = spec.Kind
              DistinctLevels = distinctLevels spec.Channel tokens
              Capacity = spec.Capacity })

    /// Score the per-unit channels (shared by `score` and `scoreAnimated`). Findings are emitted in
    /// deterministic order: table order, then ascending unit index (overload before per-unit errors).
    let private scoreTokens (tokens: Token list) : (int * int) list * Finding list * ChannelUsage list =
        let keyed =
            overloadFindings tokens
            @ (tokens |> List.mapi unitFindings |> List.concat)
            |> List.sortBy fst

        let keys = keyed |> List.map fst
        let findings = keyed |> List.map snd
        keys, findings, usageOf tokens

    let private verdictOf findings =
        if List.isEmpty findings then Clean else HasWarnings

    let score (tokens: Token list) : Report =
        let _, findings, usage = scoreTokens tokens

        { Findings = findings
          Usage = usage
          Verdict = verdictOf findings }

    let scoreAnimated (board: (Motion * Token) list) : Report =
        let tokens = board |> List.map snd
        let _, findings, usage = scoreTokens tokens

        // Whole-board motion load (FR-010): count distinct *non-Idle* rhythms; > 1 simultaneous rhythm
        // is a board-level Warning (Units = []). A single rhythm (any count of moving units) never flags.
        let activeRhythms =
            board |> List.map fst |> List.filter (fun m -> m <> Idle) |> List.distinct

        let motionFindings =
            if List.length activeRhythms > 1 then
                [ { Channel = Motion
                    Severity = Warning
                    Message =
                        sprintf "Motion overloaded: %d distinct active rhythms across the board, budget 1" (List.length activeRhythms)
                    Units = [] } ]
            else
                []

        // Motion sorts last (channelOrder = 11), so appending preserves the deterministic order.
        let allFindings = findings @ motionFindings

        { Findings = allFindings
          Usage = usage
          Verdict = verdictOf allFindings }
