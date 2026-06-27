# Profile readiness — `governed` (US1 / CV-1..CV-5)

**Pinned version:** `0.1.50-preview.1` · **Generated:** `dotnet new fs-gg-ui --profile governed --name Product -o /tmp/fsgg-governed`

| Step | Result | Evidence |
|------|--------|----------|
| Generate | ✅ | profile-stripped product (Scene + Testing) |
| Restore | ✅ `restore_exit=0` | no NU1101 / no version conflict (CV-1) |
| Build (`-c Debug`) | ✅ `build_exit=0` | no Scene-API compile error (CV-2) |
| Scene evidence (`--scene-evidence`) | ✅ `exit=0` | `size=320x200;capabilities=3;hash=dff00c27…d184cb90` (CV-3) |
| Layout evidence (`--layout-evidence`) | ✅ `exit=0` | `status=ok profile=headless-governed proof-level=ReadableLayout` (CV-3) |
| Governance (`dotnet test Product.Tests` — GovernanceTests + `FS.GG.UI.Testing` assertions) | ✅ `test_exit=0` | **Passed! 5/5** (CV-3) |

**Resolved `FS.GG.UI.*` set (CV-1/CV-5):**
- `FS.GG.UI.Scene/0.1.50-preview.1`
- `FS.GG.UI.Testing/0.1.50-preview.1`
- `FS.GG.UI.Diagnostics/0.1.50-preview.1` (transitive)

Single version literal `0.1.50-preview.1` — CV-4. No phantom `Color`/`SkillSupport` — CV-5. **Profile GREEN.**
