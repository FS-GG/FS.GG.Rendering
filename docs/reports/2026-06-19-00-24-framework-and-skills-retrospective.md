# Framework and Skills Retrospective After AntShowcase Visual Readiness

**Report date:** 2026-06-19 00:24 Europe/Vienna
**Repository:** FS.GG.Rendering
**Feature 162 baseline after merge:** `main` at `4b086a5` (`chore: bump FS.GG.UI packages to 0.1.24-preview.1`)
**Feature 163 follow-up:** Package-feed validation lanes are implemented; the current post-merge package set is `0.1.25-preview.1`.
**Feature 165 follow-up:** Structured render/layout inspection metadata is implemented; the current package set before merge is `0.1.26-preview.1`.
**Feature 166 follow-up:** Validation lane runner hardening is implemented on `166-validation-lane-runner`; it adds stable required/optional lanes, request preflight, run-id evidence isolation, structured summaries, no-progress classification, and schedule-safety checks. Current required-lane evidence intentionally exposes `Controls.Tests` as `no-progress-timeout`.
**Primary work observed:** Feature 162, AntShowcase visual readiness implementation, plus earlier local/Codex/Claude skill parity work
**Scope:** Problems encountered in the framework, validation workflow, and skills while implementing and validating the AntShowcase visual overhaul. This report focuses on improvements possible in library code, sample infrastructure, generated readiness tooling, and coding-agent skills.

---

## 1. Executive summary

Feature 162 was implementable with the existing FS.GG.UI primitives: Controls, Scene, SkiaViewer screenshot capture, Testing helpers, pure MVU boundaries, and the Ant Design theme package were enough to produce a complete visual-readiness workflow. The AntShowcase now has accepted preferred-size and minimum-size screenshot evidence, contact sheets, coverage checks, and tests.

The work also exposed several friction points:

1. Package-version drift is easy. AntShowcase package references were stale relative to the packable projects and had to be updated manually.
2. Per-feature readiness evidence is ignored by default. Feature 162 had to be explicitly allowlisted in `.gitignore` before the evidence package could be committed.
3. Full-solution test execution is not robust enough as a single validation gate. `Controls.Tests` stopped producing output for several minutes and the full run had to be canceled.
4. Visual-readiness behavior is mostly sample-owned. Screenshot matrix capture, completeness checks, contact-sheet generation, reviewer-defect gating, and summary assembly belong in reusable testing/tooling APIs rather than a sample app edge.
5. Feature 165 adds structured visual/layout metadata for deterministic assertions. The first slice covers Scene inspection records, a Controls `Control.renderTree` adapter, Testing validation/readiness helpers, summaries, and explicit unsupported facts; retained damage metadata and broad sample adoption remain follow-up scope.
6. Generated summary commands can overwrite richer hand-written readiness notes. The visual-readiness summarizer rewrote `validation-summary.md` to a minimal link-only summary after a detailed summary had been added.
7. Skills helped with domain orientation, but they do not yet encode several recurring repository traps: local package-feed drift, readiness ignore rules, concurrent test output locks, post-merge package bump requirements, and how to preserve evidence honesty.
8. A post-readiness interactive diagnostic found severe input-lag risk in the synchronous post-input render path. Pointer routing itself was fast, but a state-changing click can force hundreds of milliseconds of retained render/lowering/layout/text work on the same event path, causing later mouse events to queue.

The highest-value improvement is a shared "visual readiness, local package feed, and responsiveness diagnostics" toolkit: library-side APIs for screenshot evidence, a CLI or script for pack/update/restore validation, live phase timing for input/update/render/present, and skill guidance that requires these checks before a feature is marked done.

---

## 2. What went well

### 2.1 Existing architecture supported the feature

The framework already had enough primitives to build a proper sample-facing readiness workflow:

- Pure AntShowcase Core modules could own deterministic state, visual config, page profiles, shell layout, and a pure visual-readiness workflow.
- The app edge could call `Viewer.captureScreenshotEvidence` to produce actual screenshots rather than synthetic placeholders.
- Existing Scene and Controls abstractions were sufficient to recompose pages and templates without lower-level package changes.
- The existing package-only consumer model was preserved: AntShowcase still consumes `FS.GG.UI.*` packages rather than `src/` project references, except where tests intentionally reference projects.

### 2.2 Tests scaled well at the sample level

AntShowcase-specific tests were stable after implementation:

- `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- list`
  - Result: 19 pages, 13 catalog pages, 6 template pages.
- `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- coverage`
  - Result: `96/96 controls mapped`, 0 unreferenced, 0 duplicated.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Coverage|PageRender|ThemeInvariance|Template|Interaction|Feedback|Degrade|Visual"`
  - Result: 70 passed.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore`
  - Result: 78 passed.

### 2.3 Evidence was real and useful

The final visual-readiness evidence was based on actual PNG capture:

- Preferred size: `1600x1000`, light/dark, 19 pages, 38/38 screenshots, accepted.
- Minimum size: `1280x800`, representative dense/template pages, light/dark, 12/12 screenshots, accepted.
- Contact sheets were generated for both themes and both evidence sets.
- Reviewer-defect files were required before readiness could be accepted.

This is the right direction: visual readiness should be backed by concrete image artifacts and explicit reviewer classification, not just render-tree tests.

---

## 3. Problems encountered

### 3.1 AntShowcase package pins drifted from current packages

**Observation**

The packable projects were at `0.1.23-preview.1` before the post-merge bump, but AntShowcase still referenced `0.1.0-preview.1`. After the merge workflow, all packable projects were bumped to `0.1.24-preview.1`, and AntShowcase package pins had to be manually updated again.

**Impact**

This weakens the meaning of AntShowcase as a package-only consumer validation target. If the sample restores old packages from the local feed, it can pass while not validating the current framework packages.

**Root cause**

There is no single source of truth for the "current local package version" consumed by samples. The sample intentionally disables central package management and pins versions inline, which is valid for consumer realism but creates drift risk.

**Improvement**

Add a repo tool that does all of the following in one command:

1. Detect current packable `FS.GG.UI.*` versions.
2. Pack them to the local feed.
3. Update sample package pins to the detected version.
4. Clear NuGet global packages.
5. Restore and build consumer samples from the local feed.
6. Fail if any sample still references an older package version.

Candidate command:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase
```

The same logic should be referenced by the relevant skills.

---

### 3.2 Readiness evidence was ignored by default

**Observation**

The repo `.gitignore` contains:

```gitignore
specs/*/readiness/
```

Several prior features are allowlisted explicitly. Feature 162 readiness evidence had to be allowlisted before `specs/162-enhance-showcase-visuals/readiness/**` could be staged and committed.

**Impact**

The implementation produced the expected readiness files, but a normal `git add specs/162-enhance-showcase-visuals` silently omitted the evidence package. Without a manual `git check-ignore` pass, the commit would have missed the files that demonstrate readiness.

**Root cause**

Per-feature readiness evidence is usually transient, but some features intentionally commit it. The policy is encoded as a broad ignore rule with manual exceptions. The implementation skill does not currently call out this trap.

**Improvement**

Add a small helper:

```sh
scripts/allowlist-readiness-evidence.sh specs/162-enhance-showcase-visuals
```

It should:

- Add the `.gitignore` exception.
- Verify `git check-ignore -v` no longer matches readiness files.
- Print the staged evidence file count.

Skills should require this check whenever tasks mention committed readiness artifacts, screenshots, contact sheets, or validation summaries.

---

### 3.3 Full-solution test validation was not reliable as one command

**Observation**

`dotnet test FS.GG.Rendering.slnx -c Release --no-restore --no-build` was attempted. Many projects completed successfully, but `Controls.Tests` stopped producing output for several minutes. The full run was canceled and the caveat was recorded in Feature 162 readiness docs.

Before cancellation, the reported projects had no failures. The AntShowcase-specific full test suite passed separately.

**Impact**

The full-solution test command did not produce a clean final signal. This makes it unsuitable as the only readiness gate for large changes unless timeouts, per-project isolation, or logging improvements are added.

**Root cause**

The test suite has at least one long or potentially stuck project. A single solution-wide `dotnet test` invocation hides which test is active and provides no structured timeout policy.

**Improvement**

Split validation into named lanes:

1. Fast package/library lane.
2. Controls lane with explicit timeout and blame logging.
3. Rendering/harness lane.
4. Sample lane.
5. Optional full solution lane.

Add command-level timeout wrappers, for example:

```sh
timeout 10m dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --no-build --logger "console;verbosity=normal"
```

The output should identify the last started test or test list. If Expecto cannot provide this cleanly, add test grouping or custom logging around known long-running tests.

---

### 3.4 Running two test commands against one output directory caused a file lock

**Observation**

After the package bump, the focused AntShowcase filter and the full AntShowcase suite were run concurrently. One failed with:

```text
The process cannot access the file '.../AntShowcase.Tests.runtimeconfig.json' because it is being used by another process.
```

Rerunning the focused command sequentially passed.

**Impact**

This was not a framework failure, but it is an easy validation mistake. Parallelizing `dotnet test` commands against the same project and configuration can race on generated runtime files.

**Root cause**

Both test invocations shared `samples/AntShowcase/AntShowcase.Tests/bin/Release/net10.0`.

**Improvement**

Add skill guidance:

- Do not run multiple `dotnet test` commands concurrently for the same project/configuration.
- If parallel test runs are necessary, use separate output directories or avoid rebuild with prebuilt outputs.

Possible command pattern:

```sh
dotnet test path/to/project.fsproj -c Release -p:BaseOutputPath=/tmp/fs-gg-test-output/lane-a/
```

---

### 3.5 Visual-readiness summary generation overwrote richer manual content

**Observation**

The visual-readiness `--summarize` command rewrote `validation-summary.md` to a minimal link list after a richer validation summary had been manually written.

**Impact**

The generated summary can erase important context:

- Package-feed validation status.
- Compatibility status.
- Regression status.
- Full-solution test caveat.
- Post-merge package bump status.

This creates a risk that evidence docs become less honest or less useful after rerunning the generator.

**Root cause**

The generator owns the whole file. It does not preserve custom sections and does not yet emit the full validation narrative.

**Improvement**

Use a managed-section format:

```md
<!-- VISUAL-READINESS:START -->
generated content
<!-- VISUAL-READINESS:END -->

manual validation notes stay outside
```

Or make the generator produce a separate file, such as:

```text
visual-readiness/generated-summary.md
```

Then the top-level `validation-summary.md` can link to generated files without being overwritten.

---

