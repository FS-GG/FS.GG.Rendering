# Collision helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/Collision.fs` — a small collision pass (broad-phase over `SpatialGrid`,
  narrow-phase over `Geometry`, and a response rule) that you **own and edit**.

Unlike the framework packages (`FS.GG.UI.Scene`/`.Canvas`), this is **not** a referenced API — it is
copied into your product for you to change: tweak the response rule, add collision layers, or delete
the file entirely. Its `Compile` item is `Exists`-guarded, so deleting `Collision.fs` keeps the build
green (`Product.fsproj` stays a "durable — do not touch" file).

Detection still reuses the framework primitives — the fragment adds only the game-opinionated
*response* layer that does not belong in a frozen package.

See the **`fs-gg-collision`** skill for the full detection → broad-phase → response guidance and the
common footguns. Guidance-only; no backing package (`no-public-surface`).
