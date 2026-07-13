# Feature 163 Readiness

Feature 163 evidence is split by reviewer question:

- `package-proof/AS-OF.md`: **read this first** — what the `package-proof/` record proves, at which
  commit, and how to regenerate it. The directory is a dated snapshot of one run, not a live claim;
  the live claim is the `packaged-consumer` CI lane, which re-proves on every push.
- `package-proof/package-versions.md`: discovered source package versions.
- `package-proof/package-pins.md`: selected sample pins and stale/current status.
- `package-proof/source-rules.nuget.config`: generated package source rules.
- `package-proof/source-proof.md`: local-feed and isolated-cache proof.
- `package-proof/source-proof.json`: machine-checkable proof result.
- `package-proof/restore.log`, `package-proof/build.log`, `package-proof/assets/`: verbatim `dotnet`
  output from the proved run — restore proves the packages resolve, the build proves they compose.
- `lanes/summary.md`: named validation lane status table.
- `lanes/summary.json`: machine-checkable lane summary.
- `diagnostics/`: lane timeout, no-progress, and host limitation diagnostics.
- `compatibility-ledger.md`: contract impact.
- `package-validation.md`: package-feed, source-proof, lane, and Package.Tests evidence.
- `regression-validation.md`: existing validation preservation checks.
- `validation-summary.md`: reviewer entry point.
- `fsi/package-feed-authoring.fsx`: package-feed FSI authoring transcript.
- `fsi/validation-lanes-authoring.fsx`: validation-lane FSI authoring transcript.
