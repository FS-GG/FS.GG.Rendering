# Data Model: Modifier Layer IR Foundation

This feature has no persisted data model. The entities below describe internal composition values,
compatibility evidence, and the optional glyph-run proof vocabulary that implementation and tests must pin.

## ModifierEffect

Represents one ordered visual or structural effect applied to content.

**Fields**
- `kind`: The closed effect kind, initially including clip, opacity, offset, transform, background, overlay,
  cache boundary, local z-order, and layer hint.
- `payload`: Effect-specific data, such as clip shape, opacity value, offset vector, transform matrix,
  background scene, overlay scene, cache policy, z-order value, or layer hint.
- `classification`: The effect's invalidation category.
- `source`: Compatibility origin, such as authored modifier, legacy clip, legacy translate, legacy perspective,
  existing cache boundary, or legacy overlay.

**Validation Rules**
- Effects are applied in documented list order; reversing order must be visible when the effects are not
  commutative.
- Identity effects normalize away without changing output, diagnostics, or fingerprints.
- Equivalent adjacent effects normalize to one effect when the algebra is defined for that effect pair.
- Unsupported or malformed payloads must produce diagnostics rather than silent disappearance.

## EffectClassification

Represents the invalidation impact of a modifier change.

**Fields**
- `affectsLayout`: `true` when a change requires measurement and paint invalidation.
- `affectsPaint`: `true` when a change requires repaint.
- `affectsOrder`: `true` when a change requires recomputing sibling/layer ordering or hit priority.
- `reason`: Short diagnostic text used by evidence and failure messages.

**Validation Rules**
- Layout-affecting effects also affect paint.
- Paint-only and order-only changes must not report layout invalidation.
- The retained work-reduction metrics and focused invalidation tests must read the same classification table.

## ModifierChain

Represents the ordered effects around one content value.

**Fields**
- `effects`: Ordered `ModifierEffect` list.
- `normalizedEffects`: The canonical list after dropping identities and combining supported adjacent effects.
- `fingerprintInput`: The deterministic data folded into cache/replay fingerprints.
- `diagnostics`: Ordering, unsupported-effect, and normalization diagnostics.

**Relationships**
- A composition node owns zero or more modifier chains.
- A normalization observation compares original and normalized chains for output, diagnostics, and fingerprint
  equivalence.

**Validation Rules**
- Empty chains are equivalent to no modifier.
- Normalization must be idempotent.
- Fingerprints for equivalent normalized chains must be byte-stable across repeated runs.

## CompositionNode

Represents internal content before lowering to final `Scene`.

**Fields**
- `content`: Primitive scene contribution, grouped child contribution, legacy-lowered contribution, portal
  anchor, or glyph-run proof contribution.
- `modifiers`: Ordered `ModifierChain`.
- `children`: Ordered child `CompositionNode` list.
- `declIndex`: Declaration index used to break equal local z-order ties.
- `controlId`: Optional control identity used for bounds, hit testing, diagnostics, and retained evidence.

**Relationships**
- A parent composes child nodes through local z-order ordering.
- A portal node contributes content to a `LayerHost` rather than to its lexical parent's in-flow output.
- Legacy scene/control forms lower into composition nodes before final paint and hit ordering are computed.

**Validation Rules**
- Equal local z-order values fall back to declaration order.
- Local z-order affects only siblings in the same parent scope.
- A missing control id cannot create a bindable hit target; diagnostics must identify missing anchor evidence
  when a portal needs it.

## Portal

Represents content authored at one location but rendered into an ordered layer host.

**Fields**
- `targetLayer`: The `LayerHost` id.
- `anchorId`: The originating control id or resolved anchor key.
- `anchorBounds`: Resolved bounds used by future anchored behavior and current diagnostics.
- `content`: The composition node rendered in the target layer.
- `diagnostics`: Missing target, missing anchor, or unsupported transform/clip escape evidence.

**Relationships**
- Portal content is collected while traversing the lexical tree and emitted through its target layer host.
- Portal content escapes ancestor clipping when the target layer requires it.
- Hit order for portal content is derived from the same ordered contribution stream as paint order.

