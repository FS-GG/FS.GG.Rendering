# Contract: Tier matrix

Each tier's display dependency, the seam it drives, what it is authoritative for, what it is **not**,
and how it degrades. Drives `RunPlan` (pure) and the executors.

| Tier | Display dep | Driven by | proofLevel | Authoritative for | NOT authoritative for | Degrades when |
|---|---|---|---|---|---|---|
| **T0** | none | `Perf.runScript`, `captureRespondsProof`, `runBounded` (offscreen) | `deterministic` | determinism, tree equality, retained routing, non-blank offscreen PNG | renderer-vs-desktop pixels, live-host, timing | never (always runs) |
| **T1** | offscreen/Skia | `captureScreenshotEvidence`, `runBounded` | `offscreen-pixels` | renderer pixel output (readback) | desktop visibility, focus, live input | never (offscreen) |
| **T2** | X11 + WM | `SkiaViewer.run` + `xdotool` (XTEST) + `maim` | `live-host` | window creation, visibility, focus, real mouse/keyboard, desktop screenshot | timing/vsync fidelity | no DISPLAY / Wayland / no window → `skipped`/`failed` |
| **T3** | Xorg/KMS + vblank | `runBounded` + `FrameMetrics` | `timing` | frame interval, paint/compose/swap timing; `vsync-faithful` **iff** present facts | functional correctness (assumes T0/T1) | missing vblank/swap facts → no `vsync-faithful` label |
| **T-uinput** | `/dev/uinput`+`/dev/input` | `ydotool` (uinput backend) | `kernel-input` | evdev/libinput input path | everything above | `/dev/uinput` absent → `skipped: opt-in unavailable` |

## Perf modes (T3)

| Mode | Kind | Notes |
|---|---|---|
| `throughput` | timing | unpaced max frames |
| `paced-60` | timing | 60 Hz target |
| `paced-native` | timing | native refresh (119.93 Hz here) — needs vblank facts |
| `stress-resize` | live-host + timing | resize storm |
| `input-latency` | timing | input→visible-change latency |

Each mode declares in `run.json` whether it is `deterministic`, `live-host`, or `timing` evidence.

## Acceptance (maps to spec)

- [ ] Each tier emits the proofLevel + authoritative/not-authoritative scope above. *(FR-004..008, SC-004)*
- [ ] T0/T1 run with no desktop; T2/T3/T-uinput opt-in and degrade per the table. *(FR-012, SC-001/005)*
- [ ] Perf modes declare their evidence kind. *(FR-009)*
