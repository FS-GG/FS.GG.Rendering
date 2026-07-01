# T009 — Live after-observation under `sdd` (SC-002)

The fix was packed from the working tree and dev-installed (`FS.GG.UI.Template@99.0.0-dev*`, ephemeral;
committed `.fsproj` version stays `0.1.58-preview.1` — no version bump, FR-007) so `dotnet new fs-gg-ui`
reflects the edited `template.json`. Live scaffolds:

| profile / lifecycle | `count(.claude/skills/fs-gg-*)` | `count(.agents/skills/fs-gg-*)` |
|---|---|---|
| app / sdd | **0** (was 8) | 8 = S(app) |
| game / sdd | **0** (was 8) | 8 = S(game) |

- `count(.claude/skills/fs-gg-*) == 0` under `sdd` for both `app` and `game` — the intrusion SDD flags as `providerWroteSddTree` is gone (SC-002, C-2/C-3).
- `set(.agents/skills/fs-gg-*) == S(profile)` — the provider surface is intact (C-1, FR-002; never shrinks).

`.agents/skills/` S(app) == S(game) == { fs-gg-scene, fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input,
fs-gg-ui-widgets, fs-gg-styling, fs-gg-layout, fs-gg-symbology } (8 skills). `fs-gg-testing` is
`governed`-only and correctly absent from app/game.

The env-gated `scripts/validate-lifecycle-template.fsx` live run (T016) additionally proves this for
`app`/`headless-scene`/`governed`/`sample-pack` with the new `claude-product-skills=0` observation
(the report the Feature 204/219 gates read).
