# Contract: Deprecation notice / recorded action (public feed)

Copy-ready content for the **public package feed (nuget.org)** — a repository this feature does not
own. Delivered as content **plus a recorded action**, not applied from here (Constitution
Principle VI; FR-008/009; SC-006/007). The R8 implementation materializes this as a doc in the tree
(e.g. `docs/bridge/package-deprecation-notice.md`) and updates `docs/bridge/old-repo-redirect.md`
Block B to match.

## Recorded action — NOT yet applied

- **Target**: the published `FS.Skia.UI.*` package listings on nuget.org (10 runtime packages + the
  `FS.Skia.UI.Template` package).
- **Status**: **NOT yet applied.** This repository cannot apply it: the public feed is owned by the
  package publisher, outside this working tree.
- **Precondition (publish-before-deprecate, FR-007)**: the replacement `FS.GG.UI.*` packages
  (`0.1.0-preview.1`) MUST be published/available **before** these deprecations are actioned.
- **Owner action required**: whoever holds publish rights (1) publishes the new `FS.GG.UI.*`
  packages, then (2) marks each old ID below **deprecated with the forward pointer** — **not**
  deleted or unlisted, so existing version pins keep resolving.
- **Honesty note**: until the owner applies it, do not describe the old packages as deprecated. This
  notice is the source of truth for *what* to apply and that it is *outstanding*.

## Old → new mapping (deprecate each old ID with this forward pointer)

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

## Block — per-package deprecation message (paste into each old `FS.Skia.UI.*` listing)

```markdown
This package has been renamed. Active development continues as **FS.GG.UI.<Module>**:
https://www.nuget.org/packages/FS.GG.UI.<Module>

`FS.Skia.UI.<Module>` is deprecated and frozen at its last published version; it is **not** deleted,
so existing version pins keep resolving. Please migrate to `FS.GG.UI.<Module>` — the public API is
unchanged apart from the `FS.Skia.UI` → `FS.GG.UI` namespace prefix.
```

(Substitute `<Module>` per row above; for the template, `FS.GG.UI.Template`.)

## Apply checklist (for the owner)

1. Publish all eleven `FS.GG.UI.*` `0.1.0-preview.1` packages to nuget.org (replacements must exist
   first).
2. For each old `FS.Skia.UI.*` listing: mark **Deprecated**, set the alternate package to its
   `FS.GG.UI.*` replacement, and prepend the deprecation message block. Do **not** unlist/delete.
3. Update `docs/bridge/old-repo-redirect.md` Block B (now superseded) if not already aligned.
4. Flip this notice's status from *NOT yet applied* to *applied* **only after** steps 1–2 are done.
