namespace FS.GG.UI.Controls

/// Accessibility facts a catalog entry advertises for a control: its `Role`,
/// where the accessible name comes from (`NameSource`), reported `StateMetadata`,
/// `FocusBehavior`, `KeyboardOperation`, and the `ContrastEvidence` backing it.
type CatalogAccessibility =
    { Role: string
      NameSource: string
      StateMetadata: string list
      FocusBehavior: string
      KeyboardOperation: string
      ContrastEvidence: string }

/// One control's full authoring contract as published by `Catalog`: identity
/// (`Id`/`DisplayName`/`Category`/`Module`), `Purpose`, its `RequiredAttributes`
/// and `CommonAttributes`, bindable `Events`, `VisualStates`, `Accessibility`,
/// plus `Examples`/`Tests`/`Evidence` and `SupportStatus`/`Owner` provenance.
type ControlDefinition =
    { Id: string
      DisplayName: string
      Category: string
      Module: string
      Purpose: string
      RequiredAttributes: string list
      CommonAttributes: string list
      Events: string list
      VisualStates: string list
      Accessibility: CatalogAccessibility
      Examples: string list
      Tests: string list
      Evidence: string list
      SupportStatus: string
      Owner: string }

/// Discovery surface for the standard control library: enumerate every control's
/// authoring contract (required/supported attributes, events) and validate
/// authored `Control` values against the published schema.
module Catalog =
    /// The full list of `ControlDefinition` entries — one per supported control —
    /// to enumerate the whole authoring catalog.
    val supportedControls: ControlDefinition list
    /// The machine-readable `ControlSchema` per standard control, pairing each
    /// kind with its accepted attributes and events for validation.
    val standardSchema: ControlSchema list
    /// Every `StandardControlKind` the catalog knows about — the entry point for
    /// enumerating which controls can be authored and queried.
    val knownControlKinds: unit -> StandardControlKind list
    /// The `StandardAttributeName`s a control of `kind` must carry to be valid —
    /// the mandatory subset of its authoring contract.
    val requiredAttributes: kind: StandardControlKind -> StandardAttributeName list
    /// Every `StandardAttributeName` a control of `kind` accepts (required plus
    /// optional), to discover the full attribute surface for that control.
    val supportedAttributes: kind: StandardControlKind -> StandardAttributeName list
    /// The `StandardEventKind`s a control of `kind` can raise — the bindable
    /// events a consumer may wire handlers to.
    val supportedEvents: kind: StandardControlKind -> StandardEventKind list
    /// Checks an authored `control` against the catalog schema, returning a
    /// `ControlDiagnostic` for each missing required or unsupported attribute.
    val validateStandardControl: control: Control<'msg> -> ControlDiagnostic list
    /// The number of entries in `supportedControls` — how many distinct controls
    /// the catalog documents.
    val supportedCount: unit -> int
    /// The distinct `Category` values across the catalog, to group controls when
    /// presenting a discovery index.
    val categories: unit -> string list
    /// Self-checks the catalog itself, returning any `ControlDiagnostic` for
    /// internal inconsistencies between definitions and the schema.
    val validate: unit -> ControlDiagnostic list
    /// Renders the whole catalog as a Markdown reference table — a ready-to-read
    /// summary of every control's authoring contract.
    val markdownSummary: unit -> string
