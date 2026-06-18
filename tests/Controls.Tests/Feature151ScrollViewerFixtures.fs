module Feature151ScrollViewerFixtures

open System
open FS.GG.UI.Controls
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type Msg = Noop

type ScrollCase =
    { CaseId: string
      ScrollViewerId: ControlId
      Control: Control<Msg>
      ExpectedVerticalOverflow: bool
      DynamicControl: Control<Msg> option }

let theme = Theme.light

let viewport: Size = { Width = 240; Height = 120 }

let row index =
    TextBlock.create
        [ Attr.height 20.0
          TextBlock.text (sprintf "row %02d" index) ]

let rows count = [ for index in 1..count -> row index ]

let textBlock text width height =
    TextBlock.create
        [ Attr.width width
          Attr.height height
          TextBlock.text text ]

let stack children = Stack.create [ Stack.children children ]

let scrollViewer id children =
    Control.create "scroll-viewer" [ Attr.children [ stack children ] ] |> Control.withKey id

let emptyScrollViewer id =
    Control.create "scroll-viewer" [] |> Control.withKey id

let nestedScrollViewer () =
    scrollViewer "sv-nested-scroll" [ scrollViewer "sv-inner" (rows 12) ]

let clippedParent () =
    Stack.create
        [ Attr.height 100.0
          Stack.children [ scrollViewer "sv-clipped-parent" (rows 12) ] ]

let layeredParent () =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "header" ]
                Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "layer" ]); Attr.selected true ]
                scrollViewer "sv-layered-parent" (rows 10) ] ]

let invalidIntrinsicFallback () =
    scrollViewer "sv-invalid-intrinsic-fallback" [ textBlock "invalid" Double.NaN 20.0 ]

let cases =
    [ { CaseId = "empty-content"
        ScrollViewerId = "sv-empty-content"
        Control = emptyScrollViewer "sv-empty-content"
        ExpectedVerticalOverflow = false
        DynamicControl = None }
      { CaseId = "smaller-than-viewport"
        ScrollViewerId = "sv-smaller-than-viewport"
        Control = scrollViewer "sv-smaller-than-viewport" [ textBlock "small" 40.0 20.0 ]
        ExpectedVerticalOverflow = false
        DynamicControl = None }
      { CaseId = "exact-fit"
        ScrollViewerId = "sv-exact-fit"
        Control = scrollViewer "sv-exact-fit" (rows 4)
        ExpectedVerticalOverflow = false
        DynamicControl = None }
      { CaseId = "barely-overflowing"
        ScrollViewerId = "sv-barely-overflowing"
        Control = scrollViewer "sv-barely-overflowing" (rows 8)
        ExpectedVerticalOverflow = true
        DynamicControl = None }
      { CaseId = "substantially-overflowing"
        ScrollViewerId = "sv-substantially-overflowing"
        Control = scrollViewer "sv-substantially-overflowing" (rows 20)
        ExpectedVerticalOverflow = true
        DynamicControl = None }
      { CaseId = "nested-scroll"
        ScrollViewerId = "sv-nested-scroll"
        Control = nestedScrollViewer ()
        ExpectedVerticalOverflow = false
        DynamicControl = None }
      { CaseId = "clipped-parent"
        ScrollViewerId = "sv-clipped-parent"
        Control = clippedParent ()
        ExpectedVerticalOverflow = true
        DynamicControl = None }
      { CaseId = "layered-parent"
        ScrollViewerId = "sv-layered-parent"
        Control = layeredParent ()
        ExpectedVerticalOverflow = true
        DynamicControl = None }
      { CaseId = "text-natural-size"
        ScrollViewerId = "sv-text-natural-size"
        Control = scrollViewer "sv-text-natural-size" [ textBlock "natural text" 180.0 28.0; textBlock "second line" 180.0 28.0 ]
        ExpectedVerticalOverflow = false
        DynamicControl = None }
      { CaseId = "dynamic-content-change"
        ScrollViewerId = "sv-dynamic-content-change"
        Control = scrollViewer "sv-dynamic-content-change" (rows 2)
        ExpectedVerticalOverflow = false
        DynamicControl = Some(scrollViewer "sv-dynamic-content-change" (rows 16)) }
      { CaseId = "invalid-intrinsic-fallback"
        ScrollViewerId = "sv-invalid-intrinsic-fallback"
        Control = invalidIntrinsicFallback ()
        ExpectedVerticalOverflow = false
        DynamicControl = None } ]

let render control = Control.renderTree theme viewport control

let viewportOf item =
    let rendered = render item.Control
    Control.scrollViewport rendered item.ScrollViewerId

let changedViewportOf item =
    item.DynamicControl
    |> Option.bind (fun control -> Control.scrollViewport (render control) item.ScrollViewerId)
