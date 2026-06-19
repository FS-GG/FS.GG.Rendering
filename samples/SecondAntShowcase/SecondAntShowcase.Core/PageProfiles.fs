module SecondAntShowcase.Core.PageProfiles

type PageDensity =
    | Compact
    | Standard
    | Dense
    | Large
    | Template

type TransientSurfacePolicy =
    | ClosedBaseline
    | ControlledRegion

type PageVisualProfile =
    { PageId: string
      Density: PageDensity
      SectionColumns: int
      HasLargeDemonstrationRegion: bool
      TransientPolicy: TransientSurfacePolicy
      MinimumSizeRepresentative: bool }

let profile pageId density columns large transient minimum =
    { PageId = pageId
      Density = density
      SectionColumns = columns
      HasLargeDemonstrationRegion = large
      TransientPolicy = transient
      MinimumSizeRepresentative = minimum }

let all =
    [ profile "display-typography" Standard 2 false ClosedBaseline true
      profile "cards-stats-media" Dense 2 true ClosedBaseline true
      profile "buttons" Compact 2 false ClosedBaseline true
      profile "text-numeric-input" Dense 2 false ControlledRegion true
      profile "selection-toggles" Dense 2 false ControlledRegion true
      profile "layout-containers" Dense 2 true ClosedBaseline true
      profile "navigation-menus" Dense 2 false ControlledRegion true
      profile "overlays" Large 1 true ControlledRegion true
      profile "feedback-status" Dense 2 true ControlledRegion true
      profile "data-collections" Large 1 true ClosedBaseline true
      profile "charts-statistical" Large 1 true ClosedBaseline true
      profile "charts-advanced" Large 1 true ClosedBaseline true
      profile "graphs-custom" Large 1 true ClosedBaseline true
      profile "tpl-workbench" Template 1 true ClosedBaseline true
      profile "tpl-list" Template 1 true ClosedBaseline true
      profile "tpl-detail" Template 1 true ClosedBaseline true
      profile "tpl-form" Template 1 false ClosedBaseline true
      profile "tpl-result" Template 1 false ClosedBaseline true
      profile "tpl-exception" Template 1 false ClosedBaseline true ]

let tryFind pageId =
    all |> List.tryFind (fun p -> p.PageId = pageId)

let byPageId pageId =
    match tryFind pageId with
    | Some p -> p
    | None -> profile pageId Standard 2 false ClosedBaseline false

let hasProfile pageId =
    tryFind pageId |> Option.isSome

let largeRegionPageIds =
    all |> List.filter (fun p -> p.HasLargeDemonstrationRegion) |> List.map _.PageId
