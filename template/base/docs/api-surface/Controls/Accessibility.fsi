namespace FS.Skia.UI.Controls

/// Build and validate a control's accessibility contract: `metadata`/`defaultFor` plus `keyboard`/`contrast` evidence.
module Accessibility =
    /// Describe a control's keyboard contract: whether it is `focusable` and its activation/navigation keys.
    val keyboard: focusable: bool -> activationKeys: string list -> navigationKeys: string list -> KeyboardOperation
    /// Record `ContrastEvidence` for a foreground/background pair against the `requiredRatio`.
    val contrast: foreground: FS.Skia.UI.Scene.Color -> background: FS.Skia.UI.Scene.Color -> ratio: float -> requiredRatio: float -> ContrastEvidence
    /// Assemble full `AccessibilityMetadata` from role, name source, state, focus order, keyboard, contrast, and nav range.
    val metadata:
        role: AccessibilityRole ->
        nameSource: string ->
        state: string list ->
        focusOrder: int option ->
        keyboard: KeyboardOperation ->
        contrast: ContrastEvidence option ->
        navRange: NavRange option ->
            AccessibilityMetadata

    /// Build default `AccessibilityMetadata` for a `ControlKind` with the given accessible `label`.
    val defaultFor: kind: ControlKind -> label: string -> AccessibilityMetadata
    /// Check a `control`'s accessibility contract and return any `ControlDiagnostic` violations.
    val validate: control: Control<'msg> -> ControlDiagnostic list
