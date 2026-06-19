/// The single page registry (contracts/page-registry.md): the 13 Catalog family pages
/// (the coverage bijection) ++ the 6 Template enterprise pages (exempt, R2/R4). Family
/// pages first, then templates — the nav rail lists all 19, each reachable in one direct
/// selection (SC-008).
module SecondAntShowcase.Core.PageRegistry

open SecondAntShowcase.Core.Model

/// All 19 pages in nav order: family pages, then template pages.
let all: Page list = Pages.familyPages @ Templates.all

/// Just the Catalog-kind (family) pages — the domain of the coverage bijection.
let catalogPages: Page list = all |> List.filter (fun p -> p.Kind = Catalog)

/// Just the Template-kind (enterprise) pages.
let templatePages: Page list = all |> List.filter (fun p -> p.Kind = Template)

/// Page lookup by id; falls back to the first page for an unknown id.
let byId (id: string): Page =
    all |> List.tryFind (fun p -> p.Id = id) |> Option.defaultValue (List.head all)
