# Root-cause / classification map (T004)

Derived from research.md + verified against repo discovery. Captured 2026-06-30.

## (a) Defunct ids the catalog lists that resolve to NO `SKILL.md` anywhere

Verified with `find . -name SKILL.md | xargs grep -l '^name: <id>$'` → `<NONE>`:

| id family | ids | resolves? |
|---|---|---|
| 8 defunct `fs-gg-*` | `fs-gg-controls-host`, `fs-gg-typed-controls`, `fs-gg-viewer-host`, `fs-gg-design-tokens`, `fs-gg-evidence-mode`, `fs-gg-reconciliation`, `fs-gg-layout-readability`, `fs-gg-template-update` | ❌ none |
| `fsdocs-*` | `fsdocs-api-doc`, `fsdocs-build`, `fsdocs-examples`, `fsdocs-setup`, `fsdocs-technical` | ❌ none |
| `fsharp-*` | `fsharp-build-orchestration`, `fsharp-code-generation`, `fsharp-graph-algorithms`, `fsharp-io-globbing`, `fsharp-parsing`, `fsharp-shell-process` | ❌ none |
| stale `speckit-*` | `speckit-archive-readiness`, `speckit-evidence-audit`, `speckit-evidence-graph` | ❌ none (not in discoverable speckit surface) |

The stale `speckit-evidence-graph` / `speckit-evidence-audit` are also referenced by the
"Closed `owns:` vocabulary → implied skill" table — that table dangles too.

## (b) Reference forms the two docs actually use

- `skillist-reference.md`: **markdown table rows** — column-1 `` `id` ``, column-2 resolved path.
  Three tables: "Valid `skillist` ids", "Directory-name → accepted id", "Closed `owns:` vocabulary".
- `scaffold-map.md`: **prose inline code-spans** used as "see the X skill" pointers, at lines
  `131` (`fs-gg-typed-controls`), `140` (`fs-gg-controls-host`), `149` (`fs-gg-viewer-host`),
  `153` (`fs-gg-scene` — this one resolves).

## (c) Hypothesized shipping set — CONFIRMED by live scaffold (see produced-surface.md)

7 profile-wired product skills + the 16 discoverable `speckit-*` command skills, all resolvable via
consumer paths `.agents/skills/<id>/` and `.claude/skills/<id>/`.

## Path-column defect (FR-003)

Every resolvable row in today's catalog uses a **framework-only** path (`src/*/skill/SKILL.md`,
`template/fragments/*/skill/SKILL.md`) that the consumer's package does not carry — even rows whose
id is valid (`fs-gg-elmish`, `fs-gg-scene`, …). The rewrite must use consumer paths.
