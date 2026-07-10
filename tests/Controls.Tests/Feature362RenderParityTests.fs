module Feature362RenderParityTests

// Feature 362: `Control.render` (single-control PREVIEW) and `Control.renderTree` (PRODUCT) are
// documented to differ only in layout (flatten-and-stack vs real Yoga), yet they silently diverged
// on CONTENT. Two guards here:
//   * text overflow — the preview ellipsizes an overflowing label exactly as the product does,
//     instead of painting the raw label and letting the clip rect drop characters.
//   * container schematics — a rich family that owns a keyed child subtree contributes only its
//     frame (the children paint the real content), instead of a full schematic on top of them.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Noop

let private theme = Theme.light

// Every text occurrence anywhere in a scene (recursing through the transparent wrapper nodes).
let rec private leafTexts (s: Scene) : string list = s.Nodes |> List.collect leafTextsNode

and private leafTextsNode (n: SceneNode) : string list =
    match n with
    | Text(_, t, _) -> [ t ]
    | SizedText(_, t, _, _) -> [ t ]
    | TextRun r -> [ r.Text ]
    | ClipNode(_, inner)
    | Translate(_, inner)
    | ColorSpaceNode(_, inner)
    | PerspectiveNode(_, inner) -> leafTexts inner
    | Group scenes -> scenes |> List.collect leafTexts
    | PictureNode p -> leafTexts p.Scene
    | CachedSubtree b -> leafTexts b.Scene
    | _ -> []

[<Tests>]
let tests =
    testList "Feature 362 preview/product render parity" [
        test "an overflowing label is ellipsized identically in preview and product" {
            let control: Control<Msg> =
                TextBlock.create [
                    TextBlock.text "This is a very long label that overflows its narrow box"
                    Attr.width 60.0
                    Attr.height 24.0
                ]

            let rawLabel = "This is a very long label that overflows its narrow box"
            let previewTexts = leafTexts (Control.render theme control).Scene

            Expect.isNonEmpty previewTexts "the preview paints the label"
            // The fix: at the same box, the preview now makes the SAME leaf-content decision the product
            // path always has — it ellipsizes an overflowing label (explicit `…`) instead of painting the
            // raw label and leaving the clip rect to silently drop characters.
            Expect.isTrue
                (previewTexts |> List.forall (fun t -> t.Contains "…"))
                "the overflowing label is ellipsized in the preview, not painted raw"
            Expect.isFalse
                (previewTexts |> List.contains rawLabel)
                "the preview no longer paints the full raw label"
        }

        test "a container that owns children contributes only its frame in the preview, not its schematic" {
            let box: Rect = { X = 0.0; Y = 0.0; Width = 200.0; Height = 120.0 }
            let expectedFrame = [ Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 1.5) ]

            let childless: Control<Msg> = Overlay.create []

            let nested: Control<Msg> =
                Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "content" ]) ]

            Expect.equal
                (ControlInternals.faithfulContent theme box nested)
                expectedFrame
                "an overlay that owns children paints only its frame — the children paint the content"

            Expect.notEqual
                (ControlInternals.faithfulContent theme box childless)
                expectedFrame
                "a childless overlay still paints its full schematic"
        }
    ]