### 3.6 Visual-readiness infrastructure lives in the sample app

**Observation**

The feature added sample-specific code for:

- CLI parsing for visual-readiness.
- Screenshot matrix expansion.
- Stale screenshot cleanup.
- PNG completeness checks.
- Contact sheet generation.
- Reviewer-defect template generation and parsing.
- Summary JSON/markdown emission.

The app needed a direct `SkiaSharp` dependency for contact-sheet composition.

**Impact**

This code is useful beyond AntShowcase. Keeping it sample-owned increases duplication risk for future samples and generated products.

**Root cause**

There is no shared library-level visual evidence API yet. `SkiaViewer` can capture screenshots, but the surrounding readiness workflow is not packaged.

**Improvement**

Move reusable pieces into `FS.GG.UI.Testing` or a new validation helper namespace. Suggested API surface:

```fsharp
type ScreenshotTarget =
    { PageId: string
      ThemeId: string
      Size: int * int
      OutputPath: string }

type ScreenshotCompleteness =
    | Complete
    | Missing
    | WrongSize of expected: int * int * actual: int * int
    | Undecodable
    | Degraded of reason: string

type VisualEvidenceSummary =
    { Targets: ScreenshotTarget list
      Results: ScreenshotCompleteness list
      ContactSheets: string list
      ReviewerStatus: ReviewerStatus
      Readiness: ReadinessStatus }
```

Candidate modules:

- `FS.GG.UI.Testing.VisualEvidence`
- `FS.GG.UI.Testing.ContactSheet`
- `FS.GG.UI.Testing.ReviewerDefects`
- `FS.GG.UI.Testing.ReadinessSummary`

The sample should provide only app-specific page rendering and theme selection.

**Feature 164 update — implemented 2026-06-19**

The high-value generic pieces now live in `FS.GG.UI.Testing`:

- `VisualCaptureMatrix` owns deterministic page/theme/size target expansion, duplicate detection, and safe relative path validation.
- `VisualCompleteness` owns PNG completeness classification, content identity, dimensions, degraded records, and stale-artifact diagnostics.
- `VisualReviewerClassifications` owns reviewer Markdown template generation and parsing.
- `VisualReadiness` owns reviewer/capture readiness aggregation.
- `VisualReadinessMarkdown` owns generated Markdown/JSON and managed-section updates.

AntShowcase is migrated as the first adopter. It still owns page registry, theme selection,
real screenshot capture through `Viewer.captureScreenshotEvidence`, and contact-sheet PNG
composition. It now writes shared contact-sheet metadata and embeds the shared readiness
report in `summary.json`. The preferred run produced 38/38 screenshots and the minimum run
produced 12/12 screenshots; both remain blocked until reviewer classifications are filled,
which is the intended acceptance gate.

---

### 3.7 Visual/layout assertions are still too indirect

**Observation**

Feature 162 tests can assert:

- Page registry coverage.
- Visual profile coverage.
- Shell region disjointness.
- Non-empty render trees.
- Theme alias resolution.
- Accepted size declarations.
- Screenshot completeness.

They cannot directly assert all visual properties that matter:

- Text does not visibly overlap.
- Dense controls stay inside intended sections.
- Z-order is visually correct.
- Clipping boundaries are intentional.
- Text extents fit available regions.
- Large surfaces do not overpaint neighboring sections.

Those checks still require screenshot review.

**Impact**

Visual correctness remains partly manual. Screenshots are valuable, but failures are discovered by review rather than precise test assertions.

**Root cause**

The framework does not expose enough stable render/layout metadata to tests. Scene output is renderable, but tests do not have a first-class artifact describing bounds, text extents, clipping, z-order, and ownership.

**Improvement**

Add a structured visual inspection artifact emitted from the render/layout pipeline:

```fsharp
type VisualNodeInspection =
    { ControlId: string option
      Kind: string
      Bounds: Rect
      Clip: Rect option
      ZIndex: int
      TextRuns: TextRunInspection list
      PaintRole: string option
      SurfaceRole: string option
      Children: VisualNodeInspection list }

type TextRunInspection =
    { Text: string
      Bounds: Rect
      Baseline: float
      IsClipped: bool
      FitsContainer: bool }
```

**Feature 165 update — implemented 2026-06-19**

The first framework-level inspection slice now exists:

- `FS.GG.UI.Scene.VisualInspection*` records define dependency-light scopes, nodes, regions, text runs, paint coverage, clip facts, unsupported facts, findings, artifacts, summaries, stable status tokens, and deterministic finding ids.
- `FS.GG.UI.Controls.ControlInspection` derives inspection artifacts from the existing `Control.renderTree` path without changing rendered `Scene`, bounds, diagnostics, event bindings, bound ids, or node counts.
- `FS.GG.UI.Testing.VisualInspectionValidation` validates required regions, paint coverage, ordinary-region overlap, text containment, clip intent, overlay exceptions, unsupported required facts, stable identity, and visual order.
- `VisualInspectionReadiness` and `VisualInspectionMarkdown` aggregate reviewer-readable Markdown, machine-readable JSON, and safe managed-section updates.

Focused Feature 165 tests passed for Scene, Controls, and Testing. The representative evidence under
`specs/165-render-layout-inspection/readiness/inspection/` records accepted deterministic inspection
for a bounded sample plus an explicit non-required unsupported transform fact. Screenshot readiness
remains separate and unchanged.

This would allow tests to express assertions such as:

- All text bounds are inside their owning section.
- Shell regions are disjoint after final layout, not only after declared calculations.
- No content node overlaps the feedback/status region.
- No dark/light theme leaves unpainted root background.

This is a framework-level investment with high payoff for every generated product.

---

### 3.8 Runtime diagnostics are not clearly categorized for users

**Observation**

Running the interactive showcase emitted GTK module warnings and expected control diagnostics:

```text
Gtk-Message: Failed to load module "colorreload-gtk-module"
Gtk-Message: Failed to load module "window-decorations-gtk-module"
[ControlDiagnostic Info] Control 'line-chart' requires offscreen composition ...
```

The interactive session still ended successfully.

**Impact**

Expected backend-cost diagnostics can look like errors to a human running the sample. GTK module warnings are environment noise but appear in the same stream as product diagnostics.

**Root cause**

Diagnostics are textual and mixed in stdout/stderr without severity routing or summary grouping.

**Improvement**

Categorize runtime diagnostics:

- Environment warnings.
- Expected backend-cost diagnostics.
- Recoverable rendering limitations.
- Actual readiness blockers.

For sample apps, print a compact summary at the end or write structured diagnostics to an artifact file. Keep console output concise unless verbose mode is requested.

---

### 3.9 Interactive input lag was traced to synchronous render work after input

**Observation**

After the visual-readiness work, manual interactive use of AntShowcase showed massive mouse input lag: clicks could appear delayed by seconds. A focused local diagnosis separated deterministic app-side cost from live viewer/event-loop cost.

The deterministic checks showed:

- `Shell.view buttons`: approximately `0.11 ms` average over 100 runs.
- `Control.renderTree buttons`: approximately `397 ms` average over 10 runs.
- `Control.renderTree buttons clicked`: approximately `396 ms` average over 10 runs.
- `ControlsElmish.Perf.runScript` for one content-button click: approximately `762 ms` average over 10 runs.
- Single-page `Control.renderTree` costs across AntShowcase ranged from approximately `333 ms` to `1118 ms`.

The live viewer diagnostics showed:

- Pointer move routing was usually sub-millisecond.
- Discrete click routing was small, typically around `0.02 ms` to `4 ms`.
- State-changing clicks left large post-render footprints visible on following samples, for example `remeasure=38..77`, `repaint=57..96`, `dirtyArea=1600000`, and text-measure activity such as `text=0/60` or `text=78/0`.
- Backend present timing sometimes reported paint work around `60 ms` to `118 ms`.

The observed keyboard contract was minimal but present: AntShowcase maps key-down `Enter` and `Space` to `PageMsg ButtonClicked`; key-up and other keys are ignored unless focused-control routing consumes them first. The same post-input render stall would affect keyboard activations that change the model.

**Impact**

The lag is user-visible and can stack. Even if pointer routing is fast, every state-changing input can synchronously perform expensive retained render/lowering/layout/text/present work. While that work is running, later mouse samples and clicks queue behind it, which feels like delayed clicks.

This also means a screenshot-ready sample can still be interactively poor. Visual readiness and responsiveness readiness are related but separate gates.

**Root cause**

The live viewer dispatch path applies input messages and immediately calls `host.View` / retained render synchronously on the event path:

1. Native input arrives.
2. `host.MapPointer` or `host.MapKey` produces product messages.
3. `host.Update` changes the model.
4. The viewer immediately recomputes the current scene through the retained render path.

The product view construction itself is cheap; the expensive work is below it, primarily `Control.renderTree` / retained lowering / layout / text measurement / paint preparation. Damage tracking also appears too broad for localized updates: button clicks still reported full-frame dirty area (`1600000` at `1600x1000`).

**Architectural decision**

The input/render boundary should be treated as a required scheduler rewrite, not as a local optimization. The problematic seam is the live `dispatchHostMsg` behavior: it folds product messages and recomputes `currentScene` inside the input-triggered call stack. Caching and damage narrowing remain useful, but they do not fix the core failure mode while any expensive render-phase work can still run before the event callback returns.

Target behavior:

```text
native input callback
  -> normalize and timestamp input
  -> enqueue ViewerInputEnvelope
  -> signal frame loop and return

frame/update loop
  -> drain queued inputs by priority
  -> preserve discrete order
  -> coalesce continuous pointer moves
  -> map input to product messages
  -> fold product updates
  -> mark view dirty
  -> recompute retained scene at most once for the frame

render/present loop
  -> paint/present latest dirty scene
  -> emit correlated input-to-present timing
```

The retained controls architecture should remain, but it must sit behind an invalidation/scheduler boundary instead of being invoked directly by every input-produced message.

**Improvement**

Responsiveness needs first-class framework support:

1. Add phase timing around `MapPointer`/`MapKey`, product `Update`, `host.View`, retained `step`, text measurement, layout, paint walk, and present.
2. Report a single input-to-present latency record for each discrete input, not only input-routing metrics.
3. Rewrite the live scheduler so input callbacks enqueue timestamped inputs and render on the frame loop instead of performing expensive render work directly inside callbacks.
4. Keep discrete inputs ordered, but coalesce move/hover work before it reaches heavyweight render paths.
5. Fold all product messages produced by one input before scene recomputation so one click/key cannot trigger several immediate retained renders.
6. Narrow damage tracking so localized state changes do not dirty the whole frame.
7. Cache text measurement/shaping and unchanged lowered subtrees more aggressively.
8. Add responsiveness readiness tests or scripts that fail when click/key-to-present latency exceeds a budget.

