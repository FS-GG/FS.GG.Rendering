/// The coverage check (FR-003 / SC-001): every `Catalog.supportedControls` id maps to
/// exactly one Catalog-kind page, with zero unreferenced and zero duplicated. Reads the
/// catalog from the public `FS.GG.UI.Controls.Catalog` surface, so it fails on catalog
/// drift in either direction. Template pages are EXCLUDED by the `Kind` filter (R2/R4).
module SecondAntShowcase.Core.CoverageMap

open FS.GG.UI.Controls
open SecondAntShowcase.Core.Model

/// All catalog control ids (the domain of the map) — the live 96 after the R1 feed refresh.
let catalogIds (): string list =
    Catalog.supportedControls |> List.map (fun d -> d.Id)

/// All ids assigned across the Catalog-kind pages (with multiplicity, to detect duplicates).
let assignedIds (): string list =
    PageRegistry.catalogPages |> List.collect (fun p -> p.ControlIds)

/// Run the check. Empty/empty ⇒ pass (bijection). `Unreferenced` are catalog ids on zero
/// Catalog pages plus assigned ids no longer in the catalog; `Duplicated` are ids on >1.
let check (): CoverageResult =
    let catalog = catalogIds ()
    let catalogSet = Set.ofList catalog
    let assigned = assignedIds ()
    let assignedSet = Set.ofList assigned

    let unreferenced =
        (catalog |> List.filter (fun id -> not (assignedSet.Contains id)))
        @ (assigned |> List.filter (fun id -> not (catalogSet.Contains id)))
        |> List.distinct

    let duplicated =
        assigned
        |> List.countBy id
        |> List.choose (fun (k, n) -> if n > 1 then Some k else None)

    { Unreferenced = unreferenced; Duplicated = duplicated }

/// True when the registry is a clean bijection with the catalog.
let isClean (result: CoverageResult): bool = Model.isClean result

/// One-line human summary for the `coverage` CLI subcommand.
let summary (): string =
    let result = check ()
    let catalogCount = List.length (catalogIds ())
    let catalogPageCount = List.length PageRegistry.catalogPages
    let templatePageCount = List.length PageRegistry.templatePages
    if isClean result then
        sprintf
            "%d/%d controls mapped, %d pages (%d catalog + %d template), 0 unreferenced, 0 duplicated"
            catalogCount
            catalogCount
            (catalogPageCount + templatePageCount)
            catalogPageCount
            templatePageCount
    else
        sprintf
            "DRIFT: %d catalog controls, %d catalog pages, %d unreferenced [%s], %d duplicated [%s]"
            catalogCount
            catalogPageCount
            (List.length result.Unreferenced)
            (String.concat "; " result.Unreferenced)
            (List.length result.Duplicated)
            (String.concat "; " result.Duplicated)
