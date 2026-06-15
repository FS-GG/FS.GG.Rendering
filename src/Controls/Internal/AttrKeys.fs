namespace FS.GG.UI.Controls

// Feature 105 (US3, FR-007): the closed set of control-intrinsic attribute names, as a typed
// key so a mistyped internal name is a compile error rather than a silent runtime miss.
// `module internal` with no `.fsi` — assembly-internal, off the public surface (the established
// WidgetLowering / SceneRenderer precedent), compiled before Control.fs and DataGrid.fs so both
// route their closed-set attribute reads through it. The public `StandardAttributeName` DU is
// unchanged (D3); the string-keyed `tryLast`/`hasAttr` stay for genuinely dynamic names.
// `width`/`height`/`orientation` already single-source through feature 101's `[<Literal>]` attr
// names (compile-checked there), so they are not duplicated here.
module internal AttrKeys =
    type AttrKey =
        | Text
        | Value
        | StyleClasses
        | VisualState
        | Slot
        | Accessibility
        | Nodes
        | RichTextRuns
        | Rows
        | VisibleRange
        | Columns
        | SelectedRows
        | FocusedCell

    // The single string boundary: each key projects to exactly the existing literal name, so a
    // read routed through a key is byte-identical to the prior string read.
    let nameOf (key: AttrKey) : string =
        match key with
        | Text -> "text"
        | Value -> "value"
        | StyleClasses -> "styleClasses"
        | VisualState -> "visualState"
        | Slot -> "slot"
        | Accessibility -> "accessibility"
        | Nodes -> "nodes"
        | RichTextRuns -> "richTextRuns"
        | Rows -> "rows"
        | VisibleRange -> "visibleRange"
        | Columns -> "columns"
        | SelectedRows -> "selectedRows"
        | FocusedCell -> "focusedCell"

    // Last-writer read, mirroring `ControlInternals.tryLast` / `DataGrid` `tryLast` semantics.
    let tryKey (key: AttrKey) (attrs: Attr<'msg> list) =
        attrs |> List.rev |> List.tryFind (fun attr -> attr.Name = nameOf key)

    let hasKey (key: AttrKey) (attrs: Attr<'msg> list) =
        attrs |> List.exists (fun attr -> attr.Name = nameOf key)