Candidate budgets:

- Pointer/key routing: `< 4 ms` p95.
- Product update + view: `< 8 ms` p95 for typical generated screens.
- Input-to-present: `< 50 ms` p95 for interaction demos, with explicit disclosure when software rendering or unsupported graphics backends prevent this.

---

## 4. Library improvement opportunities

### 4.1 Add a package-feed validation library/script

**Priority:** High

The package-only consumer path is important enough to deserve a first-class script.

Required behavior:

- Detect packable projects from `PackageId` or `IsPackable=true`.
- Read and bump package versions when requested.
- Pack to `~/.local/share/nuget-local`.
- Clear NuGet global cache.
- Update sample package pins.
- Restore/build selected sample projects.
- Verify no stale package version remains in source-controlled sample files.

This can live as:

- `scripts/refresh-local-package-feed.fsx`
- `scripts/bump-packages.fsx`
- or a small `build` target if the repo already has a build orchestration pattern.

### 4.2 Promote visual-readiness helpers to `FS.GG.UI.Testing`

**Priority:** High

Status after Feature 164: mostly implemented.

Moved common readiness workflow pieces out of AntShowcase:

- Screenshot target matrix.
- Completeness checks.
- Reviewer-defect parsing.
- Summary JSON/markdown serializers.
- Managed-section summary writing.

Still sample-owned by design:

- Theme alias normalization and page registry decisions.
- Real screenshot capture.
- Contact-sheet PNG composition.

This makes future generated products and samples cheaper to validate without forcing
sample-specific rendering or image composition into the Testing package.

### 4.3 Add render/layout inspection metadata

**Priority:** High

Status after Feature 165: implemented for the dependency-light Scene model, Controls render-tree
adapter, Testing validators, summaries, and representative evidence. Remaining follow-up scope is
retained-render inspection, damage/dirty-rect metadata, and broader sample/generated-product adoption.

Expose a stable test artifact with:

- Resolved control bounds.
- Text bounds and fit status.
- Clip rectangles.
- Surface roles.
- Z-order.
- Control IDs and ownership.

This would convert many visual-readiness claims from manual screenshot review into deterministic tests.

### 4.4 Add a validation lane runner with timeouts

**Priority:** Medium-high

**Implementation status on 2026-06-19:** Implemented by
`166-validation-lane-runner`. The runner now supports `--list`, `--required`,
repeatable `--lane`, `--include-optional`, `--out`, `--run-id`,
`--replace-run`, and `--json`; writes run-id-scoped Markdown/JSON summaries and
per-lane logs/results/diagnostics; rejects unknown or duplicate lane requests
before work starts; and classifies `no-progress-timeout` separately from total
timeout. Readiness evidence is under
`specs/166-validation-lane-runner/readiness/`.

Create a validation runner that executes named lanes with:

- Per-project timeout.
- Last active test reporting.
- Separate logs per project.
- No shared output races.
- Exit summary.

This is more useful than a raw `dotnet test FS.GG.Rendering.slnx` when diagnosing a large repo.

### 4.5 Make generated summaries preserve manual context

**Priority:** Medium

All generated readiness files should either:

- Write only inside managed markers, or
- Write to clearly generated files and leave top-level summaries hand-authored.

This prevents a rerun from erasing caveats, limitations, or reviewer notes.

### 4.6 Standardize diagnostics output

**Priority:** Medium

Diagnostics should be structured and easy to filter. A useful minimal model:

```fsharp
type DiagnosticSeverity =
    | Info
    | Warning
    | Error

type DiagnosticCategory =
    | Environment
    | BackendCost
    | RenderingLimitation
    | ReadinessBlocker
    | DeveloperAction
```

The console can still print text, but artifacts and tests should consume structured records.

### 4.7 Add responsiveness diagnostics and decouple input from rendering

**Priority:** High

The framework should expose responsiveness as a first-class validation surface, not a one-off manual diagnosis.

Recommended API additions:

```fsharp
type InputPhaseTiming =
    { InputId: int64
      Cause: FrameCause
      RoutedAt: System.DateTimeOffset
      RoutingDuration: System.TimeSpan
      UpdateDuration: System.TimeSpan
      ViewDuration: System.TimeSpan
      RetainedStepDuration: System.TimeSpan
      LayoutDuration: System.TimeSpan
      TextDuration: System.TimeSpan
      PaintDuration: System.TimeSpan
      PresentDuration: System.TimeSpan
      InputToPresentDuration: System.TimeSpan
      ProductModelChanged: bool
      DirtyArea: int
      RepaintedNodeCount: int }
```

Recommended runtime change:

- Input callbacks should enqueue normalized input events and return quickly.
- The frame loop should drain queued inputs in order, fold model updates, then render once per frame.
- Pointer moves should remain coalesced; discrete press/release/click/key events should remain ordered and never be dropped.

Recommended test/tooling additions:

- A headless responsiveness script that replays click/key inputs against real sample pages.
- A live diagnostic mode that writes JSONL timing records.
- A summary that reports p50/p95/max input-to-present latency by page and control type.

---

## 5. Skills and agent-workflow improvement opportunities

### 5.1 Add local-package-feed drift checks to FS.GG skills

**Priority:** High

Relevant skills should instruct agents to check package drift whenever they touch samples:

- `fs-gg-project`
- `fs-gg-product-testing`
- `fs-gg-samples`
- `fs-gg-ui-widgets`
- `fs-gg-ant-design`
- `speckit-implement`
- `speckit-merge`

Recommended skill rule:

> If a sample consumes `FS.GG.UI.*` through package references, compare sample pins against packable project versions before validation. If they differ, update pins or record why the sample intentionally targets an older package.

### 5.2 Add readiness-ignore guidance

**Priority:** High

Skills should mention:

- `specs/*/readiness/` is ignored by default.
- If readiness artifacts are deliverables, add a `.gitignore` allowlist.
- Verify with `git check-ignore -v`.
- Verify with `git diff --cached --name-only` that evidence artifacts are staged.

This should be in `speckit-implement` and possibly a dedicated readiness/evidence skill.

### 5.3 Add "do not parallelize same test project" guidance

**Priority:** Medium-high

The general developer instruction encourages parallelizing independent reads and commands. For dotnet tests, the skills should constrain that:

> Do not run two `dotnet test` invocations for the same project/configuration concurrently unless they have isolated output directories.

This prevents runtimeconfig/file-lock failures.

### 5.4 Add visual-readiness skill

**Priority:** Medium-high

Create a focused skill for visual evidence workflows:

Name candidate:

```text
fs-gg-visual-readiness
```

Use when:

- Capturing screenshot evidence.
- Generating contact sheets.
- Validating accepted sizes/themes.
- Writing reviewer-defect classifications.
- Updating readiness summaries.

The skill should require:

- Real screenshot capture when available.
- Explicit degraded-capture disclosure.
- Reviewer classification before "accepted".
- Contact-sheet spot check.
- Avoiding generated-summary overwrite of manual context.

### 5.5 Strengthen `speckit-merge` package bump guidance

**Priority:** Medium

The merge skill correctly required a package bump after merging. It should also say:

- After bumping packages, update package-consuming samples to the new version.
- Repack directly into the configured local feed.
- Clear global packages.
- Restore/build package-consuming samples after the cache clear.
- Update readiness ledgers to the final bumped version.

This avoids a mismatch between source package versions and sample validation docs.

### 5.6 Maintain Claude/Codex skill parity with a generated index

**Priority:** Medium

Claude skill wrappers were added to close parity gaps, but parity can drift again.

Add a generated parity report:

```text
docs/reports/skills-parity.md
```

or a script:

```sh
scripts/check-agent-skill-parity.sh
```

It should compare:

- `.agents/skills`
- `.codex/skills` or managed Codex skill sources where applicable
- `.claude/skills`

The check should report:

- Missing skills by agent.
- Wrapper-only skills.
- Stale descriptions.
- Broken source paths.

### 5.7 Add evidence-honesty rules to implementation skills

**Priority:** Medium

Feature 162 needed an explicit caveat:

- New tests passed, but failing-first output was not preserved.
- Full-solution test was attempted but canceled because `Controls.Tests` stopped producing output.

Skills should continue to require this level of honesty:

- Do not mark a full gate green if it was canceled.
- Record targeted substitute gates separately.
- Do not hide environment-limited or incomplete evidence.
- If failing-first evidence was not preserved, state that in tasks or readiness notes.

### 5.8 Add responsiveness-diagnostics guidance

**Priority:** High

The FS.GG skills should instruct agents to check interactivity separately from screenshot readiness when a generated product or showcase is meant to be used live.

Recommended skill rule:

> For interactive samples, validate at least one pointer activation and one keyboard activation through the deterministic `Perf.runScript` path, then use live `OnFrameMetrics` or a responsiveness diagnostic mode when the user reports lag. Distinguish input routing cost from post-update render/present cost.

Skills should also document the AntShowcase keyboard baseline:

- `Enter` and `Space` on key-down activate the representative command.
- Key-up is ignored.
- Other keys route only through focused controls or chord/fallthrough handlers.

This prevents agents from treating "click dispatched" as equivalent to "interaction feels responsive."

---

## 6. Prioritized action plan

### P0: Close immediate validation/tooling traps

1. Add a package-feed refresh/check script for samples.
2. Add skill guidance for package-pin drift.
3. Add skill guidance for ignored readiness evidence and `git check-ignore`.
4. Add skill guidance against concurrent `dotnet test` runs for the same output path.
5. Add a responsiveness diagnostic lane that records pointer/key routing, update, render, paint, present, queue depth, and input-to-present latency.
6. Start the input/render scheduler rewrite: native input callbacks must enqueue timestamped input envelopes and return; the frame loop must drain input, fold updates, and recompute the scene at most once per dirty frame.

### P1: Make visual readiness reusable

1. Move screenshot completeness and summary serializers into `FS.GG.UI.Testing`.
2. Move contact sheet generation into shared testing/tooling code.
3. Introduce managed-section summary writing.
4. Create `fs-gg-visual-readiness` skill.

### P2: Improve framework inspectability

