# Feature 173 Compatibility Notes

- Scope: additive responsiveness readiness evidence for `samples/SecondAntShowcase`.
- Public framework surface: `ViewerResponsivenessReadiness.Rejected` and `ViewerResponsivenessBudget.InputToVisibleMax`.
- Sample surface: `ResponsivenessWorkflow` MVU boundary and expanded responsiveness evidence summary fields.
- Compatibility: existing headless substitute behavior remains available but is classified as `environment-limited`, not accepted.
- Package validation: `scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase` passed with 14 packages and 18 pins, then the sample restore and final `--no-restore` test passed.
- Readiness caveat: final accepted live responsiveness is blocked in this environment because no measured visible presentation boundary was available.
