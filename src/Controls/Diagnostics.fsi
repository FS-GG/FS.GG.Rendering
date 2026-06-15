namespace FS.GG.UI.Controls

/// Constructors for `ControlDiagnostic` values reported by the controls runtime and validation passes.
module Diagnostics =
    /// Builds a `ControlDiagnostic` from an explicit `code`, `severity`, and `message`, optionally scoped to a `controlId` and `kind`.
    val create:
        controlId: ControlId option ->
        kind: ControlKind ->
        code: ControlDiagnosticCode ->
        severity: ControlDiagnosticSeverity ->
        message: string ->
            ControlDiagnostic

    /// Reports that a control of `kind` is missing a required attribute `name`.
    val missingRequired: controlId: ControlId option -> kind: ControlKind -> name: string -> ControlDiagnostic
    /// Reports that attribute `name` was supplied more than once on a control of `kind`.
    val duplicateAttribute: controlId: ControlId option -> kind: ControlKind -> name: string -> ControlDiagnostic
    /// Reports that a control of `kind` lacks the accessibility metadata it requires.
    val missingAccessibility: controlId: ControlId option -> kind: ControlKind -> ControlDiagnostic
    /// Reports that the same `key` identifies two sibling controls, a `ControlId` collision.
    val keyCollision: key: ControlId -> kind: ControlKind -> ControlDiagnostic
    /// Reports that a control of `kind` requested a `capability` the host environment does not support.
    val unsupportedEnvironment: kind: ControlKind -> capability: string -> ControlDiagnostic
    /// Reports that standard control `kind` does not support the standard attribute `name`.
    val unsupportedStandardAttribute: kind: StandardControlKind -> name: StandardAttributeName -> ControlDiagnostic
    /// Reports that standard control `kind` does not raise the standard event `eventKind`.
    val unsupportedStandardEvent: kind: StandardControlKind -> eventKind: StandardEventKind -> ControlDiagnostic
    /// Reports that standard control `kind` omits a required standard attribute `name`.
    val missingStandardAttribute: kind: StandardControlKind -> name: StandardAttributeName -> ControlDiagnostic
    /// Reports that a control `kind` declares a non-standard `extensionName` outside the standard contract.
    val customExtension: kind: string -> extensionName: string -> ControlDiagnostic
    /// Reports a `packageId` reference at `path` that no longer resolves to a current package.
    val stalePackageReference: packageId: string -> path: string -> ControlDiagnostic
    /// Reports that `packageId` exposes a transitive `dependencyPath` that should not leak across the package boundary.
    val dependencyLeak: packageId: string -> dependencyPath: string -> ControlDiagnostic
    /// Reports that the catalog entry for `controlId` omits a `requiredField`.
    val catalogOmission: controlId: string -> requiredField: string -> ControlDiagnostic
    /// Reports that `runtimeName` is defined more than once, the duplicate residing at `path`.
    val duplicateRuntimeDefinition: runtimeName: string -> path: string -> ControlDiagnostic
    /// Reports that an `eventKind` binding targets a `controlId` that no longer exists in the tree.
    val staleEventTarget: controlId: ControlId -> eventKind: string -> ControlDiagnostic
    /// Reports that `scopeName` owned by `owner` cannot be expanded as requested.
    val unsupportedScopeExpansion: scopeName: string -> owner: string -> ControlDiagnostic

    /// Feature 113 (Phase 5) — the stability-diagnostic report (report-only, NOT an enforced gate).
    /// Given TWO builds of the SAME logical control (sub)tree (`first`/`second` — the same model run
    /// through `View` twice), walk them in parallel and return one `UnstableReuseInput` finding per
    /// attribute/event that compared UNEQUAL despite no semantic change: a rebuilt `UntypedValue`, a
    /// per-frame event closure (reference-fresh each build), or an unstable key. Each finding names the
    /// control (`ControlId` + `ControlKind`) and the offending attribute/event name. An empty list ⇒
    /// the tree's inputs are stable across builds (the case memoization can exploit). Stable structural
    /// values (`TextValue`, `StringListValue`, …) and reference-shared event handlers do not flag. Pure,
    /// total, deterministic; never compares a function with `=` (uses reference identity for closures).
    val stabilityReport: first: Control<'msg> -> second: Control<'msg> -> ControlDiagnostic list
