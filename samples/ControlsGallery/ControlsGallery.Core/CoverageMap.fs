/// The coverage check (FR-003 / SC-001): every `Catalog.supportedControls` id maps to
/// exactly one page, with zero unreferenced and zero duplicated. Reads the catalog from
/// the public `FS.GG.UI.Controls.Catalog` surface, so it fails on catalog drift.
module ControlsGallery.Core.CoverageMap

open FS.GG.UI.Controls
open ControlsGallery.Core.Model

/// All catalog control ids (the domain of the map).
let catalogIds (): string list =
    Catalog.supportedControls |> List.map (fun d -> d.Id)

/// All ids assigned across the 10 pages (with multiplicity, to detect duplicates).
let assignedIds (): string list =
    Pages.all |> List.collect (fun p -> p.ControlIds)

/// Run the check. Empty/empty ⇒ pass (bijection). `Unreferenced` are catalog ids on
/// zero pages (incl. ids assigned to a page that no longer exist in the catalog count
/// only via the symmetric difference below).
let check (): CoverageResult =
    let catalog = catalogIds ()
    let catalogSet = Set.ofList catalog
    let assigned = assignedIds ()
    let assignedSet = Set.ofList assigned

    // catalog ids appearing on zero pages, plus registry ids that no longer exist in
    // the catalog — both are drift the check must reject.
    let unreferenced =
        (catalog |> List.filter (fun id -> not (assignedSet.Contains id)))
        @ (assigned |> List.filter (fun id -> not (catalogSet.Contains id)))
        |> List.distinct

    // ids assigned to more than one page.
    let duplicated =
        assigned
        |> List.countBy id
        |> List.choose (fun (k, n) -> if n > 1 then Some k else None)

    { Unreferenced = unreferenced; Duplicated = duplicated }

/// True when the registry is a clean bijection with the catalog.
let isClean (result: CoverageResult): bool =
    List.isEmpty result.Unreferenced && List.isEmpty result.Duplicated

/// One-line human summary for the `coverage-check` CLI subcommand.
let summary (): string =
    let result = check ()
    let catalogCount = List.length (catalogIds ())
    let pageCount = List.length Pages.all
    if isClean result then
        sprintf "%d/%d controls mapped, %d pages, 0 unreferenced, 0 duplicated" catalogCount catalogCount pageCount
    else
        sprintf
            "DRIFT: %d catalog controls, %d pages, %d unreferenced [%s], %d duplicated [%s]"
            catalogCount
            pageCount
            (List.length result.Unreferenced)
            (String.concat "; " result.Unreferenced)
            (List.length result.Duplicated)
            (String.concat "; " result.Duplicated)
