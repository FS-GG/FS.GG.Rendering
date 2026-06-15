namespace FS.GG.UI.Controls

module Attr =
    let standardAttributeName name =
        match name with
        | StandardAttributeName.Text -> "text"
        | StandardAttributeName.Value -> "value"
        | StandardAttributeName.Children -> "children"
        | StandardAttributeName.Series -> "series"
        | StandardAttributeName.Values -> "values"
        | StandardAttributeName.Columns -> "columns"
        | StandardAttributeName.Rows -> "rows"
        | StandardAttributeName.Items -> "items"
        | StandardAttributeName.Nodes -> "nodes"
        | StandardAttributeName.VisibleRange -> "visibleRange"
        | StandardAttributeName.SelectedRows -> "selectedRows"
        | StandardAttributeName.FocusedCell -> "focusedCell"
        | StandardAttributeName.Custom value -> value

    let standardEventName eventKind =
        match eventKind with
        | StandardEventKind.Click -> "onClick"
        | StandardEventKind.Changed -> "onChanged"
        | StandardEventKind.Selected -> "onSelected"
        | StandardEventKind.FocusChanged -> "onFocusChanged"
        | StandardEventKind.SortChanged -> "onSortChanged"
        | StandardEventKind.Custom value -> value

    let standardValue value =
        match value with
        | StandardText value -> TextValue value
        | StandardBool value -> BoolValue value
        | StandardFloat value -> FloatValue value
        | StandardStringList values -> StringListValue values
        | StandardMessage msg -> MessageValue msg
        | StandardEvent map -> UntypedValue map
        | StandardUntyped value -> UntypedValue value

    let create name category value =
        { Name = name
          Category = category
          Value = value }

    let standardAttribute name value =
        create (standardAttributeName name) Data (standardValue value)

    let customAttribute name (value: obj) =
        create name Data (UntypedValue value)

    let standardEvent eventKind msg =
        create (standardEventName eventKind) Event (MessageValue msg)

    let customEvent eventKind msg =
        create eventKind Event (MessageValue msg)

    let text value = create "text" Content (TextValue value)
    let value value = create "value" Content (TextValue value)
    let items values = create "items" Data (StringListValue values)
    let child control = create "child" Children (ChildValue control)
    let children controls = create "children" Children (ChildrenValue controls)
    let enabled value = create "enabled" State (BoolValue value)
    let visible value = create "visible" State (BoolValue value)
    let readOnly value = create "readOnly" State (BoolValue value)
    let loading value = create "loading" State (BoolValue value)
    let selected value = create "selected" State (BoolValue value)
    let width value = create "width" Layout (FloatValue value)
    let height value = create "height" Layout (FloatValue value)
    let padding value = create "padding" Layout (FloatValue value)
    let margin value = create "margin" Layout (FloatValue value)
    let style name = create "style" Style (TextValue name)
    let styleClasses classes = create "styleClasses" Style (StyleClassesValue classes)
    let visualState state = create "visualState" State (VisualStateValue state)
    let theme theme = create "theme" Theme (ThemeValue theme)
    let validation state = create "validation" Validation (ValidationValue state)
    let accessibility metadata = create "accessibility" Accessibility (AccessibilityValue metadata)
    let on eventKind msg = create eventKind Event (MessageValue msg)
    let onWith eventKind map = create eventKind Event (EventValue map)
