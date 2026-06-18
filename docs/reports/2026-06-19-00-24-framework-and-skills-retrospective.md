# Framework and Skills Retrospective After AntShowcase Visual Readiness

**Report date:** 2026-06-19 00:24 Europe/Vienna
**Repository:** FS.GG.Rendering
**Baseline after merge:** `main` at `4b086a5` (`chore: bump FS.GG.UI packages to 0.1.24-preview.1`)
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
5. The framework lacks structured visual/layout metadata for assertions. The feature can assert declared regions and non-empty render trees, but "readable and not overlapping" still depends heavily on screenshots and human review.
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

**Improvement**

Responsiveness needs first-class framework support:

1. Add phase timing around `MapPointer`/`MapKey`, product `Update`, `host.View`, retained `step`, text measurement, layout, paint walk, and present.
2. Report a single input-to-present latency record for each discrete input, not only input-routing metrics.
3. Queue input messages and render on the frame loop instead of performing expensive render work directly inside input callbacks.
4. Keep discrete inputs ordered, but coalesce move/hover work before it reaches heavyweight render paths.
5. Narrow damage tracking so localized state changes do not dirty the whole frame.
6. Cache text measurement/shaping and unchanged lowered subtrees more aggressively.
7. Add responsiveness readiness tests or scripts that fail when click/key-to-present latency exceeds a budget.

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

Move common readiness workflow pieces out of AntShowcase:

- Screenshot target matrix.
- Theme alias normalization.
- Completeness checks.
- Contact sheet generation.
- Reviewer-defect parsing.
- Summary JSON/markdown serializers.
- Managed-section summary writing.

This would make future generated products and samples much cheaper to validate.

### 4.3 Add render/layout inspection metadata

**Priority:** High

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
5. Add a responsiveness diagnostic lane that records pointer/key routing, update, render, paint, present, and input-to-present latency.

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
current package version: 0.1.24-preview.1
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
6. Split full validation into named lanes with timeouts.
7. Change visual-readiness summary generation to use managed sections or generated-only output files.
8. Add a responsiveness diagnostic mode and an AntShowcase latency budget report.
9. Decouple live input dispatch from synchronous retained rendering so input callbacks enqueue work and return quickly.

---

## 12. Bottom line

The framework is capable enough to produce useful, real visual evidence today, but too much of the readiness workflow lives in sample-specific code and agent memory. The next improvement step should be to turn Feature 162's workflow into reusable infrastructure:

- Library APIs for visual evidence and inspection.
- Scripts for package-feed validation and readiness staging.
- Responsiveness diagnostics that separate input routing, update, render, paint, and present latency.
- Skills that remember the repository-specific traps.

That combination would make future generated products easier to validate, make screenshot evidence more trustworthy, prevent "renders correctly but feels unusable" regressions, and reduce the amount of manual correction required during implementation and merge.
