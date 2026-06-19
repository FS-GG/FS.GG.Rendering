# Research: Structured Render/Layout Inspection Metadata

## R1 - Contract Ownership and Package Boundary

**Decision**: Add the dependency-light inspection data model to `FS.GG.UI.Scene`, add inspected-Control extraction in `FS.GG.UI.Controls`, and add validation/reporting helpers in `FS.GG.UI.Testing`.

**Rationale**: `Testing` already references `Scene`, and `Controls` already references `Scene`. Placing shared model types in Scene avoids adding a Testing dependency to Controls or a Controls/Layout dependency to Testing. Scene already has precedent for dependency-light layout evidence through `LayoutEvidenceReport`.

**Alternatives considered**: Putting all inspection APIs in Testing was rejected because Testing would need to reference Controls or could not provide a Controls adapter. Putting the model in Controls was rejected because Testing would need a Controls dependency. Creating a new package was rejected because it would add package and baseline overhead before the boundary proves necessary.

## R2 - Data Source for Initial Inspection

**Decision**: Derive the first inspection artifacts from `Control.renderTree`, its evaluated bounds, the authored/lowered control tree, the produced `Scene`, existing control diagnostics, layout results available inside Controls, and supported Scene traversal.

**Rationale**: The feature needs deterministic facts about the screen that are already produced by the current render pipeline. This avoids screenshot parsing, live viewer dependencies, and a new renderer. It also lets tests seed known defects with ordinary controls and scenes.

**Alternatives considered**: Pixel-readback analysis was rejected because it duplicates visual-readiness screenshot work and is harder to classify semantically. A live SkiaViewer-only inspector was rejected because inspection must run headless. Reading private renderer state from Testing was rejected because it breaks package boundaries.

## R3 - Stable Identity Strategy

**Decision**: Use authored control keys when available, otherwise use the same deterministic structural path scheme already used by `Control.renderTree` bounds and event bindings. Inspection findings use stable rule ids plus affected node/region ids.

**Rationale**: Existing Controls code already correlates `Bounds`, `EventBindings`, and `BoundIds` through `Key ?? structural-path`. Reusing that identity rule makes inspection evidence repeatable and aligns findings with hit-testing and event routing.

**Alternatives considered**: Random ids and run-order counters were rejected because summaries would churn. Requiring every node to have an authored key was rejected because existing controls and generated products may not provide one.

## R4 - Coordinate Space and Transform Handling

**Decision**: Report bounds in the final logical output coordinate space using `Scene.Rect` and `Scene.Size`. Simple nested offsets and supported translated scenes are normalized into that space. Complex transforms that cannot be represented safely in a rectangular artifact are reported as unsupported facts.

**Rationale**: Reviewers and tests need one coordinate space for containment and overlap checks. Explicit unsupported findings are safer than silently dropping transformed content or pretending rectangular bounds are exact when they are not.

**Alternatives considered**: Reporting local coordinates only was rejected because cross-region assertions become ambiguous. Flattening every transform into approximate rectangles was rejected for the first version because it can hide false positives and false negatives.

## R5 - Text Measurement and Fit Classification

**Decision**: Store text bounds, owner bounds, measured size, baseline/vertical placement when available, and a fit classification of inside, overflow, clipped, truncated, wrapped, unsupported, or unavailable. Exactness is recorded separately so approximate measurement cannot be mistaken for exact proof.

**Rationale**: The retrospective asks specifically for deterministic checks of text overlap and clipping. Existing Scene text measurement and Controls text handling can supply enough facts for initial checks, but exact glyph shaping or backend font availability may vary. The artifact must make that distinction visible.

**Alternatives considered**: Treating all text measurements as exact was rejected because fallback measurement can differ from host rendering. Omitting text when exact metrics are unavailable was rejected because unsupported facts must be explicit.

## R6 - Paint Coverage and Intentional Exceptions

**Decision**: Validation uses explicit rule checks for required paint coverage, ordinary-region overlap, text containment, clipping, and ordering. Intentional overlap or clipping is accepted only through caller-provided exception records with owner, reason, affected ids, and matching rule scope.

**Rationale**: Visual readiness needs to fail accidental missing backgrounds and overlap while allowing legitimate overlays, popups, and clipped scroll regions. Requiring explicit exceptions keeps validation reviewable.

**Alternatives considered**: A global allow-overlap flag was rejected because it would hide defects. Hardcoding known overlay ids in validators was rejected because samples and generated products need reusable rules.

## R7 - Legacy Layout Evidence Compatibility

**Decision**: Keep `LayoutEvidenceReport` and `GeneratedLayoutValidation` supported. The new inspection model may include adapters or summary links, but it must not remove or reinterpret existing layout evidence behavior in this feature.

**Rationale**: Existing Scene and Testing tests already cover `LayoutEvidenceReport` as a narrower proof type. This feature is additive and should not break earlier generated-product layout validation.

**Alternatives considered**: Replacing `LayoutEvidenceReport` immediately was rejected because it would add migration cost unrelated to the new inspection capability.

## R8 - Validation Commands and Evidence

**Decision**: Validate with focused Scene, Controls, and Testing tests plus package-surface and generated-product gates:

```sh
dotnet test tests/Scene.Tests/Scene.Tests.fsproj
dotnet test tests/Controls.Tests/Controls.Tests.fsproj
dotnet test tests/Testing.Tests/Testing.Tests.fsproj
./fake.sh build -t CapabilityCheck
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
./fake.sh build -t GeneratedProductCheck
```

**Rationale**: The feature can change three public package contracts and must prove package boundaries, surface baselines, focused semantics, and generated-product validation behavior.

**Alternatives considered**: Only running Testing tests was rejected because Scene and Controls will own public surface and extraction behavior. Only running a full solution test was rejected because focused failure evidence is easier to diagnose and previous full-solution runs have had progress issues.
