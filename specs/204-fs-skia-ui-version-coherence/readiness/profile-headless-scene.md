# Profile readiness — `headless-scene` (US1 / CV-1..CV-5)

**Pinned version:** `0.1.50-preview.1` · **Generated:** `dotnet new fs-gg-ui --profile headless-scene --name Product -o /tmp/fsgg-headless-scene`

| Step | Result | Evidence |
|------|--------|----------|
| Generate | ✅ | template engine emitted the profile-stripped product (Scene-only) |
| Restore (`dotnet restore src/Product/Product.fsproj`) | ✅ `restore_exit=0` | no NU1101 / no version conflict (CV-1) |
| Build (`dotnet build -c Debug`) | ✅ `build_exit=0` | no Scene-API compile error (CV-2) |
| Scene evidence (`--scene-evidence`) | ✅ `exit=0` | `size=320x200;capabilities=3;hash=dff00c277aaa30c072d2b0b99d4806537a22b7f9ee2c00aabf9b8ba8d184cb90` (CV-3) |
| Layout evidence (`--layout-evidence`) | ✅ `exit=0` | `status=ok profile=headless-governed proof-level=ReadableLayout overlap-status=NoLayoutOverlap` (CV-3) |
| Governance (`dotnet test Product.Tests`) | ✅ `test_exit=0` | **Passed! 4/4** (CV-3) |

**Resolved `FS.GG.UI.*` set (CV-1/CV-5):**
- `FS.GG.UI.Scene/0.1.50-preview.1`

Single version literal `0.1.50-preview.1` (not `0.1.0-preview.1`) — CV-4. No phantom
`FS.GG.UI.Color` / `FS.GG.UI.SkillSupport` in the resolved graph — CV-5. **Profile GREEN.**
