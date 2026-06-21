namespace Rendering.Harness

/// Environment probe: records display / GL / refresh / extension facts and the effective backend
/// (X11 vs Wayland vs none). Shells to installed X11 tools; every failure degrades to a `None`
/// fact, never a crash.
module Probe =

    /// Probe the current environment. Detects the effective backend honestly (X11 only when a
    /// usable `DISPLAY` answers), so a Wayland-only session is reported as `Wayland`, not `X11`.
    val probe: unit -> ProbeFacts
