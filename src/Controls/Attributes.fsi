namespace FS.GG.UI.Controls

/// Builder functions (`Attr`) for constructing the typed `Attr<'msg>` values that
/// configure a control — covering content, layout, state, style, theme, and event attributes.
module Attr =
    /// Low-level escape hatch (`create`) building an `Attr<'msg>` from an explicit `name`,
    /// `AttrCategory`, and `AttrValue<'msg>`; the foundation the typed builders below wrap.
    val create: name: string -> category: AttrCategory -> value: AttrValue<'msg> -> Attr<'msg>
    /// Schema-checked builder (`standardAttribute`) producing an `Attr<'msg>` from a typed
    /// `StandardAttributeName` and `StandardAttributeValue<'msg>`, keeping the attribute within
    /// the recognised contract surface.
    val standardAttribute: name: StandardAttributeName -> value: StandardAttributeValue<'msg> -> Attr<'msg>
    /// Builder (`customAttribute`) for a consumer-defined attribute outside the standard set:
    /// a free-form `name` carrying an untyped `obj` value, lowered under the `Data` category.
    val customAttribute: name: string -> value: obj -> Attr<'msg>
    /// Event builder (`standardEvent`) wiring a typed `StandardEventKind` to dispatch a fixed
    /// `msg` when that built-in event fires on the control.
    val standardEvent: eventKind: StandardEventKind -> msg: 'msg -> Attr<'msg>
    /// Event builder (`customEvent`) wiring a free-form `eventKind` string to dispatch a fixed
    /// `msg`, for events outside the `StandardEventKind` set.
    val customEvent: eventKind: string -> msg: 'msg -> Attr<'msg>
    /// Content builder (`text`) setting the control's display text — the label of a button or
    /// text-block, or the caption shown by content-bearing kinds.
    val text: value: string -> Attr<'msg>
    /// Content builder (`value`) setting the current value of an input control such as a
    /// text-box, carried as the `Value` attribute.
    val value: value: string -> Attr<'msg>
    /// Data builder (`items`) supplying the ordered string entries of a list-like control as
    /// the `Items` attribute.
    val items: values: string list -> Attr<'msg>
    /// Children builder (`child`) attaching a single nested `Control<'msg>` to a container.
    val child: control: Control<'msg> -> Attr<'msg>
    /// Children builder (`children`) attaching an ordered list of nested `Control<'msg>` to a
    /// container such as a stack, grid, or panel.
    val children: controls: Control<'msg> list -> Attr<'msg>
    /// State builder (`enabled`): when `false` the control is disabled (non-interactive,
    /// rendered in its `Disabled` visual state); omitted defaults to enabled.
    val enabled: value: bool -> Attr<'msg>
    /// State builder (`visible`): when `false` the control is hidden from layout and paint;
    /// omitted defaults to visible.
    val visible: value: bool -> Attr<'msg>
    /// State builder (`readOnly`): when `true` an input control such as a text-box displays
    /// its value but rejects edits; omitted defaults to editable.
    val readOnly: value: bool -> Attr<'msg>
    /// State builder (`loading`): when `true` the control shows its busy/`Loading` visual
    /// state; omitted defaults to not loading.
    val loading: value: bool -> Attr<'msg>
    /// State builder (`selected`): when `true` marks the control as selected (e.g. a toggle,
    /// radio, or list item in its `Selected` visual state); omitted defaults to unselected.
    val selected: value: bool -> Attr<'msg>
    /// Layout builder (`width`) requesting a fixed control width in device-independent pixels;
    /// omitted lets the control size to its content/container.
    val width: value: float -> Attr<'msg>
    /// Layout builder (`height`) requesting a fixed control height in device-independent
    /// pixels; omitted lets the control size to its content/container.
    val height: value: float -> Attr<'msg>
    /// Layout builder (`padding`) setting uniform inner spacing in pixels between the control's
    /// edge and its content; omitted defaults to no padding.
    val padding: value: float -> Attr<'msg>
    /// Layout builder (`margin`) setting uniform outer spacing in pixels around the control
    /// within its parent; omitted defaults to no margin.
    val margin: value: float -> Attr<'msg>
    /// Style builder (`style`) attaching a single named style class by string — the free-form
    /// counterpart to the typed `styleClasses` builder.
    val style: name: string -> Attr<'msg>
    /// Feature 093 (E3): attach an ordered list of style classes (list order = attach order).
    /// Lowers to a single `Style`-category attribute carrying `StyleClassesValue`. Absent ≡
    /// `[]` ≡ the behaviour-preserving base case (FR-005). The last `styleClasses` attribute on
    /// a control wins (the codebase's last-writer attribute convention).
    val styleClasses: classes: StyleClass list -> Attr<'msg>
    /// Feature 093 (E3): set the control's current `VisualState` for the resolver. A host wires
    /// its `ControlRuntime` Hover/Press/Focus state into this each frame; it rides the control
    /// through the keyed reconciler so a state-driven look survives a sibling shift (FR-006,
    /// SC-005). Absent ≡ `Normal` ≡ the behaviour-preserving base case.
    val visualState: state: VisualState -> Attr<'msg>
    /// Theme builder (`theme`) attaching a `Theme` palette/metrics to the control subtree,
    /// overriding the inherited theme for it and its descendants.
    val theme: theme: Theme -> Attr<'msg>
    /// Validation builder (`validation`) attaching a `ValidationState` (`Valid`/`Invalid`/`Pending`)
    /// to an input control, surfacing its validity in the resolved visual state.
    val validation: state: ValidationState -> Attr<'msg>
    /// Accessibility builder (`accessibility`) attaching explicit `AccessibilityMetadata`
    /// (role, name source, keyboard contract, contrast/navigation data) to override the
    /// control's inferred semantics.
    val accessibility: metadata: AccessibilityMetadata -> Attr<'msg>
    /// Event builder (`on`) subscribing to an event by `eventKind` string and dispatching a
    /// fixed `msg` when it fires, ignoring the event payload.
    val on: eventKind: string -> msg: 'msg -> Attr<'msg>
    /// Event builder (`onWith`) subscribing to an event by `eventKind` string and computing the
    /// dispatched message from the `ControlEvent` via `map`, giving access to the payload.
    val onWith: eventKind: string -> map: (ControlEvent -> 'msg) -> Attr<'msg>
