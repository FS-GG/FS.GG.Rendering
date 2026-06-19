# Feature 169 Migration Notes

## Inventory

- Controls diagnostics now expose `Diagnostics.toRuntimeDiagnostic` without renaming existing `ControlDiagnosticCode` values. Existing severity tokens map to the runtime severity taxonomy, and known backend-cost, environment, rendering-limitation, developer-action, and readiness-blocker categories are assigned from existing codes.
- SkiaViewer host diagnostics now expose `Host.Diagnostics.toRuntimeDiagnostic` without renaming existing host stage names. Fatal and error render failures map to runtime error severity; host capability and GL startup stages map to environment, frame-render warnings map to rendering limitation, and damage-scoped decisions map to backend cost.
- Controls.Elmish adapter diagnostics now expose `adapterDiagnosticToRuntimeDiagnostic`. Existing adapter diagnostic codes/messages are preserved and classified as developer-action warnings.
- AntShowcase adds a `diagnostics` command that emits classified runtime diagnostics for the sample CLI and host-decision path. The command is intentionally usable without opening a live viewer.
- Rendering.Harness validation lanes now carry optional typed `DiagnosticSummary` metadata, link runtime diagnostic artifacts when present, and include an optional `diagnostics` lane selected by `--include diagnostics`.

## Behavior Changes

- Missing classification and invalid or expired exceptions fail closed through the runtime diagnostics readiness rules.
- Developer-action warnings/errors require review unless accepted by a valid exception.
- Readiness-blocker diagnostics block readiness.
- Environment errors produce `environment-limited` rather than silently passing.
- Artifact write failures produce developer-action diagnostics rather than disappearing.

## Compatibility

- Existing Controls, SkiaViewer, and Controls.Elmish diagnostic producers keep their current public records and helpers.
- The new runtime diagnostics adapters are additive public APIs.
- Package-consuming AntShowcase projects now reference `FS.GG.UI.Diagnostics` at the current local package version.
- No telemetry, process-global sinks, background upload behavior, or external services were added.
