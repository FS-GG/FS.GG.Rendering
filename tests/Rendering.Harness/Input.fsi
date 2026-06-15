namespace Rendering.Harness

/// Declarative input-script layer (feature 122). ONE backend-agnostic script (click / key /
/// injected-wait) interpreted by a selectable backend: `Pure` (deterministic MVU replay,
/// gate-runnable), `X11XTest` (live X11 window via XTEST, env-gated), `Uinput` (kernel evdev/libinput
/// path, env-gated). Every run emits no-overclaim `Evidence` (a non-empty `NotAuthoritativeFor`); the
/// run/skip/fail decision comes from the pure `RunPlan.plan` (the executor only interprets a `Run`).
/// Harness-only - no product surface.
module Input =

    /// One declarative action. `Wait` is an INJECTED duration - deterministic, never wall-clock.
    type InputStep =
        | Click of int * int
        | Key of string
        | Wait of int

    /// A named, ordered, backend-agnostic scenario.
    type InputScript = { Name: string; Steps: InputStep list }

    /// The selectable interpreter. DISTINCT from the display `Backend` (`X11`/`Wayland`/`NoDisplay`).
    type InputBackend =
        | Pure
        | X11XTest
        | Uinput

    /// Parse a `--backend` token (`pure|x11-xtest|uinput`); `None` if unrecognised.
    val parseBackend: token: string -> InputBackend option

    /// Stable string form (`pure|x11-xtest|uinput`).
    val backendToken: backend: InputBackend -> string

    /// The named script catalog, resolved by `--script <name>`.
    val scripts: Map<string, InputScript>

    /// Resolve a script by name; `None` if unknown.
    val tryScript: name: string -> InputScript option

    /// Interpret `script` under `backend`. Computes the run/skip/fail decision via `RunPlan.plan` (the
    /// executor only interprets a `Run`) and returns no-overclaim `Evidence` with a non-empty
    /// `NotAuthoritativeFor`. TOTAL: any backend on any environment yields valid evidence (a disclosed
    /// `Skipped`/classified-fail rather than a throw or a hang).
    ///
    /// - `Pure`: replays the script against the harness demo MVU app (`Live.update`/`Live.view`) so pure
    ///   and live prove the SAME scenario; deterministic + headless (`Wait` injected, no wall-clock);
    ///   proves input->repaint by a `before <> after` scene change; emits byte-reproducible evidence.
    /// - `X11XTest`: drives a live viewer window with real XTEST input (delegates to the proven live
    ///   path) and confirms a visible change; honest-skips with no display, fail-classified on Wayland.
    /// - `Uinput`: requires `/dev/uinput`; honest-skips promptly when absent (the kernel-drive executor
    ///   is the env-gated Workstream A4 follow-up).
    val run: backend: InputBackend -> script: InputScript -> facts: ProbeFacts -> selfDll: string -> outDir: string -> Evidence.Evidence
