# Contracts — Feature 231

## C1 — Shipped skill-manifest (consumed by: standalone materialize, repo gates, P3 composition gate)

- Path in product: `.agents/skills/skill-manifest.json` (every lifecycle).
- Shape: `skill-manifest` schema v1 — `{ "schemaVersion": 1, "skills": [ { "id", "scope":
  "product", "sha256", "resolvablePath" } ] }`, entries sorted by `id`; `sha256` =
  lowercase-hex SHA256 over the canonical `SKILL.md` UTF-8 bytes; `resolvablePath` =
  `.agents/skills/<id>/SKILL.md`.
- Guarantee: for every entry whose `resolvablePath` exists in a scaffold, the file's bytes
  hash to `sha256` (verbatim emission), and after the materialize step (standalone) or the
  orchestrator fan-out (sdd) the same bytes exist under every root in `AGENT_SKILL_ROOTS`.

## C2 — Vendored algorithm (consumed by: fsx driver, parity gate)

`FsGg.Vendored.SkillMirror` exposes exactly the `Fsgg.SkillMirror` surface —
`providerSourceRoot`, `sha256`, `skillPath`, `skillIdOfPath`, `mirrorTargetRoots`,
`retargetSkillPath`, `MirrorWrite`, `mirror`, `verify` (+ `ExpectedSkill`/`ActualCopy`/
`SkillDrift` with a local `SkillScope`) — and a vendored `agentSkillRoots` constant.
Behavioral contract: for all inputs, vendored output = library output (Package.Tests parity
gate, `FS.GG.Contracts == 1.4.0`).

## C3 — Materialize driver CLI (consumed by: MSBuild target, lifecycle validator live loop)

```
dotnet fsi .specify/scripts/fs-gg/materialize-skill-roots.fsx [--enforce] [--product-root <dir>]
```

- Mirrors every file under `<root>/.agents/skills/**` into `.claude/skills/**` +
  `.codex/skills/**` (byte-identical, skip-if-identical, dirs created).
- Verifies (manifest-driven) presence ∧ cross-root identity ∧ digest-match; prints
  `fs-gg-skill-roots: ok (<n> skills, <k> files mirrored)` or per-skill drift lines
  `fs-gg-skill-roots: DRIFT <id> missing=[..] divergent=<b> hash-mismatch=[..]`.
- Exit code: `0` always without `--enforce` (advisory, roadmap staged rollout); with
  `--enforce`, non-zero iff drift or IO failure.
- No-ops (exit 0, message) when `.agents/skills/` is absent.

## C4 — Emission contract (consumed by: Feature 204/219/231 gates; supersedes Feature 230's)

- spec-kit: `.agents/skills/` = `speckit-*` ∪ profile-selected catalog skills ∪
  `fs-gg-project` ∪ (conditionals) ∪ `skill-manifest.json`; `.claude/skills/` at generation =
  base `fs-gg-project` only; `.codex/` at generation = absent; post-build all three roots =
  byte-identical union.
- sdd/none: product skills + manifest to `.agents/skills/` **only**; no materialize script;
  no `.claude`/`.codex` writes by the template.
- No emitted skill body may reference a path absent from the product tree it ships into
  (no-dangling-route guard, all profiles × lifecycles).