1. Emit structured layout/render inspection artifacts.
2. Add tests for text bounds, clipping, z-order, and section containment.
3. Add root/surface paint coverage checks for themes.
4. Add damage-area assertions for localized interactions so a button click cannot silently repaint the whole frame.
5. Add retained-render/text-cache metrics that identify why a localized update still remeasures or repaints broad regions.

### P3: Improve validation lane reliability

1. Split solution validation into named lanes.
2. Add per-lane timeouts.
3. Add test progress logging.
4. Keep the full-solution command as an aggregate, not the only authoritative signal.

### P4: Keep agent skills synchronized

1. Add a skill parity checker.
2. Generate a periodic skill parity report.
3. Ensure Claude wrappers and Codex skills point to equivalent repository guidance.

---

## 7. Proposed ownership boundaries

### Library code

Owns:

- Structured render/layout inspection.
- Visual evidence data types.
- Completeness validators.
- Contact sheet generation if it can avoid dragging runtime-only dependencies into core packages.
- Stable diagnostic categories.

### Sample code

Owns:

- Page registry.
- Theme aliases relevant to the sample.
- Which pages count as minimum-size representatives.
- Reviewer judgement and defect classification.

### Scripts/build tooling

Owns:

- Version bumping.
- Local feed packing.
- Sample package pin refresh.
- Cache clearing.
- Validation lane orchestration.

### Skills

Own:

- Remembering repository traps.
- Choosing the correct validation lane.
- Preserving evidence honesty.
- Coordinating package bump, sample pin update, readiness evidence, and branch cleanup.

---

## 8. Concrete skill updates recommended

Add the following notes to the FS.GG and Spec Kit skills that handle samples, generated products, validation, and merge:

1. **Package pin drift**
   - Before validating a package-consuming sample, compare sample `PackageReference Include="FS.GG.UI.*"` versions to packable project versions.
   - If versions differ, update pins or document why not.

2. **Readiness evidence staging**
   - `specs/*/readiness/` is ignored unless allowlisted.
   - If readiness evidence is required, update `.gitignore`, then verify with `git check-ignore -v` and `git diff --cached --name-only`.

3. **Test parallelism**
   - Parallelize reads freely.
   - Do not parallelize `dotnet test` invocations against the same project/configuration without isolated output paths.

4. **Visual readiness**
   - Real screenshot evidence is preferred.
   - Degraded capture must be disclosed.
   - Reviewer classifications are required before accepted readiness.
   - Regenerators must not erase manual caveats.

5. **Post-merge package bump**
   - After merge, bump all packable projects.
   - Pack to the local feed.
   - Clear global packages.
   - Update sample pins to the bumped version.
   - Restore/build/test package-consuming samples again.

6. **Evidence honesty**
   - If failing-first test output was not preserved, record that.
   - If a full validation gate was canceled or hung, record it as incomplete and provide targeted substitute gates.

---

## 9. Recommended library API sketch

This section is intentionally concrete enough to guide an implementation task.

```fsharp
namespace FS.GG.UI.Testing

type VisualSize =
    { Width: int
      Height: int }

type VisualTheme =
    { Id: string
      DisplayName: string }

type VisualPage =
    { Id: string
      Title: string
      Category: string }

type VisualCaptureTarget =
    { Page: VisualPage
      Theme: VisualTheme
      Size: VisualSize
      OutputPath: string }

type VisualCaptureStatus =
    | Captured
    | Missing
    | Degraded of reason: string
    | Undecodable of reason: string
    | WrongSize of expected: VisualSize * actual: VisualSize

type VisualCaptureRecord =
    { Target: VisualCaptureTarget
      Status: VisualCaptureStatus
      Bytes: int64 option
      Sha256: string option }

type ReviewerSeverity =
    | None
    | Minor
    | Major
    | Blocking

type ReviewerClassification =
    { PageId: string
      ThemeId: string
      Severity: ReviewerSeverity
      Class: string
      Reviewer: string
      Timestamp: string
      Notes: string }

type VisualReadinessStatus =
    | Accepted
    | Blocked
    | EnvironmentLimited
    | PendingReview

type VisualReadinessReport =
    { Seed: int
      Size: VisualSize
      Targets: VisualCaptureTarget list
      Captures: VisualCaptureRecord list
      ReviewerClassifications: ReviewerClassification list
      ContactSheets: string list
      Status: VisualReadinessStatus }
```

Minimal helper modules:

```fsharp
module VisualCaptureMatrix =
    val expand : pages: VisualPage list -> themes: VisualTheme list -> size: VisualSize -> outDir: string -> VisualCaptureTarget list

module VisualCompleteness =
    val validatePng : expected: VisualCaptureTarget -> VisualCaptureRecord

module ContactSheet =
    val write : records: VisualCaptureRecord list -> outPath: string -> Result<string, string>

module ReviewerDefects =
    val writeTemplate : pages: VisualPage list -> themes: VisualTheme list -> outPath: string -> unit
    val parse : path: string -> Result<ReviewerClassification list, string list>

module VisualReadinessMarkdown =
    val writeSummary : report: VisualReadinessReport -> outPath: string -> unit
```

---

## 10. Recommended scripts

### 10.1 `scripts/check-sample-package-pins.fsx`

Purpose:

- Fail when package-consuming samples reference stale `FS.GG.UI.*` versions.

Expected output:

```text
sample: samples/AntShowcase
current package version: 0.1.25-preview.1
all FS.GG.UI package references match
```

### 10.2 `scripts/refresh-local-feed.fsx`

Purpose:

- Pack all packable projects to `~/.local/share/nuget-local`.
- Clear global packages.
- Optionally restore/build samples.

### 10.3 `scripts/allowlist-readiness.fsx`

Purpose:

- Add `.gitignore` exceptions for a feature readiness directory.
- Verify evidence can be staged.

### 10.4 `scripts/run-validation-lanes.fsx`

Purpose:

- Run named validation lanes with timeouts and separate logs.

Example lanes:

```text
build
pack
ant-showcase
controls
rendering-harness
solution-tests
```

---

## 11. Suggested follow-up tasks

1. Create a Spec Kit feature for reusable visual-readiness tooling in `FS.GG.UI.Testing`.
2. Create a Spec Kit feature for structured render/layout inspection metadata.
3. Add a package-feed refresh/check script and wire it into AntShowcase docs.
4. Update FS.GG skills with package-pin, readiness-ignore, and test-parallelism guidance.
5. Add a skill parity checker for Claude/Codex/local agent skills.
6. [x] Split full validation into named lanes with timeouts. Implemented by
   `166-validation-lane-runner`; remaining follow-up is to investigate the
   `Controls.Tests` no-progress blocker it now exposes.
7. Change visual-readiness summary generation to use managed sections or generated-only output files.
8. Add a responsiveness diagnostic mode and an AntShowcase latency budget report.
9. Decouple live input dispatch from synchronous retained rendering so input callbacks enqueue work and return quickly.

---

## 12. Research-informed refinements to the recommendations

The recommendations above were checked against current public guidance and the repository's own Spec Kit constitution/templates. The research does not change the direction, but it sharpens several implementation choices.

### 12.1 Split the work into several Spec Kit features, not one mega-feature

GitHub Spec Kit's public workflow is explicitly sequential: specify the "what" and "why", plan the "how", then break the plan into phased tasks before implementation. The Microsoft Spec Kit overview also calls out the `.specify` templates, `constitution.md`, and the `/specify` -> `/plan` -> `/tasks` sequence as the intended operating model.

**Refinement**

Use one umbrella report, but create separate Spec Kit feature directories for independently deliverable slices:

1. Package-feed determinism and validation lanes.
2. Shared visual-readiness tooling.
3. Structured render/layout inspection metadata.
4. Responsiveness diagnostics and input-loop scheduling.
5. Skill/parity updates and evidence guidance.

This keeps each `spec.md`, `plan.md`, and `tasks.md` coherent, limits branch conflicts, and creates natural parallel work streams after foundational contracts are agreed.

### 12.2 Use `dotnet test --blame-hang`, not only shell `timeout`

The earlier recommendation used shell-level `timeout` wrappers. Microsoft documents `dotnet test --blame-hang` and `--blame-hang-timeout` for collecting hang evidence and terminating hung test hosts. Shell timeouts can still protect a lane runner, but they do not produce the same test-order and dump artifacts.

**Refinement**

Each validation lane should write:

- TRX output to a lane-specific results directory.
- Hang blame artifacts for long tests.
- Console output with normal verbosity.
- A lane summary that distinguishes test failure, lane timeout, hang dump produced, and external cancellation.

Candidate command pattern:

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj \
  -c Release --no-restore --no-build \
  --logger "trx;LogFileName=controls.trx" \
  --results-directory artifacts/test-results/controls \
  --blame-hang --blame-hang-timeout 10m \
  --blame-hang-dump-type mini
