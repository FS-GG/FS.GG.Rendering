namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

/// Feature 189 (US1, FR-002): chart `*Geom` producers + the shared `emptyState`/`pillGeom`/`normIndexed`
/// helpers + the `withPoints` empty-guard combinator, relocated verbatim from `ControlInternals`.
/// `module internal`; opens `ControlPrimitives` for the shared drawing/palette helpers. Byte-identical.
module internal ChartGeometry =
    open ControlPrimitives
    /// Honest empty state (FR-011): a faint frame + a "(no data)" caption within bounds, so an
    /// empty/missing-data control reads as a recognizable empty control, never an off-canvas blank.
    let emptyState (theme: Theme) (box: Rect) (caption: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + 8.0) (box.Y + box.Height * 0.5) 12.0 theme.Muted caption ]

    // ---- chart geometry ---------------------------------------------------------------------

    let normIndexed (box: Rect) (pts: ChartPoint list) : Point list =
        match pts with
        | [] -> []
        | _ ->
            let ys = pts |> List.map (fun p -> p.Y)
            let minY = min 0.0 (List.min ys)
            let maxY = List.max ys
            let span = if maxY - minY < 1e-9 then 1.0 else maxY - minY
            let n = List.length pts
            pts
            |> List.mapi (fun i p ->
                let fx = if n <= 1 then 0.5 else float i / float (n - 1)
                let fy = (p.Y - minY) / span
                { X = box.X + fx * box.Width; Y = box.Y + box.Height - fy * box.Height })

    /// Feature 189 (US1, FR-002 / data-model C3): the shared empty-points guard. Collapses the
    /// repeated `match pts with [] -> emptyState theme box caption | nonEmpty -> body` skeleton into
    /// one combinator; divergent bodies stay in `body` (feature-180/181 lesson — skeleton only, the
    /// `normIndexed`-scrutinee, `[] | [ _ ]`, and value-only geoms keep their own guards). Routing a
    /// `match pts with [] -> emptyState theme box "(no data)" | _ -> body` through this is byte-identical.
    let withPoints theme (box: Rect) (caption: string) (pts: ChartPoint list) (body: ChartPoint list -> Scene list) : Scene list =
        match pts with
        | [] -> emptyState theme box caption
        | nonEmpty -> body nonEmpty

    let lineGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match normIndexed box pts with
        | [] -> emptyState theme box "(no data)"
        | (head :: _) as ps ->
            let baseY = box.Y + box.Height
            let areaCmds =
                Path.moveTo head.X baseY
                :: (ps |> List.map (fun p -> Path.lineTo p.X p.Y))
                @ [ Path.lineTo (List.last ps).X baseY; Path.close ]
            let area = Scene.path (Path.create Winding areaCmds) (Paint.withOpacity 0.22 (Paint.fill theme.Accent))
            let lineCmds = Path.moveTo head.X head.Y :: (List.tail ps |> List.map (fun p -> Path.lineTo p.X p.Y))
            let stroke = Scene.path (Path.create Winding lineCmds) (Paint.stroke theme.Accent 3.0)
            let dots = ps |> List.map (fun p -> Scene.circle p 3.5 theme.Accent)
            area :: stroke :: dots

    let barGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            let n = List.length pts
            let gap = 6.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            pts
            |> List.mapi (fun i p ->
                let h = (max 0.0 p.Y / maxY) * box.Height
                let bx = box.X + float i * (bw + gap)
                Scene.rectangle (bx, box.Y + box.Height - h, bw, h) (colorAt theme i))

    let pieGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let total = pts |> List.sumBy (fun p -> max 0.0 p.Y)
            let total = if total < 1e-9 then 1.0 else total
            let r = (min box.Width box.Height) / 2.0 - 2.0
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let bounds: Rect = { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r }
            pts
            |> List.indexed
            |> List.fold
                (fun (start, acc) (i, p) ->
                    let sweep = (max 0.0 p.Y / total) * 360.0
                    start + sweep, Scene.arc bounds start sweep (Paint.fill (colorAt theme i)) :: acc)
                (-90.0, [])
            |> snd
            |> List.rev

    /// L-shaped axes (left + bottom) so a sparse point cloud reads as a plotted chart, not
    /// scattered dots floating on the canvas.
    let axes theme (box: Rect) : Scene list =
        [ Scene.line { X = box.X; Y = box.Y } { X = box.X; Y = box.Y + box.Height } (Paint.stroke theme.Foreground 1.5)
          Scene.line { X = box.X; Y = box.Y + box.Height } { X = box.X + box.Width; Y = box.Y + box.Height } (Paint.stroke theme.Foreground 1.5) ]

    let scatterGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            // Inset the plot area so axes and edge points stay inside the canvas.
            let plot: Rect = { X = box.X + 6.0; Y = box.Y + 4.0; Width = box.Width - 12.0; Height = box.Height - 12.0 }
            let xs = pts |> List.map (fun p -> p.X)
            let ys = pts |> List.map (fun p -> p.Y)
            let minX, maxX = List.min xs, List.max xs
            let minY, maxY = List.min ys, List.max ys
            let sx = if maxX - minX < 1e-9 then 1.0 else maxX - minX
            let sy = if maxY - minY < 1e-9 then 1.0 else maxY - minY
            let dots =
                pts
                |> List.map (fun p ->
                    let cx = plot.X + (p.X - minX) / sx * plot.Width
                    let cy = plot.Y + plot.Height - (p.Y - minY) / sy * plot.Height
                    Scene.circle { X = cx; Y = cy } 5.5 theme.Accent)
            axes theme plot @ dots

    let graphGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let r = (min box.Width box.Height) / 2.0 - 12.0
            let positions =
                pts
                |> List.mapi (fun i _ ->
                    let a = float i / float n * 2.0 * System.Math.PI - System.Math.PI / 2.0
                    { X = cx + r * cos a; Y = cy + r * sin a })
            let edges =
                (positions @ [ List.head positions ])
                |> List.pairwise
                |> List.map (fun (a, b) -> Scene.line a b (Paint.stroke theme.Foreground 2.0))
            let nodes = positions |> List.map (fun p -> Scene.circle p 8.0 theme.Accent)
            edges @ nodes

    // ---- Feature 133 (D2C.1) net-new chart geometry -----------------------------------------
    // Every function is a pure schematic built from existing Scene primitives, coloured ONLY from
    // theme-role values via `chartPalette`/`chartColorAt`/`lerpColor` (no inline hex, no theme
    // identity branch). Empty/degenerate data resolves to the honest `emptyState` so the resolver
    // stays total (FR-007, spec Edge Cases).

    /// Scaled bar heights over [0, max] for a point list — shared by column/histogram/waterfall.
    let scaledBars (box: Rect) (pts: ChartPoint list) : (float * float) list =
        let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
        pts |> List.map (fun p -> p.Y, (max 0.0 p.Y / maxY) * box.Height)

    /// `area-chart` — a filled region under the series outline (distinct, heavier fill than line).
    let areaGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match normIndexed box pts with
        | [] -> emptyState theme box "(no data)"
        | (head :: _) as ps ->
            let baseY = box.Y + box.Height
            let areaCmds =
                Path.moveTo head.X baseY
                :: (ps |> List.map (fun p -> Path.lineTo p.X p.Y))
                @ [ Path.lineTo (List.last ps).X baseY; Path.close ]
            let area = Scene.path (Path.create Winding areaCmds) (Paint.withOpacity 0.38 (Paint.fill theme.Accent))
            let lineCmds = Path.moveTo head.X head.Y :: (List.tail ps |> List.map (fun p -> Path.lineTo p.X p.Y))
            let stroke = Scene.path (Path.create Winding lineCmds) (Paint.stroke theme.Accent 2.5)
            axes theme box @ [ area; stroke ]

    /// `column-chart` — vertical bars in the categorical palette.
    let columnGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let gap = 6.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            scaledBars box pts
            |> List.mapi (fun i (_, h) ->
                let bx = box.X + float i * (bw + gap)
                Scene.rectangle (bx, box.Y + box.Height - h, bw, h) (chartColorAt theme i))

    /// `histogram` — adjacent (gapless) frequency bars in a single accent fill, hairline-separated.
    let histogramGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let bw = box.Width / float n
            scaledBars box pts
            |> List.mapi (fun i (_, h) ->
                let bx = box.X + float i * bw
                [ Scene.rectangle (bx, box.Y + box.Height - h, bw, h) theme.Accent
                  Scene.rectangleWithPaint { X = bx; Y = box.Y + box.Height - h; Width = bw; Height = h } (Paint.stroke theme.Background 1.0) ])
            |> List.collect id

    /// `box-plot` — a box-and-whisker schematic per category (box around the value, median + whiskers).
    let boxPlotGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let gap = 10.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            scaledBars box pts
            |> List.mapi (fun i (_, h) ->
                let cx = box.X + float i * (bw + gap) + bw / 2.0
                let half = bw / 2.0
                let median = box.Y + box.Height - h
                let boxTop = max box.Y (median - box.Height * 0.14)
                let boxBot = min (box.Y + box.Height) (median + box.Height * 0.14)
                [ Scene.line { X = cx; Y = boxTop - 10.0 } { X = cx; Y = boxBot + 10.0 } (Paint.stroke theme.Muted 1.5)
                  Scene.rectangleWithPaint { X = cx - half; Y = boxTop; Width = bw; Height = boxBot - boxTop } (Paint.fill (chartColorAt theme i))
                  Scene.line { X = cx - half; Y = median } { X = cx + half; Y = median } (Paint.stroke theme.Foreground 2.0) ])
            |> List.collect id

    /// `heatmap` — a near-square grid of cells, intensity ramped `Muted`→`Accent` from the value.
    let heatmapGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let cols = max 1 (int (ceil (sqrt (float n))))
            let rows = max 1 (int (ceil (float n / float cols)))
            let cw = box.Width / float cols
            let ch = box.Height / float rows
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            pts
            |> List.mapi (fun i p ->
                let r = i / cols
                let c = i % cols
                let t = max 0.0 p.Y / maxY
                Scene.rectangle (box.X + float c * cw, box.Y + float r * ch, cw - 2.0, ch - 2.0) (lerpColor theme.Muted theme.Accent t))

    /// `radar-chart` — radial spokes + the value polygon, normalized to the canvas radius.
    let radarGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] | [ _ ] -> emptyState theme box "(no data)"
        | _ ->
            let n = List.length pts
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let r = (min box.Width box.Height) / 2.0 - 6.0
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            let angle i = float i / float n * 2.0 * System.Math.PI - System.Math.PI / 2.0
            let spokes =
                [ for i in 0 .. n - 1 ->
                    let a = angle i
                    Scene.line { X = cx; Y = cy } { X = cx + r * cos a; Y = cy + r * sin a } (Paint.stroke theme.Muted 1.0) ]
            let verts =
                pts |> List.mapi (fun i p ->
                    let a = angle i
                    let rr = r * (max 0.0 p.Y / maxY)
                    { X = cx + rr * cos a; Y = cy + rr * sin a })
            let head = List.head verts
            let polyCmds = Path.moveTo head.X head.Y :: (List.tail verts |> List.map (fun v -> Path.lineTo v.X v.Y)) @ [ Path.close ]
            let poly = Scene.path (Path.create Winding polyCmds) (Paint.withOpacity 0.35 (Paint.fill theme.Accent))
            spokes @ [ poly ]

    /// `rose-chart` — Nightingale polar-area sectors; sector radius scales with the value.
    let roseGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let rMax = (min box.Width box.Height) / 2.0 - 2.0
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            let sweep = 360.0 / float n
            pts
            |> List.mapi (fun i p ->
                let r = rMax * sqrt (max 0.0 p.Y / maxY)
                let bounds: Rect = { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r }
                Scene.arc bounds (-90.0 + float i * sweep) sweep (Paint.fill (chartColorAt theme i)))

    /// `waterfall-chart` — running cumulative bars; rises in `Success`, falls in `Danger`.
    let waterfallGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let gap = 6.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            let cumulative = (pts |> List.scan (fun acc p -> acc + p.Y) 0.0)
            let allLevels = cumulative
            let lo = List.min allLevels
            let hi = List.max allLevels
            let span = if hi - lo < 1e-9 then 1.0 else hi - lo
            let yOf v = box.Y + box.Height - (v - lo) / span * box.Height
            pts
            |> List.mapi (fun i p ->
                let prev = List.item i cumulative
                let curr = List.item (i + 1) cumulative
                let bx = box.X + float i * (bw + gap)
                let top = min (yOf prev) (yOf curr)
                let h = abs (yOf curr - yOf prev)
                let color = if p.Y >= 0.0 then theme.Success else theme.Danger
                Scene.rectangle (bx, top, bw, max 1.0 h) color)

    /// `funnel-chart` — centred trapezoid stack, each band narrowing with its value.
    let funnelGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let bandH = box.Height / float n
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            let cx = box.X + box.Width / 2.0
            let widthAt i =
                match List.tryItem i pts with
                | Some p -> box.Width * (max 0.0 p.Y / maxY)
                | None -> 0.0
            pts
            |> List.mapi (fun i _ ->
                let wTop = widthAt i
                let wBot = widthAt (i + 1)
                let yTop = box.Y + float i * bandH
                let yBot = yTop + bandH
                let cmds =
                    [ Path.moveTo (cx - wTop / 2.0) yTop
                      Path.lineTo (cx + wTop / 2.0) yTop
                      Path.lineTo (cx + wBot / 2.0) yBot
                      Path.lineTo (cx - wBot / 2.0) yBot
                      Path.close ]
                Scene.path (Path.create Winding cmds) (Paint.fill (chartColorAt theme i)))

    /// `gauge-chart` — a 180° track with the value arc + a needle; `value` is a fraction in [0,1].
    let gaugeGeom theme (box: Rect) (value: float) : Scene list =
        let v = max 0.0 (min 1.0 value)
        let cx = box.X + box.Width / 2.0
        let cy = box.Y + box.Height * 0.85
        let r = (min (box.Width / 2.0) box.Height) - 6.0
        let r = max 8.0 r
        let bounds: Rect = { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r }
        let track = Scene.arc bounds 180.0 180.0 (Paint.stroke theme.Muted 8.0)
        let valArc = Scene.arc bounds 180.0 (180.0 * v) (Paint.stroke theme.Accent 8.0)
        let a = System.Math.PI + System.Math.PI * v
        let needle = Scene.line { X = cx; Y = cy } { X = cx + r * cos a; Y = cy + r * sin a } (Paint.stroke theme.Foreground 2.5)
        [ track; valArc; needle; Scene.circle { X = cx; Y = cy } 4.0 theme.Foreground ]

    /// `sankey-diagram` — source/target node columns linked by translucent flow bands.
    let sankeyGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let n = List.length pts
            let leftN = max 1 ((n + 1) / 2)
            let rightN = max 1 (n - leftN)
            let nodeW = 12.0
            let nodeColumn count x =
                [ for i in 0 .. count - 1 ->
                    let h = box.Height / float count - 6.0
                    let y = box.Y + float i * (box.Height / float count) + 3.0
                    (x, y, h) ]
            let left = nodeColumn leftN box.X
            let right = nodeColumn rightN (box.X + box.Width - nodeW)
            let bands =
                [ for (lx, ly, lh) in left do
                    for (rx, ry, rh) in right do
                        let cmds =
                            [ Path.moveTo (lx + nodeW) (ly + lh / 2.0)
                              Path.lineTo rx (ry + rh / 2.0) ]
                        yield Scene.path (Path.create Winding cmds) (Paint.withOpacity 0.25 (Paint.stroke theme.Accent 3.0)) ]
            let leftNodes = left |> List.mapi (fun i (x, y, h) -> Scene.rectangle (x, y, nodeW, h) (chartColorAt theme i))
            let rightNodes = right |> List.mapi (fun i (x, y, h) -> Scene.rectangle (x, y, nodeW, h) (chartColorAt theme (i + leftN)))
            bands @ leftNodes @ rightNodes

    /// `chord-diagram` — nodes on a ring linked by chords across the circle.
    let chordGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] | [ _ ] -> emptyState theme box "(no data)"
        | _ ->
            let n = List.length pts
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let r = (min box.Width box.Height) / 2.0 - 8.0
            let positions =
                [ for i in 0 .. n - 1 ->
                    let a = float i / float n * 2.0 * System.Math.PI - System.Math.PI / 2.0
                    { X = cx + r * cos a; Y = cy + r * sin a } ]
            let chords =
                [ for i in 0 .. n - 1 do
                    let j = (i + n / 2) % n
                    let a = positions.[i]
                    let b = positions.[j]
                    yield Scene.path (Path.create Winding [ Path.moveTo a.X a.Y; Path.lineTo b.X b.Y ]) (Paint.withOpacity 0.3 (Paint.stroke theme.Accent 2.0)) ]
            let ring = Scene.arc { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r } 0.0 360.0 (Paint.stroke theme.Muted 1.0)
            let nodes = positions |> List.mapi (fun i p -> Scene.circle p 6.0 (chartColorAt theme i))
            ring :: chords @ nodes

    /// `treemap` — slice-and-dice nested rectangles sized by value, intensity-ramped.
    let treemapGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let total = pts |> List.sumBy (fun p -> max 0.0 p.Y)
            let total = if total < 1e-9 then 1.0 else total
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            // Horizontal slice-and-dice: each tile gets a width-share of the row proportional to value.
            let mutable x = box.X
            [ for i in 0 .. pts.Length - 1 do
                let p = pts.[i]
                let w = box.Width * (max 0.0 p.Y / total)
                let t = max 0.0 p.Y / maxY
                yield Scene.rectangle (x, box.Y, max 1.0 (w - 2.0), box.Height) (lerpColor theme.Muted theme.Accent t)
                x <- x + w ]

    /// `sunburst` — a centre hub ringed by value-proportional arc segments.
    let sunburstGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        withPoints theme box "(no data)" pts <| fun pts ->
            let total = pts |> List.sumBy (fun p -> max 0.0 p.Y)
            let total = if total < 1e-9 then 1.0 else total
            let cx = box.X + box.Width / 2.0
            let cy = box.Y + box.Height / 2.0
            let rOuter = (min box.Width box.Height) / 2.0 - 2.0
            let bounds: Rect = { X = cx - rOuter; Y = cy - rOuter; Width = 2.0 * rOuter; Height = 2.0 * rOuter }
            let ring =
                pts
                |> List.indexed
                |> List.fold
                    (fun (start, acc) (i, p) ->
                        let sweep = (max 0.0 p.Y / total) * 360.0
                        start + sweep, Scene.arc bounds start sweep (Paint.fill (chartColorAt theme i)) :: acc)
                    (-90.0, [])
                |> snd
                |> List.rev
            ring @ [ Scene.circle { X = cx; Y = cy } (rOuter * 0.4) theme.Background ]

    // ---- shared pill (relocated from the widget block; chart-compiled so widgets can reach it) ----
    /// A compact filled pill with contrasting text — the shared shape for tag/segment chips.
    let pillGeom theme (x: float) (cy: float) (fill: Color) (fg: Color) (label: string) : float * Scene list =
        let h = 24.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 12.0; Weight = None }).Width
        let w = max 36.0 (textW + 18.0)
        let by = cy - h / 2.0
        w, [ Scene.rectangle (x, by, w, h) fill
             mkText theme (x + 9.0) (by + h / 2.0 + 4.0) 12.0 fg label ]
