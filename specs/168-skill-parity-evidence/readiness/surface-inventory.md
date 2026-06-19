# Skill Surface Inventory

## Canonical Sources

- Package skills: `src/*/skill/SKILL.md`.
- Generated product and template skills: `template/**/skill/SKILL.md` and
  `template/product-skills/*/SKILL.md`.
- Ant guidance: `.claude/skills/fs-gg-ant-design/SKILL.md`.
- Spec Kit command skills: `.agents/skills/speckit-*` and
  `.claude/skills/speckit-*`, treated as command surfaces.

## Supported Wrappers

- Codex/local agent wrappers: `.agents/skills/*/SKILL.md`.
- Claude wrappers: `.claude/skills/*/SKILL.md`.

Wrappers route to canonical sources through the standard
`Before acting, read the canonical instructions in:` text followed by a
repository-relative `SKILL.md` path.

## Intentional External Exclusions

Machine-local Codex skill installs under `$CODEX_HOME` or user home directories
are excluded from required repository parity because they are not reproducible
from this checkout.
