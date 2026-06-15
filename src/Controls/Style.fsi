namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

// Feature 093 (E3): `ResolvedStyle` (the record) is declared on `Types.fsi` — BEFORE `Theme` —
// so the overlapping field names (`Foreground`/`FontFamily`/`FontSize`) resolve to `Theme` for
// the many unannotated `theme.*` accesses in the renderer (F# picks the last-declared type for
// an ambiguous bare field). Same public surface, same namespace; only the declaration site
// moved. The resolver itself stays here.

/// Feature 093 (E3) — the single, pure, total, deterministic state→style resolver that replaces
/// procedural per-kind styling for the migrated controls. No selector matching, no specificity
/// algebra, no cross-control cascade (permanent roadmap non-goals).
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// The state-to-style resolver surface: `resolve` folds theme, base style, classes, and visual state into a `ResolvedStyle`.
module Style =
    /// Pure, total, deterministic. Precedence (FR-003), last-writer-wins per `ResolvedStyle`
    /// field:
    ///   `baseStyle` (the migrated kind's default, supplied by the caller)
    ///     &lt; each class in attach order (earlier &lt; later)
    ///     &lt; current visual state.
    /// A visual state's value for a field overrides any class's value for the same field; a
    /// later-attached class overrides an earlier one. `theme` carries the active palette
    /// (DTCG-generated `DesignTokens`); every colour the variant/state layers read originates
    /// from it — no inline literals (FR-008). For the default (no-class, `Normal`) case
    /// `resolve theme baseStyle [] Normal = baseStyle` exactly (parity, FR-005, SC-003).
    /// Total over every `(Theme, ResolvedStyle, StyleClass list, VisualState)`: every
    /// `StyleVariant`, any `Custom` string (unknown ⇒ identity delta, never an exception or
    /// silent drop), and all eight `VisualState` cases (FR-002, FR-004).
    val resolve:
        theme: Theme ->
        baseStyle: ResolvedStyle ->
        classes: StyleClass list ->
        state: VisualState ->
            ResolvedStyle