**Validation Rules**
- A missing target layer or missing anchor evidence fails safely with actionable diagnostics.
- Empty portal layers render equivalently to no portal layers.
- Portal content anchored inside clipped ancestors is not clipped by the ancestor after it is lifted.

## LayerHost

Represents an ordered render destination for normal and portal content.

**Fields**
- `id`: Layer identifier, such as content, popup, tooltip, modal, drag feedback, or toast.
- `order`: Bottom-to-top ordering value.
- `contributions`: Ordered composition contributions targeting this layer.
- `hitPriority`: Derived top-to-bottom priority used by hit tests.

**Relationships**
- The final render output paints in-flow content and layer hosts bottom-to-top.
- Hit testing scans the same contributions in reverse paint order.

**Validation Rules**
- Higher layers paint above lower layers and receive hits first.
- Paint and hit ordering evidence must match for every tested layer scenario.
- Layer ordering must be deterministic across repeated runs.

## LegacyLowering

Represents compatibility mapping from existing public or internal forms into the new composition foundation.

**Fields**
- `legacyForm`: Existing form, such as `ClipNode`, `Translate`, `PerspectiveNode`, `CachedSubtree`, text node,
  `Overlay` container, or retained fragment overlay.
- `loweredForm`: Composition node/modifier/portal representation.
- `compatibilityStatus`: Supported unchanged, deprecated with migration, or intentionally changed.
- `migrationNote`: Guidance for any deprecated or changed form.

**Validation Rules**
- Existing clip, translation, perspective, cached subtree, text, and overlay scenarios must stay compatible
  unless a ledger entry documents an intentional change.
- Cache-enabled and cache-disabled paths must remain equivalent after lowering.
- Public surface changes must have surface baseline evidence and versioning recommendation.

## GlyphRunData

Represents the stable public Scene shaped-text proof data for future text work.

**Fields**
- `text`: Source text used for diagnostics and fallback proof.
- `font`: Existing `FontSpec` value.
- `glyphs`: Ordered glyph records, including glyph id or fallback code, advance, offset, cluster, and position
  data needed by the proof.
- `metrics`: Measured advance, height, and baseline used for layout and drawing proof.
- `fingerprint`: Deterministic fingerprint input for cache/protocol work.
- `fallbackDiagnostics`: Explicit fallback/tofu diagnostics for unsupported sample characters.

**Relationships**
- Glyph-run proof data is declared in `Scene.fsi` and can be measured and drawn consistently by the existing
  bundled-font/Skia path.
- Existing `Text`, `TextRun`, and `SizedText` behavior remains the compatibility baseline unless explicitly
  opted into proof behavior.

**Validation Rules**
- Repeated equivalent glyph-run data produces identical fingerprints.
- Measured advance used for layout equals the advance used for drawing in proof cases.
- Complex shaping requirements beyond the proof are recorded as deferred, not partially implemented.

## CompatibilityEvidence

Represents a verification record for behavior that changed or stayed compatible.

**Fields**
- `scenario`: Modifier, layer, portal, legacy lowering, cache parity, retained parity, glyph-run, surface, or
  pixel scenario.
- `expectedRelation`: Structural equality, pixel equality, documented intentional delta, or diagnostic outcome.
- `commands`: Validation commands that produced the evidence.
- `status`: Pass, intentional delta, blocked environmental limitation, or pre-existing failure.
- `ledgerEntry`: Link or note for public surface or pixel changes.

**Validation Rules**
- Every intentional public or pixel change has a ledger entry before readiness.
- Verification limitations distinguish pre-existing/environmental failures from feature regressions.
- P3 planning evidence names remaining risks and deferred work explicitly.

## State Transitions

This feature adds no runtime state machine. The implementation transition is architectural:

1. Existing composition forms render through feature 139's shared current-node assembly seam.
2. Modifier/layer/portal/glyph-run tests are added and fail against missing P2 semantics.
3. Legacy forms lower into the internal composition model.
4. Modifier chains normalize and classify invalidation through one table.
5. Portal layers replace ad hoc overlay ordering while preserving compatibility.
6. Final `Scene`, diagnostics, fingerprints, hit priority, and retained evidence are emitted from one ordered
   contribution stream.
7. Surface/pixel compatibility evidence records any intentional public or visual change.
