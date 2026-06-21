# Contract — Harness path map (`tests/Rendering.Harness` → `tools/Rendering.Harness`)

The relocation is correct **iff** every genuine reference below resolves to the new path and the
build/lanes stay green. Verified against the working tree by ripgrep. **Critical rule:** the *test*
project `tests/Rendering.Harness.Tests/` does **not** move — only the *CLI* `Rendering.Harness`
moves. Literals naming the `.Tests` project keep their `tests/` path.

## 1. Solution (`FS.GG.Rendering.slnx`)

| Old | New |
|-----|-----|
| `<Project Path="tests/Rendering.Harness/Rendering.Harness.fsproj" />` | `<Project Path="tools/Rendering.Harness/Rendering.Harness.fsproj" />` |
| `<Project Path="tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj" />` | **unchanged** (test project stays) |

## 2. Dependent test project ProjectReference

`tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`:
`..\Rendering.Harness\Rendering.Harness.fsproj` → `..\..\tools\Rendering.Harness\Rendering.Harness.fsproj`
(sibling-under-`tests/` becomes up-one-then-into-`tools/`).

## 3. Linked `TestAssertions.fs` includes (4 projects)

Each `<Compile Include="..\Rendering.Harness\TestAssertions.fs">` →
`..\..\tools\Rendering.Harness\TestAssertions.fs` (same depth change as #2). Files:
`tests/Layout.Tests`, `tests/Scene.Tests`, `tests/SkiaViewer.Tests`, `tests/Controls.Tests`
(each at line ~11).

## 4. Helper scripts (`scripts/`)

All three pass the harness `.fsproj` as a process arg:
`ArgumentList.Add("tests/Rendering.Harness/Rendering.Harness.fsproj")` →
`ArgumentList.Add("tools/Rendering.Harness/Rendering.Harness.fsproj")`.
Files (line ~12): `check-agent-skill-parity.fsx`, `run-validation-lanes.fsx`,
`refresh-local-feed-and-samples.fsx`.

## 5. Harness-internal command literals — CLASSIFY EACH

| File:line | Literal names… | Action |
|-----------|----------------|--------|
| `Compositor.fs:2617` | `tests/Rendering.Harness.Tests/…Tests.fsproj` (test) | **keep `tests/`** |
| `Compositor.fs:2876` | `tests/Rendering.Harness.Tests/…Tests.fsproj` (test) | **keep `tests/`** |
| `ValidationLanes.fs:431` | `tests/Rendering.Harness.Tests/…Tests.fsproj` (test) | **keep `tests/`** |
| `ValidationLanes.fs:448` | `tests/Rendering.Harness.Tests/bin/Release` (test) | **keep `tests/`** |
| `ValidationLanes.fs:523` | `dotnet test tests/Rendering.Harness.Tests/…Tests.fsproj …` (test) | **keep `tests/`** |
| `Live.fs:170` | `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj …` (CLI) | **→ `tools/`** |

> All category-5 hits except `Live.fs:170` reference the **test** project and must NOT be rewritten.
> Only the CLI run-command in `Live.fs` moves. Re-grep after editing to confirm no CLI literal
> remains under `tests/` and no test literal was flipped to `tools/`.

## 6. Feature 170 lane-test assertion

`tests/Rendering.Harness.Tests/Feature170RetainedInspectionLaneTests.fs:27` asserts the lane command
contains `tests/Rendering.Harness.Tests/…Tests.fsproj` (the **test** project). Since the lane command
(`ValidationLanes.fs:523`) keeps `tests/`, this assertion is **unchanged** — verify it still passes
rather than editing it.

## 7. FSX evidence scripts (`specs/**`) — `#r`/`open`, recompute depth per file

| File | Reference |
|------|-----------|
| `specs/168-skill-parity-evidence/readiness/fsi/skill-parity-authoring.fsx:6` | `#r "…/tests/Rendering.Harness/bin/Release/net10.0/Rendering.Harness.dll"` → `…/tools/…` |
| `specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.fsx:1` | `#r "…/tests/Rendering.Harness/bin/Debug/…"` → `…/tools/…` |
| `specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.fsx:1` | `#r "…/tests/Rendering.Harness/bin/Debug/…"` → `…/tools/…` |
| `specs/163-package-feed-validation-lanes/readiness/fsi/package-feed-authoring.fsx:1` | `open Rendering.Harness` — namespace unchanged; verify `#r` (if any) path |
| `specs/163-package-feed-validation-lanes/readiness/fsi/validation-lanes-authoring.fsx:1` | `open Rendering.Harness` — namespace unchanged; verify `#r` (if any) path |

These are frozen historical evidence: update only the **path** so they still resolve; do not alter
recorded evidence semantics. If any cannot be safely updated, call it out (spec Edge Cases) rather
than leaving it silently broken. `open Rendering.Harness` is a *namespace* and stays the same.

## 8. Skill doc

`src/Diagnostics/skill/SKILL.md` — 2 mentions (`tests/Rendering.Harness/SkillParity.fs` and a
`dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj …` example) → `tools/`.

## Acceptance

- `dotnet build FS.GG.Rendering.slnx` succeeds; `dotnet test` matches baseline.
- `rg "tests/Rendering\.Harness/"` (CLI, trailing slash) returns **zero** genuine references
  (SC-002). `tests/Rendering.Harness.Tests/` references remain.
- The three script lanes + Feature 170 lane test pass unchanged (FR-004).
