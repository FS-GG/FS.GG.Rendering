module SecondAntShowcase.Core.ShellLayout

open FS.GG.UI.Scene

type Rect =
    { X: float
      Y: float
      Width: float
      Height: float }

type ShellRegions =
    { TopBar: Rect
      Navigation: Rect
      Content: Rect
      Feedback: Rect
      Status: Rect }

val calculate: size: Size -> ShellRegions
val intersects: a: Rect -> b: Rect -> bool
val contains: outer: Rect -> inner: Rect -> bool
val allDisjoint: regions: ShellRegions -> bool
val truncateLabel: maxChars: int -> text: string -> string
val navLabelFits: label: string -> bool
