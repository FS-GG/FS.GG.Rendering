module Feature138ShellChromeTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private leaf id h : Control<unit> =
    { Kind = "text-block"
      Key = Some id
      Attributes = [ Attr.height h; Attr.width 1.0 ]
      Children = []
      Content = Some id
      Accessibility = None }

let private panel id attrs children : Control<unit> =
    { Kind = "panel"
      Key = Some id
      Attributes = Attr.padding 0.0 :: Attr.gap 0.0 :: attrs
      Children = children
      Content = None
      Accessibility = None }

let private row id attrs children : Control<unit> =
    panel id (Attr.create "orientation" AttrCategory.Layout (TextValue "horizontal") :: attrs) children

let private shell (viewport: Size) (contentHeight: float) : Control<unit> =
    let w = float viewport.Width
    let h = float viewport.Height
    let headerH = 48.0
    let footerH = 32.0
    let navW = 96.0
    let bodyH = h - headerH - footerH

    panel
        "root"
        [ Attr.width w; Attr.height h ]
        [ panel "header" [ Attr.width w; Attr.height headerH; Attr.flexShrink 0.0 ] [ leaf "header.text" 12.0 ]
          row
              "body"
              [ Attr.flexGrow 1.0; Attr.flexShrink 1.0; Attr.minHeight 0.0; Attr.maxHeight bodyH ]
              [ panel "nav" [ Attr.width navW; Attr.flexShrink 0.0 ] [ leaf "nav.text" 12.0 ]
                panel
                    "content"
                    [ Attr.flexGrow 1.0
                      Attr.flexShrink 1.0
                      Attr.minWidth 0.0
                      Attr.minHeight 0.0
                      Attr.maxWidth (w - navW)
                      Attr.maxHeight bodyH ]
                    [ leaf "content.tall" contentHeight ] ]
          panel "footer" [ Attr.width w; Attr.height footerH; Attr.flexShrink 0.0 ] [ leaf "footer.text" 12.0 ] ]

let private bounds viewport contentHeight =
    let _, b, _ = ControlInternals.evaluateLayout viewport (shell viewport contentHeight)
    b

let private box id b = Map.find id b

let private expectClose actual expected message =
    Expect.floatClose Accuracy.medium actual expected message

let private assertShell viewport contentHeight =
    let b = bounds viewport contentHeight
    let w = float viewport.Width
    let h = float viewport.Height
    let headerH = 48.0
    let footerH = 32.0
    let navW = 96.0
    let bodyH = h - headerH - footerH

    expectClose (box "header" b).Height headerH "header keeps fixed height"
    expectClose (box "header" b).Y 0.0 "header remains at top"
    expectClose (box "footer" b).Height footerH "footer keeps fixed height"
    expectClose (box "footer" b).Y (h - footerH) "footer remains visible at bottom"
    expectClose (box "body" b).Y headerH "body starts below header"
    expectClose (box "body" b).Height bodyH "body receives remaining height"
    expectClose (box "nav" b).Width navW "navigation keeps fixed width"
    expectClose (box "content" b).X navW "content starts after navigation"
    expectClose (box "content" b).Width (w - navW) "content receives remaining width"
    Expect.isLessThanOrEqual (box "content" b).Height bodyH "content remains bounded"

[<Tests>]
let tests =
    testList "Feature138ShellChrome" [
        test "fixed chrome and flexible content at 640x480" {
            assertShell { Width = 640; Height = 480 } 120.0
        }

        test "fixed chrome and flexible content at 400x300" {
            assertShell { Width = 400; Height = 300 } 120.0
        }

        test "tall content stays bounded while chrome remains visible at 640x480 and 400x300" {
            assertShell { Width = 640; Height = 480 } 1200.0
            assertShell { Width = 400; Height = 300 } 1200.0
        }
    ]
