# Package & template identity — migration note

> Part of the Stage R7 bridge. Records what happened to package and template **identity** across the
> repository move. Short version: **nothing changed.** Identity is retained as `FS.Skia.UI.*`.

## The move did not rename anything

Moving the rendering product from `EHotwagner/FS-Skia-UI` to
[`FS.GG.Rendering`](https://github.com/FS-GG/FS.GG.Rendering) is a **repository** change, not a
**package** change. Every package ID, root namespace, assembly name, and the template package ID is
**retained exactly as imported**. A consumer of any `FS.Skia.UI.*` package is unaffected by the
move — same IDs, same namespaces.

## Retained identities

| Identity | Value | Status |
|---|---|---|
| Runtime package IDs | `FS.Skia.UI.Color`, `FS.Skia.UI.Scene`, `FS.Skia.UI.Layout`, `FS.Skia.UI.Input`, `FS.Skia.UI.KeyboardInput`, `FS.Skia.UI.SkiaViewer`, `FS.Skia.UI.Elmish`, `FS.Skia.UI.Controls`, `FS.Skia.UI.Controls.Elmish`, `FS.Skia.UI.Testing` | Retained — unchanged by the move |
| Template package ID | `FS.Skia.UI.Template` | Retained — unchanged by the move |
| Root namespaces / assembly names | `FS.Skia.UI.<Module>` | Retained — unchanged by the move |
| Pack output location | `~/.local/share/nuget-local/` | Unchanged |

(Source of truth for the IDs: each `src/<Module>/*.fsproj` and `Directory.Build.props`; see also the
import path map in [`PROVENANCE.md`](../../PROVENANCE.md). Only ownership metadata —
`Authors`/`Company`/repository URLs — was re-pointed to FS-GG at import; the package *identity* was
not touched.)

## Rename is deferred to Stage R8 — and is not decided here

Whether to rebrand to a new identity such as `FS.GG.UI.*` is a separate, explicit release decision
deferred to migration **Stage R8**. The decision record is
[`docs/product/decisions/0001-package-identity.md`](../product/decisions/0001-package-identity.md)
(status: *deferred*).

**This note neither decides nor begins a rebrand.** It only records the current, retained mapping so
that:

- nobody mistakes "the repo moved" for "the packages were renamed"; and
- if R8 later chooses a rename, it starts from this documented baseline and publishes replacement
  packages before deprecating the old IDs (per the constitution's package-identity constraint).
