# Contract: Lane Definition and Schedule

## Default Lane Catalog

| Lane id | Role | Purpose | Default command | Timeout | No-progress |
|---------|------|---------|-----------------|---------|-------------|
| `build` | required | Build verification for the solution. | `dotnet build FS.GG.Rendering.slnx -c Release --no-restore` | 10m | 2m |
| `library-tests` | required | Fast library/package validation not tied to one sample. | `dotnet test tests/Lib.Tests/Lib.Tests.fsproj -c Release --no-restore` | 10m | 2m |
| `package-proof` | required | Package pin and local-feed source proof for package-consuming samples. | `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode proof ...` | 10m | 2m |
| `controls` | required | Controls package and rendering-control behavior validation. | `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore ...` | 15m | 2m |
| `rendering-harness` | required | Rendering harness contracts, package-feed helpers, and lane runner tests. | `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore ...` | 10m | 2m |
| `antshowcase-sample` | required | Package-consuming AntShowcase sample validation. | `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore ...` | 10m | 2m |
| `aggregate-solution` | optional | Full solution validation recorded separately from focused lanes. | `dotnet test FS.GG.Rendering.slnx -c Release --no-restore ...` | 20m | 3m |

The implementation may tune command arguments during tasks, but the lane ids,
roles, evidence requirements, and readiness semantics are stable for this feature.

## Lane Definition Fields

Each lane definition contains:

- id
- display name
- description
- readiness role
- command text
- working directory
- timeout
- no-progress timeout
- progress interval
- log path
- result path
- diagnostics path
- output root
- concurrency group
- output scope
- aggregate flag

## Scheduling Rules

- Required lanes run when `--required` is selected or no lane is specified.
- Explicit `--lane` selection runs only selected lanes.
- Optional lanes run only when explicitly selected or included.
- The runner is sequential by default.
- If parallel execution is added or requested, lanes with the same
  `ConcurrencyGroup` or `OutputScope` are serialized, isolated with separate
  output roots, or rejected before execution starts.
- Unsafe schedules name the conflicting lanes and the action needed to proceed.

## Evidence Rules

For each lane, the runner creates:

```text
<run-root>/<lane-id>/
|-- log.txt
|-- result.json
|-- diagnostics.md
`-- TestResults/ or out/
```

Every lane result includes:

- lane id
- readiness role
- status
- command
- timeout budget
- elapsed time
- last activity timestamp
- log path
- result path
- diagnostics path
- artifacts
- reason for any non-passing outcome

Failure to create or write this evidence is an `infrastructure-error`.

## Direct Command Preservation

The lane runner invokes existing commands but does not replace them. Maintainers
can still run direct commands such as:

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore
```
