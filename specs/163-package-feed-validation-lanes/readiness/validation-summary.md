# Feature 163 Validation Summary

Overall readiness: `ready` for focused Feature 163 lanes.

Reviewer entry points:

- Package proof: `package-proof/package-versions.md`, `package-proof/package-pins.md`,
  `package-proof/source-proof.md`, `package-proof/source-proof.json`
- Lane summary: `lanes/summary.md`, `lanes/summary.json`
- Diagnostics: `diagnostics/`
- Compatibility: `compatibility-ledger.md`
- Package validation: `package-validation.md`
- Regression validation: `regression-validation.md`
- Focused lane evidence: `lanes/summary.md`

Selected samples:

- `samples/AntShowcase`

Local feed and cache:

- Local feed: `~/.local/share/nuget-local/`
- Isolated proof cache: `package-proof/nuget-cache`

Required lane statuses:

| Lane | Status | Evidence |
|------|--------|----------|
| `package-proof` | `passed` | `lanes/package-proof/result.json` |
| `antshowcase-sample` | `passed` | `lanes/antshowcase-sample/result.json`, `lanes/antshowcase-sample/TestResults/antshowcase-sample.trx` |
| `controls` | `passed` | `lanes/controls/result.json`, `lanes/controls/TestResults/controls.trx` |
| `rendering-harness` | `passed` | `lanes/rendering-harness/result.json`, `lanes/rendering-harness/TestResults/rendering-harness.trx` |
| `aggregate-solution` | `not-run` | optional lane, not selected for focused evidence |

Aggregate solution validation is displayed separately from focused lanes. A skipped, canceled,
timed-out, hung, environment-limited, or not-run aggregate lane is not represented as a completed
full-solution validation.

Reviewer checklist:

- Package versions visible: `package-proof/package-versions.md`.
- Selected sample pins visible: `package-proof/package-pins.md`.
- Source rules visible: `samples/AntShowcase/nuget.config` and `package-proof/source-rules.nuget.config`.
- Lane statuses visible: `lanes/summary.md`.
- Caveats visible: aggregate full-solution validation is optional and separate from focused lanes.
