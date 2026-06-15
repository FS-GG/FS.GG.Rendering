/// Per-page seeded input scripts for headless evidence (FR-009). Keys/ticks only, with
/// injected `TimeSpan` deltas — no wall-clock, no randomness — so the same seed yields
/// the same `FrameMetrics` and therefore byte-identical evidence.
module ControlsGallery.Core.Scripts

open System
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open ControlsGallery.Core.Model

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }

let private tick (ms: float): FrameInput<GalleryMsg> = FrameInput.Tick(TimeSpan.FromMilliseconds ms)
let private press (k: ViewerKey): FrameInput<GalleryMsg> = FrameInput.Key(k, noMods)

/// Deterministic script for a page: settle, activate (Space/Enter exercise the focused
/// command via the host key map), settle, idle. Seed-independent in content but replayed
/// per the explicit `--seed` so the evidence record is keyed by it.
let forPage (_pageId: string): FrameInput<GalleryMsg> list =
    [ tick 16.0
      press Space
      tick 16.0
      press Enter
      tick 16.0
      FrameInput.Idle ]

/// All page scripts, in registry order.
let all: (string * FrameInput<GalleryMsg> list) list =
    Pages.all |> List.map (fun p -> p.Id, forPage p.Id)
