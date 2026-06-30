# T005 — Early live smoke run (quickstart Scenario C precursor)

**Goal:** observe (not assume) the real launch/host behavior the game branch will reuse, on a real
desktop session, **before** authoring the game branch.

**Host:** real session present — `DISPLAY=:1`, `WAYLAND_DISPLAY=wayland-0`,
`XDG_RUNTIME_DIR=/run/user/1000`. This is a **live** run, not `environment-limited`.

## Default entrypoint (persistent interactive host)

`dotnet run -c Debug` (no flags) on the generated `app` product launched the persistent
interactive host and held a live window open (killed by a 60 s `timeout`, exit 143) — i.e. it did
**not** self-close, which is the correct persistent-window behavior for the default entrypoint.

## Bounded, self-closing evidence (generated `app` product)

| Command | Result |
|---|---|
| `--launch-evidence` | `status=ok mode=persistent-evidence self-closed-for-evidence=true first-frame-presented=true input-dispatch=not-required` |
| `--image-evidence` | `status=ok evidence-kind=image image-decodable=True proves-scene-rendering=true` (real decodable PNG) |
| `--scene-evidence` | `renderer-mode=deterministic-scene size=320x200 hash=c8b1dccf…e6f9d21` |

## Relevance to the game family

The `app` product launches via `ControlsElmish.runInteractiveApp`; the **game family launches via
`Viewer.runApp viewerOptions generatedHost`** — the *same* call `sample-pack` already emits
(confirmed in [profile-matrix-probe.md](./profile-matrix-probe.md)). The `generatedHost` /
`Viewer.runApp` persistent-host path is therefore already exercised by sample-pack and observed
green here through the shared bounded-evidence surface (which routes through the same
`generatedHost`). The game branch reuses this durable spine unchanged (it only re-points
`generatedHost.View` at the game `view`), so the launch/host behavior is observed, not assumed.

**Conclusion:** the persistent-host launch path the game default will use is live and renders a real
first frame. Proceed to author the game branch.
</content>
