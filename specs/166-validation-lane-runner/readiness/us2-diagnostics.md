# US2 Diagnostics Evidence

Focused tests cover pass, fail, total timeout, no-progress timeout,
infrastructure error, cancellation, and MVU transition effects.

Required-lane run evidence:

```text
specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/summary.md
```

Observed blocker:

```text
controls -> no-progress-timeout
reason -> lane exceeded no-progress timeout 00:02:00
last activity -> Skipped Typed standard controls contract.FSI transcript expectations cover typed front doors and custom escape hatch
```

A direct `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
--no-restore` run reproduced the long quiet stretch and was manually canceled
after several minutes. This confirms the runner is exposing the historical
operator-risk condition instead of reporting it as green.
