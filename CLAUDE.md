<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
at specs/138-layout-attrs-metrics-fix/plan.md
<!-- SPECKIT END -->

## Ant Design — source of truth

FS.GG adopts Ant Design **as a design language only** (no React/DOM/HTML/CSS). The canonical
upstream Ant reference is the central hub
[`docs/product/ant-design/reference/ant-llms-sources.md`](docs/product/ant-design/reference/ant-llms-sources.md),
which catalogs the three Ant LLM files — `llms.txt` (index), `llms-full.txt` (full API/usage +
component tokens), `llms-semantic.md` (semantic parts). Draw Ant facts from the hub (and the
per-family pattern docs under `docs/product/ant-design/patterns/`), not from raw `ant.design` URLs
or memory. For applying Ant patterns, use the `fs-gg-ant-design` skill.