```

The outer lane runner can still enforce a larger process timeout, but the inner test command should own test-host diagnostics.

### 12.3 Prefer source mapping and isolated package caches for package-only sample proof

NuGet documentation confirms that `PackageReference` projects consume packages from the global packages folder, that global packages can be cleared with `dotnet nuget locals global-packages --clear`, and that Package Source Mapping constrains which source can serve which package IDs. The current AntShowcase `nuget.config` has `nuget-local` and `nuget.org`, but it does not map `FS.GG.UI.*` exclusively to the local feed.

**Refinement**

Strengthen the local package proof with both source mapping and isolated caches:

1. Add Package Source Mapping so `FS.GG.UI.*` resolves only from `nuget-local`, while third-party packages resolve from `nuget.org`.
2. For deterministic validation lanes, set a lane-specific package cache, for example `NUGET_PACKAGES=/tmp/fs-gg-nuget/ant-showcase`.
3. Use `dotnet nuget locals all --list` in readiness logs so the package/cache locations are explicit.
4. Reserve global cache clearing for a dedicated "cold package proof" lane; avoid making every validation run destructive.

This makes stale local package use harder to miss and reduces accidental cross-lane interference.

### 12.4 Use built-in .NET diagnostics APIs before adding heavy observability dependencies

Microsoft's .NET tracing guidance recommends that library authors instrument with `System.Diagnostics.ActivitySource` / `Activity`, leaving application authors free to choose collectors such as OpenTelemetry. Microsoft also recommends newer `System.Diagnostics.Metrics` APIs for new metrics work, while `EventCounters` remain useful for lightweight near-real-time counters and existing tooling.

**Refinement**

Do not add an OpenTelemetry dependency to core rendering packages just to collect input/render timings. Start with collector-neutral .NET primitives:

- `ActivitySource` spans for input-to-present traces.
- `System.Diagnostics.Metrics` histograms/counters for latency and queue depth.
- Optional `EventSource` / `EventCounters` only where existing dotnet tooling needs them.
- JSONL diagnostic export for deterministic local evidence and CI artifacts.

This satisfies observability needs without coupling the framework to one telemetry backend.

### 12.5 Treat responsiveness as an event-latency and long-task problem

W3C Event Timing defines a model for observing latency of user-triggered events. W3C Long Tasks describes how long UI-thread tasks block other critical tasks, including reaction to user input, and references response targets such as sub-100ms input response and 50ms long-task surfacing.

**Refinement**

The responsiveness plan should capture the same conceptual fields even though this is a desktop F#/Skia viewer, not a browser:

- Native event timestamp.
- Dispatch/routing start.
- Routing end.
- Product update end.
- View/retained render end.
- Paint/present end.
- Queue depth and coalesced/dropped move counts.
- Input-to-present duration.

The existing `< 50 ms` p95 budget remains reasonable as a first readiness target because the current measured costs are orders of magnitude above that. The plan should also record long render tasks over `50 ms` even when input-to-present cannot be measured because of host limitations.

### 12.6 State-of-the-art patterns for input-latency architecture

The current public guidance across browser, mobile, desktop, and UI-framework sources is consistent on one central point: input callbacks must stay short, and expensive rendering work should be frame-paced, measurable, and interruptible or deferrable where possible.

Relevant patterns:

1. **Measure the whole interaction, not just the handler.** Web INP/Event Timing treats an interaction as input delay, event processing, and presentation delay through the next paint. Chrome's Long Animation Frames API extends this to frame-level diagnosis by surfacing long frames, first UI event timing, render start, and blocking duration. FS.GG's current `OnFrameMetrics` is useful but incomplete because it reports routing and prior present timing, not a correlated native-input-to-present record.
2. **Keep UI-thread work items small.** WPF's dispatcher model, Avalonia's dispatcher guidance, Windows `DispatcherQueue`, and browser long-task guidance all emphasize that serial UI-thread work blocks subsequent input. The exact APIs differ, but the architecture rule is the same: long work items should be split, deferred, or moved out of the urgent input lane.
3. **Render on a frame scheduler, not directly from input callbacks.** Android `Choreographer` processes input before frame callbacks and makes frame callbacks the place for per-frame update/render work. Qt's `update()` schedules and merges paint events instead of repainting immediately. Flutter describes a structured pipeline from input through build/layout/paint/compositing/rasterization. FS.GG should follow this model: input should request/invalidate work, and the frame loop should decide when to fold queued input and render.
4. **Use priority lanes for urgent versus non-urgent work.** React 18 concurrent rendering uses priorities and interruptible rendering so urgent input can remain responsive while larger updates are in progress. Browser Prioritized Task Scheduling and `scheduler.yield()` similarly distinguish user-blocking work from background work. FS.GG does not need React's architecture, but it should add an equivalent concept at the viewer level: discrete input is urgent, pointer moves are continuous/coalescible, and expensive non-visible or secondary updates belong in a lower-priority lane.
5. **Coalesce continuous input, never reorder discrete input.** Existing FS.GG move coalescing is directionally correct. The problem is that coalescing currently happens inside the same synchronous path that can still do expensive update/view/render work. Coalescing should move to the input queue/frame-scheduler boundary, with counts reported as evidence.
6. **Use long-frame diagnostics as a first-class readiness signal.** Perfetto FrameTimeline, Chrome LoAF, W3C Long Tasks, and the web INP guidance all treat long frames and presentation delay as actionable performance facts. FS.GG should similarly produce long-frame records, input queue depth, coalesced move counts, and worst input-to-present samples.
7. **Treat renderer thread separation as a second-stage optimization, not the first move.** WPF, Avalonia, Flutter, and Skia-based systems all have strong thread-affinity constraints somewhere in the stack. Moving GL/Skia rendering to another thread may become valuable, but the first architectural correction should be a scheduler boundary that removes expensive work from native input callbacks while preserving the existing pure update/view contracts.

### 12.7 Local architecture review of the input-lag path

The local codebase already contains several good ingredients:

- `src/Controls.Elmish/ControlsElmish.fs` routes pointer input through retained render data when available, so hit-testing usually avoids a full oracle render.
- Pointer moves are coalesced before processing a previously pending move.
- `src/SkiaViewer/Host/OpenGl.fs` paces `DoUpdate()` and `DoRender()` by `TargetFrameRate`, and the live renderer avoids full scene walks for unchanged frames where possible.
- The product `View` remains pure and cheap in the measured AntShowcase case; the expensive work is lower in the retained/lowering/layout/text/paint path.

The problematic coupling is still direct:

1. Silk.NET input handlers in `src/SkiaViewer/Host/OpenGl.fs` call `dispatchViewerEvent` from mouse and keyboard callbacks.
2. The legacy event bridge in `src/SkiaViewer/SkiaViewer.fs` maps those events to `LegacyPointer` or `LegacyKey`.
3. `handlePointer` calls `host.MapPointer input currentSize currentModel`; `handleKey` calls `host.MapKey`.
4. For each produced message, `dispatchHostMsg` calls `host.Update msg currentModel`, stores the next model, and immediately executes `currentScene <- host.View currentSize currentModel`.
5. For Controls.Elmish, `host.View` is `SceneNode.Group [ renderRetained size model ]`, so it can run retained reconciliation, layout, text measurement, dirty-region work, and scene production before the input callback path returns.
6. The paced render tick then presents `renderCurrentScene()`, but by that point the expensive scene recomputation has already blocked the loop.

This explains the observed shape:

- Click/key routing itself can be tiny.
- A state-changing click can still monopolize the event loop because the post-update `View`/retained-render path is synchronous.
- Multiple product messages from one input are folded by calling `dispatchHostMsg` repeatedly, so a single discrete input can trigger multiple immediate `host.View` recomputations.
- The existing frame cap prevents unlimited presents, but it cannot prevent input lag if expensive scene recomputation happens before the next event can be processed.
- `OnFrameMetrics` currently has a blind spot: it can report input-side routing duration and previous backend present timing, but it does not correlate native input timestamp, update, scene recomputation, paint, swap, and next visible frame in one record.

### 12.8 Rewrite assessment: radical scheduler rewrite is warranted, full framework rewrite is not

A radical rewrite is warranted for the input/render scheduling boundary. A full rewrite of Controls, RetainedRender, Elmish, KeyboardInput, or the product-facing MVU model is not warranted as the first step.

The current architecture conflates five concerns in one call path:

1. Native input delivery.
2. Host input mapping.
3. Product model update.
4. Scene recomputation through retained controls.
5. Frame presentation readiness.

That coupling violates the common state-of-the-art rule from the research: native input callbacks and urgent UI dispatch should enqueue or perform small routing/update work, then return quickly. Expensive rendering should be scheduled, paced, instrumented, and preferably coalesced to one render per frame.

The recommended rewrite scope is therefore:

- Introduce a `ViewerInputEnvelope` or equivalent internal record carrying sequence id, native timestamp, input kind, priority lane, and original `ViewerPointerInput`/keyboard event.
- Add an explicit input queue in `SkiaViewer`, with separate handling for urgent discrete input, continuous/coalescible pointer moves, and lower-priority/background work.
- Change native input callbacks so they normalize/enqueue input, signal the frame loop, and return without calling product `Update` or `host.View`.
- Drain the queue from the frame/update loop: preserve all discrete input order, coalesce pointer moves, fold all resulting product messages, then call `host.View` at most once for that frame when the model/size/runtime state is dirty.
- Add a dirty/invalidation model instead of treating every input-produced message as an immediate scene recomputation.
- Correlate timing from native event timestamp through routing, update, retained step, paint, swap/present, and queue delay in one diagnostic record.
- Keep `Perf.runScript` deterministic and clock-free for golden structural assertions; add a live or benchmark-oriented path for wall-clock timing.
- Consider a render-thread or chunked retained-render follow-up only after the queue/frame scheduler exists and the timing data proves the remaining bottleneck.

What should not be rewritten initially:

- Product-facing MVU APIs and generated-product code patterns.
- The `Control<'msg>` DSL and Ant-styled widget surface.
- RetainedRender's semantic diff model, except where a timing record or dirty/invalidation hook is required.
- Keyboard routing/focus semantics, except for delivering key events through the same queued scheduler as pointer events.
- Skia/GL ownership model, until thread-affinity and context ownership are explicitly designed and tested.

This split gives the best risk/reward profile. It attacks the architectural cause of multi-second delayed clicks without discarding valuable retained-render, focus, keyboard, package, and testing work already in the framework.

The rewrite should move the principal live boundary from "input produces messages, each message calls `host.View` immediately" to "input produces timestamped envelopes, the frame scheduler drains envelopes and renders once when dirty." In package terms:

```text
SkiaViewer.Host.OpenGl
  owns native callbacks, native timestamps, wake/signal, and GL/Skia presentation

SkiaViewer
  owns queued input envelopes, priority/coalescing policy, dirty state,
  model folding, scene recomputation scheduling, and input-to-present timing

Controls.Elmish
  owns retained pointer/key routing, focus/runtime state, retained step timing,
  and adapter metrics, but does not own native event scheduling

Controls
  owns retained/lowered render performance, dirty-region precision,
  text/layout/cache behavior, and structured inspection data

Elmish and KeyboardInput
  remain pure state/update/routing capabilities consumed by the viewer boundary
```

The first implementation should avoid public churn where possible by adding internal scheduler machinery behind `Viewer.runInteractiveViewer` and the Controls.Elmish adapter. Public additions should be limited to diagnostics and optional configuration. A breaking public host change is justified only if tests prove the current `InteractiveViewerHost` contract cannot report timing or preserve semantics without ambiguity.

### 12.9 Keep visual evidence pure at the model layer and adapter-owned at the image layer

The existing recommendation to promote visual-readiness helpers to `FS.GG.UI.Testing` is still sound, but the implementation should avoid forcing SkiaSharp-specific contact-sheet composition into every test consumer.

**Refinement**

Split the tooling:

- `FS.GG.UI.Testing.VisualEvidence`: pure target/status/reviewer/summary model plus PNG completeness validation.
- `FS.GG.UI.Testing.VisualReadinessMarkdown`: generated-summary and managed-section writing.
- `FS.GG.UI.Testing.VisualInspection`: pure render/layout inspection assertions.
- Sample/app or optional adapter code: SkiaSharp contact-sheet composition and screenshot capture integration.

