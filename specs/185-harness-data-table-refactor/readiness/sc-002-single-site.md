# SC-002 — Single-Site Feature Add (walkthrough)

**Claim (SC-002):** adding a harness feature is a single `catalog` descriptor row — it compiles and
gets a CLI command with **no** new per-feature handler function.

## Walkthrough (performed, then reverted)

Added one row to `FeatureCatalog.catalog` in `tools/Rendering.Harness/FeatureCatalog.fs`:

```fsharp
descriptor 200 "200-sc002-single-site-sample"
    [ ValidationSummary; CompatibilityLedger ]
    (hdr 200 [ "Validation Summary"; "Compatibility Ledger" ])
    emptyConfig
```

No other edit. `renderHooksFor 200` falls through to `noRenderHooks` (default), so the row needs no
hook either.

## Result

- `dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release` → **Build succeeded** —
  the single row is sufficient; the type system accepted it with no new function.
- `dotnet run … -- compositor-readiness --feature 200 --out /tmp/sc002-200` → **exit 0**, emitted
  `validation-summary.md` + `compatibility-ledger.md`. The feature was dispatched through the
  descriptor-keyed command table (`descriptorByAlias` → `runReadiness`), **without** writing a
  `runFeature200*Cmd` handler.

Then the sample row was removed and the harness rebuilt green — `git diff FeatureCatalog.fs` empty
(pristine). SC-002 holds: one descriptor row = one new feature, auto-wired CLI command, zero new
handler code.
