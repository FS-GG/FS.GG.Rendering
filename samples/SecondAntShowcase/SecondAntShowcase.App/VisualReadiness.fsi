module SecondAntShowcase.App.VisualReadiness

/// Runs the SecondAntShowcase visual-readiness CLI.
///
/// Screenshot capture and contact-sheet PNG composition remain sample-owned; target
/// matrix expansion, reviewer parsing, readiness aggregation, and managed-summary
/// updates delegate to FS.GG.UI.Testing.
val run: args: string list -> int
