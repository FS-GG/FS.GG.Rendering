# Visibility helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/Visibility.fs` — a 2D-visibility pass (an exact segment-vs-bound-box cull,
  ray-segment intersection, and the angular sweep that builds the visibility polygon) that you **own and
  edit**.

Unlike the framework packages (`FS.GG.UI.Scene`/`.Canvas`), this is **not** a referenced API — it is
copied into your product for you to change: tweak the sight radius, cone the field of view, swap the
polygon output for a per-cell fog-of-war mask, or delete the file entirely. Its `Compile` item is
`Exists`-guarded, so deleting `Visibility.fs` keeps the build green (`Product.fsproj` stays a "durable —
do not touch" file).

The geometry vocabulary still reuses the framework primitives (`Point`/`Rect`) — the fragment adds only
the cull, the ray-segment intersection, and the angular sweep that do not belong in a frozen package.

See the **`fs-gg-visibility`** skill for the full segment-model → cull → sweep → polygon guidance, the
applications (line-of-sight, field-of-view, fog-of-war, 2D lighting), and the common footguns. Algorithm
reference: <https://www.redblobgames.com/articles/visibility/>. Guidance-only; no backing package
(`no-public-surface`).