This keeps the core testing package lighter and lets generated products consume the evidence model without inheriting image-composition dependencies unless needed.

### 12.10 Sources consulted

- GitHub Spec Kit README: https://github.com/github/spec-kit
- Microsoft Spec Kit overview: https://developer.microsoft.com/blog/spec-driven-development-spec-kit
- Microsoft `dotnet test` VSTest options: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest
- Microsoft NuGet global packages/cache guidance: https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders
- Microsoft NuGet Package Source Mapping: https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping
- Microsoft .NET EventCounters guidance: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/event-counters
- Microsoft .NET distributed tracing instrumentation: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
- Microsoft .NET OpenTelemetry observability overview: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- W3C Event Timing API: https://www.w3.org/TR/event-timing/
- W3C Long Tasks API: https://www.w3.org/TR/longtasks-1/
- web.dev Interaction to Next Paint: https://web.dev/articles/inp
- web.dev Optimize Interaction to Next Paint: https://web.dev/articles/optimize-inp
- web.dev Optimize long tasks: https://web.dev/articles/optimize-long-tasks
- Chrome Long Animation Frames API: https://developer.chrome.com/docs/web-platform/long-animation-frames
- Chrome `scheduler.yield()` guidance: https://developer.chrome.com/blog/use-scheduler-yield
- MDN Prioritized Task Scheduling API: https://developer.mozilla.org/en-US/docs/Web/API/Prioritized_Task_Scheduling_API
- Android `Choreographer` NDK reference: https://developer.android.com/ndk/reference/group/choreographer
- Flutter architectural overview: https://docs.flutter.dev/resources/architectural-overview
- React 18 concurrent rendering overview: https://legacy.reactjs.org/blog/2022/03/29/react-v18.html
- React `startTransition` API reference: https://react.dev/reference/react/startTransition
- WPF threading model: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model
- Avalonia threading model: https://docs.avaloniaui.net/docs/app-development/threading
- Windows `DispatcherQueue`: https://learn.microsoft.com/en-us/windows/apps/develop/dispatcherqueue
- Qt `QWidget::update()` painting behavior: https://doc.qt.io/qt-6/qwidget.html
- Skia Debugger: https://skia.org/docs/dev/tools/debugger/
- Skia `SkPictureRecorder`: https://api.skia.org/classSkPictureRecorder.html
- Perfetto FrameTimeline data source: https://perfetto.dev/docs/data-sources/frametimeline

---

## 13. Spec Kit implementation plan

This plan is written so it can be translated directly into Spec Kit features. It follows the local constitution:

- Tier 1 public surface changes use `spec.md`, `plan.md`, `.fsi` contracts, semantic tests, implementation, and docs.
- Stateful/live input work uses an Elmish/MVU-style boundary where workflow state and effects are represented explicitly.
- Tests must produce real evidence where safe, with synthetic evidence disclosed.
- Skills are advisory but should be updated when they encode repeated repository traps.

### 13.1 Recommended feature split and branch order

| Order | Feature branch candidate | Scope | Depends on | Parallel with |
|---|---|---|---|---|
| 1 | `163-package-feed-validation-lanes` | Package source mapping, local-feed refresh/check scripts, validation lane runner | none | skill docs can start after contracts |
| 2 | `164-visual-readiness-toolkit` | Shared visual evidence models, completeness checks, summary writer, AntShowcase migration | 163 for package proof only | 165 after API names are settled |
| 3 | `165-render-inspection-metadata` | Visual inspection tree, text-fit metadata, overlap/damage assertions | none; coordinate names with 164 | 164, 167 |
| 4 | `166-responsiveness-diagnostics` | Input timing model, live JSONL diagnostics, queue-depth metrics, event-loop decoupling | 163 for validation lanes; optionally 165 for inspection/damage checks | 164 and 167 |
| 5 | `167-skill-parity-and-evidence-guidance` | Skill updates, parity checker, visual/responsiveness guidance | report and agreed feature names | all code features |
| 6 | `168-runtime-diagnostics-taxonomy` | Structured runtime diagnostic categories and output routing | can be folded into 166 if desired | 164/165 if kept separate |

If team capacity is limited, combine 168 into 166 and deliver five features. If responsiveness is the highest user pain, start 166 immediately after the minimal validation runner from 163 lands.

### 13.2 Spec Kit commands per feature

For each feature:

```text
$speckit-git-feature <feature-name>
$speckit-specify <feature outcome and user-visible requirements>
$speckit-plan
$speckit-tasks
$speckit-analyze
$speckit-implement
```

After implementation:

```text
$speckit-merge
```

The plan and tasks should keep `[P]` only for tasks that edit different files and have no dependency on another incomplete task in the same phase. Do not parallelize two `dotnet test` invocations against the same project/configuration unless they use isolated output directories.

### 13.3 Feature 163: package-feed determinism and validation lanes

**Goal**

Make package-only sample validation deterministic and make long-running validation diagnosable.

**Implementation status on 2026-06-19:** Feature 163 landed the first repository-level package-feed
proof and focused validation-lane runner. Evidence is committed under
`specs/163-package-feed-validation-lanes/readiness/`.
The post-merge package bump advanced the current local-feed package set to
`0.1.25-preview.1`.

**Primary user stories**

- **US1:** A maintainer can refresh the local feed, update package-consuming samples, and prove no stale `FS.GG.UI.*` packages are used.
- **US2:** A maintainer can run named validation lanes with per-lane logs, TRX files, hang diagnostics, and no shared output races.
- **US3:** A readiness summary can distinguish green lanes, failed lanes, hung lanes, skipped lanes, and canceled lanes.

**Expected source paths**

- `scripts/refresh-local-feed-and-samples.fsx`
- `scripts/run-validation-lanes.fsx`
- `samples/AntShowcase/nuget.config`
- `tests/Rendering.Harness/PackageFeed.fsi`
- `tests/Rendering.Harness/ValidationLanes.fsi`
- `tests/Rendering.Harness.Tests/Feature163*.fs`
- `tests/Package.Tests/Feature163PackageFeedValidationTests.fs`
- `specs/163-package-feed-validation-lanes/readiness/`

**Spec and plan requirements**

- Package Source Mapping must map `FS.GG.UI.*` only to `nuget-local`.
- Validation lanes must use isolated result directories.
- The controls lane must use `--blame-hang --blame-hang-timeout`.
- The lane runner must not treat a canceled or timed-out full solution run as green.
- Package proof must record whether global packages were cleared or a lane-specific package cache
  was used. Feature 163's default proof uses an isolated cache and does not clear global caches.

**Task outline**

- [x] T001 [P] Add failing tests or FSI transcript for stale `FS.GG.UI.*` package pin detection.
- [x] T002 [P] Add failing test or scripted fixture for Package Source Mapping verification against `samples/AntShowcase/nuget.config`.
- [x] T003 Implement package-pin checking in `scripts/refresh-local-feed-and-samples.fsx --mode check`.
- [x] T004 Implement refresh/proof workflow in `scripts/refresh-local-feed-and-samples.fsx`.
- [x] T005 Update `samples/AntShowcase/nuget.config` with Package Source Mapping for `FS.GG.UI.*`.
- [x] T006 [P] Add lane model and JSON/markdown summary types in `Rendering.Harness.ValidationLanes`.
- [x] T007 Implement `scripts/run-validation-lanes.fsx` with lane-specific logs, result JSON, TRX output for dotnet lanes, `--blame-hang`, and outer timeout/no-progress handling.
- [x] T008 [P] Document lane usage in `specs/163-package-feed-validation-lanes/quickstart.md`.
- [x] T009 Run package proof, AntShowcase sample lane, controls lane, and rendering-harness lane; save logs, result JSON, TRX files, and summaries under `specs/163-package-feed-validation-lanes/readiness/`. Aggregate full-solution validation remains optional and is recorded separately from focused readiness.

**Parallel opportunities**

- T001 and T002 can be written in parallel.
- T003 and T004 can start in parallel if they do not share a helper module; otherwise create the shared helper first.
- T006 and T008 can run in parallel with script implementation.
- Validation runs for different projects can run in parallel only when output paths and package caches are isolated.

**Definition of done**

- A stale package pin fails validation.
- `FS.GG.UI.*` package restore is source-mapped to `nuget-local`.
- A hung test produces blame artifacts instead of silent no-output waiting.
- Readiness docs include commands, outputs, and package/cache locations.

### 13.4 Feature 164: shared visual-readiness toolkit

**Goal**

Move sample-owned visual readiness data and summary behavior into reusable testing APIs while keeping image-composition adapters optional.

**Primary user stories**

- **US1:** A generated product can define pages/themes/sizes and receive a capture matrix without copying AntShowcase logic.
- **US2:** A test can validate PNG completeness, degraded capture reasons, and reviewer classifications through `FS.GG.UI.Testing`.
- **US3:** A summary generator can update managed sections without erasing manual caveats.
- **US4:** AntShowcase can migrate to the shared toolkit without changing its accepted evidence semantics.

**Expected source paths**

- `src/Testing/Testing.fsi`
- `src/Testing/Testing.fs`
- `tests/Testing.Tests/`
- `samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fsi`
- `samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fs`
- `samples/AntShowcase/AntShowcase.App/VisualReadiness.fsi`
- `samples/AntShowcase/AntShowcase.App/VisualReadiness.fs`
- `samples/AntShowcase/AntShowcase.Tests/VisualReadinessTests.fs`

**Spec and plan requirements**

- Pure evidence models belong in `FS.GG.UI.Testing`.
- SkiaSharp contact-sheet composition should remain in AntShowcase or an optional adapter unless a new dependency is explicitly justified.
- Generated markdown must write inside managed markers or write generated-only files.
- Degraded capture must be represented explicitly, not as a successful capture.

**Task outline**

- [ ] T001 [P] Draft `VisualSize`, `VisualTheme`, `VisualPage`, `VisualCaptureTarget`, `VisualCaptureStatus`, `VisualCaptureRecord`, `ReviewerClassification`, and `VisualReadinessReport` in `src/Testing/Testing.fsi`.
- [ ] T002 [P] Add semantic tests for capture matrix expansion and status aggregation in `tests/Testing.Tests/`.
- [ ] T003 [P] Add semantic tests for managed-section markdown writing in `tests/Testing.Tests/`.
- [ ] T004 Implement pure visual evidence models and matrix expansion in `src/Testing/Testing.fs`.
- [ ] T005 Implement PNG completeness validation without depending on sample-specific page registries.
- [ ] T006 Implement reviewer-defect template parsing/writing in `src/Testing/Testing.fs`.
- [ ] T007 Implement managed-section markdown writer.
- [ ] T008 Migrate `samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fs` to call shared APIs.
- [ ] T009 Migrate `samples/AntShowcase/AntShowcase.App/VisualReadiness.fs` while keeping contact-sheet rendering at the app edge.
- [ ] T010 Update AntShowcase visual-readiness tests to assert shared API behavior and unchanged output semantics.
- [ ] T011 Run AntShowcase visual-readiness CLI and tests; save evidence under `specs/164-visual-readiness-toolkit/readiness/`.

