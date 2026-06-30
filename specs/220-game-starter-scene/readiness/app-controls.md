# T025 — The controls showcase stays available as the explicit `app` profile (SC-006 controls half)

`dotnet new fs-gg-ui --profile app --name Product` after all 220 edits:

- Generated `Program.fs` default `| None ->` branch →
  `ControlsElmish.runInteractiveApp viewerOptions interactiveHost` (the pointer-aware controls host,
  unchanged).
- `dotnet test tests/Product.Tests/Product.Tests.fsproj -c Debug`:

```
Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30 - Product.Tests.dll (net10.0)
```

The controls showcase still generates and passes its governance tests (same 30/30 as the pre-change
reference in [profile-matrix-probe.md](./profile-matrix-probe.md)). `app` is now the **explicit,
opt-in** controls option (re-described in `template/profiles/app.yml`, T023) — no longer "the"
default — with **no change to its generated output** (FR-006). **SC-006 controls half: GREEN.**
</content>
