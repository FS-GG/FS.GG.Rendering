# T001 — Preflight (Feature 213)

Date: 2026-06-28 · Branch verified: `213-adopt-shared-build-config`

| Prerequisite | Expected | Observed | Result |
|---|---|---|---|
| Sibling source of truth `../.github/dist/dotnet/` | present (3 managed files) | `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json` present | ✅ |
| `../.github/scripts/sync-build-config.sh` | present, executable | `-rwxr-xr-x` present | ✅ |
| .NET SDK `net10.0` | installed | `dotnet --version` → `10.0.301` | ✅ |
| Working tree branch | `213-adopt-shared-build-config` | `git rev-parse --abbrev-ref HEAD` → match | ✅ |

`.gitignore` allowlist for `specs/213-adopt-shared-build-config/readiness/` added before staging;
`git check-ignore specs/213-adopt-shared-build-config/readiness/preflight.md` → NOT IGNORED (exit 1),
satisfying the Feature 168 readiness-evidence rule.

Result: **PASS** — prerequisites satisfied; adoption may proceed.
