---
name: fs-gg-sdd-getting-started
description: Start an FS.GG SDD project — fsgg-sdd init (seed the lifecycle skeleton in an existing repo) and fsgg-sdd scaffold (empty directory to a buildable, runnable product via a template provider). Use before the lifecycle stages; then go to fs-gg-sdd-lifecycle.
---

# Getting Started: `init` and `scaffold`

Two commands establish an SDD project. Both seed the same skeleton; `scaffold`
additionally materializes a runnable product from a template provider. After
either, drive the lifecycle with [[fs-gg-sdd-lifecycle]].

## `fsgg-sdd init` — seed the skeleton in place

Use in an existing repository to add the SDD lifecycle skeleton.

```text
fsgg-sdd init --root .
```

It creates:

- `.fsgg/` — project config: `project.yml`, `sdd.yml`, `agents.yml` (and an
  optional Governance compatibility surface).
- `work/` — where your authored lifecycle sources will live, one dir per work item.
- `readiness/` — where generated readiness views are written.
- `CLAUDE.md` / `AGENTS.md` — thin agent-guidance pointer files.
- `.fsgg/constitution.md` — the seeded product constitution (the doctrine).
- `.fsgg/early-stage-guidance.md` — the authoring guide for the pre-work-model
  window (`charter`/`specify`/`clarify`/`checklist`).

`init` is **deterministic and no-clobber**: re-running it is safe and never
overwrites your edits to the constitution or guidance. **Next action:** `charter`.

## `fsgg-sdd scaffold` — empty directory to a runnable product

Use to go from nothing to a buildable, SDD-managed product in one command. It
seeds the `init` skeleton (byte-identical to `init`), then invokes an external
**template provider** (selected with `--provider`, resolved from
`.fsgg/providers.yml`) to materialize the actual product via `dotnet new`.

```sh
fsgg-sdd scaffold --root ./MyApp --provider <name> --param productName=MyApp

cd ./MyApp && dotnet build && dotnet run   # the runnable product
fsgg-sdd charter                           # continue the lifecycle
```

Options:

| Flag | Meaning |
|---|---|
| `--provider <name>` | **Required.** Template provider from `.fsgg/providers.yml`. With none, scaffold blocks and points you at `fsgg-sdd init` for the skeleton only. |
| `--param key=value` | Repeatable. Forwarded verbatim to the provider as `--key value`. |
| `--force` | Materialize into a non-empty target / pass provider force. |
| `--no-update` | Skip the managed template + agent-context refresh (otherwise `dotnet new install`/`update` runs each time). |
| `--dry-run` | Plan the `dotnet new` invocation without executing. |

