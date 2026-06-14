# Rendering harness — capability baseline

The harness **re-probes the environment on every run** (`probe` subcommand → `run.json.env`);
this file records the reference baseline so a run's facts can be compared against a known-good
host. Measured on the development container, 2026-06-14.

## Measured facts (from `harness probe`)

| Fact | Value |
|---|---|
| Effective backend | `x11` (`DISPLAY=:1`, XWayland) |
| GL renderer | `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, …)` |
| GL version | `4.6 (Compatibility Profile) Mesa 26.x` |
| Direct rendering | `true` |
| Refresh | `119.93 Hz` |
| Vblank source | `HDMI-A-1` |
| X extensions | `XTEST`, `Present`, `RANDR`, `DRI3`, `XInput` |
| Swap control | `null` (needs a live GL context to read) |
| `/dev/uinput` + `/dev/input` | **absent** |
| `WAYLAND_DISPLAY` | `wayland-0` (the harness forces X11 for the viewer) |

## What runs here today

| Tier | Status in this environment |
|---|---|
| **T0** (deterministic render + routing) | ✅ runs headless; byte-identical re-render |
| **T1** (offscreen readback) | ✅ runs headless; non-blank PNG |
| **T2** (live window + input) | ✅ **runs fully** via a nested `Xvfb` + EGL viewer — window created, non-blank capture, XTEST mouse/keyboard, confirmed visible change |
| **T3 perf — offscreen throughput** | ✅ runs headless; real per-frame timing, **not** vsync-faithful (`swapControl` absent), correctly withheld |
| **T3 perf — faithful vsync/present timing** | ✅ **runs on the GPU** via a vsync-locked GL swap loop on `:1`; present interval **locks to the real vblank** (p50 **8.33 ms** = 119.93 Hz), so `vsync-faithful` is claimed with evidence |
| **T-uinput** (kernel input) | ⛔ opt-in; `/dev/uinput` absent → clean skip (requires host `--device /dev/uinput --device /dev/input` pass-through) |

## How T2 (live window) works here — nested Xvfb + EGL

The default `:1` display is **XWayland** on a **Wayland session** (`WAYLAND_DISPLAY=wayland-0`,
`xdpyinfo` reports `XWAYLAND`). Two problems had to be solved (isolated via the minimal `Silk.NET`
repro):

1. **GLFW defaults to Wayland.** It finds the `wayland-0` socket and opens a Wayland-native window
   that X11 tools (`xdotool`/`maim`) cannot see. Fix: launch with `XDG_SESSION_TYPE=x11` and
   `WAYLAND_DISPLAY` unset so GLFW uses X11.
2. **Skia GL segfaults on the GLX path.** With X11 forced, the window is created but the Skia GL
   context crashes on GLX (on XWayland `:1` *and* on a real `Xvfb`). Fix: force GLFW **EGL** context
   creation — `glfw.WindowHint(ContextCreationApi, EglContextApi)` before the viewer creates its
   window (Silk.NET.Windowing.Glfw never sets this, so the hint survives).

So `Live.runLive`:
- spins up a **private `Xvfb :99`** (a real, non-XWayland X server; GLX/llvmpipe GL 4.6, direct),
- launches the viewer child there with `XDG_SESSION_TYPE=x11`, `WAYLAND_DISPLAY` unset, and the
  **EGL** hint,
- discovers + activates the window, captures a **non-blank** PNG (`maim -i`), injects mouse +
  keyboard via **XTEST**, and confirms a **visible change** (before vs after differ).

Verified: `live-x11` → `status: passed`, `proofLevel: live-host`, capture 33→34 distinct colours
after input. Software EGL (`libEGL: DRI3 error` → llvmpipe) suffices for the smoke; a GPU path
would need `/dev/dri` device pass-through.

## How faithful T3 (vsync/present timing) works here — a vsync-locked GL swap loop

The FS.Skia viewer's per-frame `OnFrameMetrics`/view hooks were **empirically proven unreliable**
for present timing (a standalone probe showed the callback firing erratically — 0 to ~540 times over
the same 3 s window — and `FrameCause` mislabelling every frame `PointerMove`; `view` is called once
because the loop idle-skips). So the harness does **not** time the viewer. `Live.runFaithfulPerf`
instead measures the *genuine* present cadence directly: it spawns a child (`__vsyncprobe`) that opens
an **EGL** GL window via GLFW/Silk on the real `:1` display, sets **VSync on** (swap interval 1), and
timestamps every buffer swap in a manual present loop. When the swap genuinely blocks on the vblank
the inter-swap interval locks to the refresh period.

Measured (standalone + through the harness CLI, 2026-06-14):

| | p50 | mean | p95 | fps |
|---|---|---|---|---|
| **VSync on**  | **8.33 ms** | 8.34 ms | 8.95 ms | **120.1** (= 119.93 Hz vblank) |
| VSync off | 0.074 ms | 0.10 ms | 0.12 ms | ~13 500 (free-run) |

The off-vs-on contrast proves the swap is genuinely vblank-bound, not software-paced. `runFaithfulPerf`
claims `vsync-faithful` **only** when the measured median interval LOCKS to the probed refresh period
(within 10 %); on a server with no real vblank (e.g. `Xvfb`) the interval would not lock and the claim
is withheld. This measurement is authoritative for the present path's **frame pacing** (the
GPU/driver/compositor present), not for the viewer's own paint cost (that is the offscreen-throughput
tier).

## No-overclaim guarantee

Every `run.json` declares `proofLevel` + a non-empty `notAuthoritativeFor`. In particular,
`vsync-faithful` is emitted **only** when both `present.swapControl` and `present.vblankSource`
are known — enforced by the pure `RunPlan` and covered by unit tests. On this host the faithful T3
run earns those facts *by measurement*: `swapControl` is set to `1` only after the swap-loop's median
interval is observed to lock to the probed vblank period, so the label reflects a real, reproduced
lock rather than an assumption. The offscreen-throughput T3 run, which has no live swap, leaves
`swapControl` absent and is correctly **not** labelled vsync-faithful.
