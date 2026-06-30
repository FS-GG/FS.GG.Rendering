# Contract: Product-skill leak guard

The only new external-facing contract this feature introduces. The leak guard is a repo-owned
Expecto test (`tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs`) that scans the **whole
shipped product-skill set** and fails the build when any skill body reintroduces a framework-leak
token. It reuses the existing `SkillParity` discovery surface and adds **no** new public API
(Principle II; mirrors Feature 224's catalog-currency check).

## Inputs

| Input | Source | Notes |
|---|---|---|
| Shipped product skills | `SkillParity.inventorySkills (defaultRequest root) (discoverDefaultSurfaces root)`, filtered to `entry.Path.Contains("template/product-skills")` | The same authoritative enumerator parity + Feature 224 use; never a hardcoded list of 7 (so a later-added leaky skill is also scanned) |
| Skill body text | `entry.Content` | Scanned line-by-line; no file re-reading |
| Repo root | `FS.GG.TestSupport.RepositoryRoot.value` | Shared non-packed test helper |

## Leak classes (recognized forms)

| Class | Recognizer | Mode |
|---|---|---|
| **A — framework evidence process** | `refresh-local-feed-and-samples`, `package-feed`, `specs/.*/readiness`, `\.gitignore` (allowlist context), `BaseOutputPath` | **Banned** — any match is a finding |
| **B — lifecycle feedback path** | `specs/.*/feedback` | **Conditional** — a finding only when its enclosing Markdown paragraph contains no `spec kit` / `spec-kit` gating phrase (case-insensitive) |
| **C — feature/spec-number stamp** | `[Ff]eature\s+\d+`, `spec-\d+` | **Banned** — any match is a finding (`spec-\d+` does not match `spec-kit`) |

A "paragraph" for Class B is the run of non-blank lines around the matched line (blank-line
delimited). The gating-phrase set is a small named constant so it can be extended without reshaping
the guard.

## Resolution rule

A product-skill body is **clean** iff:

1. no line matches any Class-A pattern, **and**
2. every Class-B (`specs/.../feedback`) match sits in a paragraph containing a spec-kit gating
   phrase, **and**
3. no line matches any Class-C pattern.

The gate passes iff **every** discovered product skill is clean (the Finding list is empty).

## Output / failure shape

One finding per violation, naming the offending skill, the leak class, the matched token, and
`file:line` (FR-007 / Principle VI). Example failure text:

```text
product-skill leak guard FAILED (2 leak token(s)):
  fs-gg-testing  [A framework-evidence]  'refresh-local-feed-and-samples'  template/product-skills/fs-gg-testing/SKILL.md:56
  fs-gg-elmish   [B unconditional-feedback]  'specs/<feature>/feedback/'  template/product-skills/fs-gg-elmish/SKILL.md:65
```

## Invariants the contract guarantees

1. **Whole-set coverage** — the scan enumerates the produced product-skill surface via discovery, so
   a skill that gains the leaky boilerplate later is still caught (FR-007, spec edge case).
2. **Conditional spec-kit path preserved** — a properly gated `specs/<feature>/feedback/` mention
   passes; only ungated ones fail (FR-002, edge case "spec-kit-only path is genuinely useful").
3. **Lesson-preserving by construction** — the guard bans *framing tokens*, never the surrounding
   guidance; it cannot force removal of a lesson (FR-004).
4. **No new public surface** — the guard consumes existing public `SkillParity` discovery; no
   `.fsi`/baseline delta unless a helper is deliberately promoted (Principle II conditional).
5. **Parity-safe** — the guard reads bodies only; it neither edits nor asserts over front-matter, so
   it cannot perturb the wrapper-vs-canonical parity check (FR-006).
6. **Actionable failure** — every finding names skill + class + token + file:line (FR-007 / VI).

## Verification (Principle V)

- **Negative**: inject a synthetic body carrying one token of each class (one ungated
  `specs/<feature>/feedback/`, one `package-feed`, one `feature 200`) → the guard returns exactly
  three findings, each naming its class and line.
- **Positive**: run over the real, corrected shipped set → zero findings; the existing
  `Feature168*` / `Feature223` parity tests remain green.
- **Evidence**: failing-before / passing-after recorded under
  `specs/225-deleak-product-skill-vocab/readiness/regression-evidence.md`; produced-surface
  enumeration under `readiness/produced-surface.md`.
