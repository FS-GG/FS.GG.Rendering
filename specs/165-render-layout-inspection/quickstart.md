# Quickstart: Structured Render/Layout Inspection Metadata

## Prerequisites

- Active branch: `165-render-layout-inspection`
- Active feature pointer: `.specify/feature.json` points at `specs/165-render-layout-inspection`
- .NET SDK capable of `net10.0`
- Local package feed path remains `~/.local/share/nuget-local/`

## 1. Review Contracts

Read the design artifacts before implementation:

```sh
sed -n '1,220p' specs/165-render-layout-inspection/contracts/visual-inspection-api.md
sed -n '1,220p' specs/165-render-layout-inspection/contracts/visual-inspection-validation.md
sed -n '1,220p' specs/165-render-layout-inspection/data-model.md
```

Expected outcome:

- Package boundary is clear.
- Scene owns dependency-light inspection types.
- Controls owns extraction from rendered control trees.
- Testing owns validation and summaries.

## 2. Draft Public Surfaces First

Update intended public API in `.fsi` files before `.fs` bodies:

```text
src/Scene/Scene.fsi
src/Controls/Control.fsi or src/Controls/Inspection.fsi
src/Testing/Testing.fsi
```

Expected outcome:

- The API can be exercised from public package namespaces.
- No top-level public/private/internal visibility markers are added to `.fs` files.
- Package boundaries in [visual-inspection-api.md](./contracts/visual-inspection-api.md) are preserved.

## 3. Add Semantic Tests

Create focused tests before implementation:

```sh
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature165
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature165
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature165
```

Expected failing-first coverage:

- Scene inspection model status tokens and stable ordering.
- Controls adapter emits stable ids, bounds, text facts, clip facts, paint facts, and unsupported facts.
- Testing validators detect text overflow, accidental clipping, missing paint, unclassified overlap, unsupported required facts, invalid exceptions, and stable-id churn.
- Legacy `LayoutEvidenceReport` behavior remains unchanged.

## 4. Implement and Run Focused Tests

Run focused package tests after implementation:

```sh
dotnet test tests/Scene.Tests/Scene.Tests.fsproj
dotnet test tests/Controls.Tests/Controls.Tests.fsproj
dotnet test tests/Testing.Tests/Testing.Tests.fsproj
```

Expected outcome:

- Feature 165 tests pass.
- Existing Scene, Controls, and Testing tests continue passing.
- Any skipped or environment-limited evidence is explicitly named.

## 5. Run Package and Generated-Product Gates

Run required repository gates for public surface and package validation:

```sh
./fake.sh build -t CapabilityCheck
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
./fake.sh build -t GeneratedProductCheck
```

Expected outcome:

- Surface baselines include intentional Scene, Controls, and Testing changes.
- Packages pack to the local feed.
- Generated-product validation does not gain unintended Controls/Layout dependencies through Testing.

## 6. Produce Readiness Evidence

Record readiness under:

```text
specs/165-render-layout-inspection/readiness/
```

Minimum evidence:

- `inspection/summary.md`
- `inspection/summary.json`
- focused test command outputs
- package-surface evidence
- package/local-feed evidence
- compatibility note for `LayoutEvidenceReport`

Expected outcome:

- Readiness distinguishes accepted, blocked, unsupported, environment-limited, not-inspected, and not-run states.
- Screenshot visual-readiness evidence remains separate and unchanged unless a later implementation task intentionally links to it.
