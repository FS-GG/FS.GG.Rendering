---
name: fs-gg-sdd-validate
description: The FS.GG SDD cross-cutting validation harness — fsgg-sdd validate exhaustively exercises command × output-projection × state matrices (determinism, degradation, release baseline-conformance, Governance-handoff compatibility) and emits one deterministic validation-report. Not a lifecycle stage. Use for CI/scheduled deep checks.
---

# Validate (cross-cutting harness)

`validate` is a heavy, on-demand verification of the *tool itself* — separate from
the cheap inner loop. It exhaustively exercises SDD's broad matrices and emits one
deterministic `validation-report`. It is **not** a lifecycle stage and is reachable
only as a CLI peer command, never from a lifecycle command path. It needs no
Governance runtime and computes no Governance verdict.

## Command

```text
fsgg-sdd validate
```

## What it sweeps

Every command × output projection × representative state, plus
determinism/degradation, release baseline-conformance, and Governance-handoff
compatibility. Unselected matrices are reported as `notValidated`, so a partial run
never reads as a full pass.

## Options

| Flag | Meaning |
|---|---|
| `--matrix <name>` | Restrict to one matrix. The harness defines four matrices — lifecycle, determinism, baseline, and compatibility; `compatibility` is the cheapest and the confirmed token. Omit `--matrix` to run them all; check `fsgg-sdd validate --help` for the exact token spellings of the other three. |
| `--out <path>` | Persist a **deterministic** projection (JSON or plain text) to a file — never rich ANSI. |
| `--rich` / `--text` / `--json` | Render the `validation-report` three ways (default JSON); `--rich` degrades to plain text when non-interactive or color-disabled. |

## Exit code

`0` iff the report's overall verdict passed, else `1`.

## Examples

```text
fsgg-sdd validate
fsgg-sdd validate --rich
fsgg-sdd validate --matrix compatibility --json
fsgg-sdd validate --matrix compatibility --rich --out readiness/validation.txt
```

## When to use

- In CI or on a schedule, as the deep check behind the cheap per-stage inner loop.
- Before a release, to confirm determinism and baseline-conformance.

It is not part of `init → … → ship`; do not insert it into the lifecycle order.

## A related peer: `registry validate`

`fsgg-sdd registry validate <path>` validates a cross-repo registry YAML document
and emits a deterministic `{ tool, path, valid, diagnostics[] }` verdict (exit `0`
iff valid). It is a separate CLI peer, used for the cross-repo dependency registry,
not the lifecycle.

## Related

- [[fs-gg-sdd-lifecycle]].

## Sources

- `src/FS.GG.SDD.Cli/Program.fs` (`printValidate`); `README.md`;
  `docs/quickstart.md`.
