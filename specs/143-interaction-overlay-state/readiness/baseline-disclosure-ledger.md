# Baseline Disclosure Ledger

Date: 2026-06-17

Intentional baseline changes:

- `tests/surface-baselines/FS.GG.UI.Controls.txt` now includes the additive Feature 143 overlay coordinator public surface.

No intentional pixel/golden changes were made in this increment.

Diagnostic baseline impact:

- New `ControlDiagnosticCode` cases cover missing overlay anchors, stale overlay focus targets, blocked dismissals, disabled triggers, no-fit placements, duplicate dispatch prevention, invalid overlay messages, and lower-layer modal blocking.

Interaction-log baseline impact:

- `InteractionReplayLog` is a new evidence shape for overlay scripts; existing logs are not rewritten.
