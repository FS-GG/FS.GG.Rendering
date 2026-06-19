# Evidence Summary

Command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/171-second-antshowcase-sample/readiness
```

Result: PASS with caveats.

- seed: 1
- pages exercised: 19
- screenshots: 19/19 real screenshot records reported `provesScreenshot=true`
- coverage status: clean
- interaction status: passing via test suite and scripted state evidence
- template status: passing via test suite
- theme-switch status: passing via test suite
- visual review status: blocked pending reviewer classification
- unresolved visual findings: 0
- synthetic evidence: false for the representative screenshot records generated in this run

The per-page records are under `specs/171-second-antshowcase-sample/readiness/1/`.

Visible caveat: representative evidence is not authoritative for pixel-level Ant fidelity,
live pointer hit-testing beyond seeded scripts, or chart/graph semantics beyond seeded sample
data. Final visual acceptance still requires reviewer classification for all preferred and
minimum targets.
