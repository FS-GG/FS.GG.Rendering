# US2 Responsiveness Evidence

- Added `--all-interactive` and mutual exclusion with `--page`.
- Summary budgets are `100 ms` p95 and `150 ms` max.
- `records.jsonl` includes action type, input kind, control ids, expected/observed visible result, and blocked acceptance status.
- Light run: `specs/172-fix-mouse-lag/readiness/responsiveness/resp-20260619-190047-218eb7/summary.json`, exit code `4`.
- Dark run: `specs/172-fix-mouse-lag/readiness/responsiveness/resp-20260619-190052-47cb2f/summary.json`, exit code `4`.
- Caveat: both runs are `environment-limited` because no accepted live presentation boundary was available.

Logs:
- `specs/172-fix-mouse-lag/readiness/logs/responsiveness-live-commands.log`
- `specs/172-fix-mouse-lag/readiness/logs/package-feed-sample-tests.log`
