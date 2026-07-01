# T017 / SC-005 — Regression-guard transcripts (fail pre-fix, pass post-fix)

The corrected Feature 204/219 gates are the repo-owned backstop that would have caught Feature 227's leak.

## Post-fix — GREEN

`dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` on the fixed template:

```
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14 - Package.Tests.dll (net10.0)
```

Report (`specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`, `provenance: live`):
- `gated-condition: lifecycle-workspace sources (incl. .claude/skills/ product mirror) carry lifecycle == "spec-kit"; framework product-skill sources (.agents/skills/) are profile-gated and lifecycle-independent`
- `sdd/<p>: framework-skills-present=ok` **and** `sdd/<p>: claude-product-skills=0` for every profile
- `none/<p>: framework-skills-present=ok` **and** `none/<p>: claude-product-skills=0` for every profile
- `spec-kit/<p>: generate=pass diff-vs-today=none` (GV-3 byte-identity)
- Feature 204 `gatedSourceAudit`: `framework=9`, `workspace>=15`, `0` violations; Feature 219 G-EMIT surface-specific.

## Pre-fix — RED (SC-005: the guard actually fails on the leaky shape)

Reverting **only** `.template.config/template.json` to `HEAD` (9 ungated `.claude/skills/` sources) and
re-running the **corrected** gates:

```
Failed Feature204 … GV-2 sources partition into framework-skill / lifecycle-workspace / product
  gating violations: lifecycle-workspace template/product-skills/fs-gg-scene/ -> .claude/skills/fs-gg-scene/ missing condition;
  … fs-gg-symbology, fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input, fs-gg-ui-widgets,
  fs-gg-styling, fs-gg-layout, fs-gg-testing … Should be empty.

Failed Feature219 … G-EMIT framework skill sources are profile-gated; .claude/skills/ mirror is spec-kit-only
  fs-gg-scene -> .claude/skills/fs-gg-scene/ (.claude/skills/ mirror) must be spec-kit-gated.
  Expected subject string '(profile == "app" || … )' to contain substring 'lifecycle == "spec-kit"'.

Failed!  - Failed: 2, Passed: 12, Total: 14
```

The guard names every offending `.claude/skills/` path — a future re-leak (an added product skill mirrored
into `.claude/skills/` without the spec-kit clause) fails GV-2 and G-EMIT. `template.json` was restored to
the fixed state immediately after (0 ungated `.claude/skills/` sources).
