# Contract: Skill-catalog currency check

The repo-owned gate that keeps the shipped consumer skill docs honest. This is the only *new*
external-facing contract this feature introduces. It is consumed as an Expecto test in the framework
repo's pack/test lane (research R3); if any public helper is added to `SkillParity`, that `.fsi` +
surface-area baseline is the API-surface contract and is updated in the same change.

## Inputs

| Input | Source | Notes |
|---|---|---|
| Shipped consumer docs | `template/base/docs/skillist-reference.md`, `template/base/docs/scaffold-map.md` | The files a produced package carries; scanned for skill references. |
| Produced skill surface | `SkillParity` discovery over produced-surface roots (`.agents/skills`, `.claude/skills`, `template/product-skills`, `src/**/skill`, `speckit-*` command surface) | Each entry's `name:` is a resolvable id. |
| Justified exceptions | An allowlist with reasons (mechanism finalized in tasks) | Empty by default after this feature. |

## Reference extraction (recognized forms)

- **Catalog table rows** in `skillist-reference.md`: the `` `id` `` in column 1 and the path in
  column 2 of each skill row (the "Valid ids", "Directory-name → accepted id", and "owns: → implied
  skill" tables, to the extent each names a skill id).
- **Prose code-spans** in `scaffold-map.md`: an inline `` `fs-gg-…` `` (or `speckit-…`) token used
  as a "see the X skill" pointer.

The check MUST recognize the forms the docs actually use; it MUST NOT silently ignore a reference
form that exists in the file.

## Resolution rule

A referenced `id` **resolves** iff `SkillParity` discovery yields a `SKILL.md` whose `name:`
frontmatter equals `id`, under a root the produced package carries. A reference that does not
resolve is a failure **unless** `id` is in the Justified-exceptions allowlist with a non-empty
reason.

## Output / failure shape (FR-005, FR-006)

- **Pass**: every reference resolves (or is a justified exception). No output beyond green.
- **Fail**: the check fails and emits one `Currency finding` per unresolved reference, each naming
  the `id`, the `doc`, and the `line`, e.g.:

  ```text
  catalog-currency FAILED (2 unresolved skill references):
    skillist-reference.md:17  'fs-gg-controls-host'  → no SKILL.md with name: fs-gg-controls-host in package
    scaffold-map.md:131       'fs-gg-typed-controls' → no SKILL.md with name: fs-gg-typed-controls in package
  ```

## Invariants the contract guarantees

1. **Resolvability** (FR-001/FR-003/FR-004): after this feature, every skill reference in the two
   shipped docs resolves to a real packaged skill.
2. **Drift-proofing** (FR-005): renaming/removing a skill, or adding a doc reference to a
   non-existent id, fails the check.
3. **Locatability** (FR-006): every finding names id + doc + line.
4. **No silent exemption** (FR-009): only allowlisted ids with reasons may dangle.
5. **Refresh-passes** (FR-007): the documented way to correct the catalog yields a file that passes
   the check on the first run (hand-edit-to-green under Option A, or regenerate under Option B).

## Verification (Principle V)

- **Negative**: inject a dangling id into either doc → check FAILS naming it (SC-003).
- **Positive**: corrected docs + real produced surface → check PASSES (SC-001/SC-002).
- **Evidence**: positive case is validated against an **actual scaffold** (live run, standing
  assumption), not a fixture, so the "produced surface" is real.
