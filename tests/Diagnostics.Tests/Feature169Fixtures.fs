module Feature169Fixtures

open System
open FS.GG.UI.Diagnostics

let runId = "feature169-synthetic-fixture"

let source subsystem =
    RuntimeDiagnostics.source
        (Some "FS.GG.UI.Diagnostics.Tests")
        subsystem
        (Some "diagnostics")
        (Some "feature169")

let contextWith details =
    RuntimeDiagnostics.context
        (Some runId)
        (Some(DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc)))
        None
        details

let environmentWarning =
    // SYNTHETIC: representative headless host warning without requiring a real GL host.
    RuntimeDiagnostics.create
        (source "stdout")
        (Some "HeadlessHost")
        (Some DiagnosticSeverity.Warning)
        (Some DiagnosticCategory.Environment)
        "DISPLAY is unavailable; live screenshot evidence is environment limited."
        (Some "Accept the environment limitation only for headless validation lanes.")
        (contextWith [ "stream", "stderr" ])

let backendCostAt frame =
    // SYNTHETIC: repeated backend-cost event used to verify aggregation count and context retention.
    RuntimeDiagnostics.create
        (source "opengl-host")
        (Some "DamageScopedDecision")
        (Some DiagnosticSeverity.Informational)
        (Some DiagnosticCategory.BackendCost)
        "Damage-scoped redraw used an offscreen fallback."
        (Some "No action required unless this appears in a performance-blocked lane.")
        (contextWith [ "frame", string frame; "stream", "runtime" ])

let renderingLimitation =
    // SYNTHETIC: rendering limitation fixture independent of native Skia capabilities.
    RuntimeDiagnostics.create
        (source "renderer")
        (Some "FontFallback")
        (Some DiagnosticSeverity.Warning)
        (Some DiagnosticCategory.RenderingLimitation)
        "Requested font family used a bundled substitute."
        (Some "Review only if text evidence requires that exact platform font.")
        (contextWith [ "stream", "stdout" ])

let developerAction =
    // SYNTHETIC: developer-action warning fixture for fail-closed review behavior.
    RuntimeDiagnostics.create
        (source "package-feed")
        (Some "StalePackagePin")
        (Some DiagnosticSeverity.Warning)
        (Some DiagnosticCategory.DeveloperAction)
        "Sample package pin does not match the current local feed package."
        (Some "Run scripts/refresh-local-feed-and-samples.fsx before accepting readiness.")
        (contextWith [ "stream", "stdout" ])

let blocker =
    // SYNTHETIC: blocker fixture used to verify readiness-blocking status without a failed restore.
    RuntimeDiagnostics.create
        (source "validation-lanes")
        (Some "PackageRestoreFailed")
        (Some DiagnosticSeverity.Error)
        (Some DiagnosticCategory.ReadinessBlocker)
        "Package proof did not restore the current local package."
        (Some "Refresh the local feed and rerun package validation.")
        (contextWith [ "stream", "stderr" ])

let unclassified =
    // SYNTHETIC: intentionally incomplete classification to prove fail-closed review-required status.
    RuntimeDiagnostics.create
        (source "legacy-console")
        (Some "LegacyWarning")
        None
        (Some DiagnosticCategory.DeveloperAction)
        "Legacy console warning has not been classified with a severity."
        (Some "Classify the diagnostic before accepting readiness.")
        (contextWith [ "stream", "stderr" ])

let environmentLimit =
    // SYNTHETIC: accepted environment limitation fixture for status derivation.
    RuntimeDiagnostics.create
        (source "x11")
        (Some "UnsupportedDisplay")
        (Some DiagnosticSeverity.Error)
        (Some DiagnosticCategory.Environment)
        "X11 display is unavailable in this runner."
        (Some "Record the environment limitation and rerun on a live display for live proof.")
        (contextWith [ "stream", "stderr" ])

let mixedDiagnostics =
    [ environmentWarning
      backendCostAt 1
      renderingLimitation
      developerAction
      blocker ]

let repeatedBackendCost count =
    [ for frame in 1..count -> backendCostAt frame ]

let summarize diagnostics =
    RuntimeDiagnostics.summarize (Some runId) [] [ "diagnostics-summary.json" ] diagnostics
