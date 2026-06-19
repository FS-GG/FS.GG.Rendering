# Pack Local

The documented command is unavailable:

```sh
./fake.sh build -t PackLocal
```

Result: blocked, root `./fake.sh` is absent in this checkout.

Direct substitute:

```sh
dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local
```

Result: passed. Packages were written to `~/.local/share/nuget-local`.

Warnings:

- Existing missing-readme warnings were emitted for `FS.GG.UI.Color`, `FS.GG.UI.DesignSystem`, `FS.GG.UI.Themes.AntDesign`, and `FS.GG.UI.Themes.Default`.
