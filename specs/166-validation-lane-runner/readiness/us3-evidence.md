# US3 Evidence Preservation

The lane runner writes one run-id child directory per invocation.

Single-lane ready run:

```text
specs/166-validation-lane-runner/readiness/lanes/validation-20260619-103826-f55357/
```

Required-lane blocked run:

```text
specs/166-validation-lane-runner/readiness/lanes/validation-20260619-104119-b56046/
```

Each run contains `summary.md`, `summary.json`, and per-lane `log.txt`,
`result.json`, and `diagnostics.md`. The blocked required run keeps
`aggregate-solution` visible as not selected and records targeted substitutes in
the caveats instead of converting the required set to green.
