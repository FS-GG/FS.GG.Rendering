# Capability Check

The documented command is unavailable:

```sh
./fake.sh build -t CapabilityCheck
```

Result: blocked, root `./fake.sh` is absent in this checkout.

Direct substitutes:

```sh
dotnet build FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx -c Release
```

Results:

- Debug solution build: passed, 0 warnings, 0 errors.
- Release solution build: passed, 0 warnings, 0 errors.
