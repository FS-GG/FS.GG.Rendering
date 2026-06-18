# Package-Feed Validation Ledger

Validated on 2026-06-18 from `/home/developer/projects/FS.GG.Rendering`.

## Build

- Command: `dotnet restore FS.GG.Rendering.slnx && dotnet build FS.GG.Rendering.slnx -c Release --no-restore`
- Status: passed
- Output: solution restored after the global package cache clear, then built successfully in Release with 0 warnings and 0 errors.

## Pack

- Command: `rm -rf /tmp/fs-gg-rendering-local-feed && mkdir -p /tmp/fs-gg-rendering-local-feed && dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o /tmp/fs-gg-rendering-local-feed`
- Status: passed
- Output: produced 13 `FS.GG.UI.*.0.1.23-preview.1.nupkg` packages. NuGet readme warnings were emitted for existing package metadata only.

## Local Feed

- Command: `cp /tmp/fs-gg-rendering-local-feed/FS.GG.UI.*.0.1.23-preview.1.nupkg ~/.local/share/nuget-local/ && dotnet nuget locals global-packages --clear`
- Status: passed
- Output: copied the current package set into the configured AntShowcase local feed and cleared `/home/developer/.nuget/packages/`.

## AntShowcase Build

- Command: `dotnet restore samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj && dotnet restore samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj && dotnet build samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore`
- Status: passed
- Output: AntShowcase restored from the local feed with `FS.GG.UI.*` package pins at `0.1.23-preview.1`, then built with 0 warnings and 0 errors.

## AntShowcase List

- Command: `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- list`
- Status: passed
- Output: 19 pages total: 13 catalog pages and 6 template pages.

## AntShowcase Coverage

- Command: `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- coverage`
- Status: passed
- Output: `96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated`.

## Focused Tests

- Command: `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Coverage|PageRender|ThemeInvariance|Template|Interaction|Feedback|Degrade|Visual"`
- Status: passed
- Output: 70 passed, 0 failed, 0 skipped.

## Full AntShowcase Tests

- Command: `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore`
- Status: passed
- Output: 78 passed, 0 failed, 0 skipped.

## Visual Evidence

- Preferred command: `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/162-enhance-showcase-visuals/readiness/visual-evidence`
- Preferred status: accepted, 38/38 screenshots.
- Minimum command: `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception --out specs/162-enhance-showcase-visuals/readiness/minimum-size`
- Minimum status: accepted, 12/12 screenshots.
