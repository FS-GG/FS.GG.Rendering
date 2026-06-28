# T022 — No-regression confirmation (Feature 212)

Re-ran the comprehensive baseline after all Feature 212 changes and diffed against the T001 baseline.

- `scripts/baseline-tests.fsx` → `baseline.md` (T001, before) and `baseline-after.md` (T022, after).
- **Project set identical**: same 21 discovered `*.Tests.fsproj` (tests/ + samples/) in both runs
  (`diff` → SAME project set).
- **All green both runs**: 21 PASS before, 21 PASS after; **0 FAIL** in each.
- No project moved red; no project dropped out. The edits (4 new `template/base/` files, the
  `build.fsx` pass-through targets, `.template.config` unchanged, `release.yml` gate, `.gitignore`
  allowlist) introduce **no regression** in the framework test suite — including `Build.Tests` (the
  build-orchestration tests) which stayed green.

Both baseline summaries are committed under this readiness folder for audit.
</content>
