# Package Surface Check

The documented command is unavailable:

```sh
./fake.sh build -t PackageSurfaceCheck
```

Result: blocked, root `./fake.sh` is absent in this checkout.

Direct substitute:

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Surface baselines"
```

Result: passed, 11 tests.

Additional note:

- A broader `--filter Surface` run failed in unrelated `Feature156 compatibility package.package validation records surface and FSI evidence`; the exact `Surface baselines` package-surface test list passed.
