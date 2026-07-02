# 0011 — Agent-skill roots carry the full union; `fsgg-sdd` owns the mirror

**Status**: Accepted (implementation superseded by [ADR-0014](./0014-skill-vendoring-one-manifest-one-materialize-verify.md); the five invariants stand) · **Date**: 2026-07-01

> **Pointer stub.** ADR-0011 is an **org-level** decision that lives in the shared
> `FS-GG/.github` decision log, **not** in this repo's local `0001`–`0010` sequence. It is
> surfaced here because it is cited normatively by rendering code and specs. The numbers
> `0011`–`0014` belong to the org ADR sequence and are **not** a continuation of this repo's
> local decisions — the two logs share the `NNNN` number space but are distinct.

**Canonical document**:
[`FS-GG/.github` → `docs/adr/0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md`](https://github.com/FS-GG/.github/blob/main/docs/adr/0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md)

## What it decides

A scaffolded product's three agent-skill roots (`.claude/skills/`, `.codex/skills/`,
`.agents/skills/`) must each hold the **byte-identical full union** of skills (SDD process
skills + provider UI skills), so the runtimes are interchangeable. There is a **single
mirror authority** — the `fsgg-sdd` orchestrator — and **providers are confined to
`.agents/skills/`**: the `fs-gg-ui` template MUST NOT write `.claude/skills/` or
`.codex/skills/` under any lifecycle.

## Where this repo relies on it

- `specs/229-drop-claude-skills-mirror/` — drops the template's `.claude/skills/` UI-skill
  emission (provider confinement, §3/§4); the repo-owned Feature 204/219 gates encode the
  invariant so a re-added mirror fails closed.
- `.template.config/template.json` — the `spec-kit`-lane materialize step notes that under
  `sdd`/`none` the orchestrator owns mirroring (ADR-0011 §2).
