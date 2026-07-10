# Product shape

This folder defines **what the FS.GG.Rendering product owns** — the output of migration
**Stage R2 (Define product shape)**. These are decision/definition artifacts produced *before*
any source is copied (source import is Stage R4).

## Contents

- **[module-map.md](./module-map.md)** — the authoritative catalog of product areas, their
  responsibilities, and their import disposition; the answer to "what does rendering own?"
- **[layering.md](./layering.md)** — the four UI layers (semantic controls, design-system
  primitives, themes, design-specific kits) and the one-control-set rule.
- **[docs-to-import.md](./docs-to-import.md)** — triage of source docs for Stage R4
  (import-as-is / adapt / exclude).
- **[ant-design/](./ant-design/README.md)** — Ant Design adoption (Workstream F): per-family
  interaction-pattern docs + enterprise-template recipes. The
  **[Ant source-of-truth hub](./ant-design/reference/ant-llms-sources.md)** is the canonical
  upstream Ant reference for FS.GG — it catalogs the three Ant LLM files (`llms.txt`,
  `llms-full.txt`, `llms-semantic.md`); cite it rather than raw `ant.design` URLs.
- **decisions/** — recorded product-shape decisions. **Two number spaces share this folder:**
  `0001`–`0010` are **repo-local** ADRs; `0011`–`0014` are **pointer stubs** for org-level ADRs
  that live in [`FS-GG/.github` → `docs/adr/`](https://github.com/FS-GG/.github/blob/main/docs/adr/)
  and are cited normatively by rendering code — they are *not* a continuation of the local
  sequence.
  Because the org sequence has run past `0014`, a repo-local ADR numbered `0015` would collide with
  a future stub. **Repo-local ADRs therefore resume at `0100`**; org stubs keep their org number.
  - [0001-package-identity.md](./decisions/0001-package-identity.md) — accepted at R8: rebranded
    `FS.Skia.UI.*` → `FS.GG.UI.*`.
  - [0002-template-ownership.md](./decisions/0002-template-ownership.md) — rendering repo owns
    the templates for now.
  - [0003-designsystem-namespace-relocation.md](./decisions/0003-designsystem-namespace-relocation.md)
    — relocate the design-system namespace.
  - [0004-public-token-resolver-surface.md](./decisions/0004-public-token-resolver-surface.md)
    — the public token/style-resolver surface.
  - [0005-ant-design-pattern-docs.md](./decisions/0005-ant-design-pattern-docs.md) — F6 docs-only
    scope, `Catalog.categories` coverage anchor, and the three-Ant-LLM-files source of truth.
  - [0006-antdesign-theme-and-new-controls.md](./decisions/0006-antdesign-theme-and-new-controls.md)
    — Ant Design theme + new controls adoption.
  - [0007-antdesign-charts-adoption.md](./decisions/0007-antdesign-charts-adoption.md)
    — Ant Design charts adoption.
  - [0008-g2-sample-apps.md](./decisions/0008-g2-sample-apps.md) — G2 sample apps.
  - [0009-g3-ant-showcase.md](./decisions/0009-g3-ant-showcase.md) — G3 Ant showcase.
  - [0010-fs-gg-ui-template-default-starter.md](./decisions/0010-fs-gg-ui-template-default-starter.md)
    — a replaceable `game` profile as the `fs-gg-ui-template` default starter.
  - [0100-gate-is-a-required-check.md](./decisions/0100-gate-is-a-required-check.md) — `gate`
    becomes a required check on `main`; release PRs are no longer expected-red, and the tag-window
    freeze is scheduled for removal.
  - [0101-apicompat-stays-advisory.md](./decisions/0101-apicompat-stays-advisory.md) — `API
    compatibility gate` is authorized to be required but stays advisory until it is green on `main`;
    fixes the baseline-selection defect that kept it red, and corrects ADR-0100's account of why.
    **Title and Decision superseded in part by ADR-0103** — the check is now required.
  - [0102-symbology-secondary-heading-channel.md](./decisions/0102-symbology-secondary-heading-channel.md)
    — `Token` gains an opt-in second rotation channel (`Heading2`); the fixed grammar stays fixed.
  - [0103-gate-is-fully-enforced.md](./decisions/0103-gate-is-fully-enforced.md) — the pre-merge gate
    is fully enforced: both `gate.yml` contexts are required on `main` and `enforce_admins` is on, so
    a `CP####` break blocks the merge and no admin bypasses it.
  - [0104-canvas-loop-is-a-simulation-primitive.md](./decisions/0104-canvas-loop-is-a-simulation-primitive.md)
    — the fixed-step double buffer is a simulation primitive owned by `FS.GG.Game.Core.Loop`;
    `FS.GG.UI.Canvas.Loop` is deprecated and retires at the next Canvas major, with no re-export.
  - **Org-level pointer stubs** (canonical text in `FS-GG/.github`):
    - [0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md](./decisions/0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md)
      — agent-skill roots carry the full union; `fsgg-sdd` owns the mirror; providers confined
      to `.agents/skills/`.
    - [0012-dual-publish-to-nuget-org.md](./decisions/0012-dual-publish-to-nuget-org.md)
      — dual-publish the byte-identical set to public nuget.org, gated after the org feed.
    - [0013-trusted-publishing-oidc-for-nuget-org.md](./decisions/0013-trusted-publishing-oidc-for-nuget-org.md)
      — nuget.org push via Trusted Publishing (OIDC), no stored key, in-repo workflow.
    - [0014-skill-vendoring-one-manifest-one-materialize-verify.md](./decisions/0014-skill-vendoring-one-manifest-one-materialize-verify.md)
      — one skill-manifest, one content-addressed materialize-and-verify across both lanes.

## See also

- Project rules: [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md).
- The migration roadmap (R1→R8) and the feature spec for this stage:
  `specs/001-define-product-shape/`.
