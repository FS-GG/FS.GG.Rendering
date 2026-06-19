# Visual Review Summary

Preferred command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/preferred
```

Minimum command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/minimum
```

Result: BLOCKED pending reviewer classification.

- preferred targets: 38/38 complete screenshots
- minimum targets: 38/38 complete screenshots
- total required targets: 76
- total complete screenshot records: 76
- reviewer classifications: missing
- unresolved visual findings: 0
- final visual acceptance: not accepted

The generated target summaries are:

- `specs/171-second-antshowcase-sample/readiness/preferred/summary.md`
- `specs/171-second-antshowcase-sample/readiness/minimum/summary.md`

This run produced screenshot evidence, but the feature contract requires reviewer
classification before visual fidelity can be accepted.
