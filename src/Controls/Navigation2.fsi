namespace FS.GG.UI.Controls

/// Feature 132 (D2.1): net-new *navigation* controls filling Ant Navigation overview gaps.
/// Generic, theme-agnostic, pure render + attributes + events (parent owns the active selection).
/// They never branch on theme identity.
module Navigation2 =

    /// A separated path trail (Ant `Breadcrumb`).
    module Breadcrumb =
        /// Build a `breadcrumb`; pair with `Attr.items` for the path segments.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// Numbered horizontal progress steps (Ant `Steps`).
    module Steps =
        /// Build a `steps`; pair with `Attr.items` for the step titles.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A page-number control (Ant `Pagination`).
    module Pagination =
        /// Build a `pagination`; pair with `Pagination.total`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the total page count (clamped to the rendered chip row).
        val total: pages: int -> Attr<'msg>
        /// Dispatch the selected page (payload) when a page chip is activated.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A connected single-select segment row (Ant `Segmented`).
    module Segmented =
        /// Build a `segmented`; pair with `Attr.items` for the options.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Dispatch the selected option (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A vertical in-page link list (Ant `Anchor`).
    module Anchor =
        /// Build an `anchor`; pair with `Attr.items` for the link targets.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A pinned-to-edge bar (Ant `Affix`).
    module Affix =
        /// Build an `affix`; pair with `Affix.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the affixed bar label.
        val text: value: string -> Attr<'msg>
