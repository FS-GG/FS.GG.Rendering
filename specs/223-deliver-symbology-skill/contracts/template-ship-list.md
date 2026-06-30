# Contract: Template ship-list — symbology product-skill source pair

**Surface**: `.template.config/template.json` `sources[]` (the `dotnet new fs-gg-ui` ship list).

**Owner**: this repo (producer). Consumed by every `dotnet new fs-gg-ui` scaffold.

## Required entries (exactly two, added)

```jsonc
{
  "condition": "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")",
  "source": "template/product-skills/fs-gg-symbology/",
  "target": ".agents/skills/fs-gg-symbology/"
},
{
  "condition": "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")",
  "source": "template/product-skills/fs-gg-symbology/",
  "target": ".claude/skills/fs-gg-symbology/"
}
```

## Invariants (asserted by `Feature219EmitFrameworkSkillsTests`)

- **No `lifecycle` clause** — symbology follows the product profile, emitting under `spec-kit`,
  `sdd`, and `none` alike (G-EMIT: `not condition.Contains("lifecycle == \"spec-kit\"")`).
- **Both surfaces** — one entry targets `.agents/skills/`, one targets `.claude/skills/` (G-EMIT
  dual-destination).
- **Profile predicate present** — `condition.Contains("profile ==")` (G-EMIT).
- **Source under `template/product-skills/`** — so the harness classifies it as a framework
  product-skill source (drives R3 parity rule and the validator framework-skill bucket).

## Effect on existing invariants

- **GV-3 (explicit `spec-kit` == default)**: preserved — the source emits identically under both
  invocations (research R1).
- **FR-009 (default − sdd differs only in gated paths)**: preserved — symbology is ungated, so it
  emits under `sdd` too and never appears in the gated-only diff (same as the six).
- **G-NODANGLE-SYMB**: flips from `{ fs-gg-symbology }` unwired to `{ }` unwired.

## Acceptance

`dotnet new fs-gg-ui --profile game` (and `--profile app`) produces
`.agents/skills/fs-gg-symbology/SKILL.md` and `.claude/skills/fs-gg-symbology/SKILL.md` containing
the 12788-byte product skill — under every `--lifecycle` value. (SC-001.)