**Parallel opportunities**

- T001, T002, and T003 can proceed in parallel after the API names are agreed.
- T005, T006, and T007 can proceed in parallel after T004 establishes shared types.
- AntShowcase migration should wait for the shared API to compile, but tests can be prepared in parallel.

**Definition of done**

- AntShowcase no longer owns generic matrix/status/summary logic.
- Managed markdown reruns do not erase manual validation context.
- Generated products can consume the toolkit without depending on AntShowcase.

### 13.5 Feature 165: render/layout inspection metadata

**Goal**

Expose stable inspection metadata so visual assertions can move from manual screenshot review to deterministic tests.

**Implementation status on 2026-06-19:** Feature 165 shipped the first structured inspection
contract across `FS.GG.UI.Scene`, `FS.GG.UI.Controls`, and `FS.GG.UI.Testing`. The implementation is
additive: screenshots, `LayoutEvidenceReport`, and `GeneratedLayoutValidation` remain supported.

**Primary user stories**

- **US1:** A test can inspect final control bounds, text runs, clipping, z-order, and ownership.
- **US2:** A test can assert text fit and section containment without image analysis.
- **US3:** A reviewer can consume Markdown/JSON inspection summaries with grouped statuses, findings, unsupported facts, exceptions, and related visual evidence links.
- **US4:** Samples and generated products can adopt inspection incrementally while unsupported, not-inspected, not-run, and environment-limited scopes stay visible.

**Implemented source paths**

- `src/Scene/Scene.fsi`
- `src/Scene/Scene.fs`
- `src/Controls/Inspection.fsi`
- `src/Controls/Inspection.fs`
- `src/Testing/Testing.fsi`
- `src/Testing/Testing.fs`
- `tests/Scene.Tests/Feature165VisualInspectionModelTests.fs`
- `tests/Scene.Tests/Feature165VisualInspectionPaintTests.fs`
- `tests/Controls.Tests/Feature165ControlInspectionLayoutTests.fs`
- `tests/Controls.Tests/Feature165ControlInspectionPaintTests.fs`
- `tests/Controls.Tests/Feature165ControlInspectionRegressionTests.fs`
- `tests/Testing.Tests/Feature165VisualInspectionValidationTests.fs`
- `tests/Testing.Tests/Feature165VisualInspectionExceptionTests.fs`
- `tests/Testing.Tests/Feature165VisualInspectionSummaryTests.fs`
- `tests/Testing.Tests/Feature165VisualInspectionArtifactTests.fs`
- `tests/Testing.Tests/Feature165VisualInspectionAdoptionTests.fs`

**Spec and plan requirements**

- Inspection artifacts must be stable enough for tests but not expose private renderer internals as a permanent compatibility burden without `.fsi` review.
- Text fit should be computed from the same measured text data used by rendering.
- Damage assertions should use union area and dirty rects, not summed overlapping area. This remains follow-up scope after the first structured inspection contract.
- API additions require surface baseline updates.

**Implemented task summary**

- [x] Draft dependency-light Scene inspection records and helpers.
- [x] Add a dedicated Controls inspection signature and adapter.
- [x] Add Testing validation, readiness, Markdown/JSON, and managed-section helpers.
- [x] Add semantic tests for model stability, Controls extraction, validation rules, exceptions, summaries, artifacts, adoption, and render-output regression.
- [x] Update package READMEs, compatibility notes, surface baselines, and representative readiness evidence.
- [x] Record FSI API-shape, focused tests, package-surface, package-pack, generated-product helper, and timing evidence under `specs/165-render-layout-inspection/readiness/`.

**Remaining follow-up**

- [ ] Add retained-render inspection emission after `RetainedRender.step`.
- [ ] Add damage inspection fields for dirty rect union area, repainted nodes, and shifted nodes.
- [ ] Migrate at least one AntShowcase visual-shell assertion to structured inspection evidence.
- [ ] Replace the missing `fake.sh` target dependency with a canonical repo validation command or restore the wrapper.

**Parallel opportunities**

- T002 and T003 can be written in parallel after T001.
- T004 and T006 may require coordination because both touch control rendering/text measurement.
- T005 and T007 can proceed together if one developer owns retained state and another owns damage metadata.
- AntShowcase assertions should wait for the inspection API to stabilize.

**Definition of done**

- Scene, Controls, and Testing public API and surface baselines are updated consistently.
- Focused Feature 165 tests pass for the shipped inspection contract.
- Representative inspection summaries distinguish accepted, unsupported, environment-limited, not-inspected, and not-run states without relying on screenshot evidence.

### 13.6 Future feature: responsiveness diagnostics and input-loop scheduling

Feature number note: branch `166-validation-lane-runner` was used first for the
validation-lane hardening follow-up. The responsiveness diagnostics and
input-loop scheduling work below remains recommended, but should receive a new
feature number when specified.

**Goal**

Make interactive latency measurable, then rewrite the live scheduler boundary so native input callbacks no longer perform synchronous scene recomputation.

**Primary user stories**

- **US1:** A maintainer can capture per-input timing from native input through present.
- **US2:** A generated product can run a deterministic responsiveness script for pointer and keyboard activation.
- **US3:** The live viewer queues input and renders on the frame loop, preserving discrete input order while coalescing move work.
- **US4:** AntShowcase exposes a diagnostic mode/report proving click/key latency against a stated budget.

**Expected source paths**

- `src/Controls.Elmish/ControlsElmish.fsi`
- `src/Controls.Elmish/ControlsElmish.fs`
- `src/SkiaViewer/SkiaViewer.fsi`
- `src/SkiaViewer/SkiaViewer.fs`
- `src/SkiaViewer/Host/OpenGl.fs`
- `src/SkiaViewer/Host/OpenGl.fsi`
- `tests/Elmish.Tests/FeatureXXXResponsivenessMetricsTests.fs`
- `tests/SkiaViewer.Tests/FeatureXXXInputQueueTests.fs`
- `samples/AntShowcase/AntShowcase.App/Interactive.fs`
- `samples/AntShowcase/AntShowcase.Tests/InteractionTests.fs`

**Target input/render boundary**

Current boundary to replace:

```text
OpenGl input callback
  -> dispatchViewerEvent
  -> LegacyPointer/LegacyKey
  -> handlePointer/handleKey
  -> host.MapPointer/host.MapKey
  -> for each product message:
       host.Update
       currentScene <- host.View currentSize currentModel
  -> later render tick presents currentScene
```

Target boundary:

```text
OpenGl input callback
  -> capture native timestamp and normalized input
  -> enqueue ViewerInputEnvelope
  -> signal scheduler
  -> return

Frame scheduler
  -> drain urgent discrete inputs in arrival order
  -> coalesce continuous pointer moves with metrics
  -> call host.MapPointer/host.MapKey
  -> fold all product messages into the model
  -> mark scene dirty once
  -> call host.View at most once for the dirty frame

Render/present
  -> render latest scene at frame cadence
  -> emit one correlated timing record per discrete interaction
```

This target keeps the pure product `Update`/`View` model, the retained Controls adapter, and existing keyboard/focus semantics. It changes when heavy work is allowed to run.

**Migration phases**

1. **Measurement first:** Add timing records and queue-depth fields without changing scheduling. This creates a failing/slow baseline for AntShowcase and protects the rewrite from improving perceived lag while hiding phase regressions.
2. **Internal scheduler model:** Add internal queue, priority lane, dirty-state, and frame-drain types behind the existing viewer entry points. Keep public APIs stable unless diagnostics require additive fields.
3. **Callback decoupling:** Change native pointer/key callbacks to enqueue envelopes and return. Preserve close, resize, and lifecycle events explicitly because those have different urgency and shutdown semantics.
4. **Frame-drain execution:** Move `host.MapPointer`/`host.MapKey`, product message folding, and scene recomputation into the paced frame/update loop. Recompute scene once after all folded messages for the frame, not once per message.
5. **Adapter timing and invalidation:** Let Controls.Elmish report retained-step/layout/text/damage timing into the same input timing record. Add dirty/invalidation hooks only where needed; do not rewrite retained render semantics in this feature.
6. **Readiness proof:** Capture AntShowcase click and keyboard evidence with p50/p95/max input-to-present latency, queue depth, coalesced moves, and long-frame counts.
7. **Deferred renderer work:** If the scheduler rewrite still leaves unacceptable latency, open a separate feature for chunked retained render or a render-thread design. That later feature must own GL/Skia thread-affinity risk explicitly.

**Spec and plan requirements**

- Timing instrumentation must be collector-neutral: `ActivitySource`, `System.Diagnostics.Metrics`, and JSONL are preferred over mandatory OpenTelemetry dependencies.
- Deterministic `Perf.runScript` remains clock-free for golden count/bool fields; latency timing lives in a separate live or explicitly benchmark-oriented surface.
- Input queue must preserve press/release/click/key order.
- Pointer move coalescing must remain explicit and counted.
- Rendering should be scheduled once per frame after folding queued inputs, not once per native input callback.
- This is an architectural rewrite of the input/render scheduler boundary, not a wholesale rewrite of Controls, RetainedRender, product MVU code, or the Ant-styled widget API.
- Native callbacks should enqueue timestamped input envelopes and signal the loop; update/view/render work should happen from the frame/update loop under a dirty/invalidation model.
- Multiple product messages produced by one input should be folded before scene recomputation so one discrete input cannot trigger several immediate retained renders.
- Render-thread separation is a possible follow-up only after scheduler/timing data proves it is still necessary; GL/Skia thread-affinity constraints must be designed explicitly.
- Resize and lifecycle events need explicit policy: resize may force a dirty scene, close must not be delayed behind non-urgent work, and screenshot/readback effects must continue to observe the latest committed scene.
- The old live path should remain available behind a short-lived diagnostic flag until the new scheduler has parity evidence; remove the flag once AntShowcase and SkiaViewer queue tests pass consistently.
- Any synthetic/live-window limitations must be disclosed in readiness evidence.

