# Contract: Skill Surface Inventory

## Required Inventory

The checker inventories these repository surfaces by default. Each row restates
one `SkillSurface` returned by `discoverDefaultSurfaces`, and `Feature1099` fails
when a row and its surface disagree — so this table is a checked declaration
rather than a second, drifting copy of one (#1099).

Since #1092 a surface declares **both** halves of where it looks: `Roots` is the
only source of paths the resolver has, and `Selector` is how the bodies beneath
those roots are narrowed. A selector may only narrow what `Roots` produced; it
can never introduce a path from outside them. Neither column carries prose — a
root is a directory or a single `SKILL.md`, never a glob or a sentence.

| Surface id | Kind | Roots | Selector | Role |
|------------|------|-------|----------|------|
| `codex-local` | `wrapper` | `.agents/skills` | `agent-wrappers` | Repo-local Codex/local-agent skill exposure. |
| `claude` | `wrapper` | `.claude/skills` | `agent-wrappers` | Claude skill exposure. |
| `package-canonical` | `canonical` | `src` | `area-skill-bodies` | Package-owned authoritative guidance, at the `<area>/skill/SKILL.md` convention. |
| `template-canonical` | `canonical` | `template` | `non-mirrored-bodies` | Generated-product, sample, feedback, and product guidance; the ADR-0011 mirror roots are subtracted. |
| `ant-canonical` | `canonical` | `docs/product/ant-design/skill/SKILL.md` | `every-skill-body` | Ant Design repository guidance routed to by wrappers. |
| `spec-kit-command` | `command` | `.agents/skills`, `.claude/skills` | `command-wrappers` | Spec Kit command skills and extension wrappers, selected by their `speckit-` prefix. |

### Selector vocabulary

| Selector | Narrows the bodies beneath `Roots` to |
|----------|----------------------------------------|
| `every-skill-body` | Every `SKILL.md`, and what an operator `--surface id=path` override gets. |
| `area-skill-bodies` | Only `<area>/skill/SKILL.md` bodies. |
| `non-mirrored-bodies` | Every body except the ADR-0011 agent-skill-root mirrors, which are byte-identical projections of a canonical body. |
| `agent-wrappers` | Repo-owned agent wrappers: excludes `speckit-*` command skills and the externally-owned ADR-0019/0021 coordination kit. |
| `command-wrappers` | Only `speckit-*` command skills. |

`ant-canonical`'s body moved out of `.claude/skills/` in #1080/#1082: a
byte-identical three-root union cannot contain a canonical that the other roots
route *into*, because a root cannot route to itself. The canonical now lives at
the `<area>/skill/SKILL.md` convention above, and `fs-gg-ant-design` is an
ordinary wrapper covered by wrapper parity like every other one. What the
surface asserts is unchanged; only where it looks.

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
