# T003 — Conditional map of the shared `template/base` product source (pre-220)

Every `//#if (profile == …)` / `//#else` boundary in the shared product source, with the profile
that currently hits each branch. Read directly from `template/base/` at the pre-220 baseline.

Profiles in scope: `app`, `headless-scene`, `governed`, `sample-pack` (the four template.json
choices). `game` does **not** yet exist.

## Outer split (every product file): headless vs interactive

`//#if (profile == "governed" || profile == "headless-scene")` … `//#else` … `//#endif`

| Branch | Hit by | Content |
|---|---|---|
| `//#if (governed \|\| headless-scene)` | governed, headless-scene | minimal headless `Model`/`Msg`, headless `view`, headless `layoutEvidenceForSize`, headless `Program.main` (`mode=headless-scene`) |
| `//#else` | **app, sample-pack** | controls `Model`/`View`, full `LayoutEvidence`, `WindowOptions`, interactive `Program.main` |

This is the boundary the **content split** must subdivide: today the `//#else` interactive branch
is shared by `app` + `sample-pack` (both the controls family). Feature 220 inserts a `game` branch
ahead of the controls `//#else` so the game family gets the Pong skeleton while app + sample-pack
keep the controls content byte-identical.

## Inner split inside the interactive `//#else`: controls (app) vs game-family

| File | Inner conditional | `//#if (profile == "app")` | `//#else` (today: sample-pack only) |
|---|---|---|---|
| `Program.fs` | around `open FS.GG.UI.Controls.Elmish` | controls open | (none) |
| `Program.fs` | around `let interactiveHost = …` re-export | app re-exports `interactiveHost` | (none) |
| `Program.fs` (launch) | `main`'s `\| None ->` default branch | `ControlsElmish.runInteractiveApp[WithWindowBehavior]` | `Viewer.runApp[WithWindowBehavior] … generatedHost` |
| `EvidenceCommands.fs` | `interactiveHost` definition (confirmed in T004 read) | app defines `interactiveHost` | (none) |
| `GovernanceTests.fs` | host-lock assertions (3 sites) | asserts `runInteractiveApp` | asserts `Viewer.runApp viewerOptions generatedHost` |
| `BehaviorTests.fs` | pointer-click test | app-only pointer-click test | (none) |

**Key reachability fact (verified in T004):** the game-family `//#else` of `profile == "app"`
in `Program.fs`, `GovernanceTests.fs`, and `BehaviorTests.fs` is **today reached only by
`sample-pack`** — which is why it already asserts the keyboard-only `Viewer.runApp` persistent host
(not a controls call). The latent "game family" branch the spec describes is exactly this `//#else`;
feature 220 turns it into a real, exercised path by adding the `game` profile and a distinct
content branch, **without moving sample-pack off any branch it currently hits**.

## Package/compile gates (Product.fsproj, WindowOptions.fs)

| Gate | Form (pre-220) | Hit by |
|---|---|---|
| `Product.fsproj` `WindowOptions.fs` compile | `(profile == "app" \|\| profile == "sample-pack")` | app, sample-pack |
| `Product.fsproj` SkiaViewer/Elmish/KeyboardInput/Layout/Controls/Controls.Elmish/DesignSystem/Themes.Default | `(profile == "app" \|\| profile == "sample-pack")` | app, sample-pack |
| `Product.fsproj` Testing | `(profile == "governed")` | governed |
| `WindowOptions.fs` module body | `(profile == "app" \|\| profile == "sample-pack")` | app, sample-pack |

Feature 220 extends each `(app || sample-pack)` gate to `(app || sample-pack || game)` so the game
profile pulls in the same package/compile set (T014/T015).
</content>
</invoke>
