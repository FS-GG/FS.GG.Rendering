# Old-repo redirect notice (copy-ready)

> Part of the Stage R7 bridge. This file holds **copy-ready content for a repository this one does
> not own** — the archived `EHotwagner/FS-Skia-UI` and its NuGet package pages. It is delivered as
> content plus a recorded action, not applied from here.

## Recorded action — NOT yet applied

- **Targets**: (1) the README of the archived source repo
  [`EHotwagner/FS-Skia-UI`](https://github.com/EHotwagner/FS-Skia-UI); (2) the NuGet package pages
  for `FS.Skia.UI.*`.
- **Status**: **NOT yet applied.** This repository (`FS.GG.Rendering`) cannot apply it: the source
  repo is **archived (read-only)** and outside this working tree, and the NuGet descriptions are
  owned by the package publisher.
- **Owner action required**: whoever holds write access applies the blocks below. Applying the
  README banner requires temporarily **un-archiving** the old repo (see the apply checklist).
- **Honesty note**: until the owner applies it, do not describe the old repo as redirected. This
  file is the source of truth for *what* must be applied and that it is *outstanding*.

## Block A — old-repo README banner (paste at the very top of `FS-Skia-UI/README.md`)

```markdown
> # 📦 This project has moved
>
> The rendering product (the F# / SkiaSharp UI framework) now lives in
> **[FS.GG.Rendering](https://github.com/FS-GG/FS.GG.Rendering)** — that is the canonical home for
> all current development, issues, docs, and releases.
>
> This repository is **archived**: kept as source inventory and provenance only. The description
> below is historical and no longer reflects the live product (which renders over **OpenGL (GL)**,
> not Vulkan, and does not require a governance platform). Start at
> [FS.GG.Rendering](https://github.com/FS-GG/FS.GG.Rendering) and the
> [FS-GG org profile](https://github.com/FS-GG/.github).
>
> Imported from this repo at commit `f759f399` — see
> [`PROVENANCE.md`](https://github.com/FS-GG/FS.GG.Rendering/blob/main/PROVENANCE.md) for the full
> import map.
```

## Block B — package-page deprecation/redirect (for each `FS.Skia.UI.*` NuGet description)

```markdown
Active development of this package has moved to the FS-GG org:
https://github.com/FS-GG/FS.GG.Rendering

The package identity (`FS.Skia.UI.*`) is unchanged — this is a repository move, not a rename. Any
future rename is a separate, later release decision and has not happened.
```

> Note: Block B asserts **no rename** — package identity is retained as `FS.Skia.UI.*` (see
> [`package-identity-migration.md`](./package-identity-migration.md)). Do not edit Block B to claim
> a new package ID; that would be a Stage R8 decision, not part of R7.

## Apply checklist (for the owner)

1. **Un-archive** `EHotwagner/FS-Skia-UI` (GitHub → Settings → Danger Zone → Unarchive).
2. Paste **Block A** at the top of `FS-Skia-UI/README.md`; commit.
3. **Re-archive** the repository.
4. For each published `FS.Skia.UI.*` package, prepend **Block B** to the NuGet package description
   (or the `<Description>` in the source if a republish is planned — note: republishing is a
   release action, not part of R7).
5. Tick this notice's status from *NOT yet applied* to *applied* **only after** steps 1–4 are done,
   and update any reference here accordingly.
