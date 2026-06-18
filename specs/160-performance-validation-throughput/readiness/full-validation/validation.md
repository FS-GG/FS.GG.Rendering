# Feature 160 Full Validation

Status: `passed`
Command: `dotnet test FS.GG.Rendering.slnx --no-restore`
Completed at: `2026-06-18T18:29:48Z`
Implementation commit: `current-working-tree`
Package surface baseline: `readiness/fsi`
Readiness artifact set: `throughput/summary.md`, `full-validation/validation.md`, `compatibility-ledger.md`, `package-validation.md`, `regression-validation.md`, `validation-summary.md`
Release-ready blocker: `none`

## Outcome

- `dotnet restore FS.GG.Rendering.slnx --force` completed first to repair a missing local `FsCheck 3.3.3` package cache entry.
- `dotnet build FS.GG.Rendering.slnx --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test FS.GG.Rendering.slnx --no-restore` passed across the solution on final retry.
- An earlier solution-level retry aborted on a host/windowing crash (`libdecor-gtk.so` / `glfwSetWindowPos`); `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-restore` then passed in isolation before the final solution retry passed.
- Focused throughput collection remains separate from this broad release gate.
