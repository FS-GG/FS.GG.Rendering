# 0005. Ant Design pattern docs (F6): docs-only scope, coverage anchor, and source of truth

**Status**: accepted
**Date**: 2026-06-16

## Decision

Workstream F6 ships **documentation and one advisory skill only** — no compiled product code beyond
a single Expecto coverage check (`tests/Controls.Tests/Feature131AntPatternDocsTests.fs`). It is a
**Tier 2** change: no public `.fs`/`.fsi`, no new package or dependency, no token-value or behavior
change; the surface-drift and design-token-drift gates stay green with no baseline regeneration.

Three sub-decisions:

1. **Coverage anchor** — "one pattern doc per control family" is anchored to the code-derived
   `Catalog.categories` set (11 lowercase categories), a strict superset of the Controls Gallery's
   10 presentation families (`chart` and `graph` get separate pages). The index cross-maps them.
   (Research R1.)
2. **Source of truth** — the three Ant LLM files are adopted as the canonical, repo-wide upstream
   source: `https://ant.design/llms.txt` (index), `https://ant.design/llms-full.txt` (full
   API/usage + component tokens), `https://ant.design/llms-semantic.md` (semantic parts). They are
   catalogued once in the central hub `docs/product/ant-design/reference/ant-llms-sources.md`,
   which every doc, the skill, and repo agent context cite. (Research R8/R9.)
3. **Concept, not mechanism** — FS.GG adopts Ant's design-language concepts (stable patterns,
   named semantic parts, tokens-as-materials / semantic-styles-as-application). It does **not**
   adopt Ant's React `classNames`/`styles` props or HTML/CSS DOM. Each pattern doc records the
   not-adopted mechanism. (Research R7/R8.)

## Rationale

Anchoring coverage to real code keeps the completeness check honest and drift-proof. A single
source-of-truth hub gives one retrieval-date owner and prevents ad-hoc upstream citations from
drifting. Adopting the concept (not the mechanism) keeps the one-semantic-control-set layering rule
intact and avoids any React/DOM dependency.

## Revisit trigger

Revisit if a new control category is added (a new pattern page is then required by the coverage
check), if Ant restructures its LLM docs (bump the hub's retrieval date and snapshot), or when
Workstream D2/D3 turns the enterprise-template recipes into concrete kits.
