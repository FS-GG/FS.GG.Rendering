module Feature138LayoutAttributesTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Layout

let private size: Size = { Width = 240; Height = 160 }

let private leaf id attrs : Control<unit> =
    { Kind = "text-block"
      Key = Some id
      Attributes = attrs
      Children = []
      Content = Some id
      Accessibility = None }

let private panel id attrs children : Control<unit> =
    { Kind = "panel"
      Key = Some id
      Attributes = attrs
      Children = children
      Content = None
      Accessibility = None }

let private stack attrs children : Control<unit> =
    { Kind = "stack"
      Key = Some "root"
      Attributes = attrs
      Children = children
      Content = None
      Accessibility = None }

let private row attrs children =
    stack (Attr.create "orientation" AttrCategory.Layout (TextValue "horizontal") :: attrs) children

let private rowPanel id attrs children =
    panel id (Attr.create "orientation" AttrCategory.Layout (TextValue "horizontal") :: attrs) children

let private bounds control =
    let _, b, _ = ControlInternals.evaluateLayout size control
    b

let private layoutNode control =
    let node, _, _ = ControlInternals.evaluateLayout size control
    node

let private box id control = Map.find id (bounds control)

let private expectClose actual expected message =
    Expect.floatClose Accuracy.medium actual expected message

let private horizontalGap first second =
    second.X - first.X - first.Width

let private expectUniformPadding expected actual =
    Expect.equal actual { Left = expected; Top = expected; Right = expected; Bottom = expected } "uniform padding/margin is projected"

