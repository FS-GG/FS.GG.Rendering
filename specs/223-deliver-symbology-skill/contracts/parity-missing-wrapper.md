# Contract: Parity harness — narrowed missing-wrapper rule

**Surface**: `tools/Rendering.Harness/SkillParity.fs` → `missingWrapperFindings` (`:824-861`).

**Owner**: this repo. Behavior covered by the Feature 168 parity spec.

## Rule (after change)

For each canonical entry `e` where `requiresWrapper e` is true, and for each supported wrapper
surface `s` (`codex-local`/`fixture-codex`, `claude`/`fixture-claude`):

```
isProductSkill   = e.Path under "template/product-skills"
exposedAsAlias   = isProductSkill && wrapperNames(s).Contains( e.name with "fs-gg-" -> "fs-gg-product-" )
canonicalMatch   = (NOT isProductSkill) && wrapperNames(s).Contains( e.name )
antSelfExposed   = e.SurfaceId = "ant-canonical" && s = "claude"

satisfied        = canonicalMatch || exposedAsAlias || antSelfExposed
=> emit MissingWrapper finding on surface s iff NOT satisfied
```

**The only change** from the current rule is the `NOT isProductSkill` guard on `canonicalMatch`.
Previously `wrapperNames(s).Contains(e.name)` satisfied **every** canonical kind, letting a bare
framework wrapper (`fs-gg-symbology`) mask a missing product wrapper.

## Invariants

- **Product skills are satisfied only by their `fs-gg-product-*` alias** (FR-004). A bare
  same-named framework wrapper does **not**, on its own, satisfy a product skill's requirement.
- **Non-product canonicals are unchanged** — `package-canonical`, `ant-canonical`,
  `fixture-canonical` keep satisfying via the bare name and/or `antSelfExposed` (FR-004 second
  sentence; preserves the paths `requiresWrapper` covers at `:817-822`).
- **No false positives for the six** (FR-006) — each has its `fs-gg-product-*` alias on both
  surfaces, so `exposedAsAlias` holds.

## Acceptance (new `Feature223SymbologyParityTests`, GL-free, fixture-built)

1. **Blind-spot closed (SC-003)**: a `template/product-skills` canonical with the **bare** wrapper
   present but the **product alias absent** ⇒ `MissingWrapper` finding for that skill on that
   surface. (Fails before the fix, passes after.)
2. **Alias satisfies**: same canonical with the `fs-gg-product-*` alias present ⇒ **no** finding.
3. **Regression (SC-004/FR-006)**: a fixture mirroring the six delivered product skills (alias
   present) ⇒ no `MissingWrapper` for any of them; and a non-product canonical satisfied by its bare
   name ⇒ no finding (ant/package self-exposure intact).
