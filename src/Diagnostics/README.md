# FS.GG.UI.Diagnostics

`FS.GG.UI.Diagnostics` is the dependency-light shared runtime diagnostics
package. It owns the public taxonomy used by runtime packages, sample commands,
validation lanes, and readiness artifacts.

The package intentionally has no dependency on Controls, SkiaViewer,
Controls.Elmish, Testing, samples, or the rendering harness. Producer-specific
adapter functions live beside their producers and convert into this package's
records.

The package performs no telemetry and selects no logging provider. It exposes
pure classification, aggregation, readiness evaluation, and deterministic
console/JSON/Markdown artifact rendering. Filesystem writes are explicit edge
operations and report write failures as developer-action diagnostics.