[<Tests>]
let tests =
    testList "Feature138LayoutAttributes" [
        test "public builders emit canonical layout names and values" {
            let floatBuilders =
                [ Attr.gap 3.0, "gap", 3.0
                  Attr.flexGrow 1.5, "flexGrow", 1.5
                  Attr.flexShrink 0.0, "flexShrink", 0.0
                  Attr.flexBasis 42.0, "flexBasis", 42.0
                  Attr.minWidth 10.0, "minWidth", 10.0
                  Attr.minHeight 11.0, "minHeight", 11.0
                  Attr.maxWidth 100.0, "maxWidth", 100.0
                  Attr.maxHeight 101.0, "maxHeight", 101.0 ]

            for attr, name, value in floatBuilders do
                Expect.equal attr.Name name $"builder emits {name}"
                Expect.equal attr.Category AttrCategory.Layout $"{name} is a layout attribute"
                match attr.Value with
                | FloatValue actual -> Expect.equal actual value $"{name} carries the authored float"
                | other -> failtestf "%s carried unexpected value %A" name other

            let alignBuilders =
                [ Attr.alignItems LayoutAlign.Center, "alignItems", LayoutAlign.Center
                  Attr.alignSelf LayoutAlign.End, "alignSelf", LayoutAlign.End
                  Attr.justifyContent LayoutAlign.SpaceBetween, "justifyContent", LayoutAlign.SpaceBetween ]

            for attr, name, expected in alignBuilders do
                Expect.equal attr.Name name $"builder emits {name}"
                Expect.equal attr.Category AttrCategory.Layout $"{name} is a layout attribute"
                match attr.Value with
                | UntypedValue(:? LayoutAlign as actual) -> Expect.equal actual expected $"{name} carries the authored alignment"
                | other -> failtestf "%s carried unexpected value %A" name other
        }

        test "authored attributes project into LayoutIntent fields" {
            let root =
                row
                    [ Attr.padding 3.0
                      Attr.margin 4.0
                      Attr.gap 5.0
                      Attr.alignItems LayoutAlign.Center
                      Attr.alignSelf LayoutAlign.End
                      Attr.justifyContent LayoutAlign.SpaceAround
                      Attr.flexGrow 2.0
                      Attr.flexShrink 0.0
                      Attr.flexBasis 33.0
                      Attr.minWidth 40.0
                      Attr.minHeight 41.0
                      Attr.maxWidth 140.0
                      Attr.maxHeight 141.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ] ]
                |> layoutNode

            Expect.equal root.Intent.Direction LayoutDirection.Row "orientation still projects"
            expectUniformPadding 3.0 root.Intent.Padding
            expectUniformPadding 4.0 root.Intent.Margin
            Expect.equal root.Intent.Gap { Row = 5.0; Column = 5.0 } "gap projects to both axes"
            Expect.equal root.Intent.AlignItems LayoutAlign.Center "alignItems projects"
            Expect.equal root.Intent.AlignSelf (Some LayoutAlign.End) "alignSelf projects"
            Expect.equal root.Intent.JustifyContent LayoutAlign.SpaceAround "justifyContent projects"
            Expect.equal root.Intent.FlexGrow 2.0 "flexGrow projects"
            Expect.equal root.Intent.FlexShrink 0.0 "flexShrink projects, including explicit zero"
            Expect.equal root.Intent.FlexBasis (Some 33.0) "flexBasis projects"
            Expect.equal root.Intent.MinSize { Width = Some 40.0; Height = Some 41.0 } "min size projects"
            Expect.equal root.Intent.MaxSize { Width = Some 140.0; Height = Some 141.0 } "max size projects"
        }

        test "padding gap margin alignment and justification affect evaluated bounds" {
            let padded =
                row
                    [ Attr.width 100.0; Attr.height 50.0; Attr.padding 10.0; Attr.gap 5.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            expectClose (box "a" padded).X 10.0 "padding insets first child"
            expectClose (horizontalGap (box "a" padded) (box "b" padded)) 5.0 "gap separates children"

            let margin =
                row
                    [ Attr.width 100.0; Attr.height 50.0; Attr.padding 0.0; Attr.gap 0.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0; Attr.margin 4.0 ] ]

            expectClose (box "a" margin).X 4.0 "margin offsets child x"
            expectClose (box "a" margin).Y 4.0 "margin offsets child y"

            let aligned =
                row
                    [ Attr.width 100.0
                      Attr.height 50.0
                      Attr.padding 0.0
                      Attr.gap 0.0
                      Attr.alignItems LayoutAlign.Center
                      Attr.justifyContent LayoutAlign.End ]
                    [ leaf "a" [ Attr.width 20.0; Attr.height 10.0 ] ]

            expectClose (box "a" aligned).X (float size.Width - (box "a" aligned).Width) "justifyContent end moves child on main axis"
            expectClose (box "a" aligned).Y ((float size.Height - (box "a" aligned).Height) / 2.0) "alignItems center moves child on cross axis"
        }

        test "flex grow shrink basis and min/max constraints affect evaluated bounds" {
            let noGrow =
                row
                    [ Attr.width 120.0; Attr.height 40.0; Attr.padding 0.0; Attr.gap 0.0 ]
                    [ panel "a" [ Attr.flexBasis 20.0 ] [ leaf "a.text" [ Attr.width 1.0; Attr.height 1.0 ] ]
                      panel "b" [ Attr.flexBasis 20.0 ] [ leaf "b.text" [ Attr.width 1.0; Attr.height 1.0 ] ] ]

            let flexed =
                row
                    [ Attr.width 120.0; Attr.height 40.0; Attr.padding 0.0; Attr.gap 0.0 ]
                    [ panel "a" [ Attr.flexBasis 20.0; Attr.flexGrow 1.0 ] [ leaf "a.text" [ Attr.width 1.0; Attr.height 1.0 ] ]
                      panel "b" [ Attr.flexBasis 20.0; Attr.flexGrow 3.0 ] [ leaf "b.text" [ Attr.width 1.0; Attr.height 1.0 ] ] ]

            Expect.notEqual (box "a" flexed).Width (box "a" noGrow).Width "flexGrow changes first child width"
            Expect.notEqual (box "b" flexed).Width (box "b" noGrow).Width "flexGrow changes second child width"

            let shrink =
                panel
                    "outer"
                    [ Attr.width 120.0; Attr.height 40.0 ]
                    [ rowPanel
                          "inner"
                          [ Attr.width 50.0; Attr.height 40.0; Attr.padding 0.0; Attr.gap 0.0 ]
                          [ panel "a" [ Attr.flexBasis 40.0; Attr.flexShrink 1.0 ] [ leaf "a.text" [ Attr.width 1.0; Attr.height 1.0 ] ]
                            panel "b" [ Attr.flexBasis 40.0; Attr.flexShrink 3.0 ] [ leaf "b.text" [ Attr.width 1.0; Attr.height 1.0 ] ] ] ]

            Expect.isGreaterThan (box "a" shrink).Width (box "b" shrink).Width "larger flexShrink yields the smaller box"

            let clamped =
                row
                    [ Attr.width 160.0; Attr.height 40.0; Attr.padding 0.0; Attr.gap 0.0 ]
                    [ panel "min" [ Attr.flexBasis 20.0; Attr.minWidth 40.0 ] [ leaf "min.text" [ Attr.width 1.0; Attr.height 1.0 ] ]
                      panel "max" [ Attr.flexBasis 80.0; Attr.maxWidth 30.0 ] [ leaf "max.text" [ Attr.width 1.0; Attr.height 1.0 ] ] ]

            Expect.isGreaterThanOrEqual (box "min" clamped).Width 40.0 "minWidth clamps up"
            Expect.isLessThanOrEqual (box "max" clamped).Width 30.0 "maxWidth clamps down"
        }

        test "omitted values preserve compatibility defaults and explicit zero overrides them" {
            let omitted =
                row
                    [ Attr.width 100.0; Attr.height 40.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            expectClose (box "a" omitted).X 8.0 "omitted padding keeps compatibility inset"
            expectClose (horizontalGap (box "a" omitted) (box "b" omitted)) 8.0 "omitted gap keeps compatibility spacing"

            let authoredDefaults =
                row
                    [ Attr.width 100.0; Attr.height 40.0; Attr.padding 8.0; Attr.gap 8.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            Expect.equal (bounds authoredDefaults) (bounds omitted) "authoring compatibility defaults keeps bounds unchanged"

            let zeroed =
                row
                    [ Attr.width 100.0; Attr.height 40.0; Attr.padding 0.0; Attr.gap 0.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            expectClose (box "a" zeroed).X 0.0 "explicit zero padding overrides compatibility inset"
            expectClose (horizontalGap (box "a" zeroed) (box "b" zeroed)) 0.0 "explicit zero gap overrides compatibility spacing"
        }

        test "last writer wins across canonical gap and spacing alias" {
            let spacingLast =
                row
                    [ Attr.width 100.0
                      Attr.height 40.0
                      Attr.padding 0.0
                      Attr.gap 1.0
                      Attr.create "spacing" AttrCategory.Layout (FloatValue 7.0) ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            let gapLast =
                row
                    [ Attr.width 100.0
                      Attr.height 40.0
                      Attr.padding 0.0
                      Attr.create "spacing" AttrCategory.Layout (FloatValue 7.0)
                      Attr.gap 1.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ]
                      leaf "b" [ Attr.width 10.0; Attr.height 10.0 ] ]

            expectClose (horizontalGap (box "a" spacingLast) (box "b" spacingLast)) 7.0 "spacing alias can win as the last gap writer"
            expectClose (horizontalGap (box "a" gapLast) (box "b" gapLast)) 1.0 "canonical gap can win as the last gap writer"

            let paddingLast =
                row
                    [ Attr.width 100.0; Attr.height 40.0; Attr.padding 0.0; Attr.padding 12.0 ]
                    [ leaf "a" [ Attr.width 10.0; Attr.height 10.0 ] ]

            expectClose (box "a" paddingLast).X 12.0 "canonical repeated attributes remain last-writer-wins"
        }
    ]
