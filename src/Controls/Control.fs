namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

module StandardControlKindHelpers =
    let toControlKind kind =
        match kind with
        | FS.GG.UI.Controls.StandardControlKind.TextBlock -> "text-block"
        | FS.GG.UI.Controls.StandardControlKind.Button -> "button"
        | FS.GG.UI.Controls.StandardControlKind.TextBox -> "text-box"
        | FS.GG.UI.Controls.StandardControlKind.LineChart -> "line-chart"
        | FS.GG.UI.Controls.StandardControlKind.BarChart -> "bar-chart"
        | FS.GG.UI.Controls.StandardControlKind.PieChart -> "pie-chart"
        | FS.GG.UI.Controls.StandardControlKind.ScatterPlot -> "scatter-plot"
        | FS.GG.UI.Controls.StandardControlKind.GraphView -> "graph-view"
        | FS.GG.UI.Controls.StandardControlKind.DataGrid -> "data-grid"
        | FS.GG.UI.Controls.StandardControlKind.Custom value -> value

module internal ControlInternals =
    // Feature 117 (Phase 8, FR-001/FR-004): the text-measure cache hook. `Scene.measureText` is a pure
    // function of `(text, font)`, so a cache over it is a transparent accelerator — the cached value
    // equals the un-cached value for every key (research R5). The bounded cache itself and its per-frame
    // hit/miss accounting live on `RetainedRender` (the 113/116 cache home, research R1); the retained
    // `init`/`step` install a per-pass closure here around the frame's layout + paint measurement and
    // clear it afterwards (`try/finally`). `[<ThreadStatic>]` so concurrent test `step`s route to their own
    // cache and never cross-contaminate; the default (`None`) is the direct un-cached path, byte-identical
    // to pre-117. The mutation is interpreter-edge mutation confined to the step (constitution III), exactly
    // like the existing id/work counters and the 116 picture cache.
    type private TextMeasureHookHolder() =
        [<System.ThreadStatic; DefaultValue>]
        static val mutable private slot: (string -> FontSpec -> TextMetrics) option

        static member Slot
            with get () = TextMeasureHookHolder.slot
            and set value = TextMeasureHookHolder.slot <- value

    /// Feature 117 (Phase 8): install (or clear with `None`) the per-pass text-measure cache hook on this
    /// thread. Called by `RetainedRender.step` around the frame's layout + paint measurement.
    let setMeasureTextHook (hook: (string -> FontSpec -> TextMetrics) option) =
        TextMeasureHookHolder.Slot <- hook

    /// Measure text through the active text-measure cache hook when one is installed (inside a retained
    /// `step`), else directly via the pure `Scene.measureText`. All six layout/paint text-measure call
    /// sites route through here so the cache spans both the layout pass and the paint pass of a single
    /// frame (FR-001). With no hook installed this is exactly `Scene.measureText` (byte-identical, FR-004).
    let measureText (text: string) (font: FontSpec) : TextMetrics =
        match TextMeasureHookHolder.Slot with
        | Some f -> f text font
        | None -> Scene.measureText text font

    let tryLast name (attrs: Attr<'msg> list) =
        attrs
        |> List.rev
        |> List.tryFind (fun attr -> attr.Name = name)

    let textFrom (attrs: Attr<'msg> list) =
        AttrKeys.tryKey AttrKeys.Text attrs
        |> Option.orElseWith (fun () -> AttrKeys.tryKey AttrKeys.Value attrs)
        |> Option.bind (fun attr ->
            match attr.Value with
            | TextValue value -> Some value
            | FloatValue value -> Some(string value)
            | BoolValue value -> Some(string value)
            | StringListValue values -> Some(String.concat ", " values)
            | ValidationValue Valid -> Some "valid"
            | ValidationValue(Invalid message) -> Some message
            | ValidationValue(Pending message) -> Some message
            | _ -> None)

    let boolValue name defaultValue (attrs: Attr<'msg> list) =
        tryLast name attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | BoolValue value -> Some value
            | _ -> None)
        |> Option.defaultValue defaultValue

    /// Feature 093 (E3): the ordered attached style classes carried by the last `styleClasses`
    /// attribute (last-writer convention). Absent ≡ `[]` ≡ the behaviour-preserving base case;
    /// `Control.renderTree`/`RetainedRender` feed these into `Style.resolve` for migrated kinds.
    let styleClassesOf (attrs: Attr<'msg> list) : StyleClass list =
        AttrKeys.tryKey AttrKeys.StyleClasses attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | StyleClassesValue classes -> Some classes
            | _ -> None)
        |> Option.defaultValue []

    /// Feature 093 (E3): the control's current `VisualState` carried by its `visualState`
    /// attribute (last-writer convention). Absent ≡ `Normal`. Because it rides the control's
    /// attributes, it travels through the keyed reconciler — a state-driven look survives a
    /// sibling-shifting re-render under E2's retained identity (FR-006, SC-005).
    let visualStateOf (attrs: Attr<'msg> list) : VisualState =
        AttrKeys.tryKey AttrKeys.VisualState attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | VisualStateValue state -> Some state
            | _ -> None)
        |> Option.defaultValue Normal

    /// Feature 095 (E5): build the single `Slot`-category carrier attribute from an ordered
    /// name->fill association list. Lives `internal` (no public free-form slot builder, FR-001):
    /// the typed `Props` views call this; a consumer never names a slot string. Mirrors
    /// `Attributes.styleClasses` but kept off the public surface.
    let slotFill (fills: (string * Control<'msg>) list) : Attr<'msg> =
        { Name = AttrKeys.nameOf AttrKeys.Slot; Category = Slot; Value = SlotFillsValue fills }

    /// Feature 095 (E5): the ordered slot fills carried by the last `slot` attribute (last-writer
    /// convention). Absent ≡ `[]` ≡ no slot filled ≡ the byte-identical base case (FR-003).
    let slotFillsOf (attrs: Attr<'msg> list) : (string * Control<'msg>) list =
        AttrKeys.tryKey AttrKeys.Slot attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | SlotFillsValue fills -> Some fills
            | _ -> None)
        |> Option.defaultValue []

    /// Feature 095 (E5): the fill for ONE named region, or `None` when that name is absent from the
    /// fill list (an unfilled slot ⇒ render the region's default chrome). A name PRESENT but bound
    /// to empty content still returns `Some` (absent ≠ empty, per the spec edge case).
    let slotFor (name: string) (attrs: Attr<'msg> list) : Control<'msg> option =
        slotFillsOf attrs |> List.tryFind (fun (n, _) -> n = name) |> Option.map snd

    /// Feature 105 (US3, FR-008): the closed set of declared slot regions, as an INTERNAL DU so a
    /// mistyped region is a compile error. No public `SlotName` surface is introduced (feature 095's
    /// deliberate omission is preserved): the public `AttrValue.SlotFillsValue : (string * Control)
    /// list` carrier is unchanged; the region name is projected to its string at the single
    /// consumption edge in `lowerSlots`.
    type SlotName =
        | Leading
        | Trailing
        | Header
        | Footer

    let slotName (slot: SlotName) : string =
        match slot with
        | Leading -> "leading"
        | Trailing -> "trailing"
        | Header -> "header"
        | Footer -> "footer"

    /// Feature 095 (E5): the per-kind declared slot regions, partitioned into those rendered
    /// BEFORE the kind's intrinsic content (`leading`) and those rendered AFTER (`trailing`). A
    /// kind with no declared regions returns empty lists, so lowering is total for every kind.
    let private slotRegions (kind: string) : SlotName list * SlotName list =
        match kind with
        | "button" -> [ Leading ], [ Trailing ]
        | "panel" -> [ Header ], [ Footer ]
        | _ -> [], []

    /// Feature 095 (E5): the pure, total, deterministic slot lowering `(kind + slot fills) ->
    /// Control<'msg>`. For each declared region, place the fill sub-tree if present (else nothing —
    /// the unfilled region contributes ZERO geometry, so the byte-identity holds), injecting the
    /// fills into the control's `Children` ordered by region position (leading regions, then the
    /// kind's intrinsic children, then trailing regions) and CONSUMING the slot carrier attribute.
    /// Because the fills land in `Children`, they inherit E1 dispatch, E2 retained identity, E3
    /// style resolution, and E4 focus/key routing by construction (FR-002, FR-004, FR-005, FR-006).
    /// With no slot attribute present the control is returned verbatim (the fast path), so an
    /// unfilled slot-bearing control is byte-identical to its pre-slot render (FR-003, SC-002).
    let lowerSlots (control: Control<'msg>) : Control<'msg> =
        match slotFillsOf control.Attributes with
        | [] -> control
        | fills ->
            let pick names =
                names |> List.choose (fun n -> fills |> List.tryFind (fun (fn, _) -> fn = slotName n) |> Option.map snd)

            let leadingNames, trailingNames = slotRegions control.Kind

            { control with
                Attributes = control.Attributes |> List.filter (fun a -> a.Name <> AttrKeys.nameOf AttrKeys.Slot)
                Children = pick leadingNames @ control.Children @ pick trailingNames }

    let floatValue name defaultValue (attrs: Attr<'msg> list) =
        tryLast name attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | FloatValue value -> Some value
            | _ -> None)
        |> Option.defaultValue defaultValue

    let accessibility kind (attrs: Attr<'msg> list) text =
        AttrKeys.tryKey AttrKeys.Accessibility attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | AccessibilityValue value -> Some value
            | _ -> None)
        |> Option.orElseWith (fun () -> Some(Accessibility.defaultFor kind (text |> Option.defaultValue kind)))

    let childrenFrom (attrs: Attr<'msg> list) =
        attrs
        |> List.collect (fun attr ->
            match attr.Value with
            | ChildValue child -> [ child ]
            | ChildrenValue children -> children
            | _ -> [])

    let required kind =
        match kind with
        | "text-block"
        | "label"
        | "badge"
        | "button"
        | "validation-message"
        | "tooltip"
        | "toast" -> [ "text" ]
        | "text-box"
        | "text-area" -> [ "value" ]
        | "numeric-input"
        | "slider"
        | "progress-bar" -> [ "value" ]
        | "radio-group"
        | "tabs"
        | "menu" -> [ "items" ]
        | "line-chart"
        | "bar-chart"
        | "scatter-plot" -> [ "series" ]
        | "pie-chart" -> [ "values" ]
        | "graph-view" -> [ "nodes" ]
        | "data-grid" -> [ "columns"; "rows" ]
        | _ -> []

    let hasAttr name (attrs: Attr<'msg> list) =
        attrs |> List.exists (fun attr -> attr.Name = name)

    let disabledOrReadOnly (control: Control<'msg>) =
        let enabled = boolValue "enabled" true control.Attributes
        let readOnly = boolValue "readOnly" false control.Attributes
        not enabled || readOnly

    let eventKind attrName =
        match attrName with
        | "onClick" -> "click"
        | "onChanged" -> "changed"
        | "onSelected" -> "selected"
        | value when value.StartsWith("on", StringComparison.Ordinal) ->
            value.Substring(2).ToLowerInvariant()
        | value -> value

    // FR-001 (feature 098): bindings key by the unified canonical `ControlId` — `Key ?? path`,
    // the SAME positional structural path `collectBoundsWith`/`toLayout` mint (root "0") — not the
    // old collision-prone `Key ?? Kind`. The keyed branch is byte-identical (`Key` still wins); only
    // the unkeyed fallback shifts `Kind → path`, so same-kind siblings get distinct ids.
    let eventBindings (path: string) (control: Control<'msg>) =
        let id = control.Key |> Option.defaultValue path

        control.Attributes
        |> List.choose (fun attr ->
            if attr.Category <> Event then
                None
            else
                let kind = eventKind attr.Name

                match attr.Value with
                | MessageValue msg -> Some { ControlId = id; EventKind = kind; Dispatch = fun _ -> msg }
                | EventValue map -> Some { ControlId = id; EventKind = kind; Dispatch = map }
                | _ -> None)

    let rec recursively collect (control: Control<'msg>) =
        collect control @ (control.Children |> List.collect (recursively collect))

    let fittedFontSize maxSize minSize width height family (label: string) =
        let availableWidth = max 1.0 (width - 16.0)
        let availableHeight = max 1.0 (height - 8.0)
        let upper = Math.Clamp(maxSize, minSize, max minSize availableHeight)
        let font size = { Family = family; Size = size; Weight = None }
        let fits size =
            let metrics = measureText label (font size)
            metrics.Width <= availableWidth && metrics.Height <= availableHeight

        if fits upper then
            upper
        else
            let rec search remaining low high =
                if remaining = 0 then
                    low
                else
                    let mid = (low + high) * 0.5

                    if fits mid then
                        search (remaining - 1) mid high
                    else
                        search (remaining - 1) low mid

            search 8 minSize upper

    let chartValues (control: Control<'msg>) : ChartPoint list =
        // Feature 080 (FR-002): read the structured `UntypedValue(ChartSeries list)` (series)
        // and `UntypedValue(ChartPoint list)` (pie) the typed front door actually stores,
        // preserving X/Y/Label. The flat `float list`/`float array`/`FloatValue` fallback is
        // retained for legacy untyped authoring (mapped to points with X = index). Pre-080 this
        // matched only the flat shapes, so typed charts silently yielded `[]` (root cause).
        let indexed (values: float list) =
            values |> List.mapi (fun index value -> { X = float index; Y = value; Label = None })

        let points name =
            tryLast name control.Attributes
            |> Option.bind (fun attr ->
                match attr.Value with
                | UntypedValue(:? (ChartSeries list) as series) ->
                    Some(series |> List.collect (fun s -> s.Points))
                | UntypedValue(:? (ChartPoint list) as pts) -> Some pts
                | UntypedValue(:? (float list) as values) -> Some(indexed values)
                | UntypedValue(:? (float array) as values) -> Some(indexed (Array.toList values))
                | FloatValue value -> Some [ { X = 0.0; Y = value; Label = None } ]
                | _ -> None)

        // Feature 133 (D2C.1): the net-new charts read either `series` (ChartSeries list) or
        // `values` (ChartPoint list), mirroring the existing charts; the flow diagrams (sankey/chord)
        // read `nodes` like `graph-view`. The reader is a pure function of the control's attributes.
        let nodesAsPoints () =
            AttrKeys.tryKey AttrKeys.Nodes control.Attributes
            |> Option.bind (fun attr ->
                match attr.Value with
                | StringListValue values ->
                    Some(values |> List.mapi (fun index label -> { X = float index; Y = float index; Label = Some label }))
                | _ -> None)
            |> Option.defaultValue []

        match control.Kind with
        | "line-chart"
        | "bar-chart"
        | "scatter-plot"
        // series-shaped net-new charts
        | "area-chart"
        | "column-chart"
        | "box-plot" ->
            points "series" |> Option.defaultValue []
        | "pie-chart"
        // point-shaped net-new charts
        | "histogram"
        | "heatmap"
        | "radar-chart"
        | "rose-chart"
        | "waterfall-chart"
        | "funnel-chart"
        | "gauge-chart"
        | "treemap"
        | "sunburst" ->
            points "values" |> Option.defaultValue []
        | "graph-view"
        | "sankey-diagram"
        | "chord-diagram" -> nodesAsPoints ()
        | _ -> []

    /// Read the field-name-free run projection (text, colour, size, weight) that `RichText.create`
    /// stashes in the `richTextRuns` attr, so the preview can draw real per-run colour/weight
    /// rather than the kind id. (Control.fs compiles before RichText.fs, so the typed
    /// `RichTextBlock` is intentionally not in scope here.)
    let richTextRuns (control: Control<'msg>) : (string * Color * float * int) list =
        AttrKeys.tryKey AttrKeys.RichTextRuns control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue(:? (list<string * Color * float * int>) as runs) -> Some runs
            | _ -> None)
        |> Option.defaultValue []

    // Feature 080 (FR-001/003/004/005/011) — faithful per-control preview geometry.
    //
    // Controls in `richFamilies` lower to control-specific geometry built from EXISTING Scene
    // primitives (polyline `Path` for line, `Rectangle`s for bars, `Arc`s for pie, `Circle`s for
    // scatter, item rows for collections, track+thumb/tick/toggle/tab chrome for value/selection
    // controls, a framed placeholder for `image`, a font-safe `Path` glyph for `icon`), laid out
    // BELOW the title band so the fidelity gate's "coverage outside the title band" criterion is
    // met. Every other control (text/containers — `button`, `label`, `stack`, …) keeps the
    // 079 box+label: those controls ARE their text, so a label-on-a-box is already faithful.
    let richFamilies =
        Set.ofList
            [ "line-chart"; "bar-chart"; "pie-chart"; "scatter-plot"; "graph-view"
              "list-view"; "list-box"; "multi-select-list"; "combo-box"; "tree-view"; "data-grid"
              "menu"; "context-menu"; "radio-group"; "tabs"
              "slider"; "progress-bar"; "numeric-input"; "switch"; "check-box"
              "button"; "icon-button"; "badge"; "toggle-button"; "split-button"
              "date-picker"; "time-picker"; "color-picker"; "spinner"; "image"; "icon"
              // layout / container families (built as single-Kind preview schematics, FR-001):
              "stack"; "grid"; "dock"; "wrap"; "panel"; "border"; "scroll-viewer"
              "split-view"; "toolbar"; "overlay"
              // feature 082 — text-input / rich-text / divider controls. These were previously in
              // the box+label fallback, which is faithful for static text (label/text-block) but
              // hid an editable field's chrome (text-box/text-area read as plain labels), dropped
              // rich-text's styled runs (it rendered its kind id), and drew `separator` as the word
              // "separator" instead of a divider rule. They now lower to control-specific geometry.
              "text-box"; "text-area"; "rich-text"; "separator"
              // Feature 132 (D2.1) — net-new Ant-overview controls (control-specific schematics).
              "tag"; "avatar"; "card"; "descriptions"; "statistic"; "timeline"; "empty"; "skeleton"
              "qr-code"; "watermark"; "alert"; "result"; "drawer"; "popover"; "popconfirm"; "tour"
              "float-button"; "breadcrumb"; "steps"; "pagination"; "segmented"; "anchor"; "affix"
              "collapse"; "rate"; "carousel"; "calendar"; "cascader"; "auto-complete"; "upload"
              // Feature 133 (D2C.1) — net-new generic chart controls (theme-role-driven schematics).
              "area-chart"; "column-chart"; "histogram"; "box-plot"; "heatmap"; "radar-chart"
              "rose-chart"; "waterfall-chart"; "funnel-chart"; "gauge-chart"; "sankey-diagram"
              "chord-diagram"; "treemap"; "sunburst" ]

    /// A human caption for the rich-family title band: "date-picker" -> "Date picker".
    /// Used so the thumbnail's title is the control's NAME, not its sample content (which the
    /// schematic below already shows) — fixing composite-lowering title bleed (e.g. "STACK").
    let prettyKind (kind: string) =
        match kind.Split('-') |> Array.toList with
        | [] -> kind
        | head :: tail ->
            let cap (w: string) = if w.Length = 0 then w else string (System.Char.ToUpper w[0]) + w.Substring 1
            cap head :: tail |> String.concat " "

    // Feature 101 (R7, US2 / SC-002): the ONE authoritative token per layout-driving attribute name.
    // `nodeWidth`/`nodeHeight` (`hasAttr`/`floatValue`), `orientationOf`, and `layoutAffectingAttrNames`
    // below all reference these, so no string literal of a layout-driving name is hand-duplicated.
    // `private` to this internal module (reached only via `InternalsVisibleTo`); NOT in `Control.fsi`
    // and NO behavior change (byte-identically the same three strings).
    let [<Literal>] private AttrWidth = "width"
    let [<Literal>] private AttrHeight = "height"
    let [<Literal>] private AttrOrientation = "orientation"

    /// Preview node width: explicit `width` wins; rich families fill the preview canvas.
    let nodeWidth (control: Control<'msg>) =
        if hasAttr AttrWidth control.Attributes then floatValue AttrWidth 240.0 control.Attributes
        elif Set.contains control.Kind richFamilies then 304.0
        else 240.0

    /// Preview node height: explicit `height` wins; rich families get a tall box so geometry
    /// sits below the title band (a 24-px box would put everything inside the band).
    let nodeHeight (control: Control<'msg>) =
        if hasAttr AttrHeight control.Attributes then max 20.0 (floatValue AttrHeight 24.0 control.Attributes)
        elif Set.contains control.Kind richFamilies then 132.0
        else 24.0

    let private palette (theme: Theme) =
        [ theme.Accent
          Colors.rgb 210uy 95uy 75uy
          Colors.rgb 90uy 165uy 95uy
          Colors.rgb 150uy 110uy 205uy
          Colors.rgb 215uy 165uy 65uy
          Colors.rgb 80uy 150uy 205uy ]

    let private colorAt theme i =
        let p = palette theme
        List.item (((i % p.Length) + p.Length) % p.Length) p

    // Feature 133 (D2C.1, data-model §3): the categorical series palette for the NET-NEW charts,
    // derived purely from `Theme` ROLE VALUES (never `Theme.Name`) so it diverges under the Ant
    // theme yet never branches on theme identity (FR-001/FR-007). Distinct from `palette` above —
    // that one is pinned to literals for the EXISTING charts' Default byte-identity (SC-004); the
    // net-new charts have no baseline, so every colour traces to a token-sourced role here.
    let private chartPalette (theme: Theme) : Color list =
        [ theme.Accent; theme.Danger; theme.Success; theme.Warning; theme.Muted; theme.Foreground ]

    let private chartColorAt (theme: Theme) i =
        let p = chartPalette theme
        List.item (((i % p.Length) + p.Length) % p.Length) p

    /// Blend two colours by `t` ∈ [0,1] — used by intensity ramps (heatmap/treemap) so the low→high
    /// scale runs `Muted`→`Accent` from theme roles, with no inline hex.
    let private lerpColor (a: Color) (b: Color) (t: float) : Color =
        let t = max 0.0 (min 1.0 t)
        let mix (x: byte) (y: byte) = byte (float x + (float y - float x) * t)
        Colors.rgba (mix a.Red b.Red) (mix a.Green b.Green) (mix a.Blue b.Blue) (mix a.Alpha b.Alpha)

    let private mkText (theme: Theme) (x: float) (baseline: float) (size: float) (color: Color) (s: string) =
        Scene.textRun
            { Text = s
              Position = { X = x; Y = baseline }
              Font = { Family = theme.FontFamily; Size = size; Weight = None }
              Paint = Paint.fill color }

    /// `mkText` with an explicit weight — used by the rich-text schematic to draw bold runs.
    let private mkTextW (theme: Theme) (x: float) (baseline: float) (size: float) (weight: int option) (color: Color) (s: string) =
        Scene.textRun
            { Text = s
              Position = { X = x; Y = baseline }
              Font = { Family = theme.FontFamily; Size = size; Weight = weight }
              Paint = Paint.fill color }

    let private stringListOf name (control: Control<'msg>) =
        tryLast name control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | StringListValue values -> Some values
            | _ -> None)
        |> Option.defaultValue []

    let private textValueOf name (control: Control<'msg>) =
        tryLast name control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | TextValue value -> Some value
            | _ -> None)

    /// Honest empty state (FR-011): a faint frame + a "(no data)" caption within bounds, so an
    /// empty/missing-data control reads as a recognizable empty control, never an off-canvas blank.
    let private emptyState (theme: Theme) (box: Rect) (caption: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + 8.0) (box.Y + box.Height * 0.5) 12.0 theme.Muted caption ]

    // ---- chart geometry ---------------------------------------------------------------------

    let private normIndexed (box: Rect) (pts: ChartPoint list) : Point list =
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

    let private lineGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
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

    let private barGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
            let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
            let n = List.length pts
            let gap = 6.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            pts
            |> List.mapi (fun i p ->
                let h = (max 0.0 p.Y / maxY) * box.Height
                let bx = box.X + float i * (bw + gap)
                Scene.rectangle (bx, box.Y + box.Height - h, bw, h) (colorAt theme i))

    let private pieGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private axes theme (box: Rect) : Scene list =
        [ Scene.line { X = box.X; Y = box.Y } { X = box.X; Y = box.Y + box.Height } (Paint.stroke theme.Foreground 1.5)
          Scene.line { X = box.X; Y = box.Y + box.Height } { X = box.X + box.Width; Y = box.Y + box.Height } (Paint.stroke theme.Foreground 1.5) ]

    let private scatterGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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

    let private graphGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private scaledBars (box: Rect) (pts: ChartPoint list) : (float * float) list =
        let maxY = pts |> List.map (fun p -> max 0.0 p.Y) |> List.fold max 1e-9
        pts |> List.map (fun p -> p.Y, (max 0.0 p.Y / maxY) * box.Height)

    /// `area-chart` — a filled region under the series outline (distinct, heavier fill than line).
    let private areaGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
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
    let private columnGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
            let n = List.length pts
            let gap = 6.0
            let bw = (box.Width - gap * float (n - 1)) / float n
            scaledBars box pts
            |> List.mapi (fun i (_, h) ->
                let bx = box.X + float i * (bw + gap)
                Scene.rectangle (bx, box.Y + box.Height - h, bw, h) (chartColorAt theme i))

    /// `histogram` — adjacent (gapless) frequency bars in a single accent fill, hairline-separated.
    let private histogramGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
            let n = List.length pts
            let bw = box.Width / float n
            scaledBars box pts
            |> List.mapi (fun i (_, h) ->
                let bx = box.X + float i * bw
                [ Scene.rectangle (bx, box.Y + box.Height - h, bw, h) theme.Accent
                  Scene.rectangleWithPaint { X = bx; Y = box.Y + box.Height - h; Width = bw; Height = h } (Paint.stroke theme.Background 1.0) ])
            |> List.collect id

    /// `box-plot` — a box-and-whisker schematic per category (box around the value, median + whiskers).
    let private boxPlotGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private heatmapGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private radarGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
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
    let private roseGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private waterfallGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private funnelGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private gaugeGeom theme (box: Rect) (value: float) : Scene list =
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
    let private sankeyGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private chordGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
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
    let private treemapGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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
    let private sunburstGeom theme (box: Rect) (pts: ChartPoint list) : Scene list =
        match pts with
        | [] -> emptyState theme box "(no data)"
        | _ ->
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

    // ---- collection / selection / value geometry --------------------------------------------

    let private rowsGeom theme (box: Rect) (items: string list) (selected: Set<string>) : Scene list =
        match items with
        | [] -> emptyState theme box "(empty)"
        | _ ->
            let shown = items |> List.truncate 5
            let n = List.length shown
            let rowH = box.Height / float n
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

    /// Tabular chrome for `data-grid`: a header band, column/row rules, and sample cell text laid
    /// out row-major from `cells` (first `cols` entries are the header). The preview is built as a
    /// single-Kind node so the composite header/cell tree does not flatten into stray rows.
    let private gridGeom theme (box: Rect) (cells: string list) : Scene list =
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
    let private radioGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (items: string list) (selected: string option) : Scene list =
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

    let private tabsGeom theme (box: Rect) (items: string list) (selected: string option) : Scene list =
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
    let private sliderGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (value: float) : Scene list =
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

    let private progressGeom theme (box: Rect) (value: float) : Scene list =
        let v = max 0.0 (min 1.0 value)
        let barH = 16.0
        let by = box.Y + box.Height / 2.0 - barH / 2.0
        [ Scene.rectangle (box.X, by, box.Width, barH) theme.Muted
          Scene.rectangle (box.X, by, box.Width * v, barH) theme.Accent ]

    let private numericGeom theme (box: Rect) (value: float) : Scene list =
        let cy = box.Y + box.Height / 2.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
          mkText theme (box.X + 10.0) (cy + 5.0) 16.0 theme.Foreground (sprintf "%g" value)
          Scene.line { X = box.X + box.Width - 16.0; Y = cy } { X = box.X + box.Width - 6.0; Y = cy } (Paint.stroke theme.Muted 2.0) ]

    // Feature 096 (R1): Switch joins the migrated kinds — its track paint flows through `Style.resolve`.
    // `on` still selects the base track colour (accent vs muted) so `resolve theme base [] Normal = base`
    // is byte-identical (FR-006); attached classes / runtime visual state compose on top.
    let private switchGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (on: bool) : Scene list =
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
    let private checkboxGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (on: bool) (label: string) : Scene list =
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

    let private toggleGeom theme (box: Rect) (on: bool) (label: string) : Scene list =
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

    let private pickerGeom theme (box: Rect) (text: string) : Scene list =
        let frame = Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
        let segs =
            [ for f in [ 0.34; 0.67 ] ->
                  Scene.line { X = box.X + box.Width * f; Y = box.Y } { X = box.X + box.Width * f; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.0) ]
        frame :: mkText theme (box.X + 8.0) (box.Y + box.Height / 2.0 + 5.0) 14.0 theme.Foreground text :: segs

    let private swatchGeom theme (box: Rect) : Scene list =
        let n = 5
        let sw = box.Width / float n
        [ for i in 0 .. n - 1 -> Scene.rectangle (box.X + float i * sw, box.Y, sw - 3.0, box.Height) (colorAt theme i) ]

    let private spinnerGeom theme (box: Rect) : Scene list =
        let r = (min box.Width box.Height) / 2.0 - 8.0
        let cx = box.X + box.Width / 2.0
        let cy = box.Y + box.Height / 2.0
        let bounds: Rect = { X = cx - r; Y = cy - r; Width = 2.0 * r; Height = 2.0 * r }
        // A faint full-circle track plus a bold accent sweep with a gap reads as a busy spinner.
        [ Scene.arc bounds 0.0 360.0 (Paint.stroke theme.Muted 7.0)
          Scene.arc bounds -90.0 280.0 (Paint.stroke theme.Accent 7.0) ]

    let private imageGeom theme (box: Rect) (source: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 2.0)
          Scene.line { X = box.X; Y = box.Y } { X = box.X + box.Width; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.5)
          Scene.line { X = box.X + box.Width; Y = box.Y } { X = box.X; Y = box.Y + box.Height } (Paint.stroke theme.Muted 1.5)
          mkText theme (box.X + 6.0) (box.Y + box.Height - 6.0) 11.0 theme.Foreground source ]

    let private iconGeom theme (box: Rect) (name: string) : Scene list =
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
    let private buttonGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (kind: string) (intent: string) (label: string) : Scene list =
        let h = 38.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
        let w = min box.Width (max 70.0 (textW + 32.0))
        let by = box.Y + box.Height / 2.0 - h / 2.0
        let rect = { X = box.X; Y = by; Width = w; Height = h }

        let style = StyleResolver.resolveDefault theme kind intent classes state

        if kind = "button" then
            [ Scene.rectangle (box.X, by, w, h) style.Fill
              mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 style.Foreground label ]
        else
            [ Scene.rectangleWithPaint rect (Paint.stroke style.Stroke 2.0)
              mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 style.Foreground label ]

    /// A compact accent pill with light text — a status badge.
    let private badgeGeom theme (box: Rect) (label: string) : Scene list =
        let h = 26.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 12.0; Weight = None }).Width
        let w = max 40.0 (textW + 20.0)
        let by = box.Y + box.Height / 2.0 - h / 2.0
        [ Scene.rectangle (box.X, by, w, h) theme.Accent
          mkText theme (box.X + 10.0) (by + h / 2.0 + 4.0) 12.0 theme.Background label ]

    /// A primary command button joined to a dropdown trigger (caret) — a split button.
    let private splitGeom theme (box: Rect) (label: string) : Scene list =
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
    let private regionRect theme (x: float) (y: float) (w: float) (h: float) (fill: Color) (label: string) : Scene list =
        [ Scene.rectangle (x, y, w, h) fill
          Scene.rectangleWithPaint { X = x; Y = y; Width = w; Height = h } (Paint.stroke theme.Foreground 1.0)
          mkText theme (x + 6.0) (y + h / 2.0 + 4.0) 12.0 theme.Foreground label ]

    let private itemsOr (fallback: string list) (items: string list) =
        match items with
        | [] -> fallback
        | _ -> items

    /// Vertically stacked child regions — `stack`.
    let private stackGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "One"; "Two"; "Three" ] |> List.truncate 4
        let n = max 1 (List.length shown)
        let rowH = box.Height / float n
        shown |> List.mapi (fun i it -> regionRect theme box.X (box.Y + float i * rowH) box.Width (rowH - 4.0) theme.Muted it) |> List.concat

    /// A 2-column cell grid — `grid` (distinct from `data-grid`'s tabular `gridGeom`).
    let private gridLayoutGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "A1"; "B2"; "C3"; "D4" ] |> List.truncate 4
        let cols = 2
        let cw = box.Width / float cols
        let rows = max 1 ((List.length shown + cols - 1) / cols)
        let rh = box.Height / float rows
        shown
        |> List.mapi (fun i it -> regionRect theme (box.X + float (i % cols) * cw) (box.Y + float (i / cols) * rh) (cw - 5.0) (rh - 5.0) theme.Muted it)
        |> List.concat

    /// Small chips flowing left-to-right and wrapping — `wrap`.
    let private wrapGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private dockGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Top"; "Fill" ]
        let topH = 26.0
        let leftW = 72.0
        let bodyY = box.Y + topH + 2.0
        let bodyH = box.Height - topH - 2.0
        regionRect theme box.X box.Y box.Width topH theme.Accent (List.tryItem 0 shown |> Option.defaultValue "Top")
        @ regionRect theme box.X bodyY leftW bodyH theme.Muted "Left"
        @ regionRect theme (box.X + leftW + 2.0) bodyY (box.Width - leftW - 2.0) bodyH theme.Background (List.tryItem 1 shown |> Option.defaultValue "Fill")

    /// Two side-by-side panes with a divider — `split-view`.
    let private splitViewGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Left"; "Right" ]
        let half = box.Width / 2.0
        regionRect theme box.X box.Y (half - 4.0) box.Height theme.Muted (List.tryItem 0 shown |> Option.defaultValue "Left")
        @ [ Scene.rectangle (box.X + half - 2.0, box.Y, 4.0, box.Height) theme.Foreground ]
        @ regionRect theme (box.X + half + 4.0) box.Y (half - 4.0) box.Height theme.Background (List.tryItem 1 shown |> Option.defaultValue "Right")

    /// A command strip of horizontal buttons — `toolbar`.
    let private toolbarGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private panelGeom theme (box: Rect) (label: string) : Scene list =
        let headH = 26.0
        [ Scene.rectangle (box.X, box.Y, box.Width, headH) theme.Accent
          Scene.rectangleWithPaint box (Paint.stroke theme.Foreground 1.0) ]
        @ [ mkText theme (box.X + 8.0) (box.Y + box.Height / 2.0 + 8.0) 12.0 theme.Foreground label ]

    /// A thick border framing inner content — `border`.
    let private borderGeom theme (box: Rect) (label: string) : Scene list =
        let inset = 10.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Accent 4.0) ]
        @ regionRect theme (box.X + inset) (box.Y + inset) (box.Width - 2.0 * inset) (box.Height - 2.0 * inset) theme.Muted label

    /// A scrollable viewport: content area plus a vertical scrollbar thumb — `scroll-viewer`.
    let private scrollViewerGeom theme (box: Rect) (label: string) : Scene list =
        let barW = 10.0
        let contentW = box.Width - barW - 4.0
        regionRect theme box.X box.Y contentW box.Height theme.Muted label
        @ [ Scene.rectangle (box.X + contentW + 4.0, box.Y, barW, box.Height) theme.Muted
            Scene.rectangle (box.X + contentW + 4.0, box.Y + 6.0, barW, box.Height * 0.4) theme.Accent ]

    /// Two layered, offset surfaces suggesting stacked content — `overlay`.
    let private overlayGeom theme (box: Rect) (label: string) : Scene list =
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
    let private textFieldGeom theme (box: Rect) (classes: StyleClass list) (state: VisualState) (value: string) : Scene list =
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
    let private textAreaFieldGeom theme (box: Rect) (value: string) : Scene list =
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
    let private richTextGeom theme (box: Rect) (runs: (string * Color * float * int) list) : Scene list =
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
    let private separatorGeom theme (box: Rect) : Scene list =
        let cy = box.Y + box.Height / 2.0
        [ Scene.line { X = box.X; Y = cy } { X = box.X + box.Width; Y = cy } (Paint.stroke theme.Foreground 3.0) ]

    // ---- Feature 132 (D2.1) net-new Ant-overview control geometry ---------------------------
    // Generic, theme-agnostic schematics for the net-new controls. Each reads ONLY `theme` roles
    // (token-sourced) — never theme identity — so it renders neutrally under Default and Ant-styled
    // under AntDesign with no control edits (FR-007, contract R3/R4). The varied role usage
    // (Accent / Muted / Foreground / Background / Danger / Success / Warning) guarantees the
    // resolved paint diverges when the Ant palette differs from Default (FR-013).

    /// A compact filled pill with contrasting text — the shared shape for tag/segment chips.
    let private pillGeom theme (x: float) (cy: float) (fill: Color) (fg: Color) (label: string) : float * Scene list =
        let h = 24.0
        let textW = (measureText label { Family = theme.FontFamily; Size = 12.0; Weight = None }).Width
        let w = max 36.0 (textW + 18.0)
        let by = cy - h / 2.0
        w, [ Scene.rectangle (x, by, w, h) fill
             mkText theme (x + 9.0) (by + h / 2.0 + 4.0) 12.0 fg label ]

    /// A coloured status chip — `tag`.
    let private tagGeom theme (box: Rect) (label: string) : Scene list =
        snd (pillGeom theme box.X (box.Y + box.Height / 2.0) theme.Accent theme.Background (if label = "" then "tag" else label))

    /// A round monogram — `avatar`.
    let private avatarGeom theme (box: Rect) (label: string) : Scene list =
        let r = 18.0
        let cx = box.X + r
        let cy = box.Y + box.Height / 2.0
        [ Scene.circle { X = cx; Y = cy } r theme.Accent
          mkText theme (cx - 9.0) (cy + 5.0) 13.0 theme.Background (if label = "" then "?" else label) ]

    /// A framed surface with a header band — `card`.
    let private cardGeom theme (box: Rect) (title: string) : Scene list =
        let headH = 28.0
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          Scene.rectangle (box.X, box.Y, box.Width, headH) theme.Muted
          mkText theme (box.X + 10.0) (box.Y + 19.0) 14.0 theme.Foreground (if title = "" then "Card" else title)
          mkText theme (box.X + 10.0) (box.Y + headH + 22.0) 12.0 theme.Foreground "Card content" ]

    /// A label : value term list — `descriptions`.
    let private descriptionsGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Name"; "Ant"; "Status"; "Active" ] |> List.truncate 6
        shown
        |> List.mapi (fun i it ->
            let y = box.Y + 16.0 + float i * 22.0
            let fg = if i % 2 = 0 then theme.Muted else theme.Foreground
            mkText theme box.X y 12.0 fg it)

    /// A large emphasised metric over a caption — `statistic`.
    let private statisticGeom theme (box: Rect) (value: string) : Scene list =
        [ mkText theme box.X (box.Y + 18.0) 12.0 theme.Muted "Total"
          mkText theme box.X (box.Y + 46.0) 28.0 theme.Accent (if value = "" then "0" else value) ]

    /// A vertical dotted event rail — `timeline`.
    let private timelineGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Created"; "Shipped"; "Delivered" ] |> List.truncate 6
        shown
        |> List.mapi (fun i it ->
            let y = box.Y + 16.0 + float i * 24.0
            [ Scene.circle { X = box.X + 6.0; Y = y - 4.0 } 4.0 theme.Accent
              mkText theme (box.X + 20.0) y 12.0 theme.Foreground it ])
        |> List.concat

    /// A muted "no data" placeholder with a framed glyph — `empty`.
    let private emptyGeom theme (box: Rect) (caption: string) : Scene list =
        let cx = box.X + box.Width / 2.0
        [ Scene.rectangleWithPaint { X = cx - 28.0; Y = box.Y + 10.0; Width = 56.0; Height = 36.0 } (Paint.stroke theme.Muted 1.0)
          mkText theme (cx - 28.0) (box.Y + 64.0) 12.0 theme.Muted (if caption = "" then "No data" else caption) ]

    /// Grey placeholder bars — `skeleton`.
    let private skeletonGeom theme (box: Rect) : Scene list =
        [ 0; 1; 2 ]
        |> List.map (fun i ->
            let w = box.Width * (if i = 2 then 0.6 else 0.9)
            Scene.rectangle (box.X, box.Y + 12.0 + float i * 20.0, w, 12.0) theme.Muted)

    /// A square module grid — `qr-code`.
    let private qrCodeGeom theme (box: Rect) : Scene list =
        let n = 7
        let side = min box.Width box.Height - 8.0
        let cell = side / float n
        [ for r in 0 .. n - 1 do
            for c in 0 .. n - 1 do
                if (r + c + r * c) % 2 = 0 then
                    yield Scene.rectangle (box.X + float c * cell, box.Y + float r * cell, cell - 1.0, cell - 1.0) theme.Foreground ]

    /// Faint repeated brand text — `watermark`.
    let private watermarkGeom theme (box: Rect) (label: string) : Scene list =
        let text = if label = "" then "FS.GG" else label
        let paintFaint = Paint.withOpacity 0.25 (Paint.fill theme.Muted)
        [ for r in 0 .. 2 do
            yield Scene.textRun
                { Text = text
                  Position = { X = box.X + float (r % 2) * 60.0; Y = box.Y + 24.0 + float r * 28.0 }
                  Font = { Family = theme.FontFamily; Size = 14.0; Weight = None }
                  Paint = paintFaint } ]

    /// A coloured information banner — `alert` (warning role so it diverges from accent controls).
    let private alertGeom theme (box: Rect) (label: string) : Scene list =
        let h = 36.0
        [ Scene.rectangle (box.X, box.Y, box.Width, h) theme.Warning
          Scene.rectangle (box.X, box.Y, 4.0, h) theme.Danger
          mkText theme (box.X + 12.0) (box.Y + h / 2.0 + 4.0) 13.0 theme.Background (if label = "" then "Alert" else label) ]

    /// A centred outcome panel: status dot + title — `result`.
    let private resultGeom theme (box: Rect) (title: string) : Scene list =
        let cx = box.X + box.Width / 2.0
        [ Scene.circle { X = cx; Y = box.Y + 26.0 } 14.0 theme.Success
          mkText theme (cx - 30.0) (box.Y + 62.0) 14.0 theme.Foreground (if title = "" then "Success" else title) ]

    /// A right-edge sliding surface — `drawer`.
    let private drawerGeom theme (box: Rect) (title: string) : Scene list =
        let w = box.Width * 0.55
        let x = box.X + box.Width - w
        [ Scene.rectangle (box.X, box.Y, box.Width, box.Height) theme.Muted
          Scene.rectangle (x, box.Y, w, box.Height) theme.Background
          Scene.rectangleWithPaint { X = x; Y = box.Y; Width = w; Height = box.Height } (Paint.stroke theme.Muted 1.0)
          mkText theme (x + 10.0) (box.Y + 22.0) 13.0 theme.Foreground (if title = "" then "Drawer" else title) ]

    /// A small floating callout box — `popover` (and the base for popconfirm/tour).
    let private popoverGeom theme (box: Rect) (label: string) (withActions: bool) : Scene list =
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
    let private floatButtonGeom theme (box: Rect) (label: string) : Scene list =
        let r = 22.0
        let cx = box.X + box.Width - r - 6.0
        let cy = box.Y + box.Height - r - 6.0
        [ Scene.circle { X = cx; Y = cy } r theme.Accent
          mkText theme (cx - 5.0) (cy + 6.0) 18.0 theme.Background (if label = "" then "+" else label) ]

    /// A trail of separated path labels — `breadcrumb`.
    let private breadcrumbGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private stepsGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private paginationGeom theme (box: Rect) (total: int) : Scene list =
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
    let private segmentedGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private anchorGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Intro"; "Usage"; "API" ] |> List.truncate 6
        [ yield Scene.rectangle (box.X, box.Y, 2.0, box.Height) theme.Muted
          for i, it in List.indexed shown do
            let fg = if i = 0 then theme.Accent else theme.Muted
            yield mkText theme (box.X + 12.0) (box.Y + 16.0 + float i * 22.0) 12.0 fg it ]

    /// A pinned-to-top bar — `affix`.
    let private affixGeom theme (box: Rect) (label: string) : Scene list =
        [ Scene.rectangle (box.X, box.Y, box.Width, 30.0) theme.Accent
          mkText theme (box.X + 10.0) (box.Y + 20.0) 13.0 theme.Background (if label = "" then "Affixed" else label) ]

    /// Stacked expandable section headers — `collapse`.
    let private collapseGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Panel 1"; "Panel 2"; "Panel 3" ] |> List.truncate 5
        shown
        |> List.mapi (fun i it ->
            let y = box.Y + float i * 30.0
            [ Scene.rectangle (box.X, y, box.Width, 28.0) theme.Muted
              mkText theme (box.X + 24.0) (y + 19.0) 12.0 theme.Foreground it
              mkText theme (box.X + 8.0) (y + 19.0) 12.0 theme.Accent (if i = 0 then "-" else "+") ])
        |> List.concat

    /// A row of star glyphs, the leading ones filled — `rate`.
    let private rateGeom theme (box: Rect) (value: float) : Scene list =
        let filled = int (value + 0.5)
        let cy = box.Y + box.Height / 2.0
        [ for i in 0 .. 4 do
            let color = if i < filled then theme.Warning else theme.Muted
            yield Scene.circle { X = box.X + 14.0 + float i * 26.0; Y = cy } 9.0 color ]

    /// A framed slide with position dots — `carousel`.
    let private carouselGeom theme (box: Rect) (items: string list) : Scene list =
        let shown = items |> itemsOr [ "Slide 1"; "Slide 2"; "Slide 3" ]
        let label = List.head shown
        [ yield Scene.rectangle (box.X, box.Y, box.Width, box.Height - 16.0) theme.Muted
          yield mkText theme (box.X + 12.0) (box.Y + box.Height / 2.0) 14.0 theme.Foreground label
          for i in 0 .. List.length shown - 1 do
            let color = if i = 0 then theme.Accent else theme.Background
            yield Scene.circle { X = box.X + box.Width / 2.0 - 12.0 + float i * 12.0; Y = box.Y + box.Height - 6.0 } 4.0 color ]

    /// A month day-cell grid — `calendar`.
    let private calendarGeom theme (box: Rect) : Scene list =
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
    let private cascaderGeom theme (box: Rect) (items: string list) : Scene list =
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
    let private autoCompleteGeom theme (box: Rect) (value: string) : Scene list =
        [ Scene.rectangleWithPaint { X = box.X; Y = box.Y; Width = box.Width; Height = 30.0 } (Paint.stroke theme.Accent 1.5)
          mkText theme (box.X + 8.0) (box.Y + 20.0) 13.0 theme.Foreground (if value = "" then "Search…" else value)
          Scene.rectangle (box.X, box.Y + 34.0, box.Width, 54.0) theme.Background
          Scene.rectangleWithPaint { X = box.X; Y = box.Y + 34.0; Width = box.Width; Height = 54.0 } (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + 8.0) (box.Y + 52.0) 12.0 theme.Muted "Suggestion 1"
          mkText theme (box.X + 8.0) (box.Y + 74.0) 12.0 theme.Muted "Suggestion 2" ]

    /// A dashed drop zone with an upload action — `upload`.
    let private uploadGeom theme (box: Rect) (label: string) : Scene list =
        [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0)
          mkText theme (box.X + box.Width / 2.0 - 40.0) (box.Y + box.Height / 2.0 - 6.0) 12.0 theme.Muted "Drop files here" ]
        @ snd (pillGeom theme (box.X + box.Width / 2.0 - 30.0) (box.Y + box.Height / 2.0 + 18.0) theme.Accent theme.Background (if label = "" then "Upload" else label))

    /// Dispatch a rich-family control to its faithful geometry (within `box`, below the title).
    let faithfulContent (theme: Theme) (box: Rect) (control: Control<'msg>) : Scene list =
        let label = control.Content |> Option.defaultValue ""
        let items = stringListOf "items" control
        // Feature 093 (E3): attached style classes + current VisualState for the migrated kinds.
        // `state` is `Normal` unless a `visualState` attribute is present, so the no-class default
        // case stays byte-identical to the prior procedural output (FR-005). The state rides the
        // control's attributes, so it travels through the keyed reconciler and a state-driven look
        // survives a sibling-shifting re-render under the retained identity (FR-006, SC-005).
        let classes = styleClassesOf control.Attributes
        let state = visualStateOf control.Attributes
        // Feature 129 (F4): the semantic intent — lowered to the `style` attribute by `Button.view`
        // (`Primitives.fs:99`) and, until now, never read by the renderer (so `Danger` ≡ `Primary`).
        // It is now extracted and threaded into the central resolver. A missing attribute defaults
        // to the neutral `"primary"`. The default (neutral) policy still ignores it, so this is
        // byte-identical under the default theme — but the value now reaches resolution (FR-002, R3).
        let intent = textValueOf "style" control |> Option.defaultValue "primary"
        match control.Kind with
        | "line-chart" -> lineGeom theme box (chartValues control)
        | "bar-chart" -> barGeom theme box (chartValues control)
        | "pie-chart" -> pieGeom theme box (chartValues control)
        | "scatter-plot" -> scatterGeom theme box (chartValues control)
        | "graph-view" -> graphGeom theme box (chartValues control)
        // Feature 133 (D2C.1) — net-new generic charts (theme-role-driven, no theme-identity branch).
        | "area-chart" -> areaGeom theme box (chartValues control)
        | "column-chart" -> columnGeom theme box (chartValues control)
        | "histogram" -> histogramGeom theme box (chartValues control)
        | "box-plot" -> boxPlotGeom theme box (chartValues control)
        | "heatmap" -> heatmapGeom theme box (chartValues control)
        | "radar-chart" -> radarGeom theme box (chartValues control)
        | "rose-chart" -> roseGeom theme box (chartValues control)
        | "waterfall-chart" -> waterfallGeom theme box (chartValues control)
        | "funnel-chart" -> funnelGeom theme box (chartValues control)
        | "gauge-chart" -> gaugeGeom theme box (floatValue "value" 0.5 control.Attributes)
        | "sankey-diagram" -> sankeyGeom theme box (chartValues control)
        | "chord-diagram" -> chordGeom theme box (chartValues control)
        | "treemap" -> treemapGeom theme box (chartValues control)
        | "sunburst" -> sunburstGeom theme box (chartValues control)
        | "list-view"
        | "list-box"
        | "multi-select-list"
        | "combo-box"
        | "tree-view"
        | "menu"
        | "context-menu" ->
            rowsGeom theme box (stringListOf "items" control) (stringListOf "selectedKeys" control |> Set.ofList)
        | "data-grid" -> gridGeom theme box (itemsOr [ "Name"; "Qty"; "Widget"; "12"; "Gadget"; "7" ] items)
        | "radio-group" -> radioGeom theme box classes state (stringListOf "items" control) (textValueOf "value" control)
        | "tabs" -> tabsGeom theme box (stringListOf "items" control) (textValueOf "value" control)
        | "slider" -> sliderGeom theme box classes state (floatValue "value" 0.5 control.Attributes)
        | "progress-bar" -> progressGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "numeric-input" -> numericGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "switch" -> switchGeom theme box classes state (boolValue "selected" false control.Attributes)
        | "check-box" -> checkboxGeom theme box classes state (boolValue "selected" false control.Attributes) label
        // command / button family
        | "button" -> buttonGeom theme box classes state "button" intent label
        | "icon-button" -> buttonGeom theme box classes state "icon-button" intent label
        | "badge" -> badgeGeom theme box label
        | "toggle-button" -> toggleGeom theme box (boolValue "selected" true control.Attributes) label
        | "split-button" -> splitGeom theme box label
        // layout / container family
        | "stack" -> stackGeom theme box items
        | "grid" -> gridLayoutGeom theme box items
        | "dock" -> dockGeom theme box items
        | "wrap" -> wrapGeom theme box items
        | "split-view" -> splitViewGeom theme box items
        | "toolbar" -> toolbarGeom theme box items
        | "panel" -> panelGeom theme box (if label = "" then "Panel content" else label)
        | "border" -> borderGeom theme box (if label = "" then "Bordered" else label)
        | "scroll-viewer" -> scrollViewerGeom theme box (if label = "" then "Scrollable content" else label)
        | "overlay" -> overlayGeom theme box (if label = "" then "Overlaid content" else label)
        | "date-picker"
        | "time-picker" -> pickerGeom theme box (control.Content |> Option.defaultValue control.Kind)
        | "color-picker" -> swatchGeom theme box
        | "spinner" -> spinnerGeom theme box
        | "image" -> imageGeom theme box (textValueOf "value" control |> Option.defaultValue "image")
        // text-input / rich-text / divider family (feature 082)
        | "text-box" -> textFieldGeom theme box classes state (textValueOf "value" control |> Option.defaultValue "")
        | "text-area" -> textAreaFieldGeom theme box (textValueOf "value" control |> Option.defaultValue "")
        | "rich-text" -> richTextGeom theme box (richTextRuns control)
        | "separator" -> separatorGeom theme box
        // Feature 132 (D2.1) — net-new Ant-overview controls. All paint flows from `theme` roles
        // (and, where intent matters, the resolver) — no branch on theme identity (FR-007, R4).
        | "tag" -> tagGeom theme box label
        | "avatar" -> avatarGeom theme box label
        | "card" -> cardGeom theme box label
        | "descriptions" -> descriptionsGeom theme box (stringListOf "items" control)
        | "statistic" -> statisticGeom theme box (textValueOf "value" control |> Option.orElse (control.Content) |> Option.defaultValue "")
        | "timeline" -> timelineGeom theme box (stringListOf "items" control)
        | "empty" -> emptyGeom theme box label
        | "skeleton" -> skeletonGeom theme box
        | "qr-code" -> qrCodeGeom theme box
        | "watermark" -> watermarkGeom theme box label
        | "alert" -> alertGeom theme box label
        | "result" -> resultGeom theme box label
        | "drawer" -> drawerGeom theme box label
        | "popover" -> popoverGeom theme box label false
        | "popconfirm" -> popoverGeom theme box (if label = "" then "Confirm?" else label) true
        | "tour" -> popoverGeom theme box (if label = "" then "Step 1 of 3" else label) true
        | "float-button" -> floatButtonGeom theme box label
        | "breadcrumb" -> breadcrumbGeom theme box (stringListOf "items" control)
        | "steps" -> stepsGeom theme box (stringListOf "items" control)
        | "pagination" -> paginationGeom theme box (int (floatValue "value" 4.0 control.Attributes))
        | "segmented" -> segmentedGeom theme box (stringListOf "items" control)
        | "anchor" -> anchorGeom theme box (stringListOf "items" control)
        | "affix" -> affixGeom theme box label
        | "collapse" -> collapseGeom theme box (stringListOf "items" control)
        | "rate" -> rateGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "carousel" -> carouselGeom theme box (stringListOf "items" control)
        | "calendar" -> calendarGeom theme box
        | "cascader" -> cascaderGeom theme box (stringListOf "items" control)
        | "auto-complete" -> autoCompleteGeom theme box (textValueOf "value" control |> Option.defaultValue "")
        | "upload" -> uploadGeom theme box label
        | "icon" ->
            let name =
                control.Content
                |> Option.orElseWith (fun () -> textValueOf "text" control)
                |> Option.defaultValue "icon"
            iconGeom theme box name
        | other -> emptyState theme box other

    /// Feature 113 (Phase 5) — the resolved cell/header data the `data-grid` row/column projection
    /// (`gridGeom`) consumes: the control's `items` attribute, or the same sample fallback
    /// `faithfulContent` substitutes when none is authored. This is the projection's sole control-borne
    /// input; the memoization seam (`RetainedRender.memoize`) folds it with the theme + evaluated box
    /// into the deterministic dependency value (an equal value ⇒ a byte-identical projection, FR-006).
    let dataGridCells (control: Control<'msg>) : string list =
        itemsOr [ "Name"; "Qty"; "Widget"; "12"; "Gadget"; "7" ] (stringListOf "items" control)

    let renderNode (theme: Theme) y (control: Control<'msg>) =
        let width = nodeWidth control
        let height = nodeHeight control
        let visible = boolValue "visible" true control.Attributes
        let label = control.Content |> Option.defaultValue control.Kind

        if not visible then
            Scene.group [ Scene.rectangle (0.0, y, width, height) Colors.transparent ]
        elif Set.contains control.Kind richFamilies then
            // Title band on top; control-specific geometry below it (within the canvas).
            let pad = 10.0
            let titleH = 30.0
            let box: Rect = { X = pad; Y = y + titleH; Width = width - 2.0 * pad; Height = height - titleH - pad }
            // Title band shows the control's NAME (the schematic below shows its content); this
            // fixes composite-lowering title bleed and content duplication for rich families.
            let title =
                Scene.clipped
                    (RectClip { X = 0.0; Y = y; Width = width; Height = titleH })
                    (mkText theme 8.0 (y + 19.0) 13.0 theme.Foreground (prettyKind control.Kind))
            Scene.group (title :: faithfulContent theme box control)
        else
            // Text / container controls: the control IS its text, so box + clipped label is faithful.
            let fill =
                if disabledOrReadOnly control then theme.Muted
                elif boolValue "selected" false control.Attributes then theme.Accent
                else theme.Background
            let fontSize = fittedFontSize theme.FontSize 6.0 width height theme.FontFamily label
            let textY = y + (height + fontSize) * 0.5 - 3.0
            let labelRun =
                { Text = label
                  Position = { X = 8.0; Y = textY }
                  Font = { Family = theme.FontFamily; Size = fontSize; Weight = None }
                  Paint = Paint.fill theme.Foreground }
            Scene.group [
                Scene.rectangle (0.0, y, width, height) fill
                Scene.clipped
                    (RectClip { X = 0.0; Y = y; Width = width; Height = height })
                    (Scene.textRun labelRun)
            ]

    let renderScene (theme: Theme) (control: Control<'msg>) =
        let controls = recursively (fun control -> [ control ]) control

        ((0.0, []), controls)
        ||> List.fold (fun (y, scenes) control ->
            let height = nodeHeight control
            y + height + 4.0, renderNode theme y control :: scenes)
        |> snd
        |> List.rev
        |> Scene.group

    let rec layoutNode (theme: Theme) (control: Control<'msg>) : FS.GG.UI.Layout.LayoutNode =
        // Feature 102 (R8): this `Key ?? Kind` is the legacy 080 single-control *preview*/layout id, local
        // to this offscreen `layoutNode` path. It is intentionally NOT the R3-unified `Key ?? path`
        // dispatch/recovery id (feature 098) — the divergence R3 removed was on the live dispatch path, not
        // here. Left as-is so a future reader does not "fix" it into the path-based scheme.
        let id = control.Key |> Option.defaultValue control.Kind
        let width = floatValue AttrWidth 240.0 control.Attributes
        let height = floatValue AttrHeight 28.0 control.Attributes
        let content = renderScene theme control
        let children = control.Children |> List.map (layoutNode theme)

        { LayoutDefaults.layoutNode id with
            Intent =
                { LayoutDefaults.layoutIntent with
                    Size = { Width = Some width; Height = Some height } }
            Content = Some content
            Children = children }

    let duplicateDiagnostics (control: Control<'msg>) =
        control.Attributes
        |> List.countBy _.Name
        |> List.choose (fun (name, count) ->
            if count > 1 then
                Some(Diagnostics.duplicateAttribute control.Key control.Kind name)
            else
                None)

    let requiredDiagnostics (control: Control<'msg>) =
        required control.Kind
        |> List.choose (fun name ->
            if hasAttr name control.Attributes then
                None
            else
                Some(Diagnostics.missingRequired control.Key control.Kind name))

    let keyDiagnostics (control: Control<'msg>) =
        recursively (fun control -> [ control ]) control
        |> List.choose (fun control -> control.Key |> Option.map (fun key -> key, control.Kind))
        |> List.groupBy fst
        |> List.collect (fun (key, rows) ->
            if rows.Length > 1 then
                rows |> List.tail |> List.map (fun (_, kind) -> Diagnostics.keyCollision key kind)
            else
                [])

    let controlDiagnostics (control: Control<'msg>) =
        duplicateDiagnostics control
        @ requiredDiagnostics control
        @ Accessibility.validate control

    // Feature 091 (RETAINED-PATH-1 / PARTIAL-UPDATE-1): the per-node measure + paint of
    // `Control.renderTree`, factored OUT of the render body so a single node's painted
    // contribution is a reusable unit. `Control.renderTree` and `module internal
    // RetainedRender` BOTH drive their Scene from `evaluateLayout` + `paintNode`, so the
    // wired retained path is byte-for-byte identical to a full rebuild BY CONSTRUCTION
    // (FR-005, contract C2) — the only divergence point removed entirely.
    let private orientationOf (c: Control<'msg>) =
        tryLast AttrOrientation c.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | TextValue value -> Some value
            | _ -> None)

    let private directionOf (c: Control<'msg>) =
        match c.Kind with
        | "toolbar"
        | "split-view"
        | "wrap"
        | "grid"
        | "dock" -> FS.GG.UI.Layout.Row
        | _ ->
            match orientationOf c with
            | Some "horizontal" -> FS.GG.UI.Layout.Row
            | _ -> FS.GG.UI.Layout.Column

    let private wrapOf kind =
        match kind with
        | "wrap"
        | "grid" -> FS.GG.UI.Layout.Wrap
        | _ -> FS.GG.UI.Layout.NoWrap

    /// Feature 097 (R2): the attribute NAMES the incremental dirty-set classifier (`layoutDirtySet`)
    /// keys on, so a change to a geometry-driving attribute re-measures while a content/style/state/
    /// visual-state change does not (SC-004). These are the same names `toLayout` (below) reads to
    /// derive geometry — `Size` from `width`/`height`, `Direction` from `orientation`.
    ///
    /// Feature 101 (R7): this literal is a SEPARATE, hot-path `Set` from `toLayout`'s reads — it is NOT
    /// auto-derived from them, so the two agree by maintenance discipline alone. That agreement is now
    /// *gated*, not merely asserted: the behavioral-probe equality gate in
    /// `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs` toggles each candidate attribute on
    /// representative fixtures, observes which names actually change the real `evaluateLayout` output,
    /// and fails the build the instant this set drifts from what `toLayout` reads (either direction).
    /// The shared `[<Literal>]` name tokens above remove typo drift; the gate makes membership drift
    /// impossible to ship. (A change tagged `AttrCategory.Layout` is honoured by `layoutDirtySet`
    /// independently of this name set, so a future categorised attr needs no edit here — that
    /// independence is pinned by the same test file.)
    let layoutAffectingAttrNames: Set<string> = Set.ofList [ AttrWidth; AttrHeight; AttrOrientation ]

    let rec private toLayout (path: string) (c: Control<'msg>) : FS.GG.UI.Layout.LayoutNode =
        let id = c.Key |> Option.defaultValue path
        let isLeaf = List.isEmpty c.Children

        let size: FS.GG.UI.Layout.LayoutSize =
            if isLeaf then
                { Width = Some(nodeWidth c)
                  Height = Some(nodeHeight c) }
            else
                { Width = (if hasAttr AttrWidth c.Attributes then Some(nodeWidth c) else None)
                  Height = (if hasAttr AttrHeight c.Attributes then Some(nodeHeight c) else None) }

        { LayoutDefaults.layoutNode id with
            Intent =
                { LayoutDefaults.layoutIntent with
                    Direction = directionOf c
                    Wrap = wrapOf c.Kind
                    Gap = { Row = 8.0; Column = 8.0 }
                    Padding = { Left = 8.0; Top = 8.0; Right = 8.0; Bottom = 8.0 }
                    Size = size }
            Children = c.Children |> List.mapi (fun index child -> toLayout (path + "." + string index) child) }

    /// Build the nested Yoga layout tree for `control` at `size`, evaluate it, and return the
    /// root `LayoutNode` plus the evaluated absolute bounds keyed by the SAME collision-free
    /// structural id (`Key |> defaultValue path`) the paint/bounds passes look up.
    let private availableOf (size: FS.GG.UI.Scene.Size) : FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let private boundsByIdOf (result: FS.GG.UI.Layout.LayoutResult) =
        result.Bounds
        |> List.map (fun (b: FS.GG.UI.Layout.ComputedBounds) -> b.NodeId, b.Bounds)
        |> Map.ofList

    let evaluateLayout (size: FS.GG.UI.Scene.Size) (control: Control<'msg>) =
        let root = toLayout "0" control
        let result = FS.GG.UI.Layout.Layout.evaluate (availableOf size) root
        root, boundsByIdOf result, result

    /// Feature 097 (R2): the incremental render-path seam (contract C4). Drives layout through
    /// `Layout.evaluateIncremental`, threading the previous frame's `LayoutResult` (the bounds cache)
    /// and the patch-derived `dirty` set. Returns the same `root, boundsById` shape `evaluateLayout`
    /// returns (so the reuse-driven paint walk is unchanged) plus the new `LayoutResult` to carry
    /// forward. `Bounds` are byte-identical to a full `evaluateLayout` (INV-1).
    let evaluateLayoutIncremental
        (size: FS.GG.UI.Scene.Size)
        (control: Control<'msg>)
        (previous: FS.GG.UI.Layout.LayoutResult)
        (dirty: Set<FS.GG.UI.Layout.LayoutNodeId>)
        =
        let root = toLayout "0" control
        let result = FS.GG.UI.Layout.Layout.evaluateIncremental previous (Set.toList dirty) (availableOf size) root
        root, boundsByIdOf result, result

    let private paintLeaf (theme: Theme) (box: Rect) (c: Control<'msg>) : Scene list =
        if Set.contains c.Kind richFamilies then
            faithfulContent theme box c
        else
            let label = c.Content |> Option.defaultValue c.Kind

            let fill =
                if disabledOrReadOnly c then theme.Muted
                elif boolValue "selected" false c.Attributes then theme.Accent
                else theme.Background

            let fontSize =
                fittedFontSize theme.FontSize 6.0 box.Width box.Height theme.FontFamily label

            let textY = box.Y + (box.Height + fontSize) * 0.5 - 3.0

            let labelRun =
                { Text = label
                  Position = { X = box.X + 8.0; Y = textY }
                  Font = { Family = theme.FontFamily; Size = fontSize; Weight = None }
                  Paint = Paint.fill theme.Foreground }

            [ Scene.rectangle (box.X, box.Y, box.Width, box.Height) fill
              Scene.clipped (RectClip box) (Scene.textRun labelRun) ]

    /// Paint ONE node's own contribution (`here`) at its computed box — the reusable unit a
    /// retained `RenderFragment` caches. Output depends ONLY on `theme`, the looked-up box,
    /// and the node's own `Kind`/`Content`/`Attributes`/has-children — never on its
    /// descendants — so caching it keyed by (own-paint identity, box) is sound (feature 091,
    /// contracts C2/C5). Returns `[]` for a node with no computed box.
    let paintNode
        (theme: Theme)
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (path: string)
        (c: Control<'msg>)
        : Scene list =
        let id = c.Key |> Option.defaultValue path

        match Map.tryFind id boundsById with
        | None -> []
        | Some(b: FS.GG.UI.Layout.LayoutBounds) ->
            let box: Rect = { X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }

            if List.isEmpty c.Children then
                paintLeaf theme box c
            else
                // Container: a faint frame so the nesting is visible; the real children are
                // painted by their own `paintNode` at their own computed bounds.
                [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0) ]

    /// The evaluated absolute box of a node, looked up by the same structural id `paintNode`
    /// uses (`Key |> defaultValue path`). `None` when the node was not laid out.
    let nodeBox
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (path: string)
        (c: Control<'msg>)
        : Rect option =
        let id = c.Key |> Option.defaultValue path

        Map.tryFind id boundsById
        |> Option.map (fun (b: FS.GG.UI.Layout.LayoutBounds) ->
            { X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }: Rect)

    /// The evaluated `Bounds` list (`ControlId * Rect`) `renderTree` surfaces, computed from a
    /// pre-evaluated `boundsById` so the retained path produces the identical list.
    let collectBoundsWith
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (control: Control<'msg>)
        : (ControlId * Rect) list =
        let rec go (path: string) (c: Control<'msg>) : (ControlId * Rect) list =
            // FR-001/FR-007 (feature 098): the emitted `ControlId` is the unified `Key ?? path`
            // (`layoutId`) — the same id `EventBindings`/`BoundIds`/recovery use — replacing the old
            // divergent `Key ?? Kind`. Keyed nodes are unchanged; unkeyed ids shift `Kind → path`.
            let layoutId = c.Key |> Option.defaultValue path
            let controlId: ControlId = layoutId

            let here =
                match Map.tryFind layoutId boundsById with
                | Some(b: FS.GG.UI.Layout.LayoutBounds) -> [ controlId, ({ X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }: Rect) ]
                | None -> []

            here
            @ (c.Children
               |> List.mapi (fun index child -> go (path + "." + string index) child)
               |> List.concat)

        go "0" control

    /// The recursive `EventBindings` list `renderTree` surfaces, factored so the retained path
    /// emits the identical list. Path-aware (FR-001): re-derives each node's `parent + "." + index`
    /// path (root "0") so an unkeyed node's binding keys by its `path`, not its `Kind`.
    let eventBindingsOf (control: Control<'msg>) : ControlEventBinding<'msg> list =
        let rec go (path: string) (c: Control<'msg>) : ControlEventBinding<'msg> list =
            eventBindings path c
            @ (c.Children
               |> List.mapi (fun index child -> go (path + "." + string index) child)
               |> List.concat)

        go "0" control

    /// Feature 098 (FR-002) — the canonical ids (`Key ?? path`) of every node carrying ≥1 event
    /// binding, collected over the same positional path scheme as `eventBindingsOf`/`collectBoundsWith`.
    /// The single source for `ControlRenderResult.BoundIds` at every construction site (the full
    /// rebuild AND the retained frames), so the retained path is byte-identical by construction.
    let boundIdsOf (control: Control<'msg>) : Set<ControlId> =
        let rec go (path: string) (c: Control<'msg>) (acc: Set<ControlId>) : Set<ControlId> =
            let acc =
                if List.isEmpty (eventBindings path c) then
                    acc
                else
                    Set.add (c.Key |> Option.defaultValue path) acc

            c.Children
            |> List.mapi (fun index child -> index, child)
            |> List.fold (fun acc (index, child) -> go (path + "." + string index) child acc) acc

        go "0" control Set.empty

module Control =
    let create kind (attrs: Attr<'msg> list) =
        let text = ControlInternals.textFrom attrs
        let children = ControlInternals.childrenFrom attrs

        { Kind = kind
          Key = None
          Attributes = attrs
          Children = children
          Content = text
          Accessibility = ControlInternals.accessibility kind attrs text }

    let standard kind attrs =
        create (StandardControlKindHelpers.toControlKind kind) attrs

    let customControl kind attrs =
        create kind attrs

    let lowerStandard (control: Control<'msg>) =
        control

    let lowerCustom (control: Control<'msg>) =
        control

    let withKey key (control: Control<'msg>) =
        { control with Key = Some key }

    // Feature 108 (US5, FR-014): rewrite a single AttrValue's message type. Only the two
    // handler-bearing cases (`MessageValue`/`EventValue`) actually thread `f`; the nested-control
    // cases recurse; every data-only case is reconstructed verbatim in the `'b` DU. Total over the
    // closed `AttrValue` set.
    let rec private mapAttrValue (f: 'a -> 'b) (value: AttrValue<'a>) : AttrValue<'b> =
        match value with
        | TextValue v -> TextValue v
        | BoolValue v -> BoolValue v
        | FloatValue v -> FloatValue v
        | StringListValue v -> StringListValue v
        | ValidationValue v -> ValidationValue v
        | StyleClassesValue v -> StyleClassesValue v
        | VisualStateValue v -> VisualStateValue v
        | SlotFillsValue fills -> SlotFillsValue(fills |> List.map (fun (name, child) -> name, mapControl f child))
        | AccessibilityValue v -> AccessibilityValue v
        | ThemeValue v -> ThemeValue v
        | ChildValue child -> ChildValue(mapControl f child)
        | ChildrenValue children -> ChildrenValue(children |> List.map (mapControl f))
        | MessageValue msg -> MessageValue(f msg)
        | EventValue handler -> EventValue(handler >> f)
        | UntypedValue v -> UntypedValue v

    and mapControl (f: 'a -> 'b) (control: Control<'a>) : Control<'b> =
        { Kind = control.Kind
          Key = control.Key
          Attributes =
            control.Attributes
            |> List.map (fun attr ->
                { Name = attr.Name
                  Category = attr.Category
                  Value = mapAttrValue f attr.Value })
          Children = control.Children |> List.map (mapControl f)
          Content = control.Content
          Accessibility = control.Accessibility }

    let map (f: 'a -> 'b) (control: Control<'a>) : Control<'b> = mapControl f control

    let rec count (control: Control<'msg>) =
        1 + (control.Children |> List.sumBy count)

    let diagnostics (control: Control<'msg>) =
        ControlInternals.recursively ControlInternals.controlDiagnostics control
        @ ControlInternals.keyDiagnostics control

    let render (theme: Theme) (control: Control<'msg>) =
        { Scene = ControlInternals.renderScene theme control
          Layout = ControlInternals.layoutNode theme control
          // The 080 single-control PREVIEW does not expose per-control evaluated bounds;
          // that is a `renderTree` (nested layout) feature (FR-011). Kept empty here so the
          // preview Scene stays byte-identical (FR-010).
          Bounds = []
          Diagnostics = diagnostics control
          EventBindings = ControlInternals.eventBindingsOf control
          // FR-002 (feature 098): the preview keeps `Bounds = []` but DOES populate `BoundIds`
          // (mirroring its populated `EventBindings`) in the unified `Key ?? path` scheme.
          BoundIds = ControlInternals.boundIdsOf control
          NodeCount = count control }

    // Feature 085 (FR-001/FR-002/FR-003) — faithful NESTED-tree renderer.
    //
    // Unlike `render` (the 080 single-control preview, which flattens every descendant and
    // stacks them at fixed y offsets), `renderTree` runs a REAL recursive Yoga layout over the
    // nested tree at the supplied output `size`, then paints every node — containers AND their
    // children — at its COMPUTED bounds. Two structurally different trees therefore produce
    // visibly different scenes (SC-001). `render`/`Widget.render` are left untouched (FR-003).
    //
    // Feature 091: the per-node measure (`evaluateLayout`/`toLayout`) and paint (`paintNode`)
    // are factored into `ControlInternals` so `module internal RetainedRender` drives its
    // Scene from the SAME functions — the retained/partial render path is byte-for-byte
    // identical to this full rebuild by construction (the only divergence point removed).
    // `next frame is produced by diffing against the retained previous tree` (FR-005, C2).
    let renderTree (theme: Theme) (size: FS.GG.UI.Scene.Size) (control: Control<'msg>) =
        let root, boundsById, _ = ControlInternals.evaluateLayout size control

        let rec paint (path: string) (c: Control<'msg>) : Scene list =
            ControlInternals.paintNode theme boundsById path c
            @ (c.Children
               |> List.mapi (fun index child -> paint (path + "." + string index) child)
               |> List.concat)

        { Scene = paint "0" control |> Scene.group
          Layout = root
          Bounds = ControlInternals.collectBoundsWith boundsById control
          Diagnostics = diagnostics control
          EventBindings = ControlInternals.eventBindingsOf control
          BoundIds = ControlInternals.boundIdsOf control
          NodeCount = count control }

    // FR-012: resolve which rendered control (if any) contains the point (x, y), from the
    // public render result alone — `None` in a gap. Layered over `Layout.hitTestComputed` by
    // reconstructing a `LayoutResult` whose `NodeId`s ARE the `ControlId`s in `Bounds`, so the
    // shipped topmost-wins (reverse-scan) semantics return the deepest containing control.
    let hitTest (result: ControlRenderResult<'msg>) (x: float) (y: float) : ControlId option =
        let computed: FS.GG.UI.Layout.LayoutResult =
            { Bounds =
                result.Bounds
                |> List.map (fun (controlId, (rect: Rect)) ->
                    { NodeId = controlId
                      Bounds = { X = rect.X; Y = rect.Y; Width = rect.Width; Height = rect.Height }
                      Visibility = FS.GG.UI.Layout.Visible }: FS.GG.UI.Layout.ComputedBounds)
              Diagnostics = []
              Invalidated = []
              Revision = 0L }

        FS.GG.UI.Layout.Layout.hitTestComputed (LayoutDefaults.pixelSnapPolicy 1.0) computed x y

    // FR-004/FR-004a/FR-005 (feature 090): resolve a structural hit `ControlId` — the id a
    // `PointerInteraction`/`hitTest` carries: a `Key` for an authored node, else the positional
    // path `toLayout` assigns ("0", "0.1", …) — to the NEAREST ancestor (incl. self) the consumer
    // authored with a `withKey`, returned as that ancestor's authored `ControlId` (its `Key`). A
    // click inside a CONTAINER-KEYED composite therefore recovers the container's authored id
    // instead of an opaque inner positional id ("0.1"), so the host can route its binding. `None`
    // when no keyed ancestor exists anywhere on the hit node's path — the host then falls back to
    // `MapPointer` with the raw interaction and never invents a `Kind`/root id the consumer did not
    // author. A directly-keyed leaf's hit id IS its `Key`, so it resolves to itself (FR-005,
    // non-regressive — a fixed point).
    //
    // Pure/total/deterministic: walks the already-computed `result.Layout` tree, re-deriving each
    // node's positional path by the SAME `parent + "." + index` scheme as `toLayout`. A node is
    // authored exactly when its layout `Id` differs from that positional path (`toLayout` sets
    // `Id = Key |> defaultValue path`, so `Id <> path` ⇔ the node carries an explicit `Key`). No
    // clock/randomness; resume-safe; reads existing render data only — no layout-math change.
    let nearestAuthored (result: ControlRenderResult<'msg>) (hit: ControlId) : ControlId option =
        let rec search (path: string) (nearestKeyed: ControlId option) (node: FS.GG.UI.Layout.LayoutNode) : ControlId option =
            // FR-003 (feature 098): a node is *authored* when it is KEYED (`node.Id <> path`) OR its
            // canonical id is BOUND (`node.Id ∈ result.BoundIds`). `node.Id` is already `Key ?? path`,
            // so it IS the canonical id: a directly-keyed leaf stays a fixed point, and an unkeyed-bound
            // node now returns `Some node.Id` (its path) where it returned `None` before — a single
            // one-predicate widening, no control-flow restructure.
            let authoredHere =
                if node.Id <> path || Set.contains node.Id result.BoundIds then
                    Some node.Id
                else
                    None

            let nearestForChildren =
                match authoredHere with
                | Some _ -> authoredHere
                | None -> nearestKeyed

            if node.Id = hit then
                // nearest authored ancestor including self
                match authoredHere with
                | Some id -> Some id
                | None -> nearestKeyed
            else
                node.Children
                |> List.mapi (fun index child -> index, child)
                |> List.tryPick (fun (index, child) -> search (path + "." + string index) nearestForChildren child)

        search "0" None result.Layout

    let dispatch (event: ControlEvent) (control: Control<'msg>) =
        // FR-001/D5 (feature 098): thread the positional path so the unkeyed `binding.ControlId`
        // matched here uses the unified `Key ?? path` scheme. Keyed callers (the whole
        // `InteractionTests.fs` suite) and the `event.ControlId = None` wildcard are byte-identical;
        // only the unkeyed `Kind`-id match (unused by any current consumer) shifts to the path scheme.
        let rec loop (path: string) (current: Control<'msg>) =
            let own =
                if ControlInternals.disabledOrReadOnly current then
                    []
                else
                    ControlInternals.eventBindings path current
                    |> List.filter (fun binding ->
                        binding.EventKind = event.Kind
                        && (event.ControlId.IsNone || event.ControlId = Some binding.ControlId))
                    |> List.map (fun binding -> binding.Dispatch event)

            own
            @ (current.Children
               |> List.mapi (fun index child -> index, child)
               |> List.collect (fun (index, child) -> loop (path + "." + string index) child))

        loop "0" control |> List.truncate 1

module TextBlock =
    let create attrs = Control.create "text-block" attrs
    let text value = Attr.text value

module Label =
    let create attrs = Control.create "label" attrs
    let text value = Attr.text value

module Image =
    let create attrs = Control.create "image" attrs
    let source value = Attr.value value

module Icon =
    let create attrs = Control.create "icon" attrs
    let name value = Attr.text value

module Separator =
    let create attrs = Control.create "separator" attrs

module Badge =
    let create attrs = Control.create "badge" attrs
    let text value = Attr.text value

module Button =
    let create attrs = Control.create "button" attrs
    let text value = Attr.text value
    let enabled value = Attr.enabled value
    let onClick msg = Attr.on "onClick" msg
    let onClickWith map = Attr.onWith "onClick" map

module IconButton =
    let create attrs = Control.create "icon-button" attrs
    let icon value = Attr.text value
    let onClick msg = Attr.on "onClick" msg

// Feature 105 (US1, FR-003): the per-kind `onChanged` builders below inlined three
// payload-parse shapes (bool / float / string), the float shape duplicating a nested
// number-parse lambda. They are single-sourced here over one named `tryParseFloat`.
// Hidden from consumers by absence from Control.fsi.
module ChangeAdapters =
    let tryParseFloat (value: string) : float option =
        match Double.TryParse value with
        | true, parsed -> Some parsed
        | _ -> None

    let onChangedBool (map: bool -> 'msg) : Attr<'msg> =
        Attr.onWith "onChanged" (fun event -> event.Payload |> Option.exists ((=) "true") |> map)

    let onChangedFloat (map: float -> 'msg) : Attr<'msg> =
        Attr.onWith "onChanged" (fun event ->
            event.Payload |> Option.bind tryParseFloat |> Option.defaultValue 0.0 |> map)

    let onChangedString (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith "onChanged" (fun event -> event.Payload |> Option.defaultValue "" |> map)

module CheckBox =
    let create attrs = Control.create "check-box" attrs
    let text value = Attr.text value
    let checked' value = Attr.selected value
    let onChanged map = ChangeAdapters.onChangedBool map

module Switch =
    let create attrs = Control.create "switch" attrs
    let checked' value = Attr.selected value
    let onChanged map = ChangeAdapters.onChangedBool map

module Slider =
    let create attrs = Control.create "slider" attrs
    let value value = Attr.create "value" Content (FloatValue value)
    let onChanged map = ChangeAdapters.onChangedFloat map

module NumericInput =
    let create attrs = Control.create "numeric-input" attrs
    let value value = Attr.create "value" Content (FloatValue value)
    let onChanged map = ChangeAdapters.onChangedFloat map

module TextBox =
    let create attrs = Control.create "text-box" attrs
    let value value = Attr.value value
    let readOnly value = Attr.readOnly value
    let validation state = Attr.validation state
    let onChanged map = ChangeAdapters.onChangedString map

module TextArea =
    let create attrs = Control.create "text-area" attrs
    let value value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module RadioGroup =
    let create attrs = Control.create "radio-group" attrs
    let items values = Attr.items values
    let selected value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module Stack =
    let create attrs = Control.create "stack" attrs
    let children controls = Attr.children controls
    // FR-007: opt a stack into row layout. "horizontal" lays children along the row axis;
    // any other value (or omission) keeps the default vertical column.
    let orientation value = Attr.create "orientation" Layout (TextValue value)

module Grid =
    let create attrs = Control.create "grid" attrs
    let children controls = Attr.children controls

module Dock =
    let create attrs = Control.create "dock" attrs
    let children controls = Attr.children controls

module Wrap =
    let create attrs = Control.create "wrap" attrs
    let children controls = Attr.children controls

module Border =
    let create attrs = Control.create "border" attrs
    let child control = Attr.child control

module Panel =
    let create attrs = Control.create "panel" attrs
    let children controls = Attr.children controls

module ProgressBar =
    let create attrs = Control.create "progress-bar" attrs
    let value value = Attr.create "value" Content (FloatValue value)

module Spinner =
    let create attrs = Control.create "spinner" attrs

module ValidationMessage =
    let create attrs = Control.create "validation-message" attrs
    let text value = Attr.text value

module Tabs =
    let create attrs = Control.create "tabs" attrs
    let items values = Attr.items values
    let selected value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module Menu =
    let create attrs = Control.create "menu" attrs
    let items values = Attr.items values
    let onSelected map = Attr.onWith "onSelected" (fun event -> event.Payload |> Option.defaultValue "" |> map)

module Toolbar =
    let create attrs = Control.create "toolbar" attrs
    let children controls = Attr.children controls

module Tooltip =
    let create attrs = Control.create "tooltip" attrs
    let text value = Attr.text value

module Dialog =
    let create attrs = Control.create "dialog" attrs
    let children controls = Attr.children controls

module Toast =
    let create attrs = Control.create "toast" attrs
    let text value = Attr.text value

module Overlay =
    let create attrs = Control.create "overlay" attrs
    let child control = Attr.child control
