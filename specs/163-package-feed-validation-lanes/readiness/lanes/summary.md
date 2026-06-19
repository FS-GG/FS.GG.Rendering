# Validation Lanes Summary

- Overall readiness: `ready`
- Local feed: `/home/developer/.local/share/nuget-local`
- Package cache: `specs/163-package-feed-validation-lanes/readiness/lanes/package-proof/nuget-cache`
- Artifact root: `specs/163-package-feed-validation-lanes/readiness/lanes`

## Source Rules

- FS.GG.UI.* -> nuget-local
- * -> nuget.org

## Lane Status

| Lane | Status | Required | Log | Diagnostics |
|------|--------|----------|-----|-------------|
| `package-proof` | `passed` | `True` | `specs/163-package-feed-validation-lanes/readiness/lanes/package-proof/log.txt` | `` |
| `antshowcase-sample` | `passed` | `True` | `specs/163-package-feed-validation-lanes/readiness/lanes/antshowcase-sample/log.txt` | `` |
| `controls` | `passed` | `True` | `specs/163-package-feed-validation-lanes/readiness/lanes/controls/log.txt` | `` |
| `rendering-harness` | `passed` | `True` | `specs/163-package-feed-validation-lanes/readiness/lanes/rendering-harness/log.txt` | `` |

## Caveats

- aggregate-solution is optional and reported separately from focused lanes
