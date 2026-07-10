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
        // Declared LAST on purpose. A DU case's compiler-generated tag is its declaration index, so
        // inserting next to `Heading` would renumber `Threat`..`Motion` and silently invalidate any
        // persisted tag. Declaration order carries no meaning here anyway — `table` and `channelOrder`
        // already order the channels, and neither follows this DU.
        | SecondaryHeading
        // Appended after `SecondaryHeading` for the same tag-stability reason. Like `Motion`, not a
        // `table` row: budgeted in lines, per grammar, and scored only by `scoreIn`/`scoreAnimatedIn`.
        | Label

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

    /// The fixed capacity table (research D2), §4 grammar order — and the SINGLE source of the
    /// reliable-level counts the skill's §4 prose table quotes (a test parses that table and fails on
    /// divergence). One row per per-unit channel (12); `Motion` is whole-board and has no
    /// `ChannelKind`, so it is NOT a row here (Capacity is unused for the Continuous rows — they are
    /// overload-exempt, FR-009).
    ///
    /// `Capacity` is how many distinct levels the EYE separates, not how many the grammar can DRAW.
    /// The two differ on purpose: `Speed` renders 0..6 beads (`unitFindings` errors outside that), yet
    /// only ~4 bead counts are reliably ranked at board size, so a fifth distinct speed is legal per
    /// unit and warns per board. The same holds for `Faction`/`Sigil`, whose domains are open.
    let table: ChannelSpec list =
        [ { Channel = Faction; Kind = Categorical; Capacity = 7 }
          { Channel = Klass; Kind = Categorical; Capacity = 6 }
          { Channel = Sigil; Kind = Categorical; Capacity = 12 }
          { Channel = State; Kind = Categorical; Capacity = 3 }
          { Channel = Shield; Kind = Categorical; Capacity = 3 }
          { Channel = Speed; Kind = Ordered; Capacity = 4 }
          // Size/Threat/Charge carry a float, but the eye ranks radius, stroke width and gradient
          // depth at ~4 levels — so they are Ordered and DO overload. A board using twelve distinct
          // radii is the legibility defect the doctrine names; leaving them Continuous made the
          // doctrine's most salient numbers the only ones the linter never enforced (#285).
          { Channel = Size; Kind = Ordered; Capacity = 4 }
          { Channel = Threat; Kind = Ordered; Capacity = 4 }
          { Channel = Charge; Kind = Ordered; Capacity = 4 }
          // Genuinely continuous: read as a magnitude or an angle, never as a rank of levels.
          { Channel = Health; Kind = Continuous; Capacity = 0 }
          { Channel = Heading; Kind = Continuous; Capacity = 0 }
          { Channel = SecondaryHeading; Kind = Continuous; Capacity = 0 } ]

    /// Deterministic channel order: the §4 table order, with the two non-table channels sorting last —
    /// whole-board `Motion`, then per-unit `Label`.
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
        | SecondaryHeading -> 11
        | Motion -> 12
        | Label -> 13

    /// Whole-board budget of simultaneous non-`Idle` rhythms (FR-010). `Motion` is not a `table` row —
    /// it is scored per board, not per unit — so this is its capacity, and the number the skill's §4
    /// Motion row must quote. Deliberately the strictest channel: a second rhythm competes for the same
    /// pre-attentive attention grab rather than adding a level to it, so two rhythms do not read as a
    /// rank of two, they read as noise.
    let private motionBudget = 1

    let private isFiniteF (v: float) = not (Double.IsNaN v || Double.IsInfinity v)

    /// The per-unit level each token occupies on a channel, boxed so one projection serves every
    /// channel. Structural equality decides sameness (each distinct `Custom` colour and each distinct
    /// `Mark` path is its own level). `Motion` is whole-board and occupies no per-unit level.
    ///
    /// For the Ordered FLOAT channels (Size/Threat/Charge) sameness is exact: two radii differing in the
    /// last bit are two levels, because a float ramp is not a rank. Quantise in the mapping (#285).
    let private levelKeys channel (tokens: Token list) : objnull list =
        let keys projection = tokens |> List.map projection

        match channel with
        | Faction -> keys (fun t -> box t.Faction)
        | Klass -> keys (fun t -> box t.Klass)
        | Sigil -> keys (fun t -> box t.Sigil)
        | State -> keys (fun t -> box t.State)
        | Shield -> keys (fun t -> box t.Shield)
        | Speed -> keys (fun t -> box t.Speed)
        | Size -> keys (fun t -> box t.R)
        | Threat -> keys (fun t -> box t.Threat)
        | Charge -> keys (fun t -> box t.Charge)
        | Health -> keys (fun t -> box t.Health)
        | Heading -> keys (fun t -> box t.Heading)
        // `None` is itself a level: a board that leaves the channel unset reports one distinct level,
        // exactly as an all-identical `Heading` board does. Informational only (Continuous ⇒ exempt).
        | SecondaryHeading -> keys (fun t -> box t.SecondaryHeading)
        // Neither is a table row, so neither is ever asked for a level: `Motion` is whole-board, and
        // `Label` is budgeted in lines rather than in separable levels.
        | Motion
        | Label -> []

    /// Distinct-level count for a per-unit channel via structural equality. For Continuous channels
    /// this is the informational count of distinct raw values (never drives an overload — research D3).
    let private distinctLevels channel (tokens: Token list) =
        levelKeys channel tokens |> List.distinct |> List.length

    /// The units carrying the levels a channel cannot rank (FR-003/FR-006).
    ///
    /// Levels are ranked by (frequency DESCENDING, first appearance ASCENDING); the leading
    /// `capacity` of them are the ones the eye reliably separates, and every level after that is
    /// EXCESS. The returned indices are the units holding an excess level, ascending — i.e. the
    /// least-frequent levels, which are the cheapest to fold away and the smallest set a re-map has
    /// to touch to bring the channel back inside capacity.
    ///
    /// Deterministic: the frequency/first-appearance ordering is a total order on levels, so equal
    /// input yields an equal index list (SC-001).
    let private excessUnits capacity (keys: objnull list) =
        let levels =
            keys
            |> List.indexed
            |> List.groupBy snd
            |> List.map (fun (_, members) ->
                let indices = members |> List.map fst
                List.length indices, List.min indices, indices)

        levels
        |> List.sortBy (fun (count, firstSeen, _) -> -count, firstSeen)
        |> List.skip (min capacity (List.length levels))
        |> List.collect (fun (_, _, indices) -> indices)
        |> List.sort

    /// Level-overload check (FR-003): one `Warning` per Categorical/Ordered channel whose distinct
    /// levels exceed capacity. `Units` names only the units holding the levels PAST capacity — the
    /// ones a re-map must move — not the whole scored set. Continuous channels are exempt (FR-009).
    let private overloadFindings (tokens: Token list) =
        [ for spec in table do
              match spec.Kind with
              | Continuous -> ()
              | Categorical
              | Ordered ->
                  let keys = levelKeys spec.Channel tokens
                  let used = keys |> List.distinct |> List.length

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
                            Units = excessUnits spec.Capacity keys }

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
                    Units = [ i ] }

          // SecondaryHeading: an unset channel is legal (the grammar simply omits the indicator).
          // When set it is scored exactly like Heading — angles wrap, so only non-finite is an error.
          match t.SecondaryHeading with
          | Some a when not (isFiniteF a) ->
              yield
                  (channelOrder SecondaryHeading, i),
                  { Channel = SecondaryHeading
                    Severity = Error
                    Message = sprintf "SecondaryHeading non-finite: %g" a
                    Units = [ i ] }
          | _ -> () ]

    /// One `ChannelUsage` per per-unit channel (12), in table order (FR-007). Motion excluded.
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

    /// Whole-board motion load (FR-010): count distinct *non-Idle* rhythms; more than `motionBudget`
    /// simultaneous rhythms is a board-level Warning (Units = []). A single rhythm (any count of
    /// moving units) never flags.
    let private motionLoadFindings (board: (Motion * Token) list) =
        let activeRhythms =
            board |> List.map fst |> List.filter (fun m -> m <> Idle) |> List.distinct

        if List.length activeRhythms > motionBudget then
            [ { Channel = Motion
                Severity = Warning
                Message =
                    sprintf
                        "Motion overloaded: %d distinct active rhythms across the board, budget %d"
                        (List.length activeRhythms)
                        motionBudget
                Units = [] } ]
        else
            []

    let scoreAnimated (board: (Motion * Token) list) : Report =
        let tokens = board |> List.map snd
        let _, findings, usage = scoreTokens tokens

        // Motion sorts last (channelOrder = 12), so appending preserves the deterministic order.
        let allFindings = findings @ motionLoadFindings board

        { Findings = allFindings
          Usage = usage
          Verdict = verdictOf allFindings }

    // ---- Grammar-aware scoring (#286) --------------------------------------------------------------
    // `score` is grammar-blind by contract (FR-009/SC-005) and stays so. Everything below is strictly
    // ADDITIVE: the same per-unit and whole-board findings, plus the two facts that depend on WHICH
    // grammar draws the roster. Nothing here can suppress a finding `score`/`scoreAnimated` would emit.

    /// Hard line breaks in a raw label, normalised exactly as `wrapLabel` normalises them: CRLF folded,
    /// each segment trimmed, blank segments dropped. Greedy wrapping happens AFTER this and can only add
    /// lines, so this is a lower bound on what the grammar will try to draw.
    let private hardLines (s: string) =
        s.Replace("\r\n", "\n").Split('\n')
        |> Array.filter (fun seg -> not (String.IsNullOrWhiteSpace seg))
        |> Array.length

    /// Lower bound on the lines the resolved label occupies. `Rich` runs concatenate into one flow
    /// (`joinRuns`), so only their embedded breaks count; `Laid` paragraphs each start a new line, so a
    /// two-paragraph label is at least two lines even with no `\n` in it.
    let private labelLineCount (t: Token) =
        let runsText (runs: LabelRun list) =
            runs |> List.map (fun r -> r.Text) |> String.concat ""

        match Symbology.resolvedLabel t with
        | None -> 0
        | Some(LabelText.Plain s) -> hardLines s
        | Some(LabelText.Rich runs) -> hardLines (runsText runs)
        | Some(LabelText.Laid paras) -> paras |> List.sumBy (fun p -> hardLines (runsText p.Runs))

    /// Per-unit `Label` Warning: the resolved label already needs more lines than the grammar draws, so
    /// `wrapLabel` will drop the surplus. Keyed after `Motion` (channelOrder = 13).
    let private labelBudgetFindings grammar (tokens: Token list) =
        // Read from the renderer, never re-declared here: the emitters cap at this exact number.
        let budget = Symbology.labelLineBudget grammar

        [ for i, t in List.indexed tokens do
              let lines = labelLineCount t

              if lines > budget then
                  yield
                      (channelOrder Label, i),
                      { Channel = Label
                        Severity = Warning
                        Message =
                            sprintf
                                "Label over budget under %A: %d hard lines, budget %d — the surplus is dropped and the last drawn line gains an ellipsis"
                                grammar
                                lines
                                budget
                        Units = [ i ] } ]

    /// Per-unit `Motion` Error: `animateIn` composes non-`Token` grammars from the grammar-agnostic
    /// overlays only (`Pulse`/`Blink`/`Damage`), so `Spin` and `Moving` contribute NO node. The unit is
    /// then byte-identical to an `Idle` one — the channel is dropped, not degraded, and no amount of
    /// re-mapping inside the grammar recovers it.
    let private droppedRhythmFindings grammar (board: (Motion * Token) list) =
        match grammar with
        | Grammar.Token -> [] // whole-body rotation and travel are drawn; nothing is dropped
        | g ->
            [ for i, (motion, _) in List.indexed board do
                  match motion with
                  | Spin
                  | Moving ->
                      yield
                          (channelOrder Motion, i),
                          { Channel = Motion
                            Severity = Error
                            Message =
                                sprintf
                                    "%A cannot draw Motion.%A: the rhythm is dropped, so the unit renders identically to Idle"
                                    g
                                    motion
                            Units = [ i ] }
                  | Idle
                  | Pulse
                  | Blink
                  | Damage -> () ]

    /// Merge keyed finding groups into the one deterministic order the contract promises: table order,
    /// then unit index. Whole-board findings carry index -1 and so precede the per-unit findings of the
    /// same channel. `List.sortBy` is stable, so equal keys keep their construction order.
    let private ordered (keyed: ((int * int) * Finding) list) =
        keyed |> List.sortBy fst |> List.map snd

    let scoreIn (grammar: Grammar) (tokens: Token list) : Report =
        let keys, findings, usage = scoreTokens tokens

        let allFindings =
            ordered (List.zip keys findings @ labelBudgetFindings grammar tokens)

        { Findings = allFindings
          Usage = usage
          Verdict = verdictOf allFindings }

    let scoreAnimatedIn (grammar: Grammar) (board: (Motion * Token) list) : Report =
        let tokens = board |> List.map snd
        let keys, findings, usage = scoreTokens tokens

        // The whole-board motion Warning is keyed at (Motion, -1) so it sorts ahead of the per-unit
        // dropped-rhythm Errors, matching `scoreAnimated`'s "board finding last among Motion" order.
        let motionLoad =
            motionLoadFindings board |> List.map (fun f -> (channelOrder Motion, -1), f)

        let allFindings =
            ordered (
                List.zip keys findings
                @ motionLoad
                @ droppedRhythmFindings grammar board
                @ labelBudgetFindings grammar tokens
            )

        { Findings = allFindings
          Usage = usage
          Verdict = verdictOf allFindings }
