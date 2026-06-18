module AntShowcase.Core.ShellLayout

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

let calculate (size: Size): ShellRegions =
    let width = float size.Width
    let height = float size.Height
    let topHeight = 72.0
    let statusHeight = 34.0
    let feedbackHeight = if size.Height <= 800 then 86.0 else 118.0
    let navWidth = if size.Width <= 1280 then 236.0 else 276.0
    let contentHeight = height - topHeight - feedbackHeight - statusHeight
    { TopBar = { X = 0.0; Y = 0.0; Width = width; Height = topHeight }
      Navigation = { X = 0.0; Y = topHeight; Width = navWidth; Height = contentHeight }
      Content = { X = navWidth; Y = topHeight; Width = width - navWidth; Height = contentHeight }
      Feedback = { X = 0.0; Y = topHeight + contentHeight; Width = width; Height = feedbackHeight }
      Status = { X = 0.0; Y = height - statusHeight; Width = width; Height = statusHeight } }

let intersects (a: Rect) (b: Rect): bool =
    a.X < b.X + b.Width
    && a.X + a.Width > b.X
    && a.Y < b.Y + b.Height
    && a.Y + a.Height > b.Y

let contains (outer: Rect) (inner: Rect): bool =
    inner.X >= outer.X
    && inner.Y >= outer.Y
    && inner.X + inner.Width <= outer.X + outer.Width
    && inner.Y + inner.Height <= outer.Y + outer.Height

let allDisjoint (regions: ShellRegions): bool =
    let rects = [ regions.TopBar; regions.Navigation; regions.Content; regions.Feedback; regions.Status ]
    rects
    |> List.mapi (fun index rect -> rects |> List.skip (index + 1) |> List.map (fun other -> intersects rect other))
    |> List.concat
    |> List.forall not

let truncateLabel (maxChars: int) (text: string): string =
    if maxChars <= 1 then ""
    elif text.Length <= maxChars then text
    else text.Substring(0, maxChars - 1).TrimEnd() + "…"

let navLabelFits (label: string): bool =
    label.Length <= 28
