# Contract: Shared Markdown/JSON formatting helpers (Tier 2, internal)

Extracts the **three byte-identical copies** in `src/Testing/Testing.fs` into one shared module and
points all call sites at it (FR-005). The `src/Diagnostics/Diagnostics.fs` `System.Text.Json`-based
variant is **reconciled only where bytes are unchanged** (research R4).

## Canonical definitions (must match the existing `Testing.fs` copies byte-for-byte)

```fsharp
let esc (text: string) =
    text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")

let q text = "\"" + esc text + "\""

// NOTE: comma-SPACE separator — this is the Testing.fs form and must be preserved.
let jsonStringArray values = "[" + (values |> List.map q |> String.concat ", ") + "]"

// jsonCounts / countsText: the counts serializers used by the three readiness Markdown modules,
// reproduced verbatim from the existing copies (indentation, separators, ordering unchanged).
```

Home: `FS.GG.UI.Diagnostics` (leaf, reachable by `Testing`). The helpers stay **private** (not added to
any `.fsi`) unless an existing public emitter requires exposure.

## Copies to remove (verified locations in `Testing.fs`)

| Module | esc | q | jsonStringArray | jsonCounts | countsText |
|--------|-----|---|-----------------|------------|------------|
| `VisualReadinessMarkdown` | ~1235 | ~1238 | ~1240 | ~1243 | ~1248 (`statusCountsText`) |
| `VisualInspectionMarkdown` | ~1883 | ~1886 | ~1888 | ~1891 | ~1896 |
| `RetainedInspectionMarkdown` | ~2583 | ~2586 | ~2588 | ~2591 | ~2596 |

All three sets are byte-identical → one shared definition (SC-003).

## Diagnostics.fs variant — DO NOT force-merge

`Diagnostics.fs` (~357–483) uses a different implementation: `JsonSerializer.Serialize`-based `json`,
`jsonOption`, `jsonDate`; `jsonStringArray` with **no comma-space**; `jsonCounts`/`countsText` carry an
extra `tokenOf` projection parameter. These are **behaviorally distinct**.

| ID | Obligation |
|----|------------|
| C-FH-1 | The three `Testing.fs` copies are replaced by one shared definition; every call site emits byte-identical output (FR-006, SC-004). |
| C-FH-2 | Each consolidated helper has exactly one definition repo-wide for the Testing family (SC-003). |
| C-FH-3 | The `Diagnostics.fs` variant is left intact **unless** a specific helper can be proven byte-equivalent; any reconciliation that would change a single emitted byte is rejected (spec edge case). |
| C-FH-4 | After the change, an escaping-rule edit is a single edit reflected at all Testing-family callers (US3 acceptance #2). |

## Verification

- Build clean; full `dotnet test` no new failures vs baseline.
- All Visual / VisualInspection / RetainedInspection readiness golden assertions stay green (these emit
  via the helpers and are the byte oracle for this story).
- Diff serialized evidence artifacts pre/post: zero byte changes.