**Task outline**

- [ ] T001 [P] Add failing deterministic tests for pointer/key activation metrics shape in `tests/Elmish.Tests/`.
- [ ] T002 [P] Add failing SkiaViewer tests for queued input ordering and move coalescing in `tests/SkiaViewer.Tests/`.
- [ ] T003 Draft `InputPhaseTiming`, `InputQueueMetrics`, and `OnInputTiming` in `src/Controls.Elmish/ControlsElmish.fsi`.
- [ ] T004 Add internal scheduler types: `ViewerInputEnvelope`, queue state, dirty/invalidation state, input priority lane, and frame-drain result.
- [ ] T005 Add live timing capture around native timestamp, queue delay, routing, update, view, retained step, paint, and present.
- [ ] T006 Add JSONL diagnostic sink and optional metrics counters/histograms.
- [ ] T007 Change live viewer dispatch so input callbacks enqueue normalized inputs and return quickly.
- [ ] T008 Drain queued inputs on the frame/update loop, fold model updates, and render once per dirty frame.
- [ ] T009 Preserve discrete input ordering and expose coalesced move counts.
- [ ] T010 Ensure one input that produces several product messages causes at most one scene recomputation before the next present.
- [ ] T011 Add AntShowcase diagnostic mode or environment-gated timing log in `Interactive.fs`.
- [ ] T012 Add AntShowcase responsiveness tests/scripts for content button click and `Enter`/`Space`.
- [ ] T013 Run live diagnostic capture where a window/GL host is available; save JSONL and summary under `specs/166-responsiveness-diagnostics/readiness/`.
- [ ] T014 Add docs explaining latency budgets, limitations, and how to interpret timing fields.

**Parallel opportunities**

- **Parallel A:** T001 and T002 can run in parallel because they target different packages and define the failing contracts.
- **Parallel B:** T003 diagnostics API and T004 internal scheduler model can proceed in parallel after naming is agreed, then converge before public surface review.
- **Parallel C:** T005 timing capture and T006 JSONL/metrics output can split between viewer timing and artifact serialization after T003/T004.
- **Serialized core rewrite:** T007, T008, and T010 should be one coordinated branch because they alter the live event-loop semantics and must preserve input ordering.
- **Parallel D:** T011 and T012 can begin once the diagnostic API compiles; they do not need to wait for final latency budgets.
- **Parallel E:** Documentation and readiness-summary formatting from T014 can proceed while live-window evidence from T013 is being gathered.

**Definition of done**

- A content-button click produces one correlated input-to-present timing record.
- Routing remains low-cost and separately visible from render/present.
- Discrete inputs are not dropped; moves are coalesced with counts.
- AntShowcase latency evidence no longer requires ad hoc FSI scripts.

### 13.7 Feature 167: skill parity and evidence guidance

**Goal**

Encode the repeated traps from this report into local skills and keep Claude/Codex wrappers synchronized.

**Primary user stories**

- **US1:** An agent touching samples is warned about package-pin drift and local-feed proof.
- **US2:** An agent committing readiness artifacts is warned about `.gitignore` allowlisting and `git check-ignore`.
- **US3:** An agent validating tests is warned not to parallelize the same project/configuration.
- **US4:** Claude and Codex skill wrappers can be compared automatically.
- **US5:** Agents know how to run visual-readiness and responsiveness diagnostics honestly.

**Expected source paths**

- `.agents/skills/*/SKILL.md`
- `.claude/skills/*/SKILL.md`
- `scripts/check-agent-skill-parity.sh` or `scripts/check-agent-skill-parity.fsx`
- `docs/reports/skills-parity.md`
- `src/*/skill/SKILL.md` where canonical package skills need updates

**Spec and plan requirements**

- Identify canonical skill sources before editing wrappers.
- Wrapper updates must not fork guidance from canonical package skills.
- Parity checker should report missing skills, stale descriptions, broken target paths, and wrapper/canonical drift.
- Skills should point to the Feature 163 validation scripts now that they are available.

**Task outline**

- [ ] T001 [P] Add failing parity-check fixture or dry-run expectation for missing/stale wrappers.
- [ ] T002 [P] Draft updates for package-pin drift, readiness allowlisting, test parallelism, and validation lane guidance.
- [ ] T003 [P] Draft updates for visual-readiness and responsiveness diagnostics guidance.
- [ ] T004 Implement `scripts/check-agent-skill-parity.*`.
- [ ] T005 Update canonical `.agents/skills` and package-owned `src/*/skill/SKILL.md` entries.
- [ ] T006 Update `.claude/skills` wrappers to match canonical guidance.
- [ ] T007 Generate `docs/reports/skills-parity.md`.
- [ ] T008 Run parity checker and save output under feature readiness.

**Parallel opportunities**

- T001, T002, and T003 can run in parallel.
- T005 and T006 should be sequenced unless wrapper generation is automated.
- T007 can run after T004 and the skill edits.

**Definition of done**

- Parity checker catches a deliberately broken wrapper in test or fixture mode.
- Claude and Codex guidance both mention package drift, readiness evidence, test-output isolation, visual readiness, and responsiveness diagnostics.
- Skills link to the concrete scripts and validation lanes that now exist.

### 13.8 Feature 168: runtime diagnostics taxonomy

**Goal**

Make runtime diagnostics structured and filterable so expected environment/backend-cost messages are not confused with readiness blockers.

**Primary user stories**

- **US1:** A sample run groups diagnostics by category and severity.
- **US2:** Tests can assert that expected backend-cost diagnostics are informational, not failures.
- **US3:** Readiness summaries can include diagnostic counts and blocker status.

**Expected source paths**

- `src/Controls/Diagnostics.fsi`
- `src/Controls/Diagnostics.fs`
- `src/SkiaViewer/SkiaViewer.fsi`
- `src/SkiaViewer/SkiaViewer.fs`
- `samples/AntShowcase/AntShowcase.App/Program.fs`
- `tests/Controls.Tests/DiagnosticsTests.fs`
- `tests/SkiaViewer.Tests/`

**Task outline**

- [ ] T001 [P] Add failing tests for diagnostic severity/category mapping.
- [ ] T002 Draft `DiagnosticSeverity` and `DiagnosticCategory` additions in `.fsi`.
- [ ] T003 Implement category mapping for environment warnings, backend cost, rendering limitation, readiness blocker, and developer action.
- [ ] T004 Add structured diagnostic artifact output in sample app edge.
- [ ] T005 Update readiness summaries to include diagnostic counts.
- [ ] T006 Run AntShowcase interactive/evidence commands and confirm GTK/module warnings are not reported as readiness blockers.

**Parallel opportunities**

- T001 can run in parallel with T002.
- T004 and T005 can run after T003 and can be split between app edge and summary code.

**Definition of done**

- Console output is shorter and less alarming by default.
- Structured artifacts preserve details for tests and diagnosis.
- Readiness status depends on blockers, not on all informational diagnostics.

### 13.9 Cross-feature dependency graph

```text
163 package/validation lanes
  ├─ enables stronger package evidence for 164, 165, 166, 168
  └─ feeds scripts referenced by 167

164 visual-readiness toolkit
  ├─ independent after 163 package proof is available
  └─ can consume inspection data from 165 later

165 render/layout inspection
  ├─ independent of 164 at the API level
  └─ strengthens 166 damage/latency assertions

166 responsiveness diagnostics
  ├─ uses 163 validation lanes
  ├─ can use 165 damage metadata when available
  └─ may absorb 168 if runtime diagnostics are not split

167 skills/parity
  ├─ can start immediately with current report guidance
  └─ should receive a final pass after 163/164/166 scripts and APIs land

168 diagnostics taxonomy
  └─ can run in parallel with 164/165 or merge into 166
```

### 13.10 Shared validation matrix

Use these as the baseline quickstart commands across the features, adjusted per feature path and branch.

```sh
dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --no-restore
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --logger "console;verbosity=normal"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --no-restore --logger "console;verbosity=normal"
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --logger "console;verbosity=normal"
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore
```

Lane-runner form:

```sh
dotnet fsi scripts/run-validation-lanes.fsx \
  --lane package-proof \
  --lane antshowcase-sample \
  --lane controls \
  --lane rendering-harness \
  --out specs/163-package-feed-validation-lanes/readiness/lanes
```

Package proof form:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode check \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof

dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode proof \
  --isolated-cache specs/163-package-feed-validation-lanes/readiness/package-proof/nuget-cache \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof
```

Responsiveness proof form:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj \
  -c Release --no-restore -- responsiveness --page buttons --theme light --out specs/166-responsiveness-diagnostics/readiness/buttons-light.jsonl
```

### 13.11 Evidence and readiness rules for every feature

Each feature should include:

- `research.md` recording external decisions and alternatives.
- `data-model.md` for the public records/unions/scripts introduced.
- `contracts/` for CLI arguments, JSONL schemas, markdown managed-section format, or public FSI shape.
- `quickstart.md` with commands that exercise the user-visible path.
- `tasks.md` with `[P]` markers only where files and dependencies are independent.
- A `readiness/` directory only when committed evidence is required, plus `.gitignore` allowlisting and `git check-ignore -v` proof.
- FSI transcript or packed-library semantic evidence for new public APIs.
- Real screenshot/live-window evidence where available; synthetic evidence must be named and justified.

### 13.12 Suggested MVP path

If the goal is to reduce risk fastest, do this:

1. Use Feature 163's package-pin check and validation lane runner as the first validation gate.
2. Land Feature 166's timing-only diagnostics before changing event-loop scheduling.
3. Use timing evidence to choose the smallest safe scheduling/render optimization.
4. Land Feature 167's skill guidance once the commands exist.
5. Continue with Feature 164 and 165 in parallel to improve future visual-readiness quality.

This MVP gives maintainers immediate tools to prove package correctness, diagnose test hangs, and capture input latency without waiting for the larger visual-inspection architecture.

---

## 14. Bottom line

The framework is capable enough to produce useful, real visual evidence today, but too much of the readiness workflow lives in sample-specific code and agent memory. The next improvement step should be to turn Feature 162's workflow into reusable infrastructure:

- Library APIs for visual evidence and inspection.
- Scripts for package-feed validation and readiness staging.
- Responsiveness diagnostics that separate input routing, update, render, paint, and present latency.
- Skills that remember the repository-specific traps.

That combination would make future generated products easier to validate, make screenshot evidence more trustworthy, prevent "renders correctly but feels unusable" regressions, and reduce the amount of manual correction required during implementation and merge.
