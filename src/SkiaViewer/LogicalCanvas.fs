namespace FS.GG.UI.SkiaViewer

open FS.GG.UI.Scene

type LogicalCanvasFit =
    { Scale: float
      OffsetX: float
      OffsetY: float }

[<RequireQualifiedAccess>]
module LogicalCanvas =

    let private identityFit = { Scale = 1.0; OffsetX = 0.0; OffsetY = 0.0 }

    let private isIdentity (fit: LogicalCanvasFit) =
        fit.Scale = 1.0 && fit.OffsetX = 0.0 && fit.OffsetY = 0.0

    let fit (logical: Size) (actual: Size) : LogicalCanvasFit =
        if logical.Width <= 0 || logical.Height <= 0 || actual.Width <= 0 || actual.Height <= 0 then
            identityFit
        else
            let logicalWidth = float logical.Width
            let logicalHeight = float logical.Height
            let actualWidth = float actual.Width
            let actualHeight = float actual.Height
            // Uniform: the smaller axis ratio wins, so the logical canvas fits whole and the
            // surplus on the other axis becomes a bar.
            let scale = min (actualWidth / logicalWidth) (actualHeight / logicalHeight)

            { Scale = scale
              OffsetX = (actualWidth - logicalWidth * scale) / 2.0
              OffsetY = (actualHeight - logicalHeight * scale) / 2.0 }

    let present (logical: Size) (actual: Size) (node: SceneNode) : SceneNode =
        let fitted = fit logical actual

        let canvas =
            { X = 0.0
              Y = 0.0
              Width = float logical.Width
              Height = float logical.Height }

        // The clip is unconditional: "a product cannot draw outside its logical canvas" must not be a
        // fact about the current window size. At the identity fit it is a visual no-op (the canvas
        // already covers the surface), so an unscaled render stays pixel-identical either way.
        let clipped = ClipNode(RectClip canvas, { Nodes = [ node ] })

        if isIdentity fitted then
            clipped
        else
            let transform =
                Transform.toPerspectiveTransform
                    { Transform.identity with
                        ScaleX = fitted.Scale
                        ScaleY = fitted.Scale
                        TranslateX = fitted.OffsetX
                        TranslateY = fitted.OffsetY }

            // The clip sits INSIDE the transform, so it is expressed in logical coordinates and
            // lands on the letterboxed rect once the matrix applies.
            PerspectiveNode(transform, { Nodes = [ clipped ] })

    let toLogicalPoint (logical: Size) (actual: Size) (x: float) (y: float) : float * float =
        // `fit` never yields a zero scale: a degenerate extent gives the identity, and every other
        // case is a min of two strictly positive ratios.
        let fitted = fit logical actual
        ((x - fitted.OffsetX) / fitted.Scale, (y - fitted.OffsetY) / fitted.Scale)

    /// Issue #400: scale a pointer sample from the LOGICAL window space Silk delivers it in
    /// (`IMouse.Position`, sized in `window.Size` points) into the PHYSICAL framebuffer space a
    /// product rendered at native resolution reasons in. Per-axis so a non-square DPI still maps
    /// correctly; the identity when `window` and `framebuffer` match (scale 1) or either is degenerate.
    let toPhysicalPoint (window: Size) (framebuffer: Size) (x: float) (y: float) : float * float =
        if window.Width <= 0 || window.Height <= 0 || framebuffer.Width <= 0 || framebuffer.Height <= 0 then
            (x, y)
        else
            (x * float framebuffer.Width / float window.Width, y * float framebuffer.Height / float window.Height)
