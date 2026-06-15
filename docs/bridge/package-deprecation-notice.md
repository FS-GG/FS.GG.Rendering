# Package deprecation notice — `FS.Skia.UI.*` → `FS.GG.UI.*` (R8)

**Status: NOT yet applied.** This is a **recorded action** with copy-ready content for the public
package feed (nuget.org). This repository **cannot** apply it: the public feed is owned by the
package publisher, outside this working tree (Constitution Principle VI — no overclaiming; FR-008/009,
SC-006/007). Until the owner applies it, the old packages are **not** described as deprecated — this
notice is the source of truth for *what* to apply and that it is *outstanding*.

Source contract: [`specs/008-rebrand-package-identity/contracts/deprecation-notice.md`](../../specs/008-rebrand-package-identity/contracts/deprecation-notice.md).
Decision record: [`docs/product/decisions/0001-package-identity.md`](../product/decisions/0001-package-identity.md).

## Precondition — publish before deprecate (FR-007)

The replacement `FS.GG.UI.*` packages (`0.1.0-preview.1`) MUST be published/available **before** these
deprecations are actioned. In this repository the eleven replacements have been packed to the local
feed (`~/.local/share/nuget-local/`); publishing them to nuget.org is the owner's release step.

The old `FS.Skia.UI.*` IDs are **deprecated, not deleted/unlisted** — each freezes at its last
published version so existing version pins keep resolving.

## Old → new mapping (deprecate each old ID with the forward pointer)

| Deprecate (old ID, frozen at last published version) | Replacement (publish first) |
|---|---|
| `FS.Skia.UI.Color`           | `FS.GG.UI.Color`           |
| `FS.Skia.UI.Scene`           | `FS.GG.UI.Scene`           |
| `FS.Skia.UI.Layout`          | `FS.GG.UI.Layout`          |
| `FS.Skia.UI.Input`           | `FS.GG.UI.Input`           |
| `FS.Skia.UI.KeyboardInput`   | `FS.GG.UI.KeyboardInput`   |
| `FS.Skia.UI.SkiaViewer`      | `FS.GG.UI.SkiaViewer`      |
| `FS.Skia.UI.Elmish`          | `FS.GG.UI.Elmish`          |
| `FS.Skia.UI.Controls`        | `FS.GG.UI.Controls`        |
| `FS.Skia.UI.Controls.Elmish` | `FS.GG.UI.Controls.Elmish` |
| `FS.Skia.UI.Testing`         | `FS.GG.UI.Testing`         |
| `FS.Skia.UI.Template`        | `FS.GG.UI.Template`        |

## Per-package deprecation message (paste into each old `FS.Skia.UI.*` listing)

```markdown
This package has been renamed. Active development continues as **FS.GG.UI.<Module>**:
https://www.nuget.org/packages/FS.GG.UI.<Module>

`FS.Skia.UI.<Module>` is deprecated and frozen at its last published version; it is **not** deleted,
so existing version pins keep resolving. Please migrate to `FS.GG.UI.<Module>` — the public API is
unchanged apart from the `FS.Skia.UI` → `FS.GG.UI` namespace prefix.
```

(Substitute `<Module>` per row above; for the template, `FS.GG.UI.Template`.)

## Apply checklist (for the owner with publish rights)

1. Publish all eleven `FS.GG.UI.*` `0.1.0-preview.1` packages to nuget.org (replacements must exist
   first).
2. For each old `FS.Skia.UI.*` listing: mark **Deprecated**, set the alternate package to its
   `FS.GG.UI.*` replacement, and prepend the deprecation message block. Do **not** unlist/delete.
3. Confirm [`docs/bridge/old-repo-redirect.md`](./old-repo-redirect.md) Block B is aligned with this
   notice.
4. Flip this notice's status from *NOT yet applied* to *applied* **only after** steps 1–2 are done.
