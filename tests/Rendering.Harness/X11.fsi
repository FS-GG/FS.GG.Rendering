namespace Rendering.Harness

/// Edge interpreter for the live X11 path: shells to the installed toolchain (`xdotool` window
/// search + XTEST input, `maim` per-window capture). Every call degrades to `None`/`false` on
/// failure rather than throwing. The viewer child must be launched with the X11 backend forced
/// (`XDG_SESSION_TYPE=x11`, `WAYLAND_DISPLAY` unset) or GLFW picks Wayland (invisible to these tools).
module X11 =

    /// Find a mapped window id by its title (xdotool search --name). `None` if not present.
    val findWindow: title: string -> int option

    /// Window geometry as `WxH`, or `None`.
    val geometry: windowId: int -> string option

    /// Activate/raise/focus a window (xdotool windowactivate --sync) so capture is reliable.
    val activateWindow: windowId: int -> unit

    /// Capture a single window (compositor-aware, `maim -i`) to `path`. `true` on success.
    val screenshotWindow: windowId: int -> path: string -> bool

    /// True if the PNG at `path` is non-blank (has >1 distinct pixel value / decodes to content).
    val pngNonBlank: path: string -> bool

    /// Inject a pointer click at window-relative (x,y) into `windowId` via XTEST.
    val clickAt: windowId: int -> x: int -> y: int -> unit

    /// Inject a key (xdotool keysym) into `windowId` via XTEST.
    val sendKey: windowId: int -> key: string -> unit
