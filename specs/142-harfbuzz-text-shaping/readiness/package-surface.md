# Feature 142 Package Surface

Status: focused package surface evidence complete; broad root `fake.sh` targets unavailable in this checkout.

- `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`: not run because no root `fake.sh` exists in this checkout. Only `template/base/fake.sh` is present.
- `dotnet build FS.GG.Rendering.slnx --no-restore`: PASS.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: PASS; additive shaped-text public types are baselined.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: FAIL due missing pre-existing readiness/package artifacts outside Feature 142 scope (`readiness/surface-baselines/*`, `scripts/controls-prelude.fsx`, `specs/035-api-discovery-names/readiness/*`, and `specs/036-archive-readiness-api-docs/readiness/*`).
- New-shaping-versus-pre-existing package-surface limitation: additive preview surface; no removals.
