# Regression / drift-proofing evidence (T016 / SC-003)

Captured 2026-06-30. Command: `dotnet test tests/Package.Tests -c Release --filter Feature224SkillCatalogCurrency`.

## Pre-fix RED (the gate detects the real, live drift — SC-003 negative direction)

Against today's shipped docs the check FAILED with **45 findings**, each naming `id + doc + line`:

- Unresolved ids (sample): `fs-gg-controls-host` (skillist-reference.md:17),
  `fs-gg-typed-controls` (skillist-reference.md:31 + scaffold-map.md:131),
  `fs-gg-viewer-host` (skillist-reference.md:33 + scaffold-map.md:149),
  `fs-gg-controls-host` (scaffold-map.md:140), the full `fsdocs-*`/`fsharp-*` families, and the stale
  `speckit-archive-readiness` / `speckit-evidence-audit` / `speckit-evidence-graph`.
- Framework-only path violations (FR-003): every valid id's row used `src/*/skill/SKILL.md` or
  `template/fragments/*/skill/SKILL.md` — not a consumer location.

Message shape matches the contract example:
`skillist-reference.md:17  'fs-gg-controls-host' → no SKILL.md with name: fs-gg-controls-host in package`.

## Inject → red / revert → green loop (manual, FR-005)

The `deliberate dangling-id regression` test exercises both directions in-memory:

- **Inject**: append `` | `fs-gg-does-not-exist` | .agents/skills/fs-gg-does-not-exist/SKILL.md | ``
  to the catalog content → **exactly one** finding naming `fs-gg-does-not-exist`.
- **Revert**: the real, corrected docs → **zero** findings.

The manual equivalent (quickstart §4):

```sh
# add `fs-gg-does-not-exist` to template/base/docs/skillist-reference.md
dotnet test tests/Package.Tests -c Release --filter Feature224SkillCatalogCurrency   # FAILS, names it
git checkout -- template/base/docs/skillist-reference.md
dotnet test tests/Package.Tests -c Release --filter Feature224SkillCatalogCurrency   # PASSES
```

## Post-fix GREEN

Recorded in `quickstart-evidence.md` after the catalog + scaffold-map content fix (T012/T015).
