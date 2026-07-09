# Contract: API Symbol Coverage

Supersedes `guidance-rule-coverage.md` (issue #189). The seven guidance rules
asserted that a skill body *contained specific substrings* — a testing skill had
to mention `screenshot`, `degraded`, `reviewer`, `caveat`. `content.Contains(token)`
is semantically blind: a skill whose guidance is wrong, stale, or contradicted by
the code stays green as long as it keeps saying the right words. What is checked
now is the API a skill actually names.

## Inputs

| Input | Source | Absent |
|-------|--------|--------|
| The public surface | `readiness/surface-baselines/members/*.txt` — member-granular, one line per public member. | Layer is inert; the report carries a caveat. |
| The test corpus | Every `*.fs` under `tests/`, with comments and string literals stripped. | Layer is inert; the report carries a caveat. |

Both are required. The check reports no coverage rather than a coverage it did
not earn.

Comments and string literals are stripped from **both** the skill's fences and the
test sources, so naming an API in prose — a cautionary `// do not use Foo.bar`, a
filename like `Program.fs`, an assertion message `"Foo.bar is missing"` — neither
documents it nor exercises it.

## Extraction

For each **canonical** and **command** skill entry, every ```` ```fsharp ```` fence is
scanned for qualified `Module.member` occurrences. Members are lower-camel by F#
convention, so an uppercase second segment (a nested type, union case, or
property) is deliberately not a match.

The **closed world** is the set of module names present in the surface baseline.
A `Module.member` whose module is not in that set is product-local or pseudo-code
in an author-facing example (`Stack.create`, `model.Name`) and is not judged.
This is what keeps the check free of false positives without a denylist.

## Status

| Status | Meaning | Finding | Severity |
|--------|---------|---------|----------|
| `exercised` | In the surface baseline, and named by at least one test source. | — | — |
| `unexercised` | In the surface baseline, but no test names it. The seam may be dead. | `unexercised-api-symbol` | `warning` |
| `unresolved` | Not in the surface baseline. The skill documents an API that does not exist. | `unresolved-api-symbol` | `high` |

## What this does and does not prove

- It **does** prove that a documented member exists on a module the baseline knows,
  and that some test calls it. A skill cannot document a seam that was never wired,
  nor survive a rename of a *member* it documents.
- It **does not** prove the calling test asserts anything meaningful about that API,
  nor that the test passes. `exercised` means "called by a test source", and the
  status token says exactly that.

### Known limitations

Each is a deliberate trade that keeps the check free of false positives. A finding
this check raises is real; the set of findings it can raise is not exhaustive.

- **Modules are keyed by simple name**, because that is how a fence writes them.
  Same-named modules in different namespaces (`Controls.Button`, `Controls.Typed.Button`)
  merge into one member set, so a member removed from only one of them still resolves.
- **A module the baseline does not know is not judged at all** — so a *module*-level
  typo or rename (`DataGird.visibleRange`) is skipped, not flagged. Distinguishing it
  from a product-local example (`Stack.create`) is not possible without resolving `open`s.
- **Nested-type members** (`ControlsElmish+Perf.runScript`) key under `ControlsElmish+Perf`,
  so a fence writing `Perf.runScript` is treated as product-local. Keying them under
  `Perf` would make `Model` a known module and turn the model-swap skill's product-local
  `Model.update` into a false finding.
- **A skill whose examples name no baseline module is judged not at all.** It gets no
  coverage row, so the report emits a caveat naming every such skill rather than letting
  an unchecked skill pass for a clean one.

## Report Requirements

The report includes a per-skill coverage table:

```text
| Skill | Documented | Exercised | Unexercised | Unresolved |
```

Each `unresolved` and `unexercised` symbol appears in the findings table with the
skill path and the symbol, and a remediation naming the two ways out: correct the
skill, or exercise the API.
