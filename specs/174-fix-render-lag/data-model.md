# Data Model: Fix Render Lag

## Representative Interaction Scenario

Repeatable user action used to measure responsiveness.

**Fields**

- `scenarioId`: `button-click` or `page-change`.
- `pageId`: source page, such as `buttons`.
- `destinationPageId`: optional destination page, required for navigation (`text-numeric-input`).
- `inputScript`: ordered `FrameInput` or viewer script used by the probe/runner.
- `theme`: `light` or `dark`.
- `expectedVisibleResult`: reviewer-facing visible result.
- `responsivenessBudget`: median and p95/max thresholds from the spec.

**Validation Rules**

- `button-click` must cause a product-visible button state/action update.
- `page-change` must navigate to `text-numeric-input`.
- A scenario must produce measured latency or an explicit environment limitation.

## Frame Preparation Phase Record

One phase-attributed timing record for an input-to-visible frame.

**Fields**

- `scenarioId`, `runId`, `frameIndex`.
- `inputHandlingMs`: input receipt, queue, routing, and coalescing work.
- `modelUpdateMs`: product/runtime update contribution.
- `framePreparationMs`: retained view/stamp/diff/layout/metadata work before paint.
- `layoutMs`, `textMs`, `retainedStepMs`: subcomponents when available.
- `paintMs`: scene-to-canvas paint work.
- `presentationMs`: flush/swap/presentation contribution.
- `totalInputToVisibleMs`: end-to-end visible response time when measured.
- `dominantPhase`: largest named contribution.
- `environmentStatus`: `measured`, `environment-limited`, `failed`, or `not-run`.

**Validation Rules**

- Accepted measured frames require numeric `totalInputToVisibleMs`.
- Missing live presentation cannot be substituted with deterministic timings.
- Phase names must stay stable across baseline and optimized runs.

## Retained Frame State

Internal state carried between retained frames.

**Fields**

- `root`: retained control tree with stable identities.
- `layout`: previous `LayoutResult`.
- `stateByIdentity`: UI state by `RetainedId`.
- `memo`, `pictureCache`, `textCache`: bounded cross-frame caches.
- `metadata`: bounds, event bindings, bound ids, diagnostics, and node-count facts needed by render/routing/evidence.
- `workReduction`: recomputed, remeasured, repainted, dirty-region, cache, replay, and fallback counters.

**Validation Rules**

- Metadata reused from retained state must be byte-equivalent to the full render result for unchanged semantics.
- Cache hits require complete invalidation inputs; theme, layout, text proof, modifier layer, child ordering, and explicit identity changes must invalidate where relevant.
- Bounded caches must remain within their caps.

**State Transitions**

`none -> initialized -> stepped -> stepped ...`

`initialized` uses full first-frame preparation. `stepped` may reuse retained state and metadata only when invalidation evidence proves equivalence.

## Metadata Work Record

Work-scaling proof for the retained metadata path.

**Fields**

- `baselineNodeCount`: full-tree node count.
- `metadataVisitedNodeCount`: nodes whose metadata was recomputed or re-collected.
- `changedSubtreeBound`: expected changed work bound.
- `recomputedNodeCount`, `remeasuredNodeCount`, `repaintedNodeCount`.
- `boundsChanged`, `eventBindingsChanged`, `diagnosticsChanged`: parity facts.
- `fallbackCount`: full metadata fallback count with reason.

**Validation Rules**

- For localized interactions, metadata work must scale with changed/required work, not `baselineNodeCount`.
- Any full-tree fallback must be counted and explain why it was required.
- Bounds, event bindings, bound ids, diagnostics, and node count remain equivalent to the oracle.

## Latency Record

Persisted measured or classified responsiveness result for one representative action.

**Fields**

- Feature 173 viewer/sample fields: `recordId`, `runId`, `inputKind`, `page`, `controlFamily`, `controlIds`, `visibleResponse`, `environmentStatus`, `acceptanceStatus`, and diagnostics.
- Feature 174 additions in artifacts, if needed without public surface change: `scenarioId`, `baselineProfileId`, `optimizedProfileId`, `framePreparationMs`, `dominantPreparationSubphase`, `preparationReductionPercent`, and parity summary paths.

**Validation Rules**

- Accepted records require measured live presentation, passing latency budgets, and passing parity evidence.
- Environment-limited records are not accepted.
- Missing phase attribution makes the record incomplete.

## Parity Evidence

Proof that faster preparation did not change observable behavior.

**Fields**

- `scenarioId`.
- `visualParity`: pass/fail plus artifact path.
- `interactionParity`: pass/fail for event routing and product actions.
- `metadataParity`: pass/fail for bounds, event bindings, bound ids, diagnostics, and accessibility-facing metadata.
- `surfaceParity`: public surface baseline status.
- `caveats`: skipped, blocked, environment-limited, or manual-review notes.

**Validation Rules**

- All parity categories must pass for accepted performance evidence.
- Any intentional public surface or behavior change requires Tier 1 reclassification before implementation closeout.

## Environment Limitation

Classified reason accepted live evidence could not be collected.

**Fields**

- `code`: stable limitation token.
- `stage`: `desktop-prerequisite`, `window`, `input`, `presentation`, `timing`, `artifact-write`, or `timeout`.
- `message`: actionable maintainer text.
- `blocking`: true when accepted readiness is impossible.
- `artifact`: optional diagnostic file.

**Validation Rules**

- Blocking limitations keep readiness non-accepted.
- Headless deterministic evidence must remain marked as substitute or environment-limited.

## Regression Evidence Package

Final feature evidence bundle.

**Fields**

- `baselineTrace`: 2026-06-19 baseline values used for percentage comparisons.
- `optimizedRuns`: live run directories for required scenarios.
- `deterministicTests`: retained work-scaling and parity test results.
- `visualEvidence`: visual readiness outputs.
- `surfaceEvidence`: public surface baseline zero-diff result.
- `validationSummary`: reviewer entry point.
- `caveats`: explicit non-green states.

**Validation Rules**

- Final acceptance requires passing deterministic tests plus accepted live runs for supported environments.
- Unsupported environments must list explicit limitations and cannot be summarized as green.
