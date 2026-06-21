namespace Rendering.Harness

open System

/// The harness tiers. `T-uinput` in prose/CLI is the DU case `TUinput`.
type Tier =
    | T0
    | T1
    | T2
    | T3
    | TUinput

/// Proof level an artifact may claim. Matches `run.json.proofLevel`.
type ProofLevel =
    | Deterministic
    | OffscreenPixels
    | LiveHost
    | Timing
    | KernelInput

/// Effective display backend after `WAYLAND_DISPLAY` is unset for the viewer.
type Backend =
    | X11
    | Wayland
    | NoDisplay

/// Facts the environment probe records per run.
type ProbeFacts =
    { EffectiveBackend: Backend
      Display: string option
      GlRenderer: string option
      GlVersion: string option
      GlDirect: bool
      RefreshHz: float option
      Extensions: string list
      SwapControl: int option
      VblankSource: string option
      UinputAvailable: bool }

/// Outcome of a run.
type RunStatus =
    | Passed
    | Failed
    | Skipped

/// What the pure planner decides for a tier given the probe facts.
type Degradation =
    | Run
    | Skip of reason: string
    | FailClassified of reason: string
