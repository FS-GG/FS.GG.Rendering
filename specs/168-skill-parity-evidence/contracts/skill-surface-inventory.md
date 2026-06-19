# Contract: Skill Surface Inventory

## Required Inventory

The checker inventories these repository surfaces by default:

| Surface id | Kind | Root | Role |
|------------|------|------|------|
| `codex-local` | wrapper | `.agents/skills` | Repo-local Codex/local-agent skill exposure. |
| `claude` | wrapper | `.claude/skills` | Claude skill exposure. |
| `package-canonical` | canonical | `src/*/skill` | Package-owned authoritative guidance. |
| `template-canonical` | canonical | `template/**/skill` and `template/product-skills` | Generated-product, sample, feedback, and product guidance. |
| `ant-canonical` | canonical | `.claude/skills/fs-gg-ant-design/SKILL.md` | Ant Design repository guidance routed to by wrappers. |
| `spec-kit-command` | command | `.agents/skills/speckit-*`, `.claude/skills/speckit-*` | Spec Kit command skills and extension wrappers. |

The report may list machine-local global Codex skills as excluded external
surfaces when present in the operator environment, but they are not required for
repository parity.

## Wrapper Target Format

Wrappers route to canonical guidance with a Markdown code span or code block
containing a relative target path after text equivalent to:

```text
Before acting, read the canonical instructions in:
```

The parser resolves the first `SKILL.md` path after that route text.

## Wrapper Metadata Comparison

For wrappers with a valid canonical target, the checker compares:

- `name`
- `description`
- invocation/discovery metadata when present
- wrapper route target path

Description comparison is normalized for quotes, whitespace, and trailing
periods. A materially different description produces `stale-description` unless
an exception explains why wrapper discovery text intentionally differs.

## Canonical Drift Comparison

Canonical drift is detected when:

- two canonical sources define the same `name` with different descriptions or
  incompatible guidance
- a wrapper target points at a source whose parsed `name` differs from the
  wrapper skill name without an exception
- a wrapper embeds substantial guidance that contradicts its canonical target
- package-owned and generated-product variants claim the same domain while
  routing to different rules without an exception

## Wrapper-Only Entries

A wrapper with no valid canonical target is a `wrapper-only` finding unless it
is a Spec Kit command skill or an explicit command-surface exception. Wrapper-only
findings include the wrapper path and a remediation hint to add a canonical
source or mark the entry as an intentional command skill.

## Missing Wrappers

A canonical skill that should be exposed to supported agents but is absent from
one supported wrapper surface produces `missing-wrapper`. Generated-product
skills copied into product templates may be exempt when they are not intended to
be invoked from the repository root; the exception must be explicit.

## Broken Targets

A wrapper route target that cannot be resolved is always a high-severity
`broken-target` finding. Broken targets cannot be suppressed by rule coverage
exceptions.
