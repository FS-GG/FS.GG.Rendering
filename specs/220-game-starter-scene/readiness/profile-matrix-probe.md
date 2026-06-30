# T004 — Profile-matrix instantiation probe (quickstart Scenario A)

**Goal:** confirm the plan's Decision-2 reachability hypotheses against the *real* generated output
**before** authoring the game branch, and capture the FR-007 diff baseline.

**Method:** the published package template (`FS.GG.UI.Template@0.1.53-preview.1`) was uninstalled so
`dotnet new fs-gg-ui` resolves the **local repo template** unambiguously (so this probe and the later
FR-007 re-diff compare the same template source). Each profile instantiated with
`dotnet new fs-gg-ui --profile <p> --name Product -o <scratch>/<p>`.

Local feed: `~/.local/share/nuget-local/` with a coherent `FS.GG.UI.* @ 0.1.53-preview.1` set.

## Results

| Profile | Instantiate | Default-launch branch (`Program.fs` `\| None ->`) | Package set | `Product.Tests` |
|---|---|---|---|---|
| `app` | ✅ | `ControlsElmish.runInteractiveApp viewerOptions interactiveHost` | Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, Controls.Elmish, DesignSystem, Themes.Default | ✅ **30/30** |
| `sample-pack` | ✅ | **`Viewer.runApp viewerOptions generatedHost`** | Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, Controls.Elmish, DesignSystem, Themes.Default | ✅ **29/29** |
| `governed` | ✅ | `printfn "… mode=headless-scene …"` (headless `main`) | Scene, Testing | ✅ **5/5** |
| `headless-scene` | ✅ | headless `main` (`mode=headless-scene`) | Scene | (FR-007 baseline target) |

## Confirmations (the gates the probe exists to settle)

1. **`sample-pack` emits `Viewer.runApp viewerOptions generatedHost` in the default branch** —
   ✅ CONFIRMED (`Program.fs:136`). The `game || sample-pack` launch grouping is therefore safe:
   grouping game onto `runApp` leaves sample-pack's emitted launch call **unchanged**.
2. **`sample-pack` references the controls package set** — ✅ CONFIRMED (9 controls packages above).
   So the content `//#else` controls grouping (app + sample-pack) is correct, and threading `game`
   into the `(app || sample-pack)` package/compile gates is the right package pinning.
3. **`governed` is instantiable** — ✅ CONFIRMED. `governed` has **no** `template/profiles/governed.yml`;
   it is one of the four `choice` values in `.template.config/template.json`'s `profile` symbol and is
   selected purely by `--profile governed`. Its generated source hits the
   `//#if (profile == "governed" || profile == "headless-scene")` headless branch plus the
   `(profile == "governed")` Testing gate. It is a valid FR-007 diff target for T006/T024 as-is —
   **no stale reference to correct.**

**Decision-2 hypotheses hold without adjustment.** Game-branch authoring may proceed with the
two distinct groupings (content: `game` vs controls=`app|sample-pack`; launch: `app` vs
`game|sample-pack`), each pinning sample-pack to its current behavior.
</content>
