# Phase 0 Research: fs-gg-ui `productName` Scaffold Symbol

## R1 — How can a custom `productName` parameter drive the same rename `sourceName` does today?

**Context.** Today `.template.config/template.json` declares `"sourceName": "Product"`. The dotnet templating engine binds `sourceName` to the **built-in `name` symbol** (the `-n`/`--name` value, or the output-directory name when `-n` is absent) and uses it to (a) rename files/dirs whose path contains `Product` (`src/Product/`, `Product.slnx`, `Product.fsproj`, `tests/Product.Tests/`, `load-product.fsx`) and (b) replace the `Product` token in file **contents** (`open Product`, `module Product.Tests.Program`, `#r ".../Product.dll"`, the `ProjectReference` to `Product.fsproj`, etc. — 22 content hits). `sourceName` **cannot be bound to an arbitrary symbol**; there is no supported way to make `--productName` feed `sourceName`.

**Decision.** Reproduce `sourceName`'s behavior with an engine construct that *can* be driven by a custom parameter:

1. Add `productName` — a `parameter` / `datatype: text`, `defaultValue: ""` (additive; rejected-option error disappears because the option now exists).
2. Add a **`generated` coalesce symbol** `effectiveName` = `productName` when non-empty, else the built-in `name`. (`"generator": "coalesce"`, `sourceVariableName: "productName"`, `fallbackVariableName: "name"`.)
3. Give `effectiveName` the rename duties `sourceName` used to own: `"replaces": "Product"` (content) **and** `"fileRename": "Product"` (paths).
4. **Remove the top-level `"sourceName": "Product"`** so there is exactly **one** driver for the `Product` literal (no double-substitution / order-dependent rewrites).
5. Repoint the existing `projectSlug` generated symbol's casing `source` from `name` → `effectiveName`, so the lowercased slug (`replaces: "fs-gg-ui"`) tracks `productName` too — otherwise a `--productName`-only scaffold would slug from the output-dir name, not the product name.

**Resulting behavior (FR-001/002/004/005/006):**

| Invocation | `effectiveName` | Outcome |
|---|---|---|
| `-n Foo` (no `productName`) | `Foo` | `Product`→`Foo` everywhere — same as today |
| neither flag | built-in `name` (output-dir name) | same default as today |
| `--productName Acme` (no `-n`) | `Acme` | `Product`→`Acme` everywhere — **new path works** |
| both `-n Foo` and `--productName Acme` | `Acme` | `productName` wins — defined precedence (FR-005) |
| `--productName ""`/whitespace | falls back to `name` | treated as not supplied (FR-006) |

**Rationale.** `sourceName` is effectively engine sugar for *name-driven `fileRename` + `replaces`*; moving those duties onto `effectiveName` keeps the identical mechanics while making `productName` the override source and giving an unambiguous, single rename driver. Coalesce yields the required precedence for free.

**Alternatives considered.**
- *Keep `sourceName` and add `productName` with its own `replaces`/`fileRename` on `Product`.* Rejected: two drivers rewriting the same `Product` literal (built-in `name` via `sourceName` **and** `productName`) is order-dependent and risks double-substitution; it buys no byte-identical advantage since the diff gate guards either way.
- *SDD-side remap (FS-GG/FS.GG.SDD#35): map the provider name-param to `--name`.* Rejected per spec assumption — the request prefers the Rendering side so the `productName` convention stays uniform across providers. #35 remains the coordination point; no SDD code change is required here.
- *Whitespace `productName` as a hard error.* Rejected — FR-006 wants graceful fallback so a blank param never breaks composition; coalesce-on-empty already gives this (confirm whitespace-only also coalesces; trim if the engine treats `"  "` as non-empty — see R3 risk).

## R2 — Backward-compatibility verification strategy (FR-004 / SC-003 / SC-004)

**Decision.** Prove compatibility empirically, not by reasoning. The env-gated `scripts/validate-productname-template.fsx` will, against a **pre-change baseline**:

- **SC-003 (byte-identical):** instantiate the matrix of existing paths *without* `productName` (`-n Foo`; no-name default; representative `--profile`/`--designSystem`/`--lifecycle` combos) on both the baseline template and the changed template, and assert a zero-diff tree comparison.
- **SC-004 (paths converge):** instantiate `--productName Acme` and `-n Acme` (same other flags) on the changed template and assert byte-identical trees.
- **SC-002 (healthy payload):** `dotnet build -c Release` a `--productName`-scaffolded product; assert 0 warnings / 0 errors.

**Baseline source.** Prefer the committed pre-change `template.json` (instantiate from a clean worktree / the `HEAD` template before the edit), falling back to the published `FS.GG.UI.Template@<current>` from the org feed. Capturing the baseline **before** mutating `template.json` is mandatory (the standing assumption in `plan.md`).

**Rationale.** Removing `sourceName` (R1 step 4) is the single change that could perturb existing output; a real byte-diff is the only honest proof. This mirrors the existing lifecycle/design-system validators (real `dotnet new` + diff), satisfying Principle V with real evidence.

## R3 — Risks / open items carried into tasks

- **Whitespace coalesce.** Confirm the engine's `coalesce` treats whitespace-only `productName` as "empty". If not, normalize (trim) before coalescing or via a `join`/`replace` form so FR-006 holds.
- **`fileRename` vs `sourceName` parity.** Low risk but unproven: confirm `fileRename: "Product"` renames the same set of path segments `sourceName` did (including the nested `src/Product/`, `tests/Product.Tests/`, and the `load-product.fsx` lowercase variant — note `load-product.fsx` is lowercase `product`, handled today by `projectSlug`/separate replace, **not** by `sourceName`; verify it is unaffected).
- **`rootNamespace` parameter.** It is a no-op compatibility text param (default `"Product"`, no `replaces`); leave untouched. Do **not** accidentally give it rename duties.
- **Org feed availability.** End-to-end SC-001/SC-005 require `fsgg-sdd` ≥ 0.2.0 + the template on the org feed; if the feed/toolchain is unavailable in the run environment, the env-gated live tier is skipped (disclosed) while the always-on verdict-core gate still proves the `template.json` contract fact.

## R4 — Cross-repo contract record (FR-008 / SC-005)

**Decision.** Record the `scaffold-provider` (rendering) contract change in the **org-level** coordination registry under `FS-GG/.github` (there is no in-repo `registry/dependencies.yml`), marked **additive / backward-compatible**, and cross-reference `FS-GG/FS.GG.Rendering#27` ⇄ `FS-GG/FS.GG.SDD#35`. Use the `cross-repo-coordination` skill/protocol. This is paperwork that trails the functional unblock (US3/P3), so it does not gate US1/US2 delivery.
