namespace FS.GG.UI.SkiaViewer

[<RequireQualifiedAccess>]
/// Selects how the live viewer presents each rendered frame on the OpenGL host backend
/// (feature 119). `DirectToSwapchain` is the default **readback-free** path: the Skia scene is
/// drawn straight onto the window's default framebuffer (FBO 0) through a GL-backed `SKSurface`
/// and presented by the windowing toolkit's buffer swap, with no per-frame GPU→CPU readback,
/// staging buffer, or queue stall. `OffscreenReadback` renders to an offscreen surface then reads
/// back — it backs the on-demand screenshot/evidence routine and an explicit fallback.
type ViewerPresentMode =
    /// Offscreen render then GPU→CPU readback. On the GL backend this is no longer the live
    /// present path; it backs the on-demand evidence/screenshot routine (decoupled from the live
    /// present, FR-004) and serves as an explicit fallback.
    | OffscreenReadback
    /// Render directly onto the window's default framebuffer (FBO 0) via a GL-backed `SKSurface`,
    /// then present with the toolkit buffer swap — no per-frame readback, staging buffer, or
    /// queue stall. The **default** live present path on the OpenGL backend (feature 119);
    /// unblocks feature 118 FR-002/SC-002.
    | DirectToSwapchain
