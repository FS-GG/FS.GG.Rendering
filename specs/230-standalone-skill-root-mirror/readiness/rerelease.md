# Re-release + non-goals (T011/T012) — Feature 230

## Version
- `.template.package/FS.GG.UI.Template.fsproj`: `0.1.59-preview.1` → `0.1.60-preview.1` (re-release vehicle).
- Framework `FsGgUiVersion` unchanged at `0.1.58-preview.1` (no `src/` change; Feature 209 coherence green).
- Local pack proved installable (dev `0.1.60-dev230.1`). Org-feed publish + registry flip + Templates re-pin
  are cross-repo follow-ons (publish-before-flip), tracked on the Coordination board.

## Non-goals held
- No `src/**` / `.fsi` change (Tier 2). Files: `.template.config/template.json`, template fsproj (version),
  the two Feature 20{4,19} gates, `validate-lifecycle-template.fsx`, `CLAUDE.md`, `.specify/feature.json`,
  `specs/230/**`, regenerated `specs/204-.../readiness/lifecycle-template-validation.md`.
- `sdd`/`none` behavior unchanged from Feature 229 (`.claude/`/`.codex/` product skills = 0) → Templates#47 stays unblocked.
- No skill file added/removed (only scaffold placement changed) → `docs/reports/skills-parity.md` NOT regenerated.
- Full baseline 21/21 green.

## Testing caveat (recorded)
- `dotnet new` resolves the HIGHEST installed template version. The Feature 229 `0.1.59-preview.1` packed to the
  local feed during the 229 merge shadows a `0.1.59-dev*` build; live tests must install a dev version that sorts
  above it (used `0.1.60-dev230.1`) or uninstall the feed copy first.
