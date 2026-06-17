/// Per-page seeded input scripts for headless evidence (FR-011 / R7). Keys/ticks only,
/// with injected `TimeSpan` deltas — no wall-clock, no randomness — so the same seed
/// yields the same `FrameMetrics` and therefore byte-identical evidence (SC-004).
///
/// `FrameInput` carries only `Key`/`Pointer`/`Tick`/`Idle` (no arbitrary message), so the
/// enterprise form's invalid-then-valid transition is exercised deterministically through
/// the pure `Model.update` (see `formInvalidThenValid` + `TemplateTests`), not through a
/// keyboard script — the seeded script settles and activates the focused command.
module AntShowcase.Core.Scripts

open System
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open AntShowcase.Core.Model

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }

let private tick (ms: float): FrameInput<AntShowcaseMsg> = FrameInput.Tick(TimeSpan.FromMilliseconds ms)
let private press (k: ViewerKey): FrameInput<AntShowcaseMsg> = FrameInput.Key(k, noMods)

/// Deterministic script for a page: settle, activate (Space/Enter exercise the focused
/// command via the host key map), settle, idle.
let forPage (_pageId: string): FrameInput<AntShowcaseMsg> list =
    [ tick 16.0
      press Space
      tick 16.0
      press Enter
      tick 16.0
      FrameInput.Idle ]

/// All page scripts, in registry order.
let all: (string * FrameInput<AntShowcaseMsg> list) list =
    PageRegistry.all |> List.map (fun p -> p.Id, forPage p.Id)

/// The form template's deterministic invalid-then-valid message sequence (R7 / SC-009),
/// folded through pure `Model.update`. Starts from an intentionally invalid edit (blank
/// name + bad email + un-agreed), submits (→ `Invalid`), corrects each field, then
/// submits again (→ `Submitted`).
let formInvalidThenValid: AntShowcaseMsg list =
    [ PageMsg(FormFieldChanged("Name", ""))
      PageMsg(FormFieldChanged("Email", "not-an-email"))
      PageMsg(FormFieldChanged("Agree", "false"))
      PageMsg FormSubmitted
      PageMsg(FormFieldChanged("Name", "Ada Lovelace"))
      PageMsg(FormFieldChanged("Email", "ada@example.com"))
      PageMsg(FormFieldChanged("Agree", "true"))
      PageMsg FormSubmitted ]
