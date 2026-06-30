# Merge ledger — Feature 225 (de-leak product skill vocabulary)

**Merge type:** content-only squash-merge to `main`. **No package bump.**

## Why no bump (Post-Merge Package Evidence)

The diff touches **no packable `FS.GG.UI.*` project**:

- `template/product-skills/fs-gg-*/SKILL.md` (×7) — `dotnet new` template content.
- `tests/Package.Tests/` — the new leak guard + project wiring (test project, not packed here).
- `tools/Rendering.Harness/SkillParity.fs` — internal harness tool (not an `FS.GG.UI.*` package);
  data-only guidance-rule rescope, no public-surface / `.fsi` / surface-baseline delta.

So there is no local-feed pack, sample-pin alignment, or `refresh-local-feed-and-samples` /
`package-feed` proof to record — none apply. Same pattern as siblings #35 (Feature 223) and #36
(Feature 224): package-content fixes that ride the next coherent `fs-gg-ui-template` republish, not a
standalone release.

## Delivery dependency (FR-008 / FR-009)

The de-leaked skills reach consumers only as package content via the **next `fs-gg-ui-template`
republish** (successor to the closed #33) + the downstream `providers/rendering.providers.yml` pin
alignment (FS.GG.Templates#8 pattern). This feature produces the **content + guard**, not the publish.
Recorded on board item #37 (→ In review) and parent epic #34 (→ In progress).

## Readiness allowlist proof

`.gitignore` allowlists `specs/225-deleak-product-skill-vocab/readiness/**`;
`git check-ignore` returns non-ignored for the readiness evidence, so it is committed (not silently
dropped).

## Test evidence (no degraded/synthetic checks reported as green)

- Leak guard `Feature225ProductSkillVocabulary`: **6/6 green** (real set 0 findings; red-before on 23).
- Parity `Rendering.Harness.Tests`: **212/212 green**.
- Baseline before/after identical (21 proj · 8 green · **13 pre-existing FSharp.Core lockfile reds**,
  visibly caveated as an environment limitation, not a regression); `Package.Tests` 159→165 passed,
  same 1 known `Build engine baseline` red.
