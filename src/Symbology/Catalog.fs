namespace FS.GG.UI.Symbology

open System
open System.Text

[<RequireQualifiedAccess>]
module Catalog =

    type Visual =
        | Shown of token: string
        | Hidden of reason: string

    type Entry =
        { Element: string
          Visual: Visual }

    type Catalog = { Entries: Entry list }

    /// The versioned header line every artifact carries — a machine-readable format marker #994's gate
    /// can key on, and the first thing `parse` validates.
    [<Literal>]
    let header = "# fs-gg element-visual catalog v1"

    let declaredElements (catalog: Catalog) : string list =
        catalog.Entries |> List.map (fun e -> e.Element)

    let tryFind (element: string) (catalog: Catalog) : Visual option =
        catalog.Entries
        |> List.tryPick (fun e -> if e.Element = element then Some e.Visual else None)

    let toRepresentation (visual: Visual) : Coverage.Representation =
        match visual with
        // Presence-only witness: the real approved token is resolved from the handle by the renderer;
        // coverage only asks whether the element is shown at all (see the module doc).
        | Shown _handle -> Coverage.Shown Symbology.defaultToken
        | Hidden reason -> Coverage.Hidden reason

    let coverage (declared: string list) (catalog: Catalog) : Coverage.Report<string> =
        Coverage.check declared (fun e -> tryFind e catalog |> Option.map toRepresentation)

    let validate (catalog: Catalog) : Coverage.Report<string> =
        coverage (declaredElements catalog) catalog

    let render (catalog: Catalog) : string =
        let sb = StringBuilder()
        sb.Append(header).Append('\n') |> ignore

        for entry in catalog.Entries do
            let disposition, payload =
                match entry.Visual with
                | Shown handle -> "shown", handle
                | Hidden reason -> "hidden", reason

            sb
                .Append(entry.Element)
                .Append('\t')
                .Append(disposition)
                .Append('\t')
                .Append(payload)
                .Append('\n')
            |> ignore

        sb.ToString()

    let parse (text: string) : Result<Catalog, string> =
        // Deterministic, IO-free: split on line boundaries, validate the header, then fold the rows.
        let lines =
            (text: string).Replace("\r\n", "\n").Replace("\r", "\n").Split('\n') |> Array.toList

        // The first non-blank line must be the version header.
        let rec skipBlanks =
            function
            | (l: string) :: rest when String.IsNullOrWhiteSpace l -> skipBlanks rest
            | rest -> rest

        match skipBlanks lines with
        | [] -> Error(sprintf "empty catalog: expected the version header \"%s\"" header)
        | first :: body when first.Trim() = header ->
            // Fold the remaining rows, accumulating entries and rejecting the first malformed one.
            let rec loop acc (seen: Set<string>) rows =
                match rows with
                | [] -> Ok { Entries = List.rev acc }
                | (line: string) :: rest ->
                    if String.IsNullOrWhiteSpace line || line.TrimStart().StartsWith("#") then
                        loop acc seen rest
                    else
                        let firstTab = line.IndexOf('\t')

                        if firstTab < 0 then
                            Error(sprintf "malformed row (no tab): %s" line)
                        else
                            let element = line.Substring(0, firstTab).Trim()
                            let afterElement = line.Substring(firstTab + 1)
                            let secondTab = afterElement.IndexOf('\t')

                            let disposition, payload =
                                if secondTab < 0 then
                                    afterElement.Trim(), ""
                                else
                                    afterElement.Substring(0, secondTab).Trim(),
                                    afterElement.Substring(secondTab + 1)

                            if element = "" then
                                Error(sprintf "malformed row (blank element id): %s" line)
                            elif seen.Contains element then
                                Error(sprintf "duplicate element id: %s" element)
                            else
                                match disposition with
                                | "shown" ->
                                    let handle = payload.Trim()

                                    if handle = "" then
                                        Error(
                                            sprintf
                                                "element %s is 'shown' with a blank token handle (a shown-as-nothing row) — name the approved token, or mark it 'hidden' with a reason"
                                                element
                                        )
                                    else
                                        loop
                                            ({ Element = element
                                               Visual = Shown handle }
                                             :: acc)
                                            (seen.Add element)
                                            rest
                                | "hidden" ->
                                    loop
                                        ({ Element = element
                                           Visual = Hidden(payload.Trim()) }
                                         :: acc)
                                        (seen.Add element)
                                        rest
                                | other ->
                                    Error(
                                        sprintf
                                            "element %s has an unknown disposition '%s' (expected 'shown' or 'hidden')"
                                            element
                                            other
                                    )

            loop [] Set.empty body
        | first :: _ -> Error(sprintf "expected the version header \"%s\", got: %s" header first)
