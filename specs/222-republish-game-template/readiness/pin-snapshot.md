# T003 — Pre-release pin snapshot

The two in-repo coherent-set pins, captured before the bump (T008). Both must move together to
`V = 0.1.54-preview.1` so `template-product-tests`' local-feed restore resolves.

```
$ grep -n 'FsGgUiVersion\|<Version>' template/base/Directory.Packages.props .template.package/FS.GG.UI.Template.fsproj
.template.package/FS.GG.UI.Template.fsproj:9:    <Version>0.1.53-preview.1</Version>
template/base/Directory.Packages.props:9:    <FsGgUiVersion>0.1.53-preview.1</FsGgUiVersion>
```

| Pin | File:line | Pre-release | Target `V` |
|---|---|---|---|
| Template package version | `.template.package/FS.GG.UI.Template.fsproj:9` | `0.1.53-preview.1` | `0.1.54-preview.1` |
| `FS.GG.UI.*` coherent-set pin | `template/base/Directory.Packages.props:9` | `0.1.53-preview.1` | `0.1.54-preview.1` |

`Directory.Packages.props` maps every `FS.GG.UI.*` PackageVersion to `$(FsGgUiVersion)`, so the single
bump moves the whole set. `V` is the next preview in the established cadence (0.1.50→0.1.52→0.1.53),
strictly `> 0.1.53-preview.1` (FR-001).
