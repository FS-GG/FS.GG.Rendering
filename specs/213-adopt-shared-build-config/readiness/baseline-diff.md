# T024 — After-adoption baseline diff vs T002 (Feature 213)

Date: 2026-06-28

| | Projects | Green | Red |
|---|---|---|---|
| Pre-adoption (T002, `baseline.md`) | 21 | 21 | 0 |
| Post-adoption (T024, `baseline-after.md`) | 21 | 20 | 1 |

## The single red is an environment flake, not a regression — CAVEAT

`samples/ControlsGallery/ControlsGallery.Tests` reported FAIL in the discovery-runner pass with **no
test summary line** ("build/restore failure"). Re-running that exact project in isolation:

```
dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj -m:1 -p:BuildInParallel=false
→ Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34   exit 0
```

→ **34/34 green on isolated re-run.** The runner-level failure matches the `System.OutOfMemoryException`
/ `MSB4018` / `MSB4181` thread-start failures seen when many projects build in parallel on this dev box
(memory pressure). It is an **environment limitation**, reproducible only under the parallel runner and
absent on a single-project build. Per the repository evidence rules this is disclosed as a visible
caveat rather than summarized as fully green.

## No-regression conclusion

Every test project passes when given adequate build resources (the 20 that ran green in the runner +
ControlsGallery green on isolated retry). No new red is attributable to the shared-build-config
adoption; the property/pin partition preserved all repo-specific behavior.
