# Justified-exception mechanism (T008 / FR-009)

**Decision: in-test allowlist of `(id, reason)` pairs, empty after this feature.**

The currency check exempts an unresolved skill reference **only** if its id appears in an explicit
allowlist with a **non-empty** reason. The allowlist lives in
`tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs` as a `(string * string) list`:

```fsharp
// (id, reason) — an unresolved id is exempted ONLY with a non-empty reason. Empty by default.
let justifiedExceptions : (string * string) list = []
```

Rationale:

- **No silent exemption** (FR-009): a reason-less entry (`""`/whitespace) does NOT exempt — the
  check still reports the finding. A dedicated test asserts a reason-less exemption still fails.
- Lowest-mechanism: the check is a self-contained test (R3 / Option A), so the allowlist is plain
  data beside it — no doc-annotation parser, no new public surface.
- **Default is empty**: after this feature every reference in both shipped docs resolves, so the
  allowlist carries zero entries.

Alternative rejected: inline doc annotation (e.g. an HTML comment `<!-- currency-exempt: id reason -->`
in the doc). Rejected — adds a parser and lets exemptions hide in the shipped doc; an in-test
allowlist keeps exemptions in the gate, reviewed with the test.
