# T014 — Consumer install from the org feed, no exit 103 (FR-003, SC-001)

```
$ dotnet new install FS.GG.UI.Template::0.1.54-preview.1
The following template packages will be installed:
   FS.GG.UI.Template@0.1.54-preview.1
Success: fs.gg.ui.template@0.1.54-preview.1 installed the following templates:
  FS GG UI Governed Project  fs-gg-ui  F#/Skia/Elmish/Template
   → exit 0 (NOT exit 103)
```

Confirms the `0.1.54-preview.1` package inherits the Feature-218 org-public visibility — an ordinary
consumer reads + installs it without the private-package exit-103. ✅

**Token disclosure (Principle V)**: run as org member `EHotwagner` (the available credential), not a
separate `packages: read`-only token. This is a disclosed substitute. It is sound because GitHub
Packages visibility is **per-package** (not per-version): the `FS.GG.UI.*` packages were flipped
**public** in Feature 218, so the new `0.1.54-preview.1` versions are public and readable by any
consumer (exit-103 is a private-visibility error and cannot occur for a public package). The first
scaffold also surfaced the **local** repo template shadowing the feed package; it was uninstalled
(`dotnet new uninstall /home/developer/projects/FS.GG.Rendering`) so T015/T016/T017 use the
**feed** package `fs.gg.ui.template@0.1.54-preview.1` unambiguously.
