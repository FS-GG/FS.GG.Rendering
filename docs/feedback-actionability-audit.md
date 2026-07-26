# Feedback actionability audits

`fs-gg-feedback-report` now separates report structure from claim actionability. Finalization produces
a draft, a cold-read critic pass, a workspace evidence-verification pass, and only then the durable
report plus its JSON audit.

The audit binds the exact LF-normalized report text by SHA-256 and covers every stable `§4.n` finding.
It records whether the critic had fresh subagent context, what evidence was checked, evidence digests
for workspace files, confidence limits, and one of five dispositions: `actionable`, `incomplete`,
`unsupported`, `duplicate`, or `positive-pattern`.

This closes the gap where a structurally polished report could pass while its expected/observed delta
was circular, a source locator was dead, a command did not reproduce, or a root cause and owner were
only asserted. An incomplete or unsupported finding can still be preserved honestly, but the
validator prevents that report from being presented as an actionable handoff.

Validate a scaffolded report from the product root:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- \
  validate feedback/<report>.md --audit feedback/audits/<report>.audit.json
```

The audit format and critic rubric live in the shipped skill so every materialized skill root uses
the same contract.
