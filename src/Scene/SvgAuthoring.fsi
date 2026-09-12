namespace FS.GG.UI.Scene

/// Metadata required when untrusted SVG XML crosses into the typed document boundary.
type SvgImportRequest =
    { AssetNamespace: string
      DocumentId: string
      Limits: SvgDocumentLimits }

/// Safe SVG XML import operations.
[<RequireQualifiedAccess>]
module SvgImport =
    /// Parse the deliberately small, inert SVG profile into a validated document.
    /// The importer rejects active content, external references, CSS, filters, entities and DTDs.
    val importXml: request: SvgImportRequest -> xml: string -> Result<SvgDocument, SvgDocumentIssue list>

/// Rights metadata retained with every reusable asset revision.
type SvgAssetRights =
    { License: string
      Attribution: string option
      Source: string option }

/// Exact dependency on another catalog asset revision.
type SvgAssetReference =
    { AssetId: string
      Revision: int }

/// Immutable versioned asset. `ContentHash` is the lowercase SHA-256 of the canonical document bytes.
type SvgAssetEnvelope =
    { Schema: string
      AssetId: string
      Revision: int
      ContentHash: string
      Rights: SvgAssetRights
      Dependencies: SvgAssetReference list
      Document: SvgDocument }

/// A separately versioned collection of immutable asset revisions.
type SvgAssetCatalog =
    { Schema: string
      Assets: SvgAssetEnvelope list }

[<RequireQualifiedAccess>]
/// Authorable properties supported by prefab overrides.
type SvgPrefabProperty =
    /// Override the element affine transform.
    | Transform
    /// Override whether the element is rendered.
    | Visibility
    /// Override the element paint/presentation value.
    | Presentation

[<RequireQualifiedAccess>]
/// Typed value for one prefab property override.
type SvgPrefabOverrideValue =
    /// Replacement affine transform.
    | Transform of SvgAffine
    /// Replacement visibility.
    | Visibility of bool
    /// Replacement presentation, where `None` removes inherited presentation.
    | Presentation of SvgPresentation option

/// One distinguishable property override on an asset-local element.
type SvgPrefabOverride =
    { ElementId: string
      Property: SvgPrefabProperty
      Value: SvgPrefabOverrideValue }

/// A prefab instance pinned to its last explicitly accepted asset revision.
type SvgPrefabInstance =
    { InstanceId: string
      AssetId: string
      AcceptedRevision: int
      Overrides: SvgPrefabOverride list }

/// Reviewable conflict produced when a retained override cannot apply to a revision.
type SvgPrefabConflict =
    { InstanceId: string
      ElementId: string
      Property: SvgPrefabProperty
      Message: string }

[<RequireQualifiedAccess>]
/// One operation inside an all-or-nothing authoring transaction.
type SvgAuthoringOperation =
    /// Replace the current typed document.
    | ReplaceDocument of SvgDocument
    /// Compose one affine transform onto every named element.
    | TransformElements of elementIds: string list * transform: SvgAffine
    /// Insert or replace one exact asset revision.
    | UpsertAsset of SvgAssetEnvelope
    /// Insert or replace one prefab instance.
    | PutInstance of SvgPrefabInstance
    /// Explicitly advance all matching instances to another available revision.
    | UpdateInstances of assetId: string * fromRevision: int * toRevision: int

/// Named group of operations that commits as one revision and undo entry.
type SvgAuthoringTransaction =
    { Schema: string
      Id: string
      Operations: SvgAuthoringOperation list }

/// Immutable play handoff containing canonical document bytes and persistent catalog values.
type SvgAuthoringSnapshot =
    { SourceRevision: int
      SerializedDocument: string
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list }

/// Complete accepted value restored by undo and redo.
type SvgAuthoringCheckpoint =
    { Document: SvgDocument
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list
      Conflicts: SvgPrefabConflict list }

/// Validated candidate kept outside accepted content and history until commit.
type SvgAuthoringPreview =
    { TransactionId: string
      BaseRevision: int
      Candidate: SvgAuthoringCheckpoint }

