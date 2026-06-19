# Feature 170 Compatibility

Status: `accepted`

## Additive Surface

- ✅ `src/Scene/Scene.fsi`: added retained/damage evidence records, status unions, exception records, and `RetainedInspection` helpers.
- ✅ `src/Controls/Inspection.fsi`: added `RetainedControlTransition<'msg>`, `RetainedControlInspectionRequest<'msg>`, and `ControlInspection.inspectRetained`.
- ✅ `src/Testing/Testing.fsi`: added retained inspection validation, readiness aggregation, Markdown, JSON, and managed-section helpers.
- ✅ `readiness/surface-baselines/*.txt` and `tests/surface-baselines/*.txt`: synchronized accepted public exports for Scene, Controls, and Testing.

## Compatibility Checks

- ✅ Existing `VisualInspectionArtifact` construction remains source-compatible; no retained fields were added to the existing record shape.
- ✅ Existing `CompositorDamageReadiness` validation remains accepted beside the retained inspection APIs.
- ✅ AntShowcase visual readiness count parity remains accepted: 38 preferred targets and 12 minimum targets.
- ✅ `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-restore --filter Feature170`: passed, 3 tests.
- ✅ `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-restore --filter Surface`: passed, 32 tests.

## Git Ignore Proof

- ✅ `.gitignore` allowlists `specs/170-retained-damage-inspection/readiness/**`.
- ✅ `git check-ignore -v specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md` reports the negated allowlist pattern.
- ✅ A quiet probe with a temporary file returned exit `1`, confirming retained-inspection readiness files are trackable.

## Migration Impact

No migration is required for existing package consumers. Retained inspection is additive and opt-in. Existing visual inspection, compositor damage readiness, screenshot readiness, and package surface tests continue to use their prior contracts.
