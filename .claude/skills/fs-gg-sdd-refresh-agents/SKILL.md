---
name: fs-gg-sdd-refresh-agents
description: The FS.GG SDD cross-cutting generators — fsgg-sdd refresh (regenerate the work model, agent guidance, and summary.md to currency) and fsgg-sdd agents (derive per-target Claude/Codex command + skill guidance from the work model). Not lifecycle stages. Use whenever authored sources change.
---

# Generators: `refresh` and `agents`

These two commands are **not** lifecycle stages — they bring generated views back
to currency and emit no next-stage action. Run them whenever authored sources
change, because in SDD **presence is not currency**: a generated view on disk that
has not been re-derived from its current sources is stale.

Both are **per-work-item**: each takes a **required** `--work <id>` and operates on
that one work item's generated views. "Cross-cutting" here means *not a lifecycle
stage* — they emit no next-stage action and can run at any point — **not** "operates
across every work item at once". There is no "refresh all" form: an argless
`fsgg-sdd refresh` does **not** refresh every work item, it blocks with
`missingWorkId` (`Command 'refresh' requires --work.`). Pass `--work <id>`; to bring
several work items to currency, invoke it once per id. The same is true of
`fsgg-sdd agents`.

## `fsgg-sdd refresh --work <id>`

Regenerates the SDD-owned generated views to currency:

- rebuilds `readiness/<id>/work-model.json`,
- regenerates the per-target agent guidance,
- renders the human-readable `readiness/<id>/summary.md`,
- reports the currency of `analysis.json` / `verify.json` / `ship.json`.

It does not create those three lifecycle-owned views. When the responsible stage
has not run, refresh reports its currency as `missing` with an
`awaiting-lifecycle` disposition and returns a
non-blocking typed next action for the earliest applicable command: `analyze`,
`verify`, or `ship`. Missing or malformed authored sources and unreadable upstream
views remain blocking; repair those rather than advancing the lifecycle.

It is the only command that runs under an overwrite policy that allows refreshing
generated views.

```text
fsgg-sdd refresh --work <id>
```

## `fsgg-sdd agents --work <id>`

Derives per-target Claude/Codex command and skill guidance **from**
`readiness/<id>/work-model.json` into `readiness/<id>/agent-commands/<target>/`:

- `guidance.json` — the manifest (sources, digests, generator identity),
- `commands.md` — the per-work command guidance,
- `skills.md` — the per-work skill obligations projection.

```text
fsgg-sdd agents --work <id>
```

## Why generated guidance is not authority

The files under `agent-commands/<target>/` are a **projection of the work model**,
marked generated with source digests. They are governed by `.fsgg/agents.yml`:

```yaml
policy:
  generatedGuidanceIsAuthority: false
  requireEquivalentClaudeAndCodexBehavior: true
```

- `generatedGuidanceIsAuthority: false` — the generated `commands.md`/`skills.md`
  are never a second source of truth; the authored sources + work model are the
  contract.
- `requireEquivalentClaudeAndCodexBehavior: true` — Claude and Codex guidance must
  describe aligned behavior; divergence is a diagnostic.

## Two windows

- **Before the work model exists** (`charter`/`specify`/`clarify`/`checklist`):
  `agents`/`refresh` cannot derive per-work guidance, so they emit a **non-blocking
  advisory** (exit 0) pointing at `.fsgg/early-stage-guidance.md`. Only the
  *missing* work-model case is reclassified this way; malformed/stale/blocked still
  block.
- **After `verify`/`ship` build the work model:** the `agent-commands/<target>/`
  views become the live per-work guidance.

## Currency model

Every generated view records its `sources` (each with a sha256 `digest` and
`schemaVersion`), the `generator` id + version, and a `currency`
(`current · missing · stale · malformed · blocked`). Currency is one model:
the `work-model.json` `outputDigest` plus the source digests drive staleness,
which every downstream view inherits transitively — the views themselves record
no separate digest. The `sdd.yml` `generatedViews.staleBehavior: diagnostic`
setting means a digest mismatch emits a stale diagnostic rather than
hard-failing.

## Pitfalls

- Editing an authored source and trusting the old generated view — re-run
  `refresh`. The view is stale until re-derived.
- Treating `agent-commands/<target>/skills.md` as authoritative — it is a
  generated projection, not a source of truth.

## Not the remediation verbs

`refresh` regenerates SDD-owned **generated views** from authored sources; it never
re-seeds skeleton artifacts and never touches the CLI installation. Bringing a
*scaffolded product* back into coherence with the template pin, framework, and
`fsgg-sdd` CLI is the job of two separate cross-cutting commands:

- `fsgg-sdd doctor` — a read-only drift report (installed CLI vs required minimum,
  missing seeded artifacts, a dry-run preview); it never writes and exits 0.
- `fsgg-sdd upgrade` — the reconciliation verb (CLI self-update, template re-pin,
  re-seed of missing artifacts), each shown as a diff and confirmed (or `--yes`).
  It is the only command that mutates the CLI/consumer artifacts for remediation.

See `docs/reference/doctor-upgrade.md`.

## Related

- [[fs-gg-sdd-lifecycle]] (authored-source vs generated-view), [[fs-gg-sdd-verify]].

## Sources

- `docs/quickstart.md` (cross-cutting generators); any `.fsgg/agents.yml`.
