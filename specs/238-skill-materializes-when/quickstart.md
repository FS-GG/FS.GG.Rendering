# Quickstart — validate the materializes-when / supplied-by manifest fields

Deterministic, GL-free, no `dotnet new`. Run from the repo root.

## Prerequisites

- .NET SDK (`net10.0`) — `dotnet fsi` for the generator, `dotnet test` for the guard.

## 1. Regenerate the manifest

```sh
dotnet fsi scripts/generate-skill-manifest.fsx
```

Expected: `skill-manifest: wrote …/skill-manifest.json (12 skills)`. Every entry in
`template/skill-manifest/skill-manifest.json` now carries `materializes-when` and `supplied-by`
(see `data-model.md` for the expected values), `schemaVersion` is still `1`.

## 2. Prove the generator is the single source (drift gate)

```sh
dotnet fsi scripts/generate-skill-manifest.fsx --check
```

Expected: `skill-manifest: up to date (12 skills)` (exit 0). Hand-editing the JSON, or changing a
`template.json` condition without regenerating, makes this fail.

## 3. Run the guards

```sh
# new: presence + template.json-equivalence + supplied-by + fs-gg-project honesty
dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature238"

# unchanged, must stay green (backward-compat proof: reads only the 4 base keys)
dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature231"
```

Expected: both green. Feature238 asserts, per entry, that `materializes-when` equals the live
`template.json` condition for that skill's body source, that `supplied-by` matches the catalog source
dir, and that `fs-gg-project`'s condition evaluates **false** under `lifecycle=sdd` params and **true**
under `lifecycle=spec-kit`.

## 4. Backward-compatibility spot check

```sh
# the 4 base keys are unchanged vs the pre-feature manifest — only new keys were added
git diff template/skill-manifest/skill-manifest.json
```

Expected: the diff shows **only added** `materializes-when` / `supplied-by` lines; no `sha256`,
`resolvablePath`, `scope`, `id`, or `schemaVersion` value changed.

## 5. No emission-behavior regression

```sh
dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature204|FullyQualifiedName~Feature219"
```

Expected: green — the set of skills materialized in the `spec-kit` and `sdd` lanes is unchanged; this
feature only annotated the manifest.

## What "done" looks like

- `--check` clean; Feature238 + Feature231 + Feature204 + Feature219 green.
- `skill-manifest.json` diff is additive-only.
- Issue #71's honesty condition holds: `fs-gg-project` is now recorded as a `spec-kit`-lane skill, so
  the (cross-repo) union gate can classify its sdd-lane absence as legitimate rather than `[missing]`.
