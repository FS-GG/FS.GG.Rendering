# Contract: `fs-gg-ui` template — `lifecycle` option

The external interface this feature exposes is a new CLI/template option on the `fs-gg-ui`
dotnet template. This is the contract callers and the downstream SDD scaffold depend on.

## Option surface

```
--lifecycle <spec-kit|sdd|none>     (default: spec-kit)
```

- Datatype: `choice` (single value).
- Default: `spec-kit`.
- Discoverable: `dotnet new fs-gg-ui --help` lists the option, the three values, and a
  self-describing description for each (FR-010 / SC-005).

## Behavioral contract

| Invocation | Required outcome |
|------------|------------------|
| `dotnet new fs-gg-ui` (no `--lifecycle`) | Byte-identical to the pre-feature template output, for every profile. |
| `dotnet new fs-gg-ui --lifecycle spec-kit` | Identical to the no-value invocation. |
| `dotnet new fs-gg-ui --lifecycle sdd` | Generated product emitted; gated lifecycle scaffolding absent. |
| `dotnet new fs-gg-ui --lifecycle none` | Generated product emitted; gated lifecycle scaffolding absent. |
| `dotnet new fs-gg-ui --lifecycle <other>` | Fails fast with a clear error; no partial/default output. |

"Gated lifecycle scaffolding" = `.specify/` workspace, the project constitution, the agent
skill/context files (`.agents/`, `.claude/`), and the generated agent-context tree
(`AGENTS.md`, `CLAUDE.md`, and the constitution under the generated tree).

"Generated product" = profile source, project files, product tests, and profile-specific
content (and the `designSystem=ant` overlay when selected). Never altered by `lifecycle`.

## Composition contract (FR-007 / FR-008)

- Works across all four profiles (`app`, `headless-scene`, `governed`, `sample-pack`) with
  consistent behavior.
- Composes with `designSystem` and `feedback` so any valid combination yields the union of
  intended effects — no silent override in either direction. Note `feedback`'s skill/extension
  output is itself part of the gated set, so under `sdd`/`none` a `feedback=true` request emits
  no feedback skill/extensions (the gated set wins for gated targets); this is the intended
  union, not an override, and is asserted explicitly.

## Non-regression contract (FR-002 / FR-009 / SC-001 / SC-002)

- The default path adds zero output diff vs the pre-feature template.
- Existing profile/template test suites pass with zero modifications.

## Stability / versioning

- Tier 1 contracted change to `fs-gg-ui-template`. Adding the option is additive and
  backward-compatible (existing callers that omit it are unaffected). The downstream P2 SDD
  composition epic depends on the `sdd` value being accepted and suppressing the gated set.
