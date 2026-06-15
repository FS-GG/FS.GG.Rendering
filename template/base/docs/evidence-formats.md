# Evidence formats — required shapes

<!-- GENERATED from FS.GG.UI.Build.Evidence.EvidenceFormatSchema (feature 062, FR-005).
     Single-sourced from the constants the validators enforce, so this reference, the
     failing-class diagnostics, and the scans/audit/task-parser cannot drift. Do not edit
     by hand; regenerate with ./fake.sh build -t RefreshSurfaceBaselines. Currency-checked
     by TargetMetadataDrift. -->

This reference lists, per evidence-format class, the complete required shape of each
enforced readiness file — so an author can recover the contract **before** triggering a
failure, without decompiling `FS.GG.UI.Build.dll` or copying a sibling project (FR-005).

## readiness-contract

### `governance-risk-levels.md`

- required tokens: small, medium, broad, required evidence, broad validation
- blocking: true

### `aggregate-hang-diagnostics.md`

- required tokens: verdict, stage, elapsed duration, last observed command, focused rerun, non-authoritative aggregate
- blocking: true

### `runtime-limitations.md`

- required tokens: .NET 10 desktop, OpenGL, SkiaSharp preview, unsupported macOS/mobile/browser, no software-renderer fallback
- blocking: true

## skill-loading-evidence

### `skill-loading-evidence.md`

- required tokens: TaskId, DeclaredSkillId, ResolvedSkillPath, LoadResult, LoadedAt, WorkStartedAt, EvidencePath, Exception, Provenance
- columns (in order): TaskId | DeclaredSkillId | ResolvedSkillPath | LoadResult | LoadedAt | WorkStartedAt | EvidencePath | Exception | Provenance
- ordering: loaded_at < work_started_at
- ordering: provenance ∈ { captured, asserted } (captured = observed during the run, recorded at the load action before code changes; asserted = hand-authored)
- resolved-path: .agents/skills/<id>/SKILL.md
- blocking: true

## window-visibility

### `interactive-visible-window.md`

- required tokens: status, mode, window-visible, accessible-window, first-frame-presented, self-closed-for-evidence
- token form: each required token MUST appear as `token=value` (this file is parsed as key/value, not prose)
- blocking: true

### `close-reason-separation.md`

- required tokens: close-reason, user-close-observed, evidence-close-observed
- ordering: evidence close and user close stay separated (evidence-close-observed must not be reported as user-close-observed)
- blocking: true

### `window-state-diagnostics.md`

- required tokens: diagnostic-class=environment-session, diagnostic-class=window-visibility, diagnostic-class=app-lifecycle, diagnostic-class=product-defect, native-handle, visible, focusable, renderable-surface, input-devices
- token form: each required token MUST appear as `token=value` (this file is parsed as key/value, not prose)
- ordering: diagnostic-class ∈ { environment-session, window-visibility, app-lifecycle, product-defect }
- blocking: true

### `window-options.md`

- required tokens: option=resize, option=maximize, option=startup-state, option=startup-position, option=backend
- ordering: each option row carries status/observed; an unsupported option diagnoses under diagnostic-class=window-options (never silently ignored)
- blocking: true

### `real-image-evidence.md`

- required tokens: evidence-kind, status, artifact-decodable, proves-scene-rendering, proves-desktop-visibility
- ordering: decodable image/screenshot evidence; pixel-readback alone cannot prove desktop visibility
- blocking: true

### `generated-validation.md`

- required tokens: exact-package-match, generated-tests-ran, authoritative, failure-class
- token form: each required token MUST appear as `token=value` (this file is parsed as key/value, not prose)
- ordering: exact-package-match must be true with the generated tests actually run and authoritative
- blocking: true

### `evidence-audit.md`

- required tokens: verdict
- ordering: feature-local merge-gate audit record (file presence required)
- blocking: true

## seh-acceptance

### `tasks.md (Synthetic-Evidence Inventory)`

- required tokens: accepted-seh, synthetic-error-handling-approved
- ordering: acceptance status = accepted-seh; approval label = synthetic-error-handling-approved; no backticks
- blocking: true

