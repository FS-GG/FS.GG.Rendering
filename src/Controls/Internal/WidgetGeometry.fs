namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

/// Feature 189 (US1, FR-002): widget / layout / container `*Geom` producers relocated verbatim from
/// `ControlInternals`. `module internal`; opens `ControlPrimitives` (drawing/palette helpers) and
/// `ChartGeometry` (shared `emptyState`/`pillGeom`). Byte-identical.
module internal WidgetGeometry =
    open ControlPrimitives
    open ChartGeometry
    // ---- collection / selection / value geometry --------------------------------------------

    let rowsGeom theme (box: Rect) (items: string list) (selected: Set<string>) : Scene list =
        match items with
        | [] -> emptyState theme box "(empty)"
        | _ ->
            let shown = items |> List.truncate 5
            let n = List.length shown
            // Feature 136 (US3/T032): each row gets at least `minRowHeight` so items never collapse
            // onto a shared baseline when the box is short; the rows are clipped to the box (a taller
            // stack is clipped, not overprinted — distinct y-bands always).
            let minRowHeight = 18.0
            let rowH = max minRowHeight (box.Height / float n)

            let rows =
                shown
                |> List.mapi (fun i it ->
                    let ry = box.Y + float i * rowH

                    let bg =
                        if Set.contains it selected then theme.Accent
                        elif i % 2 = 0 then theme.Muted
                        else theme.Background

                    Scene.group
                        [ Scene.rectangle (box.X, ry, box.Width, rowH - 1.5) bg
                          mkText theme (box.X + 8.0) (ry + rowH * 0.62) 12.0 theme.Foreground it ])

            [ Scene.clipped (RectClip box) (Scene.group rows) ]

    /// Tabular chrome for `data-grid`: a header band, column/row rules, and sample cell text laid
    /// out row-major from `cells` (first `cols` entries are the header). The preview is built as a
    /// single-Kind node so the composite header/cell tree does not flatten into stray rows.
    let gridGeom theme (box: Rect) (cells: string list) : Scene list =
        let cols = 2
        let rows = 2
        let cw = box.Width / float cols
        let rh = box.Height / float (rows + 1)
        let frame = Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 1.5)
        let header = Scene.rectangle (box.X, box.Y, box.Width, rh) theme.Muted
        let rowLines =
            [ for r in 1..rows -> Scene.line { X = box.X; Y = box.Y + float r * rh } { X = box.X + box.Width; Y = box.Y + float r * rh } (Paint.stroke theme.Muted 1.0) ]
        let colLines =
            [ for c in 1 .. cols - 1 -> Scene.line { X = box.X + float c * cw; Y = box.Y } { X = box.X + float c * cw; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.0) ]
        let texts =
            cells
            |> List.truncate (cols * (rows + 1))
            |> List.mapi (fun i s ->
                let r = i / cols
                let c = i % cols
                mkText theme (box.X + float c * cw + 6.0) (box.Y + float r * rh + rh * 0.66) 11.0 theme.Foreground s)
        frame :: header :: (rowLines @ colLines @ texts)

    // Feature 096 (R1): RadioGroup joins the migrated kinds — each item's ring + label paint flow
    // through `Style.resolve`. The per-item base reproduces the prior procedural colours (accent ring
    // when selected, muted otherwise; foreground label), so `resolve theme base [] Normal = base` is
    // byte-identical (FR-006); the control's runtime visual state composes on top of every item.
    let radioGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (items: string list) (selected: string option) : Scene list =
        match items with
        | [] -> emptyState theme box "(empty)"
        | _ ->
            let rowH = min 28.0 (box.Height / float (List.length items))
            items
            |> List.mapi (fun i it ->
                let cy = box.Y + float i * rowH + rowH / 2.0
                let cx = box.X + 9.0
                let isSel = selected = Some it

                let baseStyle: ResolvedStyle =
                    { Foreground = theme.Foreground
                      Fill = (if isSel then theme.Accent else theme.Muted)
                      Stroke = theme.Accent
                      StrokeWidth = 0.0
                      FontFamily = theme.FontFamily
                      FontSize = 12.0
                      FontWeight = None }

                let style = Style.resolve theme baseStyle classes state
                let outer = Scene.circle { X = cx; Y = cy } 7.0 style.Fill
                let inner = if isSel then [ Scene.circle { X = cx; Y = cy } 3.0 theme.Background ] else []
                Scene.group (outer :: inner @ [ mkText theme (cx + 16.0) (cy + 4.0) 12.0 style.Foreground it ]))

    let tabsGeom theme (box: Rect) (items: string list) (selected: string option) : Scene list =
        match items with
        | [] -> emptyState theme box "(empty)"
        | _ ->
            let n = List.length items
            let tw = box.Width / float n
            let stripH = min 30.0 box.Height
            items
            |> List.mapi (fun i it ->
                let tx = box.X + float i * tw
                let active = selected = Some it
                Scene.group
                    [ Scene.rectangle (tx, box.Y, tw - 2.0, stripH) (if active then theme.Accent else theme.Muted)
                      mkText theme (tx + 6.0) (box.Y + stripH * 0.62) 11.0 theme.Foreground it ])

    // Feature 096 (R1): Slider joins the migrated kinds — its filled track + thumb paint flow through
    // `Style.resolve`. The base reproduces the prior procedural `theme.Accent`, so
    // `resolve theme base [] Normal = base` is byte-identical (FR-006); attached classes / runtime
    // visual state compose on top (a hover/press/selected restyle of the accent fill).
    let sliderGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (value: float) : Scene list =
        let v = max 0.0 (min 1.0 value)
        let cy = box.Y + box.Height / 2.0

        let baseStyle: ResolvedStyle =
            { Foreground = theme.Foreground
              Fill = theme.Accent
              Stroke = theme.Accent
              StrokeWidth = 0.0
              FontFamily = theme.FontFamily
              FontSize = 13.0
              FontWeight = None }

        let style = Style.resolve theme baseStyle classes state

        [ Scene.rectangle (box.X, cy - 2.0, box.Width, 4.0) theme.Muted
          Scene.rectangle (box.X, cy - 2.0, box.Width * v, 4.0) style.Fill
          Scene.circle { X = box.X + box.Width * v; Y = cy } 8.0 style.Fill ]

    let progressGeom theme (box: Rect) (value: float) : Scene list =
        let v = max 0.0 (min 1.0 value)
        let barH = 16.0
        let by = box.Y + box.Height / 2.0 - barH / 2.0
        [ Scene.rectangle (box.X, by, box.Width, barH) theme.Muted
          Scene.rectangle (box.X, by, box.Width * v, barH) theme.Accent ]

    let numericGeom theme (box: Rect) (value: float) : Scene list =
        let cy = box.Y + box.Height / 2.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
          mkText theme (box.X + 10.0) (cy + 5.0) 16.0 theme.Foreground (sprintf "%g" value)
          Scene.line { X = box.X + box.Width - 16.0; Y = cy } { X = box.X + box.Width - 6.0; Y = cy } (Paint.stroke theme.Muted 2.0) ]

    // Feature 096 (R1): Switch joins the migrated kinds — its track paint flows through `Style.resolve`.
    // `on` still selects the base track colour (accent vs muted) so `resolve theme base [] Normal = base`
    // is byte-identical (FR-006); attached classes / runtime visual state compose on top.
    let switchGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (on: bool) : Scene list =
        let cy = box.Y + box.Height / 2.0
        let w = 52.0
        let thumbX = if on then box.X + w - 12.0 else box.X + 12.0

        let baseStyle: ResolvedStyle =
            { Foreground = theme.Foreground
              Fill = (if on then theme.Accent else theme.Muted)
              Stroke = theme.Accent
              StrokeWidth = 0.0
              FontFamily = theme.FontFamily
              FontSize = 13.0
              FontWeight = None }

        let style = Style.resolve theme baseStyle classes state

        [ Scene.rectangle (box.X, cy - 12.0, w, 24.0) style.Fill
          Scene.circle { X = thumbX; Y = cy } 10.0 theme.Background ]

    // Feature 093 (E3): CheckBox (rich-geometry migrant) — paint flows through `Style.resolve`.
    // `on` still drives WHICH geometry is drawn (filled box + tick vs outlined box); the resolver
    // supplies the colours. The base reproduces the prior procedural colours exactly, so
    // `resolve theme base [] Normal = base` is byte-identical (FR-005, SC-003). Attached classes /
    // visual state compose on top per the fixed precedence (FR-001/FR-003/FR-004).
    let checkboxGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (on: bool) (label: string) : Scene list =
        let s = 28.0
        let bx = box.X
        let cy = box.Y + box.Height / 2.0
        let by = cy - s / 2.0
        let boxRect = { X = bx; Y = by; Width = s; Height = s }

        let baseStyle: ResolvedStyle =
            if on then
                // Filled accent box, theme-background tick, foreground label.
                { Foreground = theme.Foreground
                  Fill = theme.Accent
                  Stroke = theme.Background
                  StrokeWidth = 3.0
                  FontFamily = theme.FontFamily
                  FontSize = 13.0
                  FontWeight = None }
            else
                // Outlined (foreground-stroked) empty box, foreground label.
                { Foreground = theme.Foreground
                  Fill = Colors.transparent
                  Stroke = theme.Foreground
                  StrokeWidth = 2.0
                  FontFamily = theme.FontFamily
                  FontSize = 13.0
                  FontWeight = None }

        let style = Style.resolve theme baseStyle classes state

        let fill =
            if on then [ Scene.rectangle (bx, by, s, s) style.Fill ]
            else [ Scene.rectangleWithPaint boxRect (Paint.stroke style.Stroke 2.0) ]
        let tick =
            if on then
                [ Scene.line { X = bx + 6.0; Y = by + 15.0 } { X = bx + 12.0; Y = by + 21.0 } (Paint.stroke style.Stroke 3.0)
                  Scene.line { X = bx + 12.0; Y = by + 21.0 } { X = bx + 23.0; Y = by + 7.0 } (Paint.stroke style.Stroke 3.0) ]
            else
                []
        let text = [ mkText theme (bx + s + 10.0) (cy + 5.0) 13.0 style.Foreground label ]
        fill @ tick @ text

    let toggleGeom theme (box: Rect) (on: bool) (label: string) : Scene list =
        // A button-shaped chip; filled accent when pressed (on), outlined when not.
        let h = 36.0
        let w = min box.Width 150.0
        let by = box.Y + box.Height / 2.0 - h / 2.0
        let rect = { X = box.X; Y = by; Width = w; Height = h }
        let textColor = if on then theme.Background else theme.Foreground
        let surface =
            if on then [ Scene.rectangle (box.X, by, w, h) theme.Accent ]
            else [ Scene.rectangleWithPaint rect (Paint.stroke theme.Accent 2.0) ]
        surface @ [ mkText theme (box.X + 12.0) (by + h / 2.0 + 5.0) 14.0 textColor label ]

    let pickerGeom theme (box: Rect) (text: string) : Scene list =
        let frame = Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
        let segs =
            [ for f in [ 0.34; 0.67 ] ->
                  Scene.line { X = box.X + box.Width * f; Y = box.Y } { X = box.X + box.Width * f; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.0) ]
        frame :: mkText theme (box.X + 8.0) (box.Y + box.Height / 2.0 + 5.0) 14.0 theme.Foreground text :: segs

    let swatchGeom theme (box: Rect) : Scene list =
        let n = 5
        let sw = box.Width / float n
        [ for i in 0 .. n - 1 -> Scene.rectangle (box.X + float i * sw, box.Y, sw - 3.0, box.Height) (colorAt theme i) ]

    let spinnerGeom theme (box: Rect) : Scene list =
        let r = (min box.Width box.Height) / 2.0 - 8.0
        let cx = box.X + box.Width / 2.0
        let cy = box.Y + box.Height / 2.0
        let bounds: Rect = { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r }
        // A faint full-circle track plus a bold accent sweep with a gap reads as a busy spinner.
        [ Scene.arc bounds 0.0 360.0 (Paint.stroke theme.Muted 7.0)
          Scene.arc bounds -90.0 280.0 (Paint.stroke theme.Accent 7.0) ]

    let imageGeom theme (box: Rect) (source: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
          Scene.line { X = box.X; Y = box.Y } { X = box.X + box.Width; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.5)
          Scene.line { X = box.X + box.Width; Y = box.Y } { X = box.X; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.5)
          mkText theme (box.X + 6.0) (box.Y + box.Height - 6.0) 11.0 theme.Foreground source ]

    let iconGeom theme (box: Rect) (name: string) : Scene list =
        // A font-independent house glyph from a `Path` (no `.notdef` box risk), plus the name.
        let cx = box.X + 22.0
        let cy = box.Y + box.Height / 2.0
        let r = 16.0
        let cmds =
            [ Path.moveTo (cx - r) cy
              Path.lineTo cx (cy - r)
              Path.lineTo (cx + r) cy
              Path.lineTo (cx + r - 3.0) cy
              Path.lineTo (cx + r - 3.0) (cy + r)
              Path.lineTo (cx - r + 3.0) (cy + r)
              Path.lineTo (cx - r + 3.0) cy
              Path.close ]
        [ Scene.path (Path.create Winding cmds) (Paint.fill theme.Accent)
          mkText theme (cx + r + 8.0) (cy + 5.0) 14.0 theme.Foreground name ]

    // ---- command / button geometry ----------------------------------------------------------

    /// A filled command button sized to its label, vertically centred. `kind = "button"` ⇒ accent
    /// fill with light text; `"icon-button"` ⇒ an accent-outlined neutral surface.
    //
    // Feature 093 (E3): Button (box+label migrant) — paint flows through the resolver.
    // Feature 129 (F4): the `baseStyle` is now obtained from the central front-half path
    // `StyleResolver.resolveDefault theme kind intent classes state`, replacing the inline
    // `primary: bool` literal dispatch. The structural bases were relocated verbatim into
    // `StyleResolver.baseStyleFor`, and the default (neutral) policy ignores `intent`, so the
    // default-theme output is byte-identical across every intent and visual state (FR-003, SC-001).
    // `kind` selects the fill-vs-outline geometry; the resolver supplies the colours; `intent` is
    // now a THREADED, consumed argument (reaches resolution) rather than dead code.
    let buttonGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (kind: string) (intent: string) (label: string) : Scene list =
        let h = 38.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
        let w = min box.Width (max 70.0 (textW + 32.0))
        let by = box.Y + box.Height / 2.0 - h / 2.0
        let rect = { X = box.X; Y = by; Width = w; Height = h }

        let style = StyleResolver.resolveDefault theme kind intent classes state

        // Feature 175 (FR-004): a filled button's fill carries hover/press, but FOCUS only moves the
        // stroke — which the filled branch otherwise ignores, so keyboard focus was invisible on every
        // button (incl. the ghost nav buttons). Paint a focus ring (the resolved focus stroke) when the
        // state involves focus; Normal/Hover/Pressed add nothing, so non-focused buttons are unchanged.
        let focusRing =
            match state with
            | Focused
            | FocusedHover -> [ Scene.rectangleWithPaint rect (Paint.stroke style.Stroke 2.0) ]
            | _ -> []

        // Feature 175 (FR-003, finding F-009): a default button's resting fill is already `theme.Accent`,
        // so `applyState Hover` (Fill = Accent) is a no-op and hover was invisible on it. Lighten the
        // fill on hover so EVERY button shows a visible hover state (a transparent-resting ghost button
        // lightens its now-Accent hover fill too). Non-hover states keep `style.Fill` (byte-identical).
        let buttonFill =
            match state with
            | Hover
            | FocusedHover -> lerpColor style.Fill Colors.white 0.18
            | _ -> style.Fill

        if kind = "button" then
            [ Scene.rectangle (box.X, by, w, h) buttonFill
              mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 style.Foreground label ]
            @ focusRing
        else
            [ Scene.rectangleWithPaint rect (Paint.stroke style.Stroke 2.0)
              mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 style.Foreground label ]

    /// A compact accent pill with light text — a status badge.
    let badgeGeom theme (box: Rect) (label: string) : Scene list =
        let h = 26.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 12.0; Weight = None }).Width
        let w = max 40.0 (textW + 20.0)
        let by = box.Y + box.Height / 2.0 - h / 2.0
        [ Scene.rectangle (box.X, by, w, h) theme.Accent
          mkText theme (box.X + 10.0) (by + h / 2.0 + 4.0) 12.0 theme.Background label ]

    /// A primary command button joined to a dropdown trigger (caret) — a split button.
    let splitGeom theme (box: Rect) (label: string) : Scene list =
        let h = 38.0
        let by = box.Y + box.Height / 2.0 - h / 2.0
        let triggerW = 30.0
        let primaryW = min (box.Width - triggerW - 2.0) 160.0
        let caretX = box.X + primaryW + 2.0 + triggerW / 2.0
        let caretY = by + h / 2.0
        let caret =
            Path.create
                Winding
                [ Path.moveTo (caretX - 6.0) (caretY - 3.0)
                  Path.lineTo (caretX + 6.0) (caretY - 3.0)
                  Path.lineTo caretX (caretY + 5.0)
                  Path.close ]
        [ Scene.rectangle (box.X, by, primaryW, h) theme.Accent
          mkText theme (box.X + 14.0) (by + h / 2.0 + 5.0) 15.0 theme.Background label
          Scene.rectangle (box.X + primaryW + 2.0, by, triggerW, h) theme.Muted
          Scene.path caret (Paint.fill theme.Foreground) ]

    // ---- layout / container geometry --------------------------------------------------------

    /// A bordered, filled, labelled region — the building block for container schematics so every
    /// region is visible against the canvas (a `theme.Background` fill alone would be invisible).
    let regionRect theme (x: float) (y: float) (w: float) (h: float) (fill: Color) (label: string) : Scene list =
        [ Scene.rectangle (x, y, w, h) fill
          Scene.rectangleWithPaint { X = x; Y = y; Width = w; Height = h } (Paint.stroke theme.Foreground 1.0)
          mkText theme (x + 6.0) (y + h / 2.0 + 4.0) 12.0 theme.Foreground label ]

    let itemsOr (fallback: string list) (items: string list) =
        match items with
        | [] -> fallback
        | _ -> items

    /// Vertically stacked child regions — `stack`.
    let stackGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "One"; "Two"; "Three" ] |> List.truncate 4
        let n = max 1 (List.length shown)
        let rowH = box.Height / float n
        shown |> List.mapi (fun i it -> regionRect theme box.X (box.Y + float i * rowH) box.Width (rowH - 4.0) theme.Muted it) |> List.concat

    /// A 2-column cell grid — `grid` (distinct from `data-grid`'s tabular `gridGeom`).
    let gridLayoutGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "A1"; "B2"; "C3"; "D4" ] |> List.truncate 4
        let cols = 2
        let cw = box.Width / float cols
        let rows = max 1 ((List.length shown + cols - 1) / cols)
        let rh = box.Height / float rows
        shown
        |> List.mapi (fun i it -> regionRect theme (box.X + float (i % cols) * cw) (box.Y + float (i / cols) * rh) (cw - 5.0) (rh - 5.0) theme.Muted it)
        |> List.concat

    /// Small chips flowing left-to-right and wrapping — `wrap`.
    let wrapGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "tag1"; "tag2"; "tag3" ] |> List.truncate 6
        let chipW = 66.0
        let chipH = 26.0
        let gap = 7.0
        let perRow = max 1 (int (box.Width / (chipW + gap)))
        shown
        |> List.mapi (fun i it ->
            let r = i / perRow
            let c = i % perRow
            regionRect theme (box.X + float c * (chipW + gap)) (box.Y + float r * (chipH + gap)) chipW chipH theme.Muted it)
        |> List.concat

    /// A docked top bar plus a left rail and a filled centre — `dock`.
    let dockGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Top"; "Fill" ]
        let topH = 26.0
        let leftW = 72.0
        let bodyY = box.Y + topH + 2.0
        let bodyH = box.Height - topH - 2.0
        regionRect theme box.X box.Y box.Width topH theme.Accent (List.tryItem 0 shown |> Option.defaultValue "Top")
        @ regionRect theme box.X bodyY leftW bodyH theme.Muted "Left"
        @ regionRect theme (box.X + leftW + 2.0) bodyY (box.Width - leftW - 2.0) bodyH theme.Background (List.tryItem 1 shown |> Option.defaultValue "Fill")

    /// Two side-by-side panes with a divider — `split-view`.
    let splitViewGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Left"; "Right" ]
        let half = box.Width / 2.0
        regionRect theme box.X box.Y (half - 4.0) box.Height theme.Muted (List.tryItem 0 shown |> Option.defaultValue "Left")
        @ [ Scene.rectangle (box.X + half - 2.0, box.Y, 4.0, box.Height) theme.Foreground ]
        @ regionRect theme (box.X + half + 4.0) box.Y (half - 4.0) box.Height theme.Background (List.tryItem 1 shown |> Option.defaultValue "Right")

    /// A command strip of horizontal buttons — `toolbar`.
    let toolbarGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "B"; "I"; "U" ] |> List.truncate 6
        let stripH = 38.0
        let strip = Scene.rectangle (box.X, box.Y, box.Width, stripH) theme.Muted
        let bw = 42.0
        let btns =
            shown
            |> List.mapi (fun i it -> regionRect theme (box.X + 8.0 + float i * (bw + 6.0)) (box.Y + 5.0) bw (stripH - 10.0) theme.Background it)
            |> List.concat
        strip :: btns

    /// A surface with a header band and a body — `panel`.
    let panelGeom theme (box: Rect) (label: string) : Scene list =
        let headH = 26.0
        [ Scene.rectangle (box.X, box.Y, box.Width, headH) theme.Accent
          Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 1.0) ]
        @ [ mkText theme (box.X + 8.0) (box.Y + box.Height / 2.0 + 8.0) 12.0 theme.Foreground label ]

    /// A thick border framing inner content — `border`.
    let borderGeom theme (box: Rect) (label: string) : Scene list =
        let inset = 10.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Accent 4.0) ]
        @ regionRect theme (box.X + inset) (box.Y + inset) (box.Width - 2.0 * inset) (box.Height - 2.0 * inset) theme.Muted label

    /// A scrollable viewport: content area plus a vertical scrollbar thumb — `scroll-viewer`.
    let scrollViewerGeom theme (box: Rect) (label: string) : Scene list =
        let barW = 10.0
        let contentW = box.Width - barW - 4.0
        regionRect theme box.X box.Y contentW box.Height theme.Muted label
        @ [ Scene.rectangle (box.X + contentW + 4.0, box.Y, barW, box.Height) theme.Muted
            Scene.rectangle (box.X + contentW + 4.0, box.Y + 6.0, barW, box.Height * 0.4) theme.Accent ]

    /// Feature 137 (US3) — the scroll affordance painted by a `scroll-viewer` *container* (the leaf
    /// `scrollViewerGeom` above is the no-content placeholder). A track at the right edge plus a thumb;
    /// the thumb is shorter than the track when the content overflows the viewport (it is scrollable).
    /// `contentHeight > box.Height` ⇒ thumb ratio `< 1` (a scroll affordance); content is confined to the
    /// box by the shared container clip (`composeContainerScene`), so it is clipped/scrollable not spilled.
    // Feature 175 (FR-001/FR-002): the thumb now tracks the live offset and is OMITTED when the
    // content fits (no draggable affordance, dead-zone honoured). For an overflowing region at
    // offset 0 this is byte-identical to the pre-175 affordance (same track + same thumb height at
    // box.Y), so existing at-rest scroll scenes are unchanged.
    let scrollAffordance theme (box: Rect) (state: ScrollState) : Scene list =
        let barW = 10.0
        let trackX = box.X + box.Width - barW
        let track = Scene.rectangle (trackX, box.Y, barW, box.Height) theme.Muted
        let thumbH = ScrollState.thumbHeight state
        if thumbH <= 0.0 then
            [ track ]
        else
            let thumbY = box.Y + ScrollState.thumbPosition box.Height state
            [ track
              Scene.rectangle (trackX, thumbY, barW, thumbH) theme.Accent ]

    /// Two layered, offset surfaces suggesting stacked content — `overlay`.
    let overlayGeom theme (box: Rect) (label: string) : Scene list =
        let off = 16.0
        regionRect theme box.X box.Y (box.Width - off) (box.Height - off) theme.Muted ""
        @ regionRect theme (box.X + off) (box.Y + off) (box.Width - off) (box.Height - off) theme.Background label

    // ---- text-input / rich-text / divider geometry (feature 082) ----------------------------

    /// A bordered single-line input field showing its value text and a caret — `text-box`. The
    /// frame + caret are what distinguish an editable field from a static label.
    // Feature 096 (R1): TextBox joins the migrated kinds — its border + label paint flow through
    // `Style.resolve`. The base reproduces the prior procedural foreground stroke/label, so
    // `resolve theme base [] Normal = base` is byte-identical (FR-006); the `Focused` runtime state
    // turns the border accent — a natural focus indicator — and other states compose on top. The
    // field background + caret stay literal (they are not state-driven chrome).
    let textFieldGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (value: string) : Scene list =
        let h = min box.Height 40.0
        let by = box.Y + box.Height / 2.0 - h / 2.0
        let field: Rect = { X = box.X; Y = by; Width = box.Width; Height = h }
        let textX = box.X + 10.0
        let baseline = by + h / 2.0 + 5.0
        let textW = (measureText value { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
        let caretX = min (box.X + box.Width - 8.0) (textX + textW + 3.0)

        let baseStyle: ResolvedStyle =
            { Foreground = theme.Foreground
              Fill = theme.Background
              Stroke = theme.Foreground
              StrokeWidth = 2.0
              FontFamily = theme.FontFamily
              FontSize = 15.0
              FontWeight = None }

        let style = Style.resolve theme baseStyle classes state

        [ Scene.rectangle (box.X, by, box.Width, h) theme.Background
          Scene.rectangleWithPaint field (Paint.stroke style.Stroke 2.0)
          Scene.clipped
              (RectClip field)
              (mkText theme textX baseline 15.0 style.Foreground value)
          Scene.line { X = caretX; Y = by + 7.0 } { X = caretX; Y = by + h - 7.0 } (Paint.stroke theme.Accent 2.0) ]

    /// A bordered multi-line input field showing each value line plus a caret — `text-area`.
    let textAreaFieldGeom theme (box: Rect) (value: string) : Scene list =
        let lineH = 22.0
        let lines = value.Replace("\r\n", "\n").Split('\n') |> Array.toList |> List.truncate 4
        let firstBaseline = box.Y + 22.0
        let texts =
            lines
            |> List.mapi (fun i ln -> mkText theme (box.X + 10.0) (firstBaseline + float i * lineH) 14.0 theme.Foreground ln)
        let lastLine = lines |> List.tryLast |> Option.defaultValue ""
        let lastW = (measureText lastLine { Family = theme.FontFamily; Size = 14.0; Weight = None }).Width
        let caretX = min (box.X + box.Width - 8.0) (box.X + 10.0 + lastW + 3.0)
        let caretY = firstBaseline + float (max 0 (List.length lines - 1)) * lineH
        [ Scene.rectangle (box.X, box.Y, box.Width, box.Height) theme.Background
          Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
          Scene.clipped (RectClip box) (Scene.group texts)
          Scene.line { X = caretX; Y = caretY - 13.0 } { X = caretX; Y = caretY + 3.0 } (Paint.stroke theme.Accent 2.0) ]

    /// Styled runs flowing left-to-right with per-run colour and weight — `rich-text`. Each run
    /// keeps its own `Foreground`/`Weight`, so the preview demonstrates rich formatting rather
    /// than collapsing to a single-colour label (or, pre-082, the kind id).
    let richTextGeom theme (box: Rect) (runs: (string * Color * float * int) list) : Scene list =
        match runs with
        | [] -> emptyState theme box "(no runs)"
        | _ ->
            let baseline = box.Y + box.Height / 2.0 + 6.0
            runs
            |> List.fold
                (fun (x, acc) (text, fg, fontSize, weight) ->
                    let size = max 8.0 fontSize
                    let font: FontSpec = { Family = theme.FontFamily; Size = size; Weight = Some weight }
                    let w = (measureText text font).Width
                    let node = mkTextW theme x baseline size (Some weight) fg text
                    x + w, node :: acc)
                (box.X + 4.0, [])
            |> snd
            |> List.rev

    /// A horizontal divider rule centred in the canvas — `separator`.
    let separatorGeom theme (box: Rect) : Scene list =
        let cy = box.Y + box.Height / 2.0
        [ Scene.line { X = box.X; Y = cy } { X = box.X + box.Width; Y = cy } (Paint.stroke theme.Foreground 3.0) ]

    // ---- Feature 132 (D2.1) net-new Ant-overview control geometry ---------------------------
    // Generic, theme-agnostic schematics for the net-new controls. Each reads ONLY `theme` roles
    // (token-sourced) — never theme identity — so it renders neutrally under Default and Ant-styled
    // under AntDesign with no control edits (FR-007, contract R3/R4). The varied role usage
    // (Accent / Muted / Foreground / Background / Danger / Success / Warning) guarantees the
    // resolved paint diverges when the Ant palette differs from Default (FR-013).


    /// A coloured status chip — `tag`.
    let tagGeom theme (box: Rect) (label: string) : Scene list =
        snd (pillGeom theme box.X (box.Y + box.Height / 2.0) theme.Accent theme.Background (if label = "" then "tag" else label))

    /// A round monogram — `avatar`.
    let avatarGeom theme (box: Rect) (label: string) : Scene list =
        let r = 18.0
        let cx = box.X + r
        let cy = box.Y + box.Height / 2.0
        [ Scene.circle { X = cx; Y = cy } r theme.Accent
          mkText theme (cx - 9.0) (cy + 5.0) 13.0 theme.Background (if label = "" then "?" else label) ]

    /// A framed surface with a header band — `card`.
    let cardGeom theme (box: Rect) (title: string) : Scene list =
        let headH = 28.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          Scene.rectangle (box.X, box.Y, box.Width, headH) theme.Muted
          mkText theme (box.X + 10.0) (box.Y + 19.0) 14.0 theme.Foreground (if title = "" then "Card" else title)
          mkText theme (box.X + 10.0) (box.Y + headH + 22.0) 12.0 theme.Foreground "Card content" ]

    /// A label : value term list — `descriptions`.
    let descriptionsGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Name"; "Ant"; "Status"; "Active" ] |> List.truncate 6
        let n = max 1 (List.length shown)
        // Feature 136 (US3/T033): scale the row spacing to the box height (capped at the natural 22px)
        // instead of a fixed 22px stride that runs past the box, and clip to the box — descriptions
        // never paint past their bounds (FR-007).
        let rowH = min 22.0 (box.Height / float n)

        let rows =
            shown
            |> List.mapi (fun i it ->
                let y = box.Y + rowH * (float i + 0.7)
                let fg = if i % 2 = 0 then theme.Muted else theme.Foreground
                mkText theme box.X y 12.0 fg it)

        [ Scene.clipped (RectClip box) (Scene.group rows) ]

    /// A large emphasised metric over a caption — `statistic`.
    let statisticGeom theme (box: Rect) (value: string) : Scene list =
        [ mkText theme box.X (box.Y + 18.0) 12.0 theme.Muted "Total"
          mkText theme box.X (box.Y + 46.0) 28.0 theme.Accent (if value = "" then "0" else value) ]

    /// A vertical dotted event rail — `timeline`.
    let timelineGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Created"; "Shipped"; "Delivered" ] |> List.truncate 6
        shown
        |> List.mapi (fun i it ->
            let y = box.Y + 16.0 + float i * 24.0
            [ Scene.circle { X = box.X + 6.0; Y = y - 4.0 } 4.0 theme.Accent
              mkText theme (box.X + 20.0) y 12.0 theme.Foreground it ])
        |> List.concat

    /// A muted "no data" placeholder with a framed glyph — `empty`.
    let emptyGeom theme (box: Rect) (caption: string) : Scene list =
        let cx = box.X + box.Width / 2.0
        [ Scene.rectangleWithPaint { X = cx - 28.0; Y = box.Y + 10.0; Width = 56.0; Height = 36.0 } (Paint.stroke theme.Muted 1.0)
          mkText theme (cx - 28.0) (box.Y + 64.0) 12.0 theme.Muted (if caption = "" then "No data" else caption) ]

    /// Grey placeholder bars — `skeleton`.
    let skeletonGeom theme (box: Rect) : Scene list =
        [ 0; 1; 2 ]
        |> List.map (fun i ->
            let w = box.Width * (if i = 2 then 0.6 else 0.9)
            Scene.rectangle (box.X, box.Y + 12.0 + float i * 20.0, w, 12.0) theme.Muted)

    /// A square module grid — `qr-code`.
    let qrCodeGeom theme (box: Rect) : Scene list =
        let n = 7
        // Feature 136 (US3/T034): enforce a minimum module-grid size so a non-empty payload always
        // shows a populated grid (each module ≥ 3px) even when the box is compressed, and clip to the
        // box so the grid never overruns (FR-007). Pre-fix `side = min(w,h)-8` collapsed to ~0 → blank.
        let side = max (float n * 3.0) (min box.Width box.Height - 8.0)
        let cell = side / float n

        let modules =
            [ for r in 0 .. n - 1 do
                  for c in 0 .. n - 1 do
                      if (r + c + r * c) % 2 = 0 then
                          yield Scene.rectangle (box.X + float c * cell, box.Y + float r * cell, cell - 1.0, cell - 1.0) theme.Foreground ]

        [ Scene.clipped (RectClip box) (Scene.group modules) ]

    /// Faint repeated brand text — `watermark`.
    let watermarkGeom theme (box: Rect) (label: string) : Scene list =
        let text = if label = "" then "FS.GG" else label
        let paintFaint = Paint.withOpacity 0.25 (Paint.fill theme.Muted)
        [ for r in 0 .. 2 do
            yield Scene.textRun
                { Text = text
                  Position = { X = box.X + float (r % 2) * 60.0; Y = box.Y + 24.0 + float r * 28.0 }
                  Font = { Family = theme.FontFamily; Size = 14.0; Weight = None }
                  Paint = paintFaint } ]

    /// A coloured information banner — `alert` (warning role so it diverges from accent controls).
    let alertGeom theme (box: Rect) (label: string) : Scene list =
        let h = 36.0
        [ Scene.rectangle (box.X, box.Y, box.Width, h) theme.Warning
          Scene.rectangle (box.X, box.Y, 4.0, h) theme.Danger
          mkText theme (box.X + 12.0) (box.Y + h / 2.0 + 4.0) 13.0 theme.Background (if label = "" then "Alert" else label) ]

    /// A centred outcome panel: status dot + title — `result`.
    let resultGeom theme (box: Rect) (title: string) : Scene list =
        let cx = box.X + box.Width / 2.0
        [ Scene.circle { X = cx; Y = box.Y + 26.0 } 14.0 theme.Success
          mkText theme (cx - 30.0) (box.Y + 62.0) 14.0 theme.Foreground (if title = "" then "Success" else title) ]

    /// A right-edge sliding surface — `drawer`.
    let drawerGeom theme (box: Rect) (title: string) : Scene list =
        let w = box.Width * 0.55
        let x = box.X + box.Width - w
        [ Scene.rectangle (box.X, box.Y, box.Width, box.Height) theme.Muted
          Scene.rectangle (x, box.Y, w, box.Height) theme.Background
          Scene.rectangleWithPaint { X = x; Y = box.Y; Width = w; Height = box.Height } (Paint.stroke theme.Muted 1.0)
          mkText theme (x + 10.0) (box.Y + 22.0) 13.0 theme.Foreground (if title = "" then "Drawer" else title) ]

    /// A small floating callout box — `popover` (and the base for popconfirm/tour).
    // Feature 183 (US3): `popoverGeom`'s `withActions: bool` becomes a 2-case kind so the 3 call sites
    // read `Plain` / `WithActions` instead of an opaque `false` / `true` (geometry unchanged).
    type PopoverKind =
        | Plain
        | WithActions

    let popoverGeom theme (box: Rect) (label: string) (kind: PopoverKind) : Scene list =
        let withActions = kind = WithActions
        let w = min box.Width 180.0
        let h = if withActions then 70.0 else 50.0
        let baseScene =
            [ Scene.rectangle (box.X, box.Y, w, h) theme.Background
              Scene.rectangleWithPaint { X = box.X; Y = box.Y; Width = w; Height = h } (Paint.stroke theme.Muted 1.0)
              mkText theme (box.X + 10.0) (box.Y + 24.0) 12.0 theme.Foreground (if label = "" then "Popover" else label) ]
        if withActions then
            baseScene
            @ snd (pillGeom theme (box.X + w - 64.0) (box.Y + h - 16.0) theme.Accent theme.Background "OK")
        else
            baseScene

    /// A circular floating action button — `float-button`.
    let floatButtonGeom theme (box: Rect) (label: string) : Scene list =
        let r = 22.0
        let cx = box.X + box.Width - r - 6.0
        let cy = box.Y + box.Height - r - 6.0
        [ Scene.circle { X = cx; Y = cy } r theme.Accent
          mkText theme (cx - 5.0) (cy + 6.0) 18.0 theme.Background (if label = "" then "+" else label) ]

    /// A trail of separated path labels — `breadcrumb`.
    let breadcrumbGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Home"; "Library"; "Item" ]
        let cy = box.Y + box.Height / 2.0
        let mutable x = box.X
        [ for i, it in List.indexed shown do
            let fg = if i = List.length shown - 1 then theme.Foreground else theme.Muted
            yield mkText theme x (cy + 4.0) 13.0 fg it
            let w = (measureText it { Family = theme.FontFamily; Size = 13.0; Weight = None }).Width
            x <- x + w + 8.0
            if i < List.length shown - 1 then
                yield mkText theme x (cy + 4.0) 13.0 theme.Muted "/"
                x <- x + 12.0 ]

    /// Numbered horizontal progress steps — `steps`.
    let stepsGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "First"; "Second"; "Third" ] |> List.truncate 4
        let n = max 1 (List.length shown)
        let stepW = box.Width / float n
        let cy = box.Y + 22.0
        shown
        |> List.mapi (fun i it ->
            let cx = box.X + float i * stepW + 16.0
            let fill = if i = 0 then theme.Accent else theme.Muted
            [ Scene.circle { X = cx; Y = cy } 12.0 fill
              mkText theme (cx - 4.0) (cy + 5.0) 13.0 theme.Background (string (i + 1))
              mkText theme (cx - 14.0) (cy + 30.0) 11.0 theme.Foreground it ])
        |> List.concat

    /// A row of page-number chips — `pagination`.
    let paginationGeom theme (box: Rect) (total: int) : Scene list =
        let n = max 1 (min total 6)
        let cy = box.Y + box.Height / 2.0
        let mutable x = box.X
        [ for i in 1 .. n do
            let fill = if i = 1 then theme.Accent else theme.Background
            let fg = if i = 1 then theme.Background else theme.Foreground
            let w, scene = pillGeom theme x cy fill fg (string i)
            yield! scene
            x <- x + w + 6.0 ]

    /// A connected single-select segment row — `segmented`.
    let segmentedGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Day"; "Week"; "Month" ] |> List.truncate 5
        let cy = box.Y + box.Height / 2.0
        let mutable x = box.X
        [ yield Scene.rectangle (box.X, cy - 16.0, box.Width, 32.0) theme.Muted
          for i, it in List.indexed shown do
            let fill = if i = 0 then theme.Background else theme.Muted
            let fg = if i = 0 then theme.Accent else theme.Foreground
            let w, scene = pillGeom theme (x + 2.0) cy fill fg it
            yield! scene
            x <- x + w + 4.0 ]

    /// A vertical in-page link list — `anchor`.
    let anchorGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Intro"; "Usage"; "API" ] |> List.truncate 6
        [ yield Scene.rectangle (box.X, box.Y, 2.0, box.Height) theme.Muted
          for i, it in List.indexed shown do
            let fg = if i = 0 then theme.Accent else theme.Muted
            yield mkText theme (box.X + 12.0) (box.Y + 16.0 + float i * 22.0) 12.0 fg it ]

    /// A pinned-to-top bar — `affix`.
    let affixGeom theme (box: Rect) (label: string) : Scene list =
        [ Scene.rectangle (box.X, box.Y, box.Width, 30.0) theme.Accent
          mkText theme (box.X + 10.0) (box.Y + 20.0) 13.0 theme.Background (if label = "" then "Affixed" else label) ]

    /// Stacked expandable section headers — `collapse`.
    let collapseGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Panel 1"; "Panel 2"; "Panel 3" ] |> List.truncate 5
        shown
        |> List.mapi (fun i it ->
            let y = box.Y + float i * 30.0
            [ Scene.rectangle (box.X, y, box.Width, 28.0) theme.Muted
              mkText theme (box.X + 24.0) (y + 19.0) 12.0 theme.Foreground it
              mkText theme (box.X + 8.0) (y + 19.0) 12.0 theme.Accent (if i = 0 then "-" else "+") ])
        |> List.concat

    /// A row of star glyphs, the leading ones filled — `rate`.
    let rateGeom theme (box: Rect) (value: float) : Scene list =
        let filled = int (value + 0.5)
        let cy = box.Y + box.Height / 2.0
        [ for i in 0 .. 4 do
            let color = if i < filled then theme.Warning else theme.Muted
            yield Scene.circle { X = box.X + 14.0 + float i * 26.0; Y = cy } 9.0 color ]

    /// A framed slide with position dots — `carousel`.
    let carouselGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Slide 1"; "Slide 2"; "Slide 3" ]
        let label = List.head shown
        [ yield Scene.rectangle (box.X, box.Y, box.Width, box.Height - 16.0) theme.Muted
          yield mkText theme (box.X + 12.0) (box.Y + box.Height / 2.0) 14.0 theme.Foreground label
          for i in 0 .. List.length shown - 1 do
            let color = if i = 0 then theme.Accent else theme.Background
            yield Scene.circle { X = box.X + box.Width / 2.0 - 12.0 + float i * 12.0; Y = box.Y + box.Height - 6.0 } 4.0 color ]

    /// A month day-cell grid — `calendar`.
    let calendarGeom theme (box: Rect) : Scene list =
        let cols = 7
        let rows = 4
        let cw = box.Width / float cols
        let rh = (box.Height - 4.0) / float rows
        [ for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                let day = r * cols + c + 1
                yield Scene.rectangleWithPaint
                          { X = box.X + float c * cw; Y = box.Y + float r * rh; Width = cw - 2.0; Height = rh - 2.0 }
                          (Paint.stroke theme.Muted 1.0)
                yield mkText theme (box.X + float c * cw + 4.0) (box.Y + float r * rh + 14.0) 10.0 theme.Foreground (string day) ]

    /// Cascading selection columns — `cascader`.
    let cascaderGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Region"; "City"; "District" ]
        let colW = box.Width / 3.0
        [ for ci in 0 .. 2 do
            yield Scene.rectangleWithPaint
                      { X = box.X + float ci * colW; Y = box.Y; Width = colW - 2.0; Height = box.Height }
                      (Paint.stroke theme.Muted 1.0)
            let label = List.tryItem ci shown |> Option.defaultValue ""
            if label <> "" then
                yield mkText theme (box.X + float ci * colW + 6.0) (box.Y + 18.0) 11.0 theme.Foreground label ]

    /// A text field with a suggestion dropdown — `auto-complete`.
    let autoCompleteGeom theme (box: Rect) (value: string) : Scene list =
        [ Scene.rectangleWithPaint { X = box.X; Y = box.Y; Width = box.Width; Height = 30.0 } (Paint.stroke theme.Accent 1.5)
          mkText theme (box.X + 8.0) (box.Y + 20.0) 13.0 theme.Foreground (if value = "" then "Search…" else value)
          Scene.rectangle (box.X, box.Y + 34.0, box.Width, 54.0) theme.Background
          Scene.rectangleWithPaint { X = box.X; Y = box.Y + 34.0; Width = box.Width; Height = 54.0 } (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + 8.0) (box.Y + 52.0) 12.0 theme.Muted "Suggestion 1"
          mkText theme (box.X + 8.0) (box.Y + 74.0) 12.0 theme.Muted "Suggestion 2" ]

    /// A dashed drop zone with an upload action — `upload`.
    let uploadGeom theme (box: Rect) (label: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + box.Width / 2.0 - 40.0) (box.Y + box.Height / 2.0 - 6.0) 12.0 theme.Muted "Drop files here" ]
        @ snd (pillGeom theme (box.X + box.Width / 2.0 - 30.0) (box.Y + box.Height / 2.0 + 18.0) theme.Accent theme.Background (if label = "" then "Upload" else label))

