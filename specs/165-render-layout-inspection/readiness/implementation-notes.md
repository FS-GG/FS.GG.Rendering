# Feature 165 Implementation Notes

## Package Boundaries

- `FS.GG.UI.Scene` owns only dependency-light inspection records, status vocabularies, and pure artifact helpers.
- `FS.GG.UI.Controls` owns extraction from `Control.renderTree`; it depends on existing Controls, Layout, DesignSystem, and Scene inputs, and does not reference Testing.
- `FS.GG.UI.Testing` owns validation, readiness aggregation, Markdown/JSON rendering, and managed-section updates over Scene-owned artifact shapes. It does not reference Controls or Layout.

## Unsupported Fact Policy

Unavailable facts are recorded as `VisualInspectionUnsupportedFact` records with a fact name, owner id when known, requirement flag, reason, diagnostic, and environment-limited flag. Validators fail required unsupported facts as `unsupported` unless an explicit environment limitation is supplied, in which case readiness becomes `environment-limited`.

## Compatibility Assumptions

The Controls adapter calls the existing `Control.renderTree` path and inspects its returned `Bounds`, `Layout`, diagnostics, and authored control tree. It does not modify `Control.renderTree`, event bindings, retained rendering, or screenshot evidence behavior. `LayoutEvidenceReport` and `GeneratedLayoutValidation` remain supported as separate legacy evidence.
