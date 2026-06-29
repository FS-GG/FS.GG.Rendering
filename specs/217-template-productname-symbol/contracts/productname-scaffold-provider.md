# Contract: `scaffold-provider` (rendering) — template parameter surface

**Contract owner**: FS.GG.Rendering (`FS.GG.UI.Template`, shortName `fs-gg-ui`)
**Consumer**: FS.GG.SDD provider-runner (`fsgg-sdd scaffold --provider rendering`)
**Change class**: additive / backward-compatible (Tier 1 contract-change)
**Related**: FS-GG/FS.GG.Rendering#27 · FS-GG/FS.GG.SDD#35 · FS.GG.Templates#30

## Accepted options (post-change)

The `fs-gg-ui` template MUST accept all of today's options **plus** `productName`:

| Option | Datatype | Default | Role |
|---|---|---|---|
| `--name` / `-n` | built-in | output-dir name | Product name (existing path). |
| `--productName` | text | `""` | Product name (SDD convention). **NEW.** When non-empty, overrides `-n`. |
| `--profile` | choice | `app` | unchanged |
| `--designSystem` | choice | `wcag` | unchanged |
| `--lifecycle` | choice | `spec-kit` | unchanged |
| (other existing text/bool symbols) | — | — | unchanged |

Supplying `--productName <value>` MUST NOT produce a templating "invalid option" error (today's exit 127).

## Behavioral guarantees

- **G1 (accept).** `dotnet new fs-gg-ui … --productName Acme` instantiates successfully.
- **G2 (drive name).** With `--productName Acme`, every name-derived artifact (project/file names, namespaces, lowercased slug) reflects `Acme`, equivalent to `-n Acme`.
- **G3 (precedence).** If both `--productName` and `-n` are supplied, `--productName` wins; the result is consistently named (never a half-renamed mix).
- **G4 (fallback).** Empty/whitespace `--productName` ⇒ behaves as if not supplied (falls back to `-n`/output-dir name).
- **G5 (additive / byte-identical).** With `--productName` absent, output for every existing path is byte-identical to the pre-change template.
- **G6 (healthy payload).** A `--productName`-scaffolded product builds in Release with 0 warnings / 0 errors.

## Provider invocation this contract must satisfy

The SDD provider-runner issues (no `-n`):

```
dotnet new fs-gg-ui -o . --designSystem wcag --lifecycle sdd --productName <Name> --profile app
```

and the orchestrated form:

```
fsgg-sdd scaffold --provider rendering --param productName=<Name>
```

Both MUST succeed and yield the expected named, buildable product (G1–G2, G6).

## Compatibility & registry

- The change is recorded in the org-level cross-repo contract/compatibility registry (`FS-GG/.github`) as an **additive** `scaffold-provider` (rendering) change, cross-referenced with #27 and SDD#35.
- No consumer migration required: existing `-n`/default consumers are unaffected (G5).
