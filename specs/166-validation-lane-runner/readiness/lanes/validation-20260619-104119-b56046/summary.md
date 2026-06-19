# Validation Lanes Summary

- Run id: `validation-20260619-104119-b56046`
- Overall readiness: `blocked`
- First blocking required lane: `controls`
- Aggregate status: `not-selected`
- Artifact root: `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046`
- Summary JSON: `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/summary.json`

## Required Lanes

| Lane | Role | Status | Elapsed | Log | Reason |
|------|------|--------|---------|-----|--------|
| `build` | `required` | `passed` | `00:00:16.9156581` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/build/log.txt` |  |
| `library-tests` | `required` | `passed` | `00:00:02.9534097` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/library-tests/log.txt` |  |
| `package-proof` | `required` | `passed` | `00:00:10.1354343` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/package-proof/log.txt` |  |
| `controls` | `required` | `no-progress-timeout` | `00:02:40.2341510` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/controls/log.txt` | lane exceeded no-progress timeout 00:02:00 |
| `rendering-harness` | `required` | `passed` | `00:00:06.0605064` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/rendering-harness/log.txt` |  |
| `antshowcase-sample` | `required` | `passed` | `00:00:58.4823513` | `specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/antshowcase-sample/log.txt` |  |

## Optional and Informational Lanes

| Lane | Role | Status | Elapsed | Log | Reason |
|------|------|--------|---------|-----|--------|
| none |  |  |  |  |  |

## Substitutions

- `package-proof` substitutes for `aggregate-solution`
- `controls` substitutes for `aggregate-solution`
- `rendering-harness` substitutes for `aggregate-solution`
- `antshowcase-sample` substitutes for `aggregate-solution`

## Caveats

- aggregate-solution was not selected; required readiness is based on focused lanes.
- non-passing lanes are not counted as green
- package-proof is a targeted substitute for aggregate-solution
- controls is a targeted substitute for aggregate-solution
- rendering-harness is a targeted substitute for aggregate-solution
- antshowcase-sample is a targeted substitute for aggregate-solution
