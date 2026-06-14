namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

/// Typed Props front door for the `CustomControl` control.
///
/// `custom-control` is the escape hatch (FR-006): it has **no fabricated `Props`
/// schema**. An author builds the `Control<'msg>` with the legacy
/// `CustomControl.create definition attrs` and lifts it into the typed tree with
/// this bridge — the same public `Widget.ofControl` seam shipped in 065.
module CustomControl =
    /// Bridge a legacy `Control<'msg>` (e.g. from `CustomControl.create`) into the
    /// typed tree. Invariant: `Widget.toControl (CustomControl.ofControl c) = c`.
    val ofControl: control: Control<'msg> -> Widget<'msg>
