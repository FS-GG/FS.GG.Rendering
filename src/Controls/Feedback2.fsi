namespace FS.GG.UI.Controls

/// Feature 132 (D2.1): net-new *feedback / overlay* controls filling Ant Feedback overview gaps.
/// Generic, theme-agnostic, pure render + attributes + events (parent owns state). Overlay controls
/// render their schematic in place (a host positions the live overlay); they never branch on theme.
module Feedback2 =

    /// A coloured information banner (Ant `Alert`).
    module Alert =
        /// Build an `alert`; pair with `Alert.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the banner message.
        val text: value: string -> Attr<'msg>
        /// Dispatch `msg` when the banner is dismissed.
        val onClose: msg: 'msg -> Attr<'msg>

    /// A centred operation-outcome panel (Ant `Result`).
    module Result =
        /// Build a `result`; pair with `Result.title`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the outcome title.
        val title: value: string -> Attr<'msg>

    /// An edge-anchored sliding panel (Ant `Drawer`).
    module Drawer =
        /// Build a `drawer`; pair with `Drawer.title`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the drawer header title.
        val title: value: string -> Attr<'msg>
        /// Dispatch `msg` when the drawer is closed.
        val onClose: msg: 'msg -> Attr<'msg>

    /// A floating callout anchored to a trigger (Ant `Popover`).
    module Popover =
        /// Build a `popover`; pair with `Popover.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the popover body text.
        val text: value: string -> Attr<'msg>

    /// A confirm callout with accept/cancel actions (Ant `Popconfirm`).
    module Popconfirm =
        /// Build a `popconfirm`; pair with `Popconfirm.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the confirmation prompt.
        val text: value: string -> Attr<'msg>
        /// Dispatch `msg` when the user confirms.
        val onConfirm: msg: 'msg -> Attr<'msg>
        /// Dispatch `msg` when the user cancels.
        val onCancel: msg: 'msg -> Attr<'msg>

    /// A guided, multi-step highlight callout (Ant `Tour`).
    module Tour =
        /// Build a `tour`; pair with `Tour.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the current step text.
        val text: value: string -> Attr<'msg>

    /// A circular floating action button (Ant `FloatButton`).
    module FloatButton =
        /// Build a `float-button`; pair with `FloatButton.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the glyph/label.
        val text: value: string -> Attr<'msg>
        /// Dispatch `msg` on activation.
        val onClick: msg: 'msg -> Attr<'msg>
