# Seed Scene/Controls/Viewer construct inventory (T004) + cross-check (T015)

Per-profile branch and the FS.GG.UI surfaces each seed file consumes. The authoritative cross-check
(SC-002) is the per-profile build: all 4 profiles compile 0/0 against the live feed, so every referenced
construct resolves in the current public surface with the assumed arity/shape.

## Headless branch (governed, headless-scene) — `//#if (profile == "governed" || "headless-scene")`

| File | Constructs |
|---|---|
| Model.fs | `open FS.GG.UI.Scene` only; plain records (Model/Msg) — no Scene constructors |
| View.fs | `Scene` (`Group`, `Rectangle`, `Text`, `Color`, `Nodes`) |
| LayoutEvidence.fs | `Size`, `LayoutEvidenceReport`, `LayoutRegionEvidence`, `Rect`, `ReadableLayout`, `ApproximateTextBounds`, `NoLayoutOverlap` |
| EvidenceCommands.fs | `SceneEvidence.render`, `Size`, `LayoutEvidenceReport` |
| Program.fs | headless CLI dispatch; no Viewer/Controls |

Result: builds 0/0; scene-evidence ok (deterministic), layout-evidence ok. No drift.

## Interactive branch (app, sample-pack) — `//#else`

| File | Constructs |
|---|---|
| Model.fs | `Scene`; `Controls` (`ChartSeries`, `DataGridColumn/Row`, `RichTextBlock`, `Attr`, `DataGrid.columns`, `RichText.*`, `Bold`), `DesignSystem` (`Theme.light`), `KeyboardInput` (`ViewerKey`, `ViewerKeyEvent`, `ViewerKeyboard.*`, `Letter/Enter/Arrow*/Escape/Backspace`), `Controls.Elmish` (`AdapterCommand`, `AdapterSubscription`, `ControlRuntimeMsg`, `DispatchHostCommand`, `ControlsElmish.subscriptions`) |
| View.fs | `Controls.Typed.*` (TextBlock/RichText/TextBox/Button/LineChart/GraphView/DataGrid/Stack), `Widget<'msg>`, `Widget.toControl`, `Control<'msg>`, `Control.renderTree`, `ControlsElmish.program`, `Theme.light`, `Scene` (`Size`, `SceneNode`, `Group`) |
| LayoutEvidence.fs | as headless + `LayoutGameplayBounds`, `LayoutOverlapDiagnostic` (`HudTextOverlap`/`HudGameplayOverlap`), `LayoutOverlaps`, `DeterministicRenderOnly` |
| WindowOptions.fs | window-behavior parsing → `Viewer` launch request types |
| EvidenceCommands.fs | `SceneEvidence.render`; `Viewer.*` (`runBounded`, `runApp`, `captureScreenshotEvidence`, `desktopSessionDiagnostic`, `validateWindowLaunchBehavior`); `ControlsElmish.runInteractiveApp` (app) |
| Program.fs | entry point; host selection — `ControlsElmish.runInteractiveApp` (app) / `Viewer.runApp` (sample-pack) |

Result after fixes: both app and sample-pack build 0/0 and emit scene/layout/launch/image evidence.
Drift found + resolved: (1) `LayoutEvidence` un-annotated helpers — annotated; (2) sample-pack missing
the Controls package set — guards widened. See `drift-map.md`.

## Cross-check verdict (SC-002)

100% of referenced Scene (and Controls/Viewer/Elmish/Layout/KeyboardInput/DesignSystem) constructs exist
in the current public surface with the assumed shape — proven by 4× clean builds. ✅
