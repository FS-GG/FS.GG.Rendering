namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

/// Feature 189 (US1 foundational, FR-001): the shared helper prelude extracted verbatim from
/// `ControlInternals` (Control.fs L124-625). `module internal` + reached by tests via
/// `InternalsVisibleTo`; nothing reaches the public surface (`FS.GG.UI.Controls.txt` unchanged).
/// Former `private` members widen to module-internal so the sibling geometry/content/assembly
/// modules can reach them; behaviour is byte-identical (same bodies, relocated).
module internal ControlPrimitives =
    // Feature 117 (Phase 8, FR-001/FR-004): the text-measure cache hook. `Scene.measureText` is a pure
    // function of `(text, font)`, so a cache over it is a transparent accelerator — the cached value
    // equals the un-cached value for every key (research R5). The bounded cache itself and its per-frame
    // hit/miss accounting live on `RetainedRender` (the 113/116 cache home, research R1); the retained
    // `init`/`step` install a per-pass closure here around the frame's layout + paint measurement and
    // clear it afterwards (`try/finally`). `[<ThreadStatic>]` so concurrent test `step`s route to their own
    // cache and never cross-contaminate; the default (`None`) is the direct un-cached path, byte-identical
    // to pre-117. The mutation is interpreter-edge mutation confined to the step (constitution III), exactly
    // like the existing id/work counters and the 116 picture cache.
    type TextMeasureHookHolder() =
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
        // Feature 136 (R2/FR-002): the un-cached base path resolves through the real-metrics measurer
        // when the rendering edge has installed one (`Scene.setRealTextMeasurer`), so box sizing equals
        // draw width. With none installed this is the pure `Scene.measureText` (byte-identical to pre-136).
        | None -> Scene.measureTextResolved text font

    let tryLast name (attrs: Attr<'msg> list) =
        attrs
        |> List.rev
        |> List.tryFind (fun attr -> attr.Name = name)

    let tryLastAny names (attrs: Attr<'msg> list) =
        attrs
        |> List.rev
        |> List.tryFind (fun attr -> names |> List.contains attr.Name)

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
    let slotRegions (kind: string) : SlotName list * SlotName list =
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

    let tryFloat name (attrs: Attr<'msg> list) =
        tryLast name attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | FloatValue value -> Some value
            | _ -> None)

    let tryFloatAny names (attrs: Attr<'msg> list) =
        tryLastAny names attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | FloatValue value -> Some value
            | _ -> None)

    let uniformSpacing value : FS.GG.UI.Layout.LayoutPadding =
        { Left = value
          Top = value
          Right = value
          Bottom = value }

    let uniformGap value : FS.GG.UI.Layout.LayoutGap =
        { Row = value; Column = value }

    let alignFromString value =
        match value with
        | "auto" -> Some FS.GG.UI.Layout.LayoutAlign.Auto
        | "start" -> Some FS.GG.UI.Layout.LayoutAlign.Start
        | "center" -> Some FS.GG.UI.Layout.LayoutAlign.Center
        | "end" -> Some FS.GG.UI.Layout.LayoutAlign.End
        | "stretch" -> Some FS.GG.UI.Layout.LayoutAlign.Stretch
        | "spaceBetween" -> Some FS.GG.UI.Layout.LayoutAlign.SpaceBetween
        | "spaceAround" -> Some FS.GG.UI.Layout.LayoutAlign.SpaceAround
        | "spaceEvenly" -> Some FS.GG.UI.Layout.LayoutAlign.SpaceEvenly
        | _ -> None

    let tryAlign name (attrs: Attr<'msg> list) =
        tryLast name attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue(:? FS.GG.UI.Layout.LayoutAlign as value) -> Some value
            | TextValue value -> alignFromString value
            | _ -> None)

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

    /// Feature 136 (US1/T016A, FR-002): the overflow affordance for text whose real-metric advance
    /// **still** exceeds its box after shrink-to-fit (`fittedFontSize` bottomed out). Returns the
    /// longest prefix of `label` that fits `maxWidth` at `size` with a trailing ellipsis (`…`) — an
    /// explicit "more here" affordance — instead of letting the box clip drop characters silently. The
    /// label is returned unchanged when it already fits; a single-character label is never truncated.
    let ellipsize family (size: float) (maxWidth: float) (label: string) : string =
        let font = { Family = family; Size = size; Weight = None }
        let width (s: string) = (measureText s font).Width

        if maxWidth <= 0.0 || label.Length <= 1 || width label <= maxWidth then
            label
        else
            let ellipsis = "…"

            let rec largestFit n =
                if n <= 0 then
                    ellipsis
                elif width (label.Substring(0, n) + ellipsis) <= maxWidth then
                    label.Substring(0, n) + ellipsis
                else
                    largestFit (n - 1)

            largestFit (label.Length - 1)

    let chartValues (control: Control<'msg>) : ChartPoint list =
        // Feature 080 (FR-002): read the structured `UntypedValue(ChartSeries list)` (series)
        // and `UntypedValue(ChartPoint list)` (pie) the typed front door actually stores,
        // preserving X/Y/Label. Feature 184 (US4): the untyped flat `float list`/`float array`
        // authoring fallback was removed (zero in-tree authors); charts are authored only through the
        // typed front door. The single `FloatValue` arm remains for a scalar-valued attribute.
        let points name =
            tryLast name control.Attributes
            |> Option.bind (fun attr ->
                match attr.Value with
                | UntypedValue(:? (ChartSeries list) as series) ->
                    Some(series |> List.collect (fun s -> s.Points))
                | UntypedValue(:? (ChartPoint list) as pts) -> Some pts
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

        // Feature 136 (US3/T035): guard degenerate data — drop non-finite (NaN/Inf) points so a
        // chart never computes wild geometry; n=0 yields [] (the geoms render an empty state).
        // Feature 183 (US1): the chart data-source routing now reads the single ControlKindRegistry
        // SSOT (byte-identical: series/values attribute or graph nodes; any other kind ⇒ []).
        let raw =
            match ControlKindRegistry.chartSource control.Kind with
            | Some ControlKindRegistry.Series -> points "series" |> Option.defaultValue []
            | Some ControlKindRegistry.Values -> points "values" |> Option.defaultValue []
            | Some ControlKindRegistry.GraphNodes -> nodesAsPoints ()
            | None -> []
        raw |> List.filter (fun p -> System.Double.IsFinite p.X && System.Double.IsFinite p.Y)

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
    // Feature 183 (US1): the `richFamilies` / `chartFamilies` membership sets moved into the single
    // ControlKindRegistry SSOT (`isRich` / `isChart`); the sites below call those instead of a local
    // `Set.contains`. The geometry-dispatch itself (`faithfulContent`) stays here — its arms call this
    // module's private `*Geom` functions, which the earlier-compiled registry cannot reference without
    // a back-edge (FR-010 / FR-011 retention; see readiness/post-change/retentions.md).

    /// A human caption for the rich-family title band: "date-picker" -> "Date picker".
    /// Used so the thumbnail's title is the control's NAME, not its sample content (which the
    /// schematic below already shows) — fixing composite-lowering title bleed (e.g. "STACK").
    let prettyKind (kind: string) =
        match kind.Split('-') |> Array.toList with
        | [] -> kind
        | head :: tail ->
            let cap (w: string) = if w.Length = 0 then w else string (System.Char.ToUpper w[0]) + w.Substring 1
            cap head :: tail |> String.concat " "

    // Feature 101/138: the layout-driving attribute names flow through the shared internal
    // AttrKeys vocabulary so builder emission, layout lowering, and dirty-set membership do not
    // hand-copy the same strings.
    let AttrWidth = AttrKeys.LayoutWidth
    let AttrHeight = AttrKeys.LayoutHeight
    let AttrOrientation = AttrKeys.LayoutOrientation

    /// Preview node width: explicit `width` wins; rich families fill the preview canvas.
    let nodeWidth (control: Control<'msg>) =
        if hasAttr AttrWidth control.Attributes then floatValue AttrWidth 240.0 control.Attributes
        elif ControlKindRegistry.isRich control.Kind then 304.0
        else 240.0

    /// Preview node height: explicit `height` wins; rich families get a tall box so geometry
    /// sits below the title band (a 24-px box would put everything inside the band).
    let nodeHeight (control: Control<'msg>) =
        if hasAttr AttrHeight control.Attributes then max 20.0 (floatValue AttrHeight 24.0 control.Attributes)
        elif ControlKindRegistry.isRich control.Kind then 132.0
        else 24.0

    let palette (theme: Theme) =
        [ theme.Accent
          Colors.rgb 210uy 95uy 75uy
          Colors.rgb 90uy 165uy 95uy
          Colors.rgb 150uy 110uy 205uy
          Colors.rgb 215uy 165uy 65uy
          Colors.rgb 80uy 150uy 205uy ]

    let colorAt theme i =
        let p = palette theme
        List.item (((i % p.Length) + p.Length) % p.Length) p

    // Feature 133 (D2C.1, data-model §3): the categorical series palette for the NET-NEW charts,
    // derived purely from `Theme` ROLE VALUES (never `Theme.Name`) so it diverges under the Ant
    // theme yet never branches on theme identity (FR-001/FR-007). Distinct from `palette` above —
    // that one is pinned to literals for the EXISTING charts' Default byte-identity (SC-004); the
    // net-new charts have no baseline, so every colour traces to a token-sourced role here.
    let chartPalette (theme: Theme) : Color list =
        [ theme.Accent; theme.Danger; theme.Success; theme.Warning; theme.Muted; theme.Foreground ]

    let chartColorAt (theme: Theme) i =
        let p = chartPalette theme
        List.item (((i % p.Length) + p.Length) % p.Length) p

    /// Blend two colours by `t` ∈ [0,1] — used by intensity ramps (heatmap/treemap) so the low→high
    /// scale runs `Muted`→`Accent` from theme roles, with no inline hex.
    let lerpColor (a: Color) (b: Color) (t: float) : Color =
        let t = max 0.0 (min 1.0 t)
        let mix (x: byte) (y: byte) = byte (float x + (float y - float x) * t)
        Colors.rgba (mix a.Red b.Red) (mix a.Green b.Green) (mix a.Blue b.Blue) (mix a.Alpha b.Alpha)

    let mkText (theme: Theme) (x: float) (baseline: float) (size: float) (color: Color) (s: string) =
        Scene.textRun
            { Text = s
              Position = { X = x; Y = baseline }
              Font = { Family = theme.FontFamily; Size = size; Weight = None }
              Paint = Paint.fill color }

    /// `mkText` with an explicit weight — used by the rich-text schematic to draw bold runs.
    let mkTextW (theme: Theme) (x: float) (baseline: float) (size: float) (weight: int option) (color: Color) (s: string) =
        Scene.textRun
            { Text = s
              Position = { X = x; Y = baseline }
              Font = { Family = theme.FontFamily; Size = size; Weight = weight }
              Paint = Paint.fill color }

    let stringListOf name (control: Control<'msg>) =
        tryLast name control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | StringListValue values -> Some values
            | _ -> None)
        |> Option.defaultValue []

    let textValueOf name (control: Control<'msg>) =
        tryLast name control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | TextValue value -> Some value
            | _ -> None)

    // Feature 191 attribute names — the canvas control's scene carrier, optional viewport transform,
    // and the volatile/no-cache marker (US1/US2). Kept as literals here (the single read site) so the
    // public `Canvas` constructor module and these readers agree by construction.
    [<Literal>]
    let CanvasSceneAttr = "canvasScene"

    [<Literal>]
    let CanvasViewportAttr = "canvasViewport"

    [<Literal>]
    let CanvasVolatileAttr = "canvasVolatile"

    /// Feature 191 (US1, C1): the immutable `Scene` carried by a `canvas` control's attributes, if any.
    let sceneValueOf (control: Control<'msg>) : Scene option =
        tryLast CanvasSceneAttr control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | SceneValue scene -> Some scene
            | _ -> None)

    /// Feature 191 (US1, FR-016): the optional content viewport (pan/zoom) transform on a `canvas`.
    let viewportOf (control: Control<'msg>) : PerspectiveTransform option =
        tryLast CanvasViewportAttr control.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue v ->
                match v with
                | :? PerspectiveTransform as t -> Some t
                | _ -> None
            | _ -> None)

    /// Feature 191 (US2, D4/FR-004): whether this control is a `canvas` marked `volatile'` (no-cache).
    let isVolatileCanvas (control: Control<'msg>) : bool =
        control.Kind = "canvas" && boolValue CanvasVolatileAttr false control.Attributes
