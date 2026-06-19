# Compatibility Ledger

Status: `accepted`

Feature 163 repository validation harness/script contracts changed, and AntShowcase package restore
evidence changed. No public `FS.GG.UI.*` framework API changed.

Changed harness/script surfaces:

- `Rendering.Harness.PackageFeed`: package discovery, pin classification, refresh, source-proof
  evidence, and MVU effect contracts.
- `Rendering.Harness.ValidationLanes`: lane definitions, lane result statuses, fail-closed summary
  computation, and process-runner effect contracts.
- `scripts/refresh-local-feed-and-samples.fsx`: maintainer package-feed entry point.
- `scripts/run-validation-lanes.fsx`: maintainer validation-lane entry point.
- `samples/AntShowcase/nuget.config`: package source mapping for `FS.GG.UI.*` local-feed proof.

Surface-drift note: the `.fsi` additions are harness-only validation surfaces. No package-visible
UI framework signature file changed in Feature 163. Running
`dotnet fsi scripts/refresh-surface-baselines.fsx` refreshed a pre-existing
`FS.GG.UI.Testing` baseline drift for Feature 160/161 helper surfaces; no Feature 163 package API
surface was added.
