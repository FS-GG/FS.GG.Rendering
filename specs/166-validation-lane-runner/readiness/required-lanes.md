# Required-Lanes Evidence

Command:

```sh
dotnet restore FS.GG.Rendering.slnx
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode refresh --out specs/166-validation-lane-runner/readiness/package-refresh
dotnet fsi scripts/run-validation-lanes.fsx --required --out specs/166-validation-lane-runner/readiness/lanes
```

Summary:

```text
specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/summary.md
overallReadiness: blocked
firstBlockingRequiredLane: controls
controls: no-progress-timeout, lane exceeded no-progress timeout 00:02:00
```

The run also shows `build`, `library-tests`, `package-proof`,
`rendering-harness`, and `antshowcase-sample` passing after restore and sample
pin refresh. The blocked result is intentional evidence that the runner fails
closed when a required lane stops producing output.
