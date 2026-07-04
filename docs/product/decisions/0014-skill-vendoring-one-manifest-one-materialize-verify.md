# 0014 — Skill vendoring & mirroring: one manifest, one materialize-and-verify

**Status**: Accepted (extends and amends [ADR-0011](./0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md); its invariants stand, mechanism wins) · **Date**: 2026-07-01

> **Pointer stub.** ADR-0014 is an **org-level** decision that lives in the shared
> `FS-GG/.github` decision log, **not** in this repo's local `0001`–`0010` sequence. It is
> surfaced here because it is cited normatively by rendering template config, scripts, tests,
> and specs. The numbers `0011`–`0014` belong to the org ADR sequence and are **not** a
> continuation of this repo's local decisions.

**Canonical document**:
[`FS-GG/.github` → `docs/adr/0014-skill-vendoring-one-manifest-one-materialize-verify.md`](https://github.com/FS-GG/.github/blob/main/docs/adr/0014-skill-vendoring-one-manifest-one-materialize-verify.md)

## What it decides

Replaces ADR-0011's four hand-maintained mirror mechanisms (and Feature 230's 36 per-root
`template.json` twins) with **one content-addressed algorithm**: a provider ships a single
**product skill-manifest** (`.agents/skills/skill-manifest.json`, per-skill canonical-body
sha256, ungated — present under every lifecycle) plus **one standalone materialize-and-verify
step** for the `spec-kit` lane. Product skills emit to `.agents/skills/` **only**, `copyOnly`
(verbatim canonical bodies that byte-match the manifest); the two lanes (standalone materialize
under `spec-kit`; `fsgg-sdd` fan-out under `sdd`) run the same algorithm and yield byte-identical
roots.

## Where this repo relies on it

- `.template.config/template.json` — the skill-manifest row, the single materialize step, and
  `.agents/skills/`-only `copyOnly` product-skill sources all cite ADR-0014.
- `scripts/generate-skill-manifest.fsx`, `scripts/validate-lifecycle-template.fsx` — generate
  and enforce the manifest (digests + cross-root identity, §3).
- `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` — asserts `.agents/skills/`-only
  emission and forbids resurrected per-root twins.
- `specs/231-skill-manifest-materialize/` — the feature that implements it.

## Condition-aware extension (Feature 238 / ADR-0017)

The manifest entry gained two **additive, optional** string fields (`schemaVersion` stays `1`):

- `materializes-when` — the **verbatim** `.template.config/template.json` `sources[].condition`
  that gates the skill's body (single source of truth). Turns a `declared ∧ absent` gap into an
  honest `declared ∧ condition-false ∧ absent`, so a downstream union gate can tell legitimate
  suppression from a real supply failure.
- `supplied-by` — the provider source directory holding the canonical `SKILL.md`.

Motivating case: `fs-gg-project` is declared `scope:product` but emits only under
`lifecycle == "spec-kit"`; recording that condition resolves the sdd-lane "supplied by nobody"
gap ([`#71`](https://github.com/FS-GG/FS.GG.Rendering/issues/71)). `scripts/generate-skill-manifest.fsx`
emits both fields; `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` re-reads
`template.json` and fails on any drift. The org-level `registry/skills.yml` + `skill-registry`
contract and the `[missing]`/`[unexpected]` gate enforcement are owned by `FS-GG/.github#164`;
see `specs/238-skill-materializes-when/`.
