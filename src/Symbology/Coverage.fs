namespace FS.GG.UI.Symbology

open System

[<RequireQualifiedAccess>]
module Coverage =

    type Representation =
        | Shown of token: Token
        | Hidden of reason: string

    type Gap =
        | Missing
        | Unreasoned

    type Finding<'element> =
        { Element: 'element
          Gap: Gap
          Message: string }

    type Verdict =
        | Covered
        | HasGaps

    type Report<'element> =
        { Findings: Finding<'element> list
          OptedOut: ('element * string) list
          Verdict: Verdict }

    let private missingMessage (element: 'element) =
        sprintf
            "gameplay element %A has no visible representation and no explicit opt-out — map it to a symbology Token (a Coverage.Shown) or declare an explicit Coverage.Hidden opt-out naming the mechanic that hides it"
            element

    let private unreasonedMessage (element: 'element) =
        sprintf
            "gameplay element %A is opted out of rendering with a blank reason — name the mechanic that hides it (fog of war, stealth, off-screen, internal marker), or give it a visible Token"
            element

    let check (elements: 'element list) (resolve: 'element -> Representation option) : Report<'element> =
        // Accumulate reversed, reverse once at the end: findings and the opt-out ledger both come back in
        // DECLARED-element order, so re-checking an equal input yields an equal report (determinism).
        let mutable findings = []
        let mutable opted = []

        for element in elements do
            match resolve element with
            | Some(Shown _) -> ()
            | Some(Hidden reason) when not (String.IsNullOrWhiteSpace reason) -> opted <- (element, reason) :: opted
            | Some(Hidden _) ->
                findings <-
                    { Element = element
                      Gap = Unreasoned
                      Message = unreasonedMessage element }
                    :: findings
            | None ->
                findings <-
                    { Element = element
                      Gap = Missing
                      Message = missingMessage element }
                    :: findings

        let findings = List.rev findings

        { Findings = findings
          OptedOut = List.rev opted
          Verdict = if List.isEmpty findings then Covered else HasGaps }

    let checkMap (elements: 'element list) (table: Map<'element, Representation>) : Report<'element> =
        check elements (fun e -> Map.tryFind e table)
