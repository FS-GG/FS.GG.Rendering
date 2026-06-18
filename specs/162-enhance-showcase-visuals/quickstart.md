# Quickstart: AntShowcase Visual Overhaul

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- Local NuGet feed at `~/.local/share/nuget-local/`.
- A host capable of screenshot capture for accepted visual readiness. Headless or unsupported
  hosts may run the command, but they must produce environment-limited evidence rather than
  accepted visual readiness.

## 1. Build and Refresh the Local Package Feed

```bash
dotnet build FS.GG.Rendering.slnx -c Release --no-restore
dotnet pack FS.GG.Rendering.slnx -c Release --no-build
find src -path '*/bin/Release/FS.GG.UI.*.0.1.0-preview.1.nupkg' -exec cp {} ~/.local/share/nuget-local/ \;
dotnet nuget locals global-packages --clear
```

Expected:

- Solution build succeeds.
- Packed `FS.GG.UI.*` packages are available in `~/.local/share/nuget-local/`.
- AntShowcase continues to consume packages only, not `src/` project references.

## 2. Build and Smoke the AntShowcase

```bash
dotnet build samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- list
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- coverage
```

Expected:

- The app builds against the local package feed.
- `list` reports 13 catalog pages and 6 template pages.
- `coverage` reports 96/96 controls mapped, 0 unreferenced, and 0 duplicated.

## 3. Run Focused Tests

```bash
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter "Coverage|PageRender|ThemeInvariance|Template|Interaction|Feedback|Degrade|Visual"
```

Expected:

- Existing AntShowcase regression tests pass.
- New visual tests prove shell region separation, navigation-label containment, content
  containment, theme-surface completeness, visual evidence artifact generation, degraded capture
  disclosure, and readiness blocking when reviewer classification is missing or critical.

## 4. Capture Preferred Visual Readiness Evidence

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- \
  visual-readiness \
  --seed 1 \
  --size 1600x1000 \
  --themes light,dark \
  --out specs/162-enhance-showcase-visuals/readiness/visual-evidence
```

Expected on a capable screenshot host:

- 38 screenshots are written: 19 pages x 2 themes.
- `summary.md` and `summary.json` record page count, theme count, accepted size, command, run id,
  capture availability, completeness status, and canonical `antLight,antDark` theme ids resolved
  from the `light,dark` CLI aliases.
- `contact-sheet-light.png` and `contact-sheet-dark.png` are written.
- `completeness/` records automated checks for missing, degraded, stale, unreadable, or wrong-size
  screenshots.
- `reviewer-defects.md` is written as the reviewer classification template.

Expected on an unsupported host:

- The command writes an environment-limited summary naming the unavailable capture capability.
- No stale screenshots are treated as current evidence.
- Visual readiness is not marked accepted.

## 5. Capture Minimum-Size Representative Evidence

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- \
  visual-readiness \
  --seed 1 \
  --size 1280x800 \
  --themes light,dark \
  --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception \
  --out specs/162-enhance-showcase-visuals/readiness/minimum-size
```

Expected:

- Representative dense, large-control, feedback/status, form, and exception pages remain
  readable.
- Pages use scrolling, pagination, or responsive layout instead of drawing outside their regions.
- The summary states that this is minimum-size representative evidence, not the full preferred
  readiness matrix, and records canonical theme ids rather than CLI aliases.

## 6. Record Reviewer Defect Classification

Edit the generated reviewer file:

```text
specs/162-enhance-showcase-visuals/readiness/visual-evidence/reviewer-defects.md
```

Expected:

- Every required preferred-size screenshot is covered by either an explicit no-defect row or a
  defect row with page id, canonical theme id, severity, readiness impact, reviewer, timestamp,
  and notes.
- Defect classes use the contract vocabulary: shell overlap, navigation label spill, top-bar
  displacement, content-footer collision, unplanned background exposure, section overpaint, clipped
  primary label, unreadable primary content, transient-surface overprint, template hierarchy
  unclear, or lower-level limitation.
- Any critical defect keeps visual readiness blocked.

## 7. Assemble Readiness Summary

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- \
  visual-readiness \
  --summarize specs/162-enhance-showcase-visuals/readiness/visual-evidence \
  --minimum-size specs/162-enhance-showcase-visuals/readiness/minimum-size \
  --out specs/162-enhance-showcase-visuals/readiness
```

Expected:

- `readiness/validation-summary.md` links preferred screenshots, contact sheets, completeness
  checks, reviewer classification, minimum-size evidence, coverage output, package-feed
  validation, compatibility notes, regression validation, and full-validation status.
- Final status is `accepted` only when screenshots are complete, reviewer classification records no
  critical defects, coverage is clean, package-only validation passes, and no lower-level
  limitation blocks the claim.

## 8. Run Broad Validation

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected:

- Full solution validation passes.
- If public package surface changed, surface-baseline and compatibility evidence are recorded in
  `readiness/package-feed.md` and `readiness/compatibility-ledger.md`.
