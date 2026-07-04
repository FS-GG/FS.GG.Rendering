# Contract — product skill-manifest JSON (`schemaVersion: 1`, additive fields)

**Artifact:** `template/skill-manifest/skill-manifest.json`
**Producer (sole):** `scripts/generate-skill-manifest.fsx`
**Consumers:** `tests/Package.Tests/Feature231*`, `Feature238*` (in-repo); the proposed
`FS-GG/.github` `registry/skills.yml` generator + `skill-union-assert.sh` (cross-repo, #164).
**Registry contract id:** `skill-registry` (proposed, ADR-0017) / superset of ADR-0014's manifest.

## Compatibility statement

This revision is **additive and backward-compatible**. `schemaVersion` stays `1`. Two optional string
keys are added to each `skills[]` entry. A consumer that reads only `{id, scope, sha256,
resolvablePath}` observes **byte-identical values** to the prior manifest (digests and paths are
unchanged); it simply ignores the new keys. Therefore this is not a breaking contract change and needs
no version negotiation.

## Schema (informal)

```jsonc
{
  "schemaVersion": 1,
  "skills": [
    {
      "id": "fs-gg-project",                       // string, unique, entries sorted asc by id
      "scope": "product",                          // const "product"
      "sha256": "<64-char lowercase hex>",         // hex(SHA256(UTF8(SKILL.md text)))
      "resolvablePath": ".agents/skills/fs-gg-project/SKILL.md",
      "materializes-when": "(lifecycle == \"spec-kit\")",   // NEW — verbatim template.json condition
      "supplied-by": "template/base/.agents/skills/fs-gg-project/"  // NEW — provider source dir, trailing slash
    }
    // … 11 more, see data-model.md for the full table
  ]
}
```

## Field contracts (the two new keys)

### `materializes-when` : string (required for every entry in this manifest)

- Value is the **verbatim** `sources[].condition` string that `.template.config/template.json` applies
  to the source emitting this skill's canonical body.
- Grammar: `==` of `profile`/`lifecycle`/`feedback` against a quoted literal or `true`, combined with
  `||`/`&&`, optionally parenthesized (the template-engine condition grammar).
- Evaluated against a params set `{profile, lifecycle, feedback}`, it yields whether the skill's body
  is emitted. In particular `fs-gg-project` → **false** for `lifecycle=sdd`, **true** for
  `lifecycle=spec-kit`.
- **Invariant:** MUST stay equal to the live template.json condition (enforced by Feature238).

### `supplied-by` : string (required for every entry in this manifest)

- Repo-relative provider source **directory** (trailing `/`) that holds the canonical `SKILL.md`.
- Equals `dirname(canonical-source(id)) + "/"`.
- For `fs-gg-project`, this is the per-skill dir `template/base/.agents/skills/fs-gg-project/`, which
  is distinct from the whole-tree source that carries its `materializes-when` gate.

## What a downstream consumer may rely on

1. Presence of both keys on every entry of a `schemaVersion: 1` manifest emitted at/after this feature.
2. `materializes-when` is a directly evaluable condition in the template-engine grammar (no
   re-encoding needed) — ADR-0017's "literally the `template.json` `sources[].condition`".
3. Distinguishing `declared ∧ condition-false ∧ absent` (legitimate) from `declared ∧ condition-true
   ∧ absent` (`[missing]`, a real supply failure) once params are supplied.

## What is explicitly out of scope of this contract revision

- The `registry/skills.yml` YAML shape and the `skill-registry` governed-contract registration
  (owned by `.github#164`).
- Any gate that fails the build on `[missing]`/`[unexpected]` (owned by `.github#164`; this manifest
  only supplies the honest input).
