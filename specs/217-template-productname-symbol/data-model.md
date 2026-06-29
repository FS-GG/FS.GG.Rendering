# Phase 1 Data Model: fs-gg-ui `productName` Scaffold Symbol

The "data" here is the `.template.config/template.json` symbol set and the rename tokens it drives. No runtime/storage entities.

## Template symbols

### `productName` (NEW) — parameter

| Field | Value |
|---|---|
| type | `parameter` |
| datatype | `text` |
| defaultValue | `""` (empty) |
| replaces | — (does **not** replace directly; feeds `effectiveName`) |
| description | Product name conforming to the SDD scaffold-provider convention. When supplied, names the generated product (overrides `-n`/`--name`). Empty/whitespace ⇒ treated as not supplied. |

**Validation rules**
- Additive: absence ⇒ behavior identical to today (FR-004).
- Empty or whitespace-only ⇒ coalesces to the built-in `name` (FR-006).
- When non-empty ⇒ authoritative product name (FR-002), precedence over `name` (FR-005).

### `productNameTrimmed` (NEW, shipped) — generated (regex) — FR-006 trim branch

| Field | Value |
|---|---|
| type | `generated` |
| generator | `regex` |
| parameters.source | `productName` |
| parameters.steps | `[{ regex: "^\\s+\|\\s+$", replacement: "" }]` (trim leading/trailing whitespace) |

**Role**: normalizes `productName` so a **whitespace-only** value (`"  "`) becomes `""` and thus
coalesces to `name`. *Discovered necessary during implementation (T012):* dotnet templating's
`coalesce` treats whitespace-only as **non-empty**, so without this trim `--productName "  "` would
NOT fall back (FR-006 would break). The plan/T012 anticipated this ("normalize via trim … so
`--productName "  "` falls back to `name`"); this is its concrete form.

### `effectiveName` (NEW) — generated (coalesce)

| Field | Value (shipped) |
|---|---|
| type | `generated` |
| generator | `coalesce` |
| parameters.sourceVariableName | `productNameTrimmed` *(was `productName` in the original sketch; now the trimmed form — see above)* |
| parameters.fallbackVariableName | `name` (built-in `-n`/output-dir name) |
| replaces | `"Product"` (file contents) |
| fileRename | `"Product"` (file/dir path segments) |

**Role**: the single driver for the capital `Product` rename token — assumes the duties previously
held by `sourceName`. Value = trimmed `productName` if non-empty else `name`.

### `effectiveNameLower` (NEW, shipped) — generated (casing) — sourceName lowercase parity

| Field | Value |
|---|---|
| type | `generated` |
| generator | `casing` |
| parameters.source | `effectiveName` |
| parameters.toLower | `true` |
| replaces | `"product"` (file **contents** only) |
| fileRename | — (**none** — see below) |

**Role**: reproduces a behaviour of `sourceName` that the original model missed. *Discovered via the
T004 byte-diff oracle (the standing assumption made empirical):* `sourceName: "Product"` performs a
**case-aware content replacement** — it also rewrites the lowercase literal `product` → lowercased
name (e.g. `-n Foo` turns `production`→`fooion`, `load-product.fsx`→`load-foo.fsx` *in content*).
A plain `replaces: "Product"` only covers the capital form, so 21 files diffed until this symbol was
added. Crucially `sourceName` did **not** `fileRename` lowercase `product` paths (`load-product.fsx`,
`docs/product.md` keep their names), so `effectiveNameLower` carries **no `fileRename`** — content
replace only. The byte-diff (SC-003, M1..M4 = 0 diffs) is the proof this matches.

### `sourceName` (REMOVED)

Top-level `"sourceName": "Product"` is **removed**. Its rename + content-replace duties move to `effectiveName` so there is exactly one driver for the `Product` literal (no double substitution). See research R1.

### `projectSlug` (MODIFIED) — generated (casing)

| Field | Before | After |
|---|---|---|
| generator | `casing` | `casing` |
| parameters.source | `name` | **`effectiveName`** |
| parameters.toLower | `true` | `true` |
| replaces | `"fs-gg-ui"` | `"fs-gg-ui"` |

**Reason**: the lowercased slug must track `productName` (not the output-dir name) on the `--productName`-only path, so a `--productName Acme` scaffold and a `-n Acme` scaffold converge (SC-004).

### Unchanged symbols

`profile`, `designSystem`, `lifecycle`, `rootNamespace` (no-op compat text, default `"Product"`, **no rename duty — keep as-is**), `packagePrefix`, `authors`, `repositoryUrl`, `targetFramework`, `initGit`, `feedback`. None gain or lose behavior.

## Rename token inventory (what `effectiveName` must cover)

Driven today by `sourceName: "Product"`; must remain covered after the move:

- **Path segments** (`fileRename`): `src/Product/`, `src/Product/Product.fsproj`, `Product.slnx`, `tests/Product.Tests/`, `tests/Product.Tests/Product.Tests.fsproj`.
- **Content** (`replaces`): `open Product`, `module Product.Tests.Program`, `#r ".../Product.dll"`, `ProjectReference … Product.fsproj`, the `src/Product` path references in `GovernanceTests.fs`, etc. (22 hits).
- **Lowercase `product` (CORRECTED during implementation)**: the original sketch said lowercase
  `product` was handled by `projectSlug`/separate replaces and "not `sourceName`". The T004 byte-diff
  oracle disproved this: `sourceName`'s **content** replace is **case-aware** and rewrites lowercase
  `product` → lowercased name too (only the *fileRename* is capital-only, so `load-product.fsx` /
  `docs/product.md` keep their **names** while their *contents* are rewritten). Parity is therefore
  preserved by the new `effectiveNameLower` symbol (`replaces: "product"`, no `fileRename`), not by
  `projectSlug` (which only replaces `fs-gg-ui`). See `readiness/rename-tokens.md` + the
  `effectiveNameLower` entry above.

## State / transitions

None. Template instantiation is a pure function of (parameters) → (generated tree). The only "transition" is the precedence resolution captured by the `effectiveName` coalesce truth-table in `research.md` R1.
