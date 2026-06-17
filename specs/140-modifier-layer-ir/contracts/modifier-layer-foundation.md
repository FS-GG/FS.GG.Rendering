# Contract: Modifier Layer Foundation

This is primarily an internal compatibility contract. Feature 140 must prove modifier/layer/portal/glyph-run
semantics while keeping existing public behavior compatible unless a public-surface or pixel disclosure entry
explicitly says otherwise.

## 1. Modifier Ordering

The foundation must support a closed initial set of effects:

- clip
- opacity
- offset
- transform
- background
- overlay
- cache boundary
- local z-order
- layer hint

Contract:

1. Effects are values, not closures.
2. Effects apply in the documented order stored on the chain.
3. Empty and identity chains are equivalent to no modifier.
4. Supported adjacent equivalent effects normalize without changing output, diagnostics, or fingerprints.
5. Normalization is idempotent.
6. Diagnostics identify unsupported effect combinations and malformed payloads.

## 2. Invalidation Classification

Every supported effect must map through one classification table:

- layout-affecting: invalidates layout and paint
- paint-affecting: invalidates paint only
- order-affecting: invalidates ordering/hit priority only unless combined with paint/layout effects

Contract:

1. Layout-affecting changes report layout and paint invalidation.
2. Paint-only changes do not report layout invalidation.
3. Order-only changes do not report layout invalidation.
4. Retained metrics and focused tests use the same classification table.
5. Any effect whose classification is unclear must fail planning/tasks rather than defaulting to layout-wide
   invalidation.

## 3. Local Z-Order

Local z-order is scoped to siblings under the same parent.

Contract:

1. Sibling paint order is stable-sorted by local z-order and declaration index.
2. Equal z-order values preserve declaration order.
3. A child cannot use local z-order to escape an ancestor clip or sibling scope.
4. Hit priority for the same scope is reverse paint order from the same ordered contribution stream.

## 4. Portal and Layer Hosts

Portal content is authored in one lexical location and rendered into an ordered layer host.

Contract:

1. Portal targets are explicit layer hosts.
2. Layer hosts paint bottom-to-top.
3. Hit testing scans layer hosts top-to-bottom using the same contribution order as painting.
4. Portal content targeting an escaping layer is not clipped by the ancestor containing its anchor.
5. Missing target layer or missing anchor evidence produces actionable diagnostics.
6. Empty layers render equivalently to no layers.
7. Legacy overlay behavior lowers to portal content and remains visually equivalent unless documented.

## 5. Legacy Lowering Compatibility

Existing scene/control forms must continue to work while internally lowering through the foundation:

- clipping
- translation
- perspective
- cached subtree
- text and text-run forms
- overlay container behavior

Contract:

1. Legacy-lowered output is equivalent to existing compatibility baselines unless a disclosure ledger records
   an intentional difference.
2. Legacy diagnostics remain equivalent or become more actionable without losing existing error information.
3. Cache-enabled versus cache-disabled parity remains valid.
4. Full rendering versus retained rendering parity remains valid for affected compatibility scenes.
5. Public constructors remain available for at least this feature unless a migration note and versioning
   recommendation explicitly say otherwise.

## 6. Glyph-Run Proof

The glyph-run proof must define stable public Scene shaped-text data without implementing full text shaping.

Contract:

1. Glyph-run proof data is declared through curated `Scene.fsi` surface.
2. Glyph-run proof data includes enough information for deterministic measurement, drawing, diagnostics, and
   fingerprinting.
3. Proof cases measure the same advance they draw.
4. Repeated equivalent glyph-run data produces stable fingerprints.
5. Existing text scenes that do not opt into proof behavior remain compatible with deterministic fallback
   rendering.
6. Full shaping, bidi, line breaking, font fallback expansion, and portable serialization are deferred and
   must not appear as implementation scope.

Public modifier/layer/portal Scene nodes are not part of this contract.

## 7. Public Surface and Migration Contract

If implementation changes public `.fsi` surface, including any `SceneNode`, `Scene`, glyph-run, Controls, or
SkiaViewer public contract:

1. The `.fsi` shape is designed before `.fs` implementation.
2. Semantic tests exercise the new surface.
3. Surface baselines are updated under the repository's readiness surface-baseline convention.
4. The compatibility plan states which existing forms remain supported, which are deprecated, and how to
   migrate.
5. The versioning recommendation states whether the preview package can absorb the change or needs a major
   compatibility signal.
6. Downstream exhaustive-match risk is called out for any public DU case addition.

## 8. Evidence Mapping

| Contract clause | Required evidence |
|---|---|
| Modifier ordering | Focused tests for each effect category and discriminating order examples |
| Normalization | At least 12 representative chains proving output, diagnostics, and fingerprints remain equivalent |
| Invalidation classification | Tests showing layout-affecting versus paint-only versus order-only metric differences |
| Local z-order | Sibling order tests, including equal z-order declaration-order fallback |
| Portal/layer hosts | Paint-order and hit-order tests across multiple layers and clipped ancestors |
| Legacy lowering | Compatibility scenes for clipping, translation, perspective, cached subtree, text, and overlay |
| Cache parity | Cache-enabled versus cache-disabled parity for modifier/layer/portal/glyph-run proof scenarios |
| Retained parity | Full versus retained parity for affected compatibility scenarios |
| Glyph-run proof | At least five deterministic sample text cases with stable fingerprints |
| Public surface | Package surface check and migration/versioning note for every intentional delta |
| Pixel changes | Disclosure ledger and rendering evidence for every intentional baseline change |

## 9. Non-Goals

- Full retained-renderer unification
- Overlay interaction state, open/close/dismiss/focus-trap behavior
- Portable scene serialization or wire protocol
- Compositor promotion, damage-scissored presentation, or texture tier
- Full HarfBuzz shaping, bidi, line breaking, or expanded font fallback
- Intrinsic layout protocol
- Public layout containers inside `SceneNode`
