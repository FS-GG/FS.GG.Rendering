# Profile readiness — `app` (US1 / CV-1..CV-5)

**Pinned version:** `0.1.50-preview.1` · **Generated:** `dotnet new fs-gg-ui --profile app --name Product -o /tmp/fsgg-app`

| Step | Result | Evidence |
|------|--------|----------|
| Generate | ✅ | full app product (Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, DesignSystem) |
| Restore | ✅ `restore_exit=0` | no NU1101 / no version conflict (CV-1) |
| Build (`-c Debug`) | ✅ `build_exit=0` | no Scene-API compile error (CV-2) |
| Scene evidence (`--scene-evidence`) | ✅ `exit=0` | `size=320x200;capabilities=1;hash=c8b1dccf…8e6f9d21` (CV-3) |
| Layout evidence (`--layout-evidence`) | ✅ `exit=0` | `status=ok proof-level=ReadableLayout hud-region=summary:0,0,640,96 gameplay-region=content:0,96,640,384` (CV-3) |
| **Live launch** (`--launch-evidence`, `DISPLAY=:1`) | ✅ `exit=0` | `status=ok mode=persistent-evidence first-frame-presented=true` (live, not environment-limited) |
| **Live screenshot** (`--image-evidence`) | ✅ `exit=0` | real PNG `readiness/game-image-evidence.png` (15.9 KB), `first-frame-presented=true` |
| Governance (`dotnet test Product.Tests`) | ✅ `test_exit=0` | **Passed! 30/30** (CV-3) |

**Resolved `FS.GG.UI.*` set (CV-1/CV-5):**
`Controls`, `Controls.Elmish`, `DesignSystem`, `Diagnostics`, `Elmish`, `KeyboardInput`, `Layout`,
`Scene`, `SkiaViewer`, `Themes.Default` — **all `@0.1.50-preview.1`**.

Single version literal `0.1.50-preview.1` — CV-4. No phantom `Color`/`SkillSupport` — CV-5. **Profile GREEN.**
