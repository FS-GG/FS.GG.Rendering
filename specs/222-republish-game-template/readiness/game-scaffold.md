# T015 — `game` profile scaffold-selectable from the feed (FR-004, SC-001)

From the **feed** package only (local source uninstalled — see `consumer-install.md`):

```
$ dotnet new fs-gg-ui --profile game --name GameProbe -o <probe> --allow-scripts yes
The template "FS GG UI Governed Project" was created successfully.   → exit 0
```

`game` choice accepted — no missing-profile / unknown-choice error. The generated starter is the
minimal, replaceable Pong-style MVU seam (Feature 220):

```
src/GameProbe/Model.fs:
  // GAME family — minimal, replaceable Pong-style starter (feature 220).
  //   REPLACE ME. This Model/Msg/update is the developer-owned game seam.
  type Ball = { … VelocityX: float; VelocityY: float }
  type PaddleSide … type PaddleDirection …
```

Generated `Directory.Packages.props` pins `FsGgUiVersion=0.1.54-preview.1` → the product restores the
freshly-published coherent set from the feed. ✅
