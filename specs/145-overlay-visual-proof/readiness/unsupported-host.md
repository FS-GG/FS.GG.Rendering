# Unsupported Host Limitation

The current Feature 145 readiness decision is closed because capable-host proof passed on X11/GL. No unsupported-host limitation is attached to the latest accepted run.

Unsupported-host handling was still exercised separately with `DISPLAY` and `WAYLAND_DISPLAY` unset:

- run id: `20260617-203612-022`
- status: environment-limited
- host capability: unsupported
- cause: missing-display
- readiness decision: environment-gated
- artifacts accepted: none
- next proof path: `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out specs/145-overlay-visual-proof/readiness`
- trust rationale: deterministic Feature 144 overlay behavior remains useful, but it is not visual proof; an unsupported-host record cannot close the visual-proof caveat.
- not authoritative for: Feature144 overlay visual-proof caveat closure, real overlay pixel order, final closed-state pixel cleanup

Host facts from the unsupported validation path:

- effective-backend=none
- display=none
- gl-renderer=none
- gl-version=none
- gl-direct=false
- refresh-hz=none
