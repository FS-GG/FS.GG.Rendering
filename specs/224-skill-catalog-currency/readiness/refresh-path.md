# Refresh path (T019 / FR-007 / SC-004)

**Option A (chosen, research R2): hand-maintained-under-check.** There is no generator; the catalog
is corrected by hand and the currency check enforces it.

## How to refresh the catalog after a skill is added/renamed/removed

1. Edit `template/base/docs/skillist-reference.md` so every `` `id` `` row names a skill the product
   ships, with a consumer path (`.agents/skills/<id>/SKILL.md` or `.claude/skills/<id>/SKILL.md`).
2. Run the check until green:

   ```sh
   dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency
   ```

The check fails — naming the **id, doc, and line** — if any row dangles or uses a framework-only
path. This is also documented in the honest header at the top of `skillist-reference.md`.

**SC-004 (refresh passes first run):** the hand-edited catalog produced by this feature passes the
check on the first run with no further edits — verified green (6/6) in `quickstart-evidence.md`.

Option B (a `scripts/refresh-skill-catalog.fsx` generator that emits rows from `SkillParity`
discovery, with the check asserting committed == regenerated) was **not** taken: the row set is small
and stable, and a generator adds a script + a produced-vs-framework path mapping for no real durability
gain over the check (Principle III). See `produced-surface.md` (R2 decision).
