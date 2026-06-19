# Feature 165 Compatibility Notes

## Public Signature Files Changed

- `src/Scene/Scene.fsi`
- `src/Controls/Inspection.fsi`
- `src/Testing/Testing.fsi`

`src/Controls/Inspection.fsi` is a new additive public module compiled immediately after `Control.fs`.

## Surface Baselines Changed

- `tests/surface-baselines/FS.GG.UI.Scene.txt`
- `tests/surface-baselines/FS.GG.UI.Controls.txt`
- `tests/surface-baselines/FS.GG.UI.Testing.txt`
- `readiness/surface-baselines/FS.GG.UI.Scene.txt`
- `readiness/surface-baselines/FS.GG.UI.Controls.txt`
- `readiness/surface-baselines/FS.GG.UI.Testing.txt`

## Legacy Layout Evidence

Existing `LayoutEvidenceReport` and `GeneratedLayoutValidation` behavior is unchanged. Feature 165 adds a broader structured inspection model and validators; it does not remove or reinterpret the legacy layout evidence contract.

## Screenshot Workflow

Screenshot visual-readiness behavior is unchanged. Structured inspection complements screenshots and can link to visual evidence, but deterministic inspection validation does not require screenshots to exist.

## Migration Notes

Consumers can adopt the feature incrementally:

- Use `ControlInspection.inspect` to derive artifacts from `Control.renderTree`.
- Use `VisualInspectionValidation.defaultRules` for the first deterministic validation pass.
- Record unsupported or unavailable facts explicitly; do not count unsupported required facts as accepted evidence.
- Keep existing screenshot readiness and `LayoutEvidenceReport` checks while adopting inspection rules scope-by-scope.
