namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

/// <summary>One command row derived from the same catalog/profile pair used for dispatch.</summary>
type SvgWorkspaceCommandRow =
    { /// <summary>Stable semantic identity used by dispatch.</summary>
      Command: CommandId
      /// <summary>Player-facing label distinct from identity.</summary>
      Label: string
      /// <summary>Current product availability evaluated once for every surface.</summary>
      Available: bool
      /// <summary>Bindings active in the supplied workspace contexts.</summary>
      Gestures: InputGesture list
      /// <summary>Accessible prose for the first active gesture.</summary>
      ShortcutText: string option
      /// <summary>A logical single-chord ARIA token when faithfully representable.</summary>
      AriaKeyShortcuts: string option
      /// <summary>Whether catalog metadata exposes this row in the palette.</summary>
      InPalette: bool
      /// <summary>Whether catalog metadata exposes this row in menus.</summary>
      InMenu: bool
      /// <summary>Catalog-declared pointer control labels.</summary>
      PointerLabels: string list }

/// <summary>A reviewable rebinding candidate before the caller accepts it.</summary>
type SvgWorkspaceRebindPreview =
    { /// <summary>Command that would receive the captured gesture.</summary>
      Command: CommandId
      /// <summary>Raw captured gesture, including previously unbound input.</summary>
      Gesture: InputGesture
      /// <summary>Stable commands whose conflicting bindings would be displaced.</summary>
      DisplacedCommands: CommandId list
      /// <summary>Versioned candidate retained for explicit acceptance and persistence.</summary>
      CandidateProfile: InputProfile
      /// <summary>Immediately usable compiled candidate.</summary>
      EffectiveProfile: EffectiveInputProfile }

/// <summary>Pure command discovery and rebind projections for SVG workspace controls.</summary>
[<RequireQualifiedAccess>]
module SvgWorkspaceCommands =
    /// <summary>Accessible prose for chords, sequences and non-keyboard alternatives.</summary>
    val gestureText: gesture: InputGesture -> string
    /// <summary>Returns an ARIA shortcut token only for representable logical single chords.</summary>
    val ariaKeyShortcuts: gesture: InputGesture -> string option
    /// <summary>Derives the sole rows used by dispatch discovery, palette, help and pointer surfaces.</summary>
    val project:
        catalog: InputCatalog ->
        profile: EffectiveInputProfile ->
        activeContexts: string list ->
        isAvailable: (CommandDescriptor -> bool) ->
            SvgWorkspaceCommandRow list
    /// <summary>Available rows whose metadata declares a palette alternative.</summary>
    val paletteRows: rows: SvgWorkspaceCommandRow list -> SvgWorkspaceCommandRow list
    /// <summary>Every currently available row for live help.</summary>
    val helpRows: rows: SvgWorkspaceCommandRow list -> SvgWorkspaceCommandRow list
    /// <summary>Available rows with at least one pointer alternative.</summary>
    val pointerRows: rows: SvgWorkspaceCommandRow list -> SvgWorkspaceCommandRow list
    /// <summary>Captures a raw gesture, reports displacement, and validates the complete candidate without mutating the prior profile.</summary>
    val previewRebind:
        catalog: InputCatalog ->
        profile: InputProfile ->
        command: CommandId ->
        context: string ->
        gesture: InputGesture ->
            Result<SvgWorkspaceRebindPreview, InputProfileDiagnostic list>
