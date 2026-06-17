namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

/// Feature 140: internal modifier/layer/portal composition foundation.
/// This module is assembly-internal and exists to make composition semantics
/// value-based, testable, and shared by Controls and retained evidence without
/// adding public Scene modifier/layer/portal nodes.
module internal Composition =

    type EffectInvalidation =
        { AffectsLayout: bool
          AffectsPaint: bool
          AffectsOrder: bool
          Reason: string }

    type ModifierEffect =
        | Clip of Clip
        | Opacity of float
        | Offset of dx: float * dy: float
        | Transform of PerspectiveTransform
        | Background of Scene
        | Overlay of Scene
        | CacheBoundary of cacheId: uint64
        | LocalZOrder of z: int
        | LayerHint of layerId: string

    type ModifierSource =
        | AuthoredModifier
        | LegacyClipSource
        | LegacyTranslateSource
        | LegacyPerspectiveSource
        | LegacyCacheSource
        | LegacyTextSource
        | LegacyOverlaySource
        | GlyphRunProof

    type ModifierEntry =
        { Effect: ModifierEffect
          Source: ModifierSource }

    type ModifierDiagnostic =
        { Code: string
          Message: string }

    type ModifierChain =
        { Effects: ModifierEntry list
          NormalizedEffects: ModifierEntry list
          FingerprintInput: string
          Diagnostics: ModifierDiagnostic list }

    val classificationTable: (string * EffectInvalidation) list
    val classify: effect: ModifierEffect -> EffectInvalidation
    val normalize: effects: ModifierEntry list -> ModifierChain
    val fingerprint: effects: ModifierEntry list -> uint64
    val applyChain: chain: ModifierChain -> content: Scene list -> Scene list

    /// Feature 141 (R1b): retained reuse reads this normalized composition evidence instead of owning a
    /// second modifier/layer/portal invalidation table.
    type RetainedReuseEvidence =
        { NormalizedModifierFingerprint: uint64
          AffectsLayout: bool
          AffectsPaint: bool
          AffectsOrder: bool
          Reasons: string list }

    val retainedReuseEvidence: chain: ModifierChain -> RetainedReuseEvidence

    type OrderedContribution =
        { Id: string
          DeclIndex: int
          LocalZ: int
          Layer: string
          Scene: Scene list
          HitBounds: Rect option }

    val contribution:
        id: string ->
        declIndex: int ->
        localZ: int ->
        layer: string ->
        scene: Scene list ->
        hitBounds: Rect option ->
            OrderedContribution

    val orderSiblings: contributions: OrderedContribution list -> OrderedContribution list
    val paintOrder: contributions: OrderedContribution list -> OrderedContribution list
    val hitOrder: contributions: OrderedContribution list -> OrderedContribution list

    type LayerHost =
        { Id: string
          Order: int
          EscapesClip: bool }

    type Portal =
        { TargetLayer: string
          AnchorId: string option
          AnchorBounds: Rect option
          Content: OrderedContribution }

    type PortalDiagnostic =
        { Code: string
          Message: string
          TargetLayer: string option
          AnchorId: string option }

    type LayerComposition =
        { Paint: OrderedContribution list
          Hit: OrderedContribution list
          Diagnostics: PortalDiagnostic list }

    val layerHost: id: string -> order: int -> escapesClip: bool -> LayerHost
    val portal:
        targetLayer: string ->
        anchorId: string option ->
        anchorBounds: Rect option ->
        content: OrderedContribution ->
            Portal
    val composeLayers:
        hosts: LayerHost list ->
        inFlow: OrderedContribution list ->
        portals: Portal list ->
            LayerComposition

    type LegacyForm =
        | LegacyClipping of Clip
        | LegacyTranslation of dx: float * dy: float
        | LegacyPerspective of PerspectiveTransform
        | LegacyCachedSubtree of cacheId: uint64
        | LegacyText
        | LegacyOverlay

    type LegacyCompatibilityStatus =
        | SupportedUnchanged
        | DeprecatedWithMigration of note: string
        | IntentionallyChanged of note: string

    val legacyLower: form: LegacyForm -> ModifierEntry list
    val compatibilityEvidence: form: LegacyForm -> LegacyCompatibilityStatus * string
