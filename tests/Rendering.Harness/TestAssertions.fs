namespace Rendering.Harness

open FS.GG.UI.Scene

/// Feature 136 (T006): shared, pure scene-assertion helpers for the US1–US4 semantic tests
/// (glyph correctness, measure/advance agreement, no-overprint, region non-overlap, container
/// clipping, overlay z-order). Depends ONLY on `FS.GG.UI.Scene`, so test projects can **link** this
/// single file (`<Compile Include="..\Rendering.Harness\TestAssertions.fs"><Link>…</Link>`) without
/// taking a project reference on the GLFW/OpenGL harness Exe. The canonical copy lives here in
/// `Rendering.Harness` (tasks.md T006).
module TestAssertions =

    /// A text occurrence extracted from a rendered scene: the drawn string, its anchor in absolute
    /// coordinates (after `Translate` accumulation) and its font size where the node carries one.
    type RenderedText =
        { Text: string
          X: float
          Y: float
          Size: float }

    /// Do two axis-aligned rects overlap with strictly positive area? Edges that merely touch
    /// (shared boundary, zero overlap area) do NOT count as an overlap.
    let rectsOverlap (a: Rect) (b: Rect) : bool =
        a.X < b.X + b.Width
        && b.X < a.X + a.Width
        && a.Y < b.Y + b.Height
        && b.Y < a.Y + a.Height

    /// Every pair of rects that overlaps (for failure diagnostics). O(n²) — fine for the small
    /// control/region sets these tests build.
    let overlappingPairs (rects: Rect list) : (Rect * Rect) list =
        let arr = List.toArray rects

        [ for i in 0 .. arr.Length - 1 do
              for j in i + 1 .. arr.Length - 1 do
                  if rectsOverlap arr.[i] arr.[j] then
                      yield arr.[i], arr.[j] ]

    /// Does any pair in the list overlap?
    let anyOverlap (rects: Rect list) : bool = overlappingPairs rects |> List.isEmpty |> not

    /// Is `inner` fully contained within `outer` (with a small slack tolerance)?
    let containedIn (tol: float) (outer: Rect) (inner: Rect) : bool =
        inner.X >= outer.X - tol
        && inner.Y >= outer.Y - tol
        && inner.X + inner.Width <= outer.X + outer.Width + tol
        && inner.Y + inner.Height <= outer.Y + outer.Height + tol

    // Walk a scene accumulating Translate offsets, collecting every text occurrence in absolute coords.
    let rec private collectText dx dy (scene: Scene) : RenderedText list =
        scene.Nodes |> List.collect (collectTextNode dx dy)

    and private collectTextNode dx dy node : RenderedText list =
        match node with
        | Text((x, y), text, _) -> [ { Text = text; X = x + dx; Y = y + dy; Size = 24.0 } ]
        | SizedText((x, y), text, size, _) -> [ { Text = text; X = x + dx; Y = y + dy; Size = size } ]
        | TextRun run ->
            [ { Text = run.Text
                X = run.Position.X + dx
                Y = run.Position.Y + dy
                Size = run.Font.Size } ]
        | GlyphRun run ->
            [ { Text = run.Data.Text
                X = run.Position.X + dx
                Y = run.Position.Y + dy
                Size = run.Data.Font.Size } ]
        | Group scenes -> scenes |> List.collect (collectText dx dy)
        | Translate((tx, ty), s) -> collectText (dx + tx) (dy + ty) s
        | ClipNode(_, s) -> collectText dx dy s
        | ColorSpaceNode(_, s) -> collectText dx dy s
        | PerspectiveNode(_, s) -> collectText dx dy s
        | PictureNode p -> collectText dx dy p.Scene
        | CachedSubtree b -> collectText dx dy b.Scene
        | _ -> []

    /// Every rendered text occurrence in the scene (absolute coords, document order).
    let renderedText (scene: Scene) : RenderedText list = collectText 0.0 0.0 scene

    /// Just the drawn strings, in document order.
    let renderedGlyphs (scene: Scene) : string list = renderedText scene |> List.map (fun t -> t.Text)

    // Walk a scene accumulating Translate offsets, collecting the absolute bounds of every node that
    // paints a filled region (rects, ellipses, images, regions). Text/line/path bounds are excluded —
    // text overlap is judged via `renderedText`.
    let rec private collectBounds dx dy (scene: Scene) : Rect list =
        scene.Nodes |> List.collect (collectBoundsNode dx dy)

    and private collectBoundsNode dx dy node : Rect list =
        let shift (r: Rect) = { r with X = r.X + dx; Y = r.Y + dy }

        match node with
        | Rectangle((x, y, w, h), _) -> [ shift { X = x; Y = y; Width = w; Height = h } ]
        | PaintedRectangle(r, _) -> [ shift r ]
        | FilledEllipse(r, _) -> [ shift r ]
        | Ellipse(r, _) -> [ shift r ]
        | Image((x, y, w, h), _) -> [ shift { X = x; Y = y; Width = w; Height = h } ]
        | RegionNode(region, _) -> region.Bounds |> List.map shift
        | Group scenes -> scenes |> List.collect (collectBounds dx dy)
        | Translate((tx, ty), s) -> collectBounds (dx + tx) (dy + ty) s
        | ClipNode(_, s) -> collectBounds dx dy s
        | ColorSpaceNode(_, s) -> collectBounds dx dy s
        | PerspectiveNode(_, s) -> collectBounds dx dy s
        | PictureNode p -> collectBounds dx dy p.Scene
        | CachedSubtree b -> collectBounds dx dy b.Scene
        | _ -> []

    /// Absolute bounds of every filled-region node in the scene.
    let drawnBounds (scene: Scene) : Rect list = collectBounds 0.0 0.0 scene

    /// Theme-pair runner: produce `render`'s result under two themes (e.g. antLight and antDark). The
    /// themes are supplied by the caller so this module stays Scene-only. Returns both so a failing
    /// test can display each side.
    let themePair (render: 'theme -> 'r) (light: 'theme) (dark: 'theme) : 'r * 'r = render light, render dark

    /// True iff `render` yields `equate`-equal results under both themes (theme-invariance oracle).
    let themeInvariant (render: 'theme -> 'r) (equate: 'r -> 'r -> bool) (light: 'theme) (dark: 'theme) : bool =
        let l, d = themePair render light dark
        equate l d

    /// Feature 146 helper: package identities are stable SHA-256 tokens.
    let packageIdentityLooksSha256 (identity: string) : bool =
        identity.StartsWith("sha256:", System.StringComparison.Ordinal)
        && identity.Length = "sha256:".Length + 64
        && identity.Substring("sha256:".Length)
           |> Seq.forall (fun ch -> System.Uri.IsHexDigit ch)

    /// Feature 146 helper: find a package diagnostic by stage and message fragment.
    let hasPackageDiagnostic stage (messageFragment: string) (diagnostics: PackageDiagnostic list) : bool =
        diagnostics
        |> List.exists (fun diagnostic ->
            diagnostic.Stage = stage
            && diagnostic.Message.Contains(messageFragment, System.StringComparison.OrdinalIgnoreCase))

    /// Feature 146 helper: compact artifact metadata check for readiness outputs.
    let artifactMetadataComplete (path: string option) (identity: string option) : bool =
        match path, identity with
        | Some value, Some hash ->
            not (System.String.IsNullOrWhiteSpace value)
            && packageIdentityLooksSha256 hash
        | _ -> false

    /// Feature 147 helper: all readiness paths for the compositor package must stay under the feature
    /// readiness directory so generated artifacts cannot masquerade as unrelated evidence.
    let feature147ReadinessPath (path: string) : bool =
        path.Replace('\\', '/').StartsWith("specs/147-compositor-damage-redraw/readiness/", System.StringComparison.Ordinal)

    /// Feature 147 helper: an accepted parity verdict must be the explicit passed token.
    let feature147ParityPassed (token: string) : bool =
        System.String.Equals(token, "passed", System.StringComparison.Ordinal)

    /// Feature 147 helper: readiness can be claimed only by the explicit ready token.
    let feature147ReadyTier (token: string) : bool =
        System.String.Equals(token, "ready", System.StringComparison.Ordinal)
