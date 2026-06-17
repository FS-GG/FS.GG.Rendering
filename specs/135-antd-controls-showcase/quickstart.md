# Quickstart: Ant Design Controls Showcase (G3)

Validation/run guide for the sample. It consumes the packed `FS.GG.UI.*` packages from
`~/.local/share/nuget-local/` — **no project references into `src/`** (FR-015 / SC-006). Building and
running it against the feed *is* the proof the Ant-theme consumer path works end to end.

## V0 — Refresh the local feed (REQUIRED precondition, Research R1)

> The local feed predates feature 132: it has **no `FS.GG.UI.Themes.AntDesign`** package and a
> `FS.GG.UI.Controls` package with only the pre-132 controls. The showcase cannot restore or reach 96-control
> coverage until the feed is refreshed.

```sh
# 1. Repack the product (includes Themes.AntDesign, which is in the slnx) → local feed
dotnet pack FS.GG.Rendering.slnx -c Release        # output → ~/.local/share/nuget-local/

# 2. Same version (0.1.0-preview.1) is reused, so invalidate the consumer cache for the refreshed ids
dotnet nuget locals global-packages --clear
#   (or, narrowly:)
# rm -rf ~/.nuget/packages/fs.gg.ui.themes.antdesign ~/.nuget/packages/fs.gg.ui.controls

# 3. Verify the new package landed and Controls now carries the net-new Ant controls
ls ~/.local/share/nuget-local/FS.GG.UI.Themes.AntDesign.0.1.0-preview.1.nupkg
unzip -p ~/.local/share/nuget-local/FS.GG.UI.Controls.0.1.0-preview.1.nupkg lib/net10.0/FS.GG.UI.Controls.dll \
  | strings | grep -i segmented   # expect a hit
```

**Expected**: the `Themes.AntDesign` nupkg exists; the `Controls` dll references the net-new controls.

## Prerequisites

- .NET `net10.0` SDK.
- V0 completed (feed carries `FS.GG.UI.Themes.AntDesign` + the 96-control `FS.GG.UI.Controls`).
- Optional for `interactive`: an X11/GL display. Headless `evidence`/`coverage` need no display.

## Build

```sh
cd samples/AntShowcase
dotnet build AntShowcase.App/AntShowcase.App.fsproj -c Release   # restores from the local feed only
```

## V1 — Coverage bijection (FR-003 / SC-001)

```sh
dotnet run --project AntShowcase.App -c Release -- coverage
```
**Expected**: `96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated`;
exit 0. Removing/adding a catalog control without updating a page makes this exit non-zero.

## V2 — List pages

```sh
dotnet run --project AntShowcase.App -c Release -- list
```
**Expected**: 13 `Catalog` pages + 6 `Template` pages with ids/titles.

## V3 — Headless deterministic evidence (FR-010 / FR-011 / SC-004)

```sh
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
ls artifacts/ant-showcase/1/                 # one dir per page
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1   # run again
# diff the two runs' run.json/state.txt → byte-identical
```
**Expected**: per-page `run.json`/`state.txt`/`summary.md` (+ `frame.png` where GL available); same-seed
runs are byte-identical; every `run.json` has a non-empty `NotAuthoritativeFor`.

## V4 — Degrade-and-disclose on a no-GL host (FR-013 / SC-005)

```sh
# On a host with no display/GL (or unset DISPLAY):
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
echo "exit=$?"
```
**Expected**: exit 0, no hang; records show `ProvesScreenshot=false` + a disclosed `UnsupportedHostReason`.

## V5 — Ant light/dark parity (FR-008 / SC-003)

Run the test suite's `ThemeInvarianceTests`, or interactively toggle the app bar.
**Expected**: identical control tree + accessibility metadata under antLight and antDark; only resolved
visuals differ; no control branches on theme identity.

## V6 — Enterprise templates + form validation (FR-005 / FR-006 / SC-002 / SC-009)

Run `TemplateTests`.
**Expected**: each of the 6 template pages is composed only of catalog controls; the form page rejects
invalid input (validation messages, no success `result`) and shows a success `result` on valid submit.

## V7 — Interactive mode (GL-gated, advisory)

```sh
dotnet run --project AntShowcase.App -c Release -- interactive display-typography
```
**Expected**: an Ant-styled window — app bar (light/dark toggle), nav rail (19 pages), scrolling content,
status strip. On a no-GL host it discloses the reason and exits 0.

## V8 — Full test suite (outside the default test tier)

```sh
dotnet test AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release
```
**Expected**: coverage, page-render, template, theme-invariance, determinism, degrade, and interaction
suites green. This project is **not** in `FS.GG.Rendering.slnx`, so the default gate is unaffected.
