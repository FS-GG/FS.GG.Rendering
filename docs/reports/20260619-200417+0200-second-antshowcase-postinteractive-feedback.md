# Second Ant Showcase Post-Interactive Feedback

Timestamp: 2026-06-19 20:04:17 +0200

## Context

The original implementation report was written at
`docs/reports/20260619-192344+0200-second-antshowcase-implementation-report.md`.
This addendum captures the defects found during the later manual interactive review and
all-page screenshot pass: pointer input felt delayed, slider input did not visibly work,
keyboard behavior was not obvious, and the light theme contained black/transparent regions
and a primary-blue navigation rail that did not match Ant Design expectations.

The fixes are local to the framework render/input path and the
`samples/SecondAntShowcase` shell. They should be treated as follow-up framework feedback,
not only as sample polish.

## Problems Encountered

1. The rendered root scene did not guarantee an opaque viewport background.
   The visual-readiness PNGs could therefore expose transparent regions as black in viewers
   or contact sheets, especially on the light theme. This was a framework issue because both
   full render and retained render produced scene groups without a root surface contract.

2. The visual-readiness workflow captured screenshots but did not fail on alpha-channel
   transparency. The screenshots existed and the page matrix was complete, but the output
   could still be visibly wrong. A simple alpha min/max check caught the problem after manual
   review.

3. The SecondAntShowcase navigation rail used normal buttons, so the side navigation read as
   a stack of filled primary controls. That is not an Ant Design menu/sider pattern. The
   framework has button variants, but no first-class Ant-like Menu/Sider primitive, so the
   sample had to approximate the pattern by applying a ghost style class to nav buttons.

4. Slider pointer input was not routed as a value-changing interaction. The Elmish binding
   path treated authored bindings mostly as click-equivalent dispatches and did not produce
   a pointer-derived numeric payload for slider `changed` bindings. Retained routing had the
   same gap, so click and drag could fail to dispatch useful `onChanged` messages.

5. Keyboard interaction was not adequately visible in the live sample. Existing sample tests
   cover pure model updates, and the framework has keyboard routing support, but the showcase
   does not yet present clear focus state, keyboard affordance, or a per-control keyboard
   smoke matrix that proves representative controls can be actuated from the keyboard.

6. The package-consuming sample loop made framework fixes easy to miss. After changing
   `src/Controls` and `src/Controls.Elmish`, the sample still consumed NuGet packages, so the
   local feed had to be repacked and caches cleared before screenshots reflected the source
   changes. This is correct for package-consumer proof, but it is a slow feedback loop during
   interactive fixes.

7. The live lag report could not be fully accepted by manual feel alone. The visible result is
   better, and the previous responsiveness work addressed the synchronous input/render
   boundary, but this follow-up still needs accepted live input-to-present measurements on a
   visible desktop session to close the performance loop rigorously.

## Fixes Applied

1. Added `ControlInternals.sceneWithViewportBackground` so render results are composed over
   `theme.Background` before being grouped.

2. Updated both `Control.renderTree` and retained render initialization/step output to use
   the same opaque viewport composition helper.

3. Changed the SecondAntShowcase side-nav buttons to use the custom `ghost` style class so
   the navigation rail no longer appears as a solid primary-blue button stack.

4. Added Elmish slider binding resolution for pointer click and primary drag interactions.
   The routed value is derived from the pointer x-coordinate against the rendered slider
   bounds, clamped to `0.0..1.0`, and dispatched through the authored `changed` binding with a
   numeric payload.

5. Added retained-routing parity coverage proving slider click and drag dispatch
   `SliderChanged` messages without retained fallback.

## Validation Performed

1. `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release`
   passed with 190 tests passed and 17 skipped.

2. Repacked local packages into `/home/developer/.local/share/nuget-local`:
   `FS.GG.UI.Controls.0.1.33-preview.1.nupkg` and
   `FS.GG.UI.Controls.Elmish.0.1.33-preview.1.nupkg`.

3. Cleared NuGet global and HTTP caches, then rebuilt the package-consuming sample with
   `dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental`.

4. `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore --logger 'console;verbosity=normal'`
   passed with 104 tests passed.

5. `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage`
   reported 96 of 96 controls mapped, 19 pages, no unreferenced controls, and no duplicated
   controls.

6. Captured all-page screenshot contact sheets after repacking:
   `artifacts/user-audit-20260619-193920+0200-final/contact-sheet-light.png` and
   `artifacts/user-audit-20260619-193920+0200-final/contact-sheet-dark.png`.
   Alpha inspection showed the final PNGs are fully opaque.

## Library and Framework Improvements

1. Make opaque root-surface composition a formal render contract. Every renderer that emits a
   complete viewport should either require an explicit background or add one consistently.

2. Add visual-readiness assertions for alpha channels, root background coverage, and obvious
   theme-token violations. A screenshot matrix should be able to fail before a human notices
   black transparent regions.

3. Promote range-control pointer semantics into the framework. Slider, rating, date picker,
   numeric stepper, and similar controls should expose control-specific pointer mapping rather
   than relying on generic click-equivalent bindings.

4. Add retained-render tests for every non-button interactive control family. Retained routing
   must prove value payloads, not only that an event target was found.

5. Provide first-class keyboard focus and keyboard activation evidence helpers. Samples should
   be able to emit a matrix showing tab/focus/enter/space/arrow behavior by control family.

6. Add Ant-style navigation primitives or sample guidance for `Menu`, `Sider`, selected state,
   item hover, and density. Using generic buttons for navigation is too easy and visually wrong.

7. Improve the package-consuming development loop with a single command that packs changed
   local projects, updates or confirms sample package pins, clears only necessary caches, and
   rebuilds the sample.

8. Extend responsiveness diagnostics to produce accepted live input-to-present evidence from
   a visible desktop session, not only deterministic or headless substitute evidence.

## Skill Improvements

1. `fs-gg-ant-design` should explicitly reject filled primary-button stacks as a menu/sider
   substitute unless the design intentionally calls for a call-to-action list.

2. `fs-gg-ant-design` should include a screenshot self-review checklist: alpha channel,
   root background, selected/hover state, Ant token family, density, and whether controls look
   like Ant components rather than generic colored rectangles.

3. `speckit-implement` should require at least one post-build live interaction pass for samples
   with interactive controls, including pointer, keyboard, and value-changing controls.

4. `fs-gg-product-keyboard-input` should define a sample keyboard evidence matrix that can be
   generated and reviewed like visual readiness.

5. `fs-gg-feedback-capture` should encourage addendum records when serious defects are found
   after an implementation report has already been written. The current phase hook model is
   useful, but post-review corrections need an equally obvious place.

## Remaining Risks

1. The slider pointer path is covered by focused Elmish tests, but other value controls need
   similar retained-routing coverage.

2. Keyboard support still needs explicit sample-level evidence and visible focus affordances.

3. The live lag improvement was manually observed, but accepted live input-to-present metrics
   should still be captured before treating responsiveness as fully closed.

