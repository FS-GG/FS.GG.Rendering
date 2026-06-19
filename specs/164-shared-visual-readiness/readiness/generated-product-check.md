# Generated Product Check

The documented command is unavailable:

```sh
./fake.sh build -t GeneratedProductCheck
```

Result: blocked, root `./fake.sh` is absent in this checkout.

Direct substitute attempted:

```sh
dotnet test template/base/tests/Product.Tests/Product.Tests.fsproj
```

Result: blocked by pre-existing template/base compile errors in `template/base/src/Product/Model.fs`, including duplicate `Model`/`Msg` definitions and missing record fields such as `CanSave`, `Page`, `Interactions`, `ContentColumn`, `ContentRow`, `TickCount`, and `LastInput`.

This blocker is not introduced by Feature 164; the Feature 164 changed files do not touch `template/base`.
