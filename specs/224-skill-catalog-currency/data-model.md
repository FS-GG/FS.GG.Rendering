# Phase 1 Data Model: Consumer Skill Catalog Currency

This feature manipulates document content and validates it against a discovered skill set; there is
no persistent storage. The "entities" below are the conceptual records the currency check reasons
over.

## Entity: Shipping skill

A skill actually delivered into a produced package.

| Field | Description |
|---|---|
| `id` | The `name:` frontmatter value of the skill's `SKILL.md` (e.g. `fs-gg-ui-widgets`). The id used everywhere, **not** the directory name. |
| `consumerPath` | Where the skill resolves **in the produced package** (e.g. `.agents/skills/<id>/SKILL.md`), not the framework-repo `src/<Pkg>/skill/SKILL.md`. |
| `profiles` | The template profiles that wire the skill (from `.template.config/template.json`): e.g. `fs-gg-ui-widgets` → `app, game`. |
| `lifecycleScope` | `all` for product skills; `spec-kit` for `speckit-*` command skills (catalog co-ships under spec-kit only). |

**Source of truth**: `SkillParity` discovery (`discoverDefaultSurfaces` / `filesForSurface` /
`readEntry`) over the produced-surface roots. **Validation rule**: an id is a Shipping skill iff
discovery finds a `SKILL.md` with that `name:` under a root the produced package carries.

**Confirmed shipping set (hypothesis — verify via live scaffold, research R1):**
`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`,
`fs-gg-testing`, `fs-gg-ui-widgets` (product skills) + the `speckit-*` command skills.

## Entity: Skill reference

A single mention of a skill id inside a shipped consumer doc.

| Field | Description |
|---|---|
| `id` | The referenced skill id. |
| `doc` | The shipped doc that contains it (`skillist-reference.md` or `scaffold-map.md`). |
| `line` | 1-based line number, for the failure message (FR-006). |
| `form` | `table-row` (catalog id/path columns) or `prose-code-span` (scaffold-map inline `` `id` `` near "skill"). |

**Validation rule**: every Skill reference's `id` MUST be a Shipping skill id, OR carry a recorded,
recognized justification (Entity: Justified exception). Otherwise it is a Currency finding.

## Entity: Justified exception

An intentionally-retained reference to a non-shipping id (e.g. a cross-repo SDD skill mentioned for
context), allowed only with an explicit recorded reason.

| Field | Description |
|---|---|
| `id` | The non-shipping id permitted to appear. |
| `reason` | A short recorded justification the check recognizes (mechanism decided in tasks: an allowlist with reasons, or an inline doc annotation). |

**Validation rule**: the check exempts an unresolved reference **only** if it appears in the
Justified-exception set with a non-empty reason; silent exemption is forbidden (FR-009). Default
expectation after this feature: the set is **empty**.

## Entity: Currency finding

The check's output when a reference fails to resolve.

| Field | Description |
|---|---|
| `id` | The unresolvable referenced id. |
| `doc` | The doc that references it. |
| `line` | Where the reference appears. |
| `message` | Actionable text naming the id and its location (FR-006), e.g. `skillist-reference.md:31 references skill 'fs-gg-typed-controls' which resolves to no SKILL.md in the package`. |

**State transition (the gate)**: `references` → resolve each against `Shipping skill` set (minus
`Justified exception`) → if any unresolved, emit `Currency finding[]` and **fail**; else **pass**.

## Relationships

```text
Shipping skill (discovered via SkillParity)  ──┐
                                               ├─ resolves ──> Skill reference (from shipped docs)
Justified exception (allowlist + reason) ──────┘                     │
                                                                     ▼
                                                      unresolved ⇒ Currency finding ⇒ check FAILS
```

## Out of scope (sibling epic #34 items)

- De-leaking framework-process vocabulary from skill *bodies* (`owns:`/`tasks.deps.yml` prose) — #37.
- Adding a consumer theming/styling skill — #38.
- Defining new per-profile skill sets — only the *existing* produced surface is validated here.
