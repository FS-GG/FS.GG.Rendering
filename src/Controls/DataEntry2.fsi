namespace FS.GG.UI.Controls

/// Feature 132 (D2.1): net-new *data-entry* controls (Ant Data Entry). These are authored as
/// parent-owned attribute + event controls rather than MVU `Model`/`Msg`/`Effect` components: their
/// selection/value is a plain attribute and changes are emitted as events, so no internal mutable
/// state exists (Constitution IV is satisfied without the DataGrid machinery — see the decision
/// record). The genuinely heavyweight Ant entries (Transfer, Mentions) are dispositioned
/// `composition` in the coverage matrix instead. Generic and theme-agnostic; no theme-identity branch.
module DataEntry2 =

    /// Cascading selection columns (Ant `Cascader`).
    module Cascader =
        /// Build a `cascader`; pair with `Attr.items` for the column path.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Dispatch the selected path (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A text field with a suggestion dropdown (Ant `AutoComplete`).
    module AutoComplete =
        /// Build an `auto-complete`; pair with `AutoComplete.value`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the current query text.
        val value: value: string -> Attr<'msg>
        /// Dispatch the new query (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A file drop zone with an upload action (Ant `Upload`).
    module Upload =
        /// Build an `upload`; pair with `Upload.text` for the button label.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the upload button label.
        val text: value: string -> Attr<'msg>
        /// Dispatch the selected file reference (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>
