namespace Rendering.Harness

/// Live X11 tier (T2). The viewer's event loop blocks, so it runs **out-of-process**: the harness
/// re-invokes itself with the internal `__viewer` command (which opens a real window via the Host
/// MVU API), launched with the **X11 backend forced** (`XDG_SESSION_TYPE=x11`, `WAYLAND_DISPLAY`
/// unset) — otherwise GLFW picks Wayland and the window is invisible to the X11 toolchain. The
/// harness then drives it via `X11` (discover, screenshot, XTEST input, verify a visible change).
module Live =

    /// Internal child entry: open the live viewer window on the demo MVU app and block on its loop.
    /// Invoked as `harness __viewer`. Returns the process exit code.
    val launchViewerChild: unit -> int

    /// Run the T2 live tier: spawn the viewer child (`selfDll`), discover its window, capture a
    /// non-blank window PNG, inject mouse+keyboard, and confirm a visible state change. Degrades
    /// cleanly (skip/fail-classified) per the probe facts.
    val runLive: facts: ProbeFacts -> selfDll: string -> outDir: string -> Evidence.Evidence

    /// Human-readable Feature 144 limitation when offscreen visual proof cannot
    /// run on the current host.
    val overlayVisualLimitation: facts: ProbeFacts -> string option

    /// Classify whether the current host may claim Feature 145 real overlay visual proof.
    val classifyOverlayVisualProofHost: facts: ProbeFacts -> Evidence.HostCapabilityResult

    /// Run the Feature 145 overlay visual-proof path and return the readiness run record.
    val runOverlayVisualProof: facts: ProbeFacts -> outDir: string -> Evidence.VisualProofRun

    /// Internal child entry (`harness __vsyncprobe <stampfile> [seconds]`): run a **vsync-locked GL
    /// swap loop** (swap interval 1) on the GPU display for `seconds`, timestamping each buffer swap,
    /// then write the stamps to `stampFile`. The FS.Skia viewer's frame-metrics hooks were proven
    /// unreliable for present timing, so the genuine present cadence is measured through a controlled
    /// swap loop instead.
    val launchVsyncProbeChild: stampFile: string -> seconds: float -> int

    /// Faithful T3: run the vsync-locked GL swap loop (`selfDll __vsyncprobe`) on the real display,
    /// measure per-swap present intervals, and claim `vsync-faithful` only when the median interval
    /// LOCKS to the real refresh period. Returns the evidence and per-swap interval samples (ms).
    val runFaithfulPerf: facts: ProbeFacts -> selfDll: string -> outDir: string -> Evidence.Evidence * float list
