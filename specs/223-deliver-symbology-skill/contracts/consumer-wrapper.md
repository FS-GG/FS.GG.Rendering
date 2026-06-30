# Contract: Consumer wrapper — `fs-gg-product-symbology`

**Surface**: repo-root wrapper trees `.claude/skills/` and `.agents/skills/` (the surfaces the
skill-parity harness audits, and the trees blanket-copied into a spec-kit scaffold).

**Owner**: this repo. Mirrors the existing six `fs-gg-product-*` wrappers exactly.

## Required files (two, added)

```
.claude/skills/fs-gg-product-symbology/SKILL.md
.agents/skills/fs-gg-product-symbology/SKILL.md
```

### Shape (mirror `fs-gg-product-scene`)

Frontmatter:
```yaml
---
name: fs-gg-product-symbology
description: Author legible unit-symbology with the fixed channel grammar (Token -> Scene) in a generated FS.GG.UI product.
---
```

Body: the standard wrapper preamble ("This is the Claude-active wrapper …" / "Codex-active …") and
the relative pointer to the canonical content:

```
../../../template/product-skills/fs-gg-symbology/SKILL.md
```

(`.claude` variant says "Claude-active"; `.agents` variant says "Codex-active" — matching the
existing per-surface wording.)

## Invariants

- **Name** is the product alias `fs-gg-product-symbology` (canonical `fs-gg-symbology` with the
  `fs-gg-` → `fs-gg-product-` rewrite the parity harness computes at `SkillParity.fs:841`).
- **Both surfaces present** — the harness checks `claude` and `codex-local` independently.
- **Routes to** `template/product-skills/fs-gg-symbology/SKILL.md` — the same target the manifest
  source vendors.
- **Coexists with** the framework wrapper `fs-gg-symbology` (the bare name) with no collision: each
  resolves to its own target (US2 acceptance #2).

## Acceptance

- Parity harness reports **no** `MissingWrapper` for symbology with both files present (SC-002:
  7 of 7 product skills reachable via their product wrappers).
- Listing the wrapper surfaces shows `fs-gg-product-symbology` alongside the other six
  `fs-gg-product-*` on both surfaces.
