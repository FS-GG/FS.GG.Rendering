# T020 — Profile/lifecycle/designSystem coverage (Feature 212, FR-008 / Quickstart E)

Swept 2026-06-28. For each profile × lifecycle: scaffold `--name P`, confirm the four root
artifacts emit, then stock `dotnet build` + `dotnet test`; `dotnet run` asserted for the
runnable **app** profile only (headless, display vars cleared).

| profile | lifecycle | artifacts | build | test | run (app) |
|---|---|---|---|---|---|
| app | spec-kit | ✅ | ✅ | ✅ | ✅ exit0 |
| app | sdd | ✅ | ✅ | ✅ | ✅ exit0 |
| app | none | ✅ | ✅ | ✅ | ✅ exit0 |
| headless-scene | spec-kit | ✅ | ✅ | ✅ | n/a |
| headless-scene | sdd | ✅ | ✅ | ✅ | n/a |
| headless-scene | none | ✅ | ✅ | ✅ | n/a |
| governed | spec-kit | ✅ | ✅ | ✅ | n/a |
| governed | sdd | ✅ | ✅ | ✅ | n/a |
| governed | none | ✅ | ✅ | ✅ | n/a |
| sample-pack | spec-kit | ✅ | ✅ | ✅ | n/a |
| sample-pack | sdd | ✅ | ✅ | ✅ | n/a |
| sample-pack | none | ✅ | ✅ | ✅ | n/a |

## designSystem byte-neutrality (wcag vs ant) of the four root artifacts

```
P.slnx: IDENTICAL (byte-neutral)
global.json: IDENTICAL (byte-neutral)
build.sh: IDENTICAL (byte-neutral)
build.cmd: IDENTICAL (byte-neutral)

All four root artifacts are byte-identical across designSystem (wcag == ant).
```
