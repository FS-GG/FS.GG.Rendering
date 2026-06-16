namespace FS.GG.UI.Controls

/// Feature 132 (D2.1): net-new *interactive* controls (Ant Data-Display / Data-Entry) whose state is
/// parent-owned via attributes + events — never internal mutable state (Constitution IV). Generic and
/// theme-agnostic; they never branch on theme identity.
module Interactive2 =

    /// Stacked expandable section headers (Ant `Collapse`).
    module Collapse =
        /// Build a `collapse`; pair with `Attr.items` for the panel headers.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Dispatch the toggled panel key (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A star rating row (Ant `Rate`).
    module Rate =
        /// Build a `rate`; pair with `Rate.value`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Set the number of filled stars (0–5).
        val value: stars: float -> Attr<'msg>
        /// Dispatch the new rating (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>

    /// A rotating slide deck (Ant `Carousel`).
    module Carousel =
        /// Build a `carousel`; pair with `Attr.items` for the slides.
        val create: attrs: Attr<'msg> list -> Control<'msg>

    /// A month day-cell grid (Ant `Calendar`).
    module Calendar =
        /// Build a `calendar`.
        val create: attrs: Attr<'msg> list -> Control<'msg>
        /// Dispatch the selected date (payload) on change.
        val onChange: map: (string -> 'msg) -> Attr<'msg>
