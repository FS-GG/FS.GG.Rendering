# Phase 1 Data Model: Surface the Keyboard-Only Host Input Boundary

**Feature**: 251-keyboard-host-boundary | **Date**: 2026-07-05

This feature introduces **no new types**. It surfaces facts about an **existing** host input contract. This document
records those facts as the "data model" the surfaced note must describe accurately (FR-006) — the two sides of the
boundary and the interop between them. All entries are *described*, not modified, by this feature.

## The keyboard side (the default game host)

| Element | Shape (as shipped) | Role in the boundary |
|---|---|---|
| `Viewer.runApp` | `options -> GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>` | The governed default persistent host for the `game` family; the path the game starter launches through (`Program.fs`). |
| `GeneratedAppHost<'model,'msg>.MapKey` | `ViewerKey -> bool -> 'msg option` | The **only** input seam on the default host — keyboard key + down/up flag in, optional product `Msg` out. **No `MapPointer`.** |
| `ViewerKey` | DU: `ArrowLeft` \| `ArrowRight` \| `ArrowUp` \| `ArrowDown` \| `Enter` \| `Space` \| `Escape` \| `Backspace` \| `Letter of char` \| `Digit of int` \| `Function of int` \| `Unknown of raw: string` | Keyboard keys only — **no** mouse-button or pointer case. The core fact of the boundary. |
| `DispatchInput of ViewerKey * isDown: bool` | Host-boundary input message | How keyboard input reaches the product on the default host — keyboard-shaped. |
| Game starter `paddleForKey` / `ViewerInput of ViewerKey * isDown` | `Model.fs` (`profile == "game"`) | The author's input-wiring site — a `ViewerKey -> command` mapping. The place the boundary comment lives. |

## The pointer side (the non-default interactive host)

| Element | Shape (as shipped) | Role in the boundary |
|---|---|---|
| `Controls.Elmish.runInteractiveApp` | interactive host runner (features 085/092) | The **non-default** host path that supports pointer input. |
| `InteractiveAppHost<'model,'msg>.MapPointer` | `ViewerPointerInput -> Size -> 'model -> 'msg list` | The mouse/pointer seam — the way to read mouse input for a mouse-aimed scheme. |
| `InteractiveAppHost.MapKey` | `ViewerKey -> bool -> 'msg list` | The interactive host's keyboard seam (note: `'msg list`, vs the default host's `'msg option`). |
| `ViewerPointerInput` | `{ Phase: ViewerPointerPhaseKind; X; Y; Button: ViewerPointerButtonKind option; DeltaX; DeltaY }` | The pointer payload delivered to `MapPointer` — carries position and button, i.e. what mouse aim needs. |

## The interop / signpost (what the note must connect)

- A game author on the **default host** has **only** `MapKey`/`ViewerKey` — keyboard.
- To read **mouse** input they must move to the **`InteractiveAppHost`/`runInteractiveApp`** path and use
  **`MapPointer`** — a different host wiring, and (because the game starter's host wiring is durable and
  governance-scanned) a deliberate, non-default choice, **not** an edit at the `Model.fs` input-wiring site.

## Validation rules (for the surfaced note)

- **Present**: the boundary appears at the game-starter input-wiring site (comment) and in the keyboard-input skill +
  fragment mirror.
- **Accurate**: every named element above matches the emitted `SkiaViewer`/`KeyboardInput` surface — the default host
  seam is `MapKey` (keyboard, `'msg option`, no `MapPointer`); `ViewerKey` has no mouse case; the pointer path is
  `InteractiveAppHost.MapPointer` via `runInteractiveApp`.
- **Non-regressive**: no durable/governance file, no evidence token, and no non-game profile output changes.

## State transitions

N/A — no stateful entity is added or changed; the feature is descriptive documentation over a static host contract.
