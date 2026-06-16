namespace FS.GG.UI.Controls

/// Feature 132 (D2.1): net-new presentational *display* controls filling Ant General / Data-Display
/// overview gaps. Each is generic and theme-agnostic — pure render + attributes + events; the parent
/// owns any state (no internal mutable state). They render neutrally under `Themes.Default` and
/// Ant-styled under `Themes.AntDesign` through the shared resolver/token seams, branching on no theme.
module Display2 =

    /// A coloured status chip (Ant `Tag`).
    module Tag =
        /// Build a `tag` from its attributes; pair with `Tag.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the chip label.
        val text: value: string -> Attr<'msg>
        /// Dispatch `msg` when the chip's close affordance is activated.
        val onClose: msg: 'msg -> Attr<'msg>

    /// A round monogram / image stand-in (Ant `Avatar`).
    module Avatar =
        /// Build an `avatar` from its attributes; pair with `Avatar.text` for initials.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the initials shown inside the avatar.
        val text: value: string -> Attr<'msg>

    /// A framed content surface with a header band (Ant `Card`).
    module Card =
        /// Build a `card` from its attributes; pair with `Card.title`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the card header title.
        val title: value: string -> Attr<'msg>

    /// A label : value term list (Ant `Descriptions`).
    module Descriptions =
        /// Build a `descriptions` term list; pair with `Attr.items` for the alternating label/value entries.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A large emphasised metric over a caption (Ant `Statistic`).
    module Statistic =
        /// Build a `statistic`; pair with `Statistic.value`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the displayed metric value.
        val value: value: string -> Attr<'msg>

    /// A vertical dotted event rail (Ant `Timeline`).
    module Timeline =
        /// Build a `timeline`; pair with `Attr.items` for the events.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A muted "no data" placeholder (Ant `Empty`).
    module Empty =
        /// Build an `empty` placeholder; pair with `Empty.text` for the caption.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the placeholder caption.
        val text: value: string -> Attr<'msg>

    /// Grey loading placeholder bars (Ant `Skeleton`).
    module Skeleton =
        /// Build a `skeleton` placeholder.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A QR-code module grid (Ant `QRCode`).
    module QrCode =
        /// Build a `qr-code`; pair with `QrCode.value` for the encoded payload.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the encoded payload (rendered as a deterministic module grid).
        val value: value: string -> Attr<'msg>

    /// Faint repeated brand text overlay (Ant `Watermark`).
    module Watermark =
        /// Build a `watermark`; pair with `Watermark.text`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the repeated watermark text.
        val text: value: string -> Attr<'msg>
