---
name: fs-gg-sdd-ship
description: Stage 10 (terminal) of the FS.GG SDD lifecycle — fsgg-sdd ship aggregates SDD-owned merge-boundary readiness into readiness/<id>/ship.json and points ship-ready work at the optional, Governance-owned protected-boundary handoff. SDD reports readiness; it never enforces. Use after verify.
---

# Ship (stage 10, terminal)

`ship` is the merge boundary. It aggregates lifecycle, verification, and evidence
state into a single readiness view and tells you whether the work item is
ship-ready. It authors **no** `work/<id>/` source. It is the terminal lifecycle
command — there is no successor inside SDD.

**Read the worked example first.** `ship` authors nothing — it aggregates what the earlier
stages produced. `docs/examples/lifecycle-artifacts/` is a complete, machine-checked instance
of that whole cascade: parser-validated on every build, and driven through the real `charter`
→ `analyze` gates by the skill↔gate doctest. When `ship` reports not-ready, it is the
reference for what a coherent set of artifacts looks like.

## Command

```text
fsgg-sdd ship --work <id>
```

## Produces / consumes

- **Consumes:** verification readiness and the aggregated authored sources.
- **Authors:** nothing under `work/<id>/`.
- **Tool generates:** `readiness/<id>/ship.json` (lifecycle readiness,
  verification readiness, disposition, findings, readiness verdict) and
  `readiness/<id>/ship-verdict.json` — the compact, **committed** merge-boundary
  verdict (ADR-0026). `ship.json` is regenerable and gitignored; the verdict is
  the one readiness view you commit, because "was this ship-ready when it
  merged?" is not a question regeneration can answer. It carries the
  disposition, blocking finding ids, verification-readiness status, generator,
  and a `sourcesDigest` binding it to the exact authored inputs.
- **Next:** the optional Governance-owned protected-boundary handoff.

## The Governance boundary (this is the key part)

**SDD reports readiness; it never enforces.** `ship` aggregates SDD-owned
merge-boundary readiness and *points* ship-ready work at the
**Governance-owned protected-boundary handoff** (`governance-handoff.json`). That
handoff is **optional and lives outside SDD** — SDD never evaluates routing,
evidence freshness, profiles, gates, audit, or release decisions. Those belong to
FS.GG.Governance.

This is why you can run the entire lifecycle, `init` → `ship`, with **no
Governance runtime installed**: the Governance config files (`.fsgg/policy.yml`,
`.fsgg/capabilities.yml`, `.fsgg/tooling.yml`) may be absent, present, or even
malformed and `ship` still succeeds — SDD reports them only as optional
compatibility facts (state `notEvaluated`).

When Governance *is* adopted, it consumes the same `ship` readiness as the input
to its protected-branch gate; SDD's side of the contract is unchanged. Adopting it
is additive — see `docs/adopting-governance.md`.

## Pitfalls

- Expecting `ship` to "enforce" a merge block — it does not. It reports readiness;
  enforcement is Governance's job (or your CI's, consuming `ship.json`).
- Treating the Governance handoff as required. It is optional; SDD ship readiness
  is useful on its own.

## Next

- Optionally adopt Governance (`docs/adopting-governance.md`), or consume
  `ship.json` from your own CI.

## Related

- [[fs-gg-sdd-verify]], [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/quickstart.md`; `docs/adopting-governance.md`;
  `src/FS.GG.SDD.Artifacts/LifecycleArtifacts/Ship.fsi`.
