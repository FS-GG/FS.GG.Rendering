# Feature 153 Live Proof Evidence

Status: `environment-limited`

Feature 153 adds the proof interpreter and evidence package for live sentinel/damage-scoped compositor attempts. The current local validation environment does not provide a capable OpenGL presentation/readback host, so no accepted partial-redraw artifacts are recorded.

## Evidence Locations

- Capable-host attempts: `attempts/README.md`
- Unsupported-host output: `unsupported/README.md`
- Unsupported-host validation: `unsupported/validation.md`
- Proof-set decision: `../proof-set.md`
- Final readiness summary: `../validation-summary.md`

## Acceptance Rule

An accepted proof set requires exactly three selected fresh matching capable-host attempts. Each selected attempt must have decodable, non-blank, non-synthetic sentinel and damage artifacts, damaged-pixel update, undamaged-pixel preservation, matching host profile, and matching proof method.

Unsupported, stale, synthetic-only, blank, undecodable, failed, host-mismatched, or proof-method-mismatched evidence is non-accepting.
