# Validation Lanes Summary

- Run id: `validation-20260619-142328-54905e`
- Overall readiness: `blocked`
- First blocking required lane: `controls`
- Aggregate status: `not-selected`
- Artifact root: `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e`
- Summary JSON: `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/summary.json`

## Required Lanes

| Lane | Role | Status | Elapsed | Log | Reason |
|------|------|--------|---------|-----|--------|
| `build` | `required` | `passed` | `00:00:04.0768851` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/build/log.txt` |  |
| `library-tests` | `required` | `passed` | `00:00:05.5516253` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/library-tests/log.txt` |  |
| `package-proof` | `required` | `passed` | `00:00:09.6445398` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/package-proof/log.txt` |  |
| `controls` | `required` | `no-progress-timeout` | `00:02:41.8572813` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/controls/log.txt` | lane exceeded no-progress timeout 00:02:00 |
| `rendering-harness` | `required` | `passed` | `00:00:06.5098467` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/rendering-harness/log.txt` |  |
| `antshowcase-sample` | `required` | `passed` | `00:01:03.6390141` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/antshowcase-sample/log.txt` |  |

## Optional and Informational Lanes

| Lane | Role | Status | Elapsed | Log | Reason |
|------|------|--------|---------|-----|--------|
| `diagnostics` | `optional` | `passed` | `00:00:02.1634925` | `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/diagnostics/log.txt` |  |

## Substitutions

- `package-proof` substitutes for `aggregate-solution`
- `controls` substitutes for `aggregate-solution`
- `rendering-harness` substitutes for `aggregate-solution`
- `antshowcase-sample` substitutes for `aggregate-solution`
- `diagnostics` substitutes for `aggregate-solution`

## Caveats

- aggregate-solution was not selected; required readiness is based on focused lanes.
- non-passing lanes are not counted as green
- package-proof is a targeted substitute for aggregate-solution
- controls is a targeted substitute for aggregate-solution
- rendering-harness is a targeted substitute for aggregate-solution
- antshowcase-sample is a targeted substitute for aggregate-solution
- diagnostics is a targeted substitute for aggregate-solution
