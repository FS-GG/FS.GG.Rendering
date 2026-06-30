# T019 — The `game` default is a runnable, live, green product (US1)

**Instantiate:** `dotnet new fs-gg-ui --profile game --name Product -o <scratch>` → success.
The generated tree resolves the game branches correctly (no leftover preprocessor markers):

- `Model.fs` → the Pong `Ball`/`MovePaddle` skeleton (no `controlsExampleView`, no `Page`).
- `Program.fs` default `| None ->` branch → `Viewer.runApp viewerOptions generatedHost`.
- `Product.fsproj` / `Directory.Packages.props` → Scene + SkiaViewer + Elmish + KeyboardInput +
  Layout + Controls + Controls.Elmish + DesignSystem + Themes.Default; `WindowOptions.fs` compiled.

## Unmodified default — `Test` is green

`dotnet test tests/Product.Tests/Product.Tests.fsproj -c Debug`:

```
Passed!  - Failed: 0, Passed: 26, Skipped: 0, Total: 26 - Product.Tests.dll (net10.0)
```

13 model-agnostic **GovernanceTests** (the durable spine) + 13 replaceable game **BehaviorTests**
(ball motion, paddle input clamp, score+re-serve, generatedHost map/tick, host-boundary effect
separation, re-pointed layout regions, layout validation). **Edge case satisfied:** the unmodified
default is a valid, live, moving product.

## Live launch / render evidence (real desktop session, `DISPLAY=:1`)

| Command | Result |
|---|---|
| `dotnet run` (no flags) | persistent interactive window launched and held open (killed by 25 s `timeout`) — the `Viewer.runApp generatedHost` default entrypoint, not a self-closing evidence path |
| `--launch-evidence` | `status=ok mode=persistent-evidence self-closed-for-evidence=true first-frame-presented=true` |
| `--image-evidence` | `status=ok image-decodable=True proves-scene-rendering=true` (real decodable PNG of the Pong scene) |
| `--layout-evidence … 640 480` | `proof-level=ReadableLayout hud-region=score:0,0,640,96 gameplay-region=playfield:0,96,640,384 accepted=True` |

The re-pointed evidence spine works: HUD → score strip, gameplay → playfield, and the game
`generatedHost` renders a real first frame at the default entrypoint. **US1 default journey: GREEN.**
</content>
