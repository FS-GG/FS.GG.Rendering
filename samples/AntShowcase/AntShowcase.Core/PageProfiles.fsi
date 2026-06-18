module AntShowcase.Core.PageProfiles

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

val all: PageVisualProfile list
val byPageId: pageId: string -> PageVisualProfile
val tryFind: pageId: string -> PageVisualProfile option
val hasProfile: pageId: string -> bool
val largeRegionPageIds: string list
