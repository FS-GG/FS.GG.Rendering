# Baseline Responsiveness

- Command: `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --page buttons --out specs/172-fix-mouse-lag/readiness/responsiveness/baseline-existing --json`
- Exit code: `4`
- Output summary: `specs/172-fix-mouse-lag/readiness/responsiveness/baseline-existing/resp-20260619-183650-776e57/summary.json`
- Readiness: `environment-limited`
- Caveat: this was the pre-change deterministic/headless substitute path and did not measure a live GL presentation boundary.

Log: `specs/172-fix-mouse-lag/readiness/logs/baseline-responsiveness.log`