What scaffold owns: the provider contract, the invocation, the
`.fsgg/scaffold-provenance.json` record (produced paths marked `generatedProduct`
— externally owned, out of SDD's refresh scope), and the post-instantiation steps
(`git init` at the product root, `chmod +x` on produced `.sh` scripts). It records
**no** provider-specific package id, template id, or path — the reference runnable
provider ships in FS.GG.Rendering.

**Boundary guard:** the provider must never write into SDD-owned trees (`.fsgg/`,
`work/`, `readiness/`, and the whole `.claude/skills/` and `.codex/skills/` roots);
scaffold guards this and fails (exit 2) if violated. The neutral `.agents/skills/`
root is the provider's to write, **except** the reserved `fs-gg-sdd-*` namespace,
which is SDD's even there.

## Three-root skill fan-out (the union model)

An FS.GG product carries the same skills in **three** agent-skill roots —
`.claude/skills/`, `.codex/skills/`, and the neutral `.agents/skills/` — so the
Claude, Codex, and neutral runtimes are interchangeable (`claude ≡ codex ≡ agents`).
`fsgg-sdd` is the **sole mirror authority**:

- `init` seeds the 16 `fs-gg-sdd-*` process skills byte-identically into **all three**
  roots (no-clobber; your edits are preserved).
- A provider writes its own `fs-gg-*` UI skills **only** into `.agents/skills/`;
  `scaffold` then fans the byte-identical **union** (seeded ∪ provider) out into all
  three roots. The mirrored `.claude`/`.codex` copies are recorded in
  `.fsgg/scaffold-provenance.json` under `mirroredPaths` (owner `mirrored`); the
  provider's canonical `.agents` skill stays in `producedPaths` (`generatedProduct`).
- `refresh` re-mirrors the union to currency; `doctor` reports a product whose three
  roots have drifted (e.g. scaffolded by a two-root CLI); `upgrade` reconciles it
  no-clobber. An incomplete fan-out is never reported complete (a mirror I/O fault
  fails at exit 2 with `scaffold.mirrorFailed`).

## CLI version coherence

A scaffolded product is produced by two inputs: the template pin and the `fsgg-sdd`
CLI that seeds the skeleton (including the 16 `fs-gg-sdd-*` skills and
`.fsgg/early-stage-guidance.md`). Scaffold records **both** in
`.fsgg/scaffold-provenance.json`: the producing CLI version (`generator.version`) and
the provider-declared minimum coherent CLI version (`requiredMinimumCliVersion`,
string-or-null). When the installed CLI is **behind** that minimum, scaffold emits a
**non-blocking** `scaffold.cliBehindMinimum` advisory (scaffold still completes, exit
code unchanged) naming the installed version, the required minimum, and how far behind
it is.

**Remedy for a behind-minimum CLI:** upgrade `fsgg-sdd`, then re-run **`fsgg-sdd init`**
in the existing product to re-seed the `fs-gg-sdd-*` skills and
`.fsgg/early-stage-guidance.md` (idempotent, no-clobber — your edits are preserved).
**`fsgg-sdd refresh` does not re-seed**; it only brings generated views to currency, so
it cannot recover the missing seeded skills. A malformed provider `minimumCliVersion`
surfaces a `scaffold.providerMinimumMalformed` warning and is recorded as `null` (never
silently ignored).

## Exit codes

- Malformed user input (unknown provider, unsupported contract version, missing
  required `--param`, target collision) → blocked, **exit 1**.
- Provider defect (provider failed, engine unavailable, provider wrote into SDD
  trees) → **exit 2**. An incomplete scaffold is never reported as complete. On these
  three defects the scaffold report carries a `providerInvocation` block (json/text/rich)
  with the provider's invoked command line, captured stdout/stderr, and exit code
  (`null` when the engine never launched) — so a failure is diagnosable from the report
  alone, with no `PATH` shim or re-run. It is `null` on success, dry-run, and user-input
  blocks, is bounded per stream with a truncation flag, and never touches
  `.fsgg/scaffold-provenance.json` (schema v1).

## Output formats

Both honor `--rich` / `--text` / `--json` (default JSON) — see
[[fs-gg-sdd-lifecycle]].

## Keeping a scaffolded product current

Two cross-cutting commands reconcile drift between a scaffolded product and its
coherent set (the template pin, the framework, and the `fsgg-sdd` CLI):

- `fsgg-sdd doctor` — a **read-only** drift report: installed CLI vs the pin's
  required minimum, which seeded artifacts are present vs expected, and a dry-run
  preview of what `upgrade` would change. It never writes and exits 0 whenever it
  reports.
- `fsgg-sdd upgrade` — the reconciliation verb: CLI self-update, template re-pin,
  and re-seed of the missing seeded artifacts, **each shown as a diff and
  confirmed** before it is applied (or all at once with `--yes`). It is the **only**
  command that mutates the CLI installation or consumer artifacts for remediation; a
  non-interactive run without `--yes` refuses (exit 1) rather than acting silently.
  CI keeps the tool pinned via `.config/dotnet-tools.json`.

See `docs/reference/doctor-upgrade.md`.

## Next

- `charter` — establish your first work item: [[fs-gg-sdd-charter]].
- Adopting an existing Spec Kit project instead? It is additive — run `init`, then
  author native sources. See `docs/migration-from-spec-kit.md`.

## Related

- [[fs-gg-sdd-lifecycle]] — the whole process map.
- [[fs-gg-sdd-charter]] — the first lifecycle stage.

## Sources

- `README.md` ("Create a new project"), `docs/quickstart.md` (`init`).
