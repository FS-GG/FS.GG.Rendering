# T018 — No exit 127 (SC-003, INV-6) — GREEN (proven live)

**Captured**: 2026-06-29. Clean-dir install of `V` **from the feed** (local in-repo template
registration uninstalled first to remove the shadow confound noted in `baseline-probe.md`).

```
$ dotnet new uninstall /home/developer/projects/FS.GG.Rendering   # remove local shadow
$ dotnet new uninstall FS.GG.UI.Template                          # remove stale 0.1.52 reg
$ dotnet new install FS.GG.UI.Template@0.1.53-preview.1 --force
Success: fs.gg.ui.template@0.1.53-preview.1 installed the following templates:
  FS GG UI Governed Project  (fs-gg-ui)  F#/Skia/Elmish/Template
INSTALL_EXIT=0

$ dotnet new fs-gg-ui --productName Acme --output ./Acme
The template "FS GG UI Governed Project" was created successfully.
SCAFFOLD_EXIT=0

$ ls ./Acme
AGENTS.md  Acme.slnx  CLAUDE.md  Directory.Build.props  Directory.Packages.props
README.md  build.cmd  build.fsx  build.sh  docs ...
```

✅ **`--productName` is honored — exit 0, NOT 127.** The packed `0.1.53-preview.1` template carries the
Feature-217 `productName` symbol, and `./Acme` scaffolded successfully. Gate A (publish half of
FR-004) is **proven live** for `V`.

> The output names the SDD scaffold-provider form (`--productName`, `--output`, no `-n`), exactly the
> invocation that exit-127'd against the pre-217 `0.1.52-preview.1` (see `baseline-probe.md`).