/// Portable editor state for atomic document, catalog and prefab transactions.
type SvgAuthoringState =
    { Revision: int
      Document: SvgDocument
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list
      Conflicts: SvgPrefabConflict list
      Undo: SvgAuthoringCheckpoint list
      Redo: SvgAuthoringCheckpoint list
      Preview: SvgAuthoringPreview option
      PlaySnapshot: SvgAuthoringSnapshot option }

[<RequireQualifiedAccess>]
/// Explicit refusal from an authoring transition.
type SvgAuthoringError =
    /// The completion was based on a different accepted revision.
    | StaleRevision of expected: int * actual: int
    /// At least one candidate value or operation failed validation.
    | InvalidTransaction of SvgDocumentIssue list
    /// A different grouped preview is already active.
    | PreviewAlreadyActive of string
    /// No grouped preview is available to commit or cancel.
    | NoActivePreview
    /// The requested transaction does not identify the active preview.
    | PreviewMismatch of string
    /// The undo history is empty.
    | NothingToUndo
    /// The redo history is empty.
    | NothingToRedo

/// Versioned asset validation, hashing and prefab resolution.
[<RequireQualifiedAccess>]
module SvgAsset =
    /// Current asset-envelope schema identifier.
    val schema: string
    /// Current asset-catalog schema identifier.
    val catalogSchema: string
    /// Serialize a validated catalog into the canonical bounded asset-envelope wire format.
    val serializeCatalog: catalog: SvgAssetCatalog -> Result<string, SvgDocumentIssue list>
    /// Decode and validate a canonical asset-envelope wire value.
    val deserializeCatalog: serialized: string -> Result<SvgAssetCatalog, SvgDocumentIssue list>
    /// Compute the lowercase SHA-256 over the canonical `fsgg.svg-document/1` bytes.
    val contentHash: document: SvgDocument -> Result<string, SvgDocumentIssue list>
    /// Validate schema, identity, revision, rights, hash, dependencies and catalog cycles.
    val validateCatalog: catalog: SvgAssetCatalog -> SvgDocumentIssue list
    /// Resolve an accepted revision and apply typed overrides. Missing or incompatible values fail loud.
    val resolveInstance:
        catalog: SvgAssetCatalog ->
        instance: SvgPrefabInstance ->
        Result<SvgDocument * SvgPrefabConflict list, SvgDocumentIssue list>

[<RequireQualifiedAccess>]
/// Atomic authoring transitions over persistent typed values.
module SvgAuthoring =
    /// Current transaction-envelope schema identifier.
    val transactionSchema: string
    /// Validate initial document, catalog and instances without accepting partial state.
    val tryCreate:
        revision: int ->
        document: SvgDocument ->
        catalog: SvgAssetCatalog ->
        instances: SvgPrefabInstance list ->
        Result<SvgAuthoringState, SvgAuthoringError>

    /// Commit all operations or none. A successful group creates exactly one undo entry and clears redo.
    val commit:
        expectedRevision: int ->
        transaction: SvgAuthoringTransaction ->
        state: SvgAuthoringState ->
        Result<SvgAuthoringState, SvgAuthoringError>

    /// Start or replace a grouped preview without changing accepted state or history.
    val preview:
        expectedRevision: int ->
        transaction: SvgAuthoringTransaction ->
        state: SvgAuthoringState ->
        Result<SvgAuthoringState, SvgAuthoringError>

    /// Commit the named validated preview as one revision and undo entry.
    val commitPreview:
        expectedRevision: int ->
        transactionId: string ->
        state: SvgAuthoringState ->
        Result<SvgAuthoringState, SvgAuthoringError>

    /// Discard the named preview without changing accepted values or history.
    val cancelPreview: transactionId: string -> state: SvgAuthoringState -> Result<SvgAuthoringState, SvgAuthoringError>
    /// Restore the complete prior checkpoint as one new revision.
    val undo: expectedRevision: int -> state: SvgAuthoringState -> Result<SvgAuthoringState, SvgAuthoringError>
    /// Restore the complete next checkpoint as one new revision.
    val redo: expectedRevision: int -> state: SvgAuthoringState -> Result<SvgAuthoringState, SvgAuthoringError>
    /// Capture canonical immutable values for a play/session handoff.
    val takePlaySnapshot: expectedRevision: int -> state: SvgAuthoringState -> Result<SvgAuthoringState, SvgAuthoringError>
