# Decision addendum — `template/base/` carries all three ADR-0011 roots (FS.GG.Rendering#1081)

**Status**: Decided 2026-07-27 (maintainer call recorded on
[FS.GG.Rendering#1081](https://github.com/FS-GG/FS.GG.Rendering/issues/1081)). Implemented in the same
item. **SUPERSEDED 2026-08-02 by
[FS.GG.Rendering#1121](https://github.com/FS-GG/FS.GG.Rendering/issues/1121), in the same change that
removed `template/base/.codex/`.**

> **Why "superseded" and not "reverted".** ADR-0067 §5, executed 2026-07-28 by
> [`.github#1636`](https://github.com/FS-GG/.github/issues/1636) — **one day** after this decision was
> recorded — narrowed the org's ordered agent-skill root set from ADR-0011's three to ADR-0065's two:
> `.claude/skills`, `.agents/skills`. `.agents/skills` is Codex CLI's own second native discovery root
> (re-measured against `codex debug prompt-input` on Codex CLI 0.145.0), so `.codex/skills` carried no
> runtime the other two did not, and its only observable effect was a duplicate model-visible catalog
> entry per skill. ADR-0065 §Retiring a root is explicit that a root leaves the declared set by
> **contract migration, never by deletion** — the distinction is between *hiding* a duplicate and
> *deciding* one is unnecessary. This decision's premise (ADR-0011's three-root union as the correct
> shape for a scaffolding base tree) was correct when it was made and stopped being correct the next
> day for a reason external to it, not because the reasoning below was wrong. **Everything below this
> notice is the ORIGINAL 2026-07-27 record, preserved for the "why" — it is history, not current
> instruction. Its concrete claims (`template/base/.codex/` exists; three roots; the `.codex/**`
> exclude entry) are FALSE as of #1121; do not act on them.**
>
> **#1121's disposition, recorded here per that item's acceptance criterion 1:** `template/base/.codex/`
> is deleted (git history preserves the original three-root tree this record documents), the
> `.codex/`-gated source row and the now-vacuous `.codex/**` exclude entry are removed from
> `.template.config/template.json`, `template/lifecycle/materialize-skill-roots.fsx` (via its vendored
> `agentSkillRoots` constant) no longer fans into `.codex/skills`, and
> `Issue1081TemplateBaseSkillRootsTests.fs` / `Feature231SkillManifestTests.fs` /
> `Feature204LifecycleTemplateTests.fs` assert TWO roots plus the retired root's absence.
>
> **The template's own emitted root set (issue #1121 acceptance criterion 6): a TEMPLATE CONSTANT,
> not derived per-scaffold from a live pin.** The standalone spec-kit lane's materialize script
> (`template/lifecycle/materialize-skill-roots.fsx`) is a vendored, dependency-free `.fsx` — no
> restore, no network — that is copied byte-for-byte into every scaffolded product at scaffold time.
> Its root set comes from the vendored `agentSkillRoots` constant in
> `template/lifecycle/skill-mirror-vendored.fs`, which this repo keeps in parity with the PINNED
> `FS.GG.Contracts` package (`Directory.Packages.local.props`, test-only) via the G-PARITY gate
> (`Feature231SkillManifestTests`). Nothing in a scaffolded product re-reads that pin at build or run
> time — a product scaffolded today carries whatever root set was vendored into this repo's
> `template/lifecycle/` at the commit it was scaffolded from, permanently. So the next bump is
> expected to come from **this repo**, deliberately: an ADR-0065 root-set change bumps
> `template/lifecycle/skill-mirror-vendored.fs`'s `agentSkillRoots` constant, in the same change as
> the `FS.GG.Contracts` pin bump the G-PARITY gate requires (see the comment on that pin in
> `Directory.Packages.local.props`) — never a runtime auto-derivation, and never a silent drift
> between what a product ships and what a later reader of this repo would expect it to ship.

**Read this before concluding that `template/base/.claude/skills/` or `template/base/.codex/skills/`
contradicts the feature this directory documents. It does not, and this file exists so that
conclusion is not reached a fourth time.**

## The apparent contradiction

Feature 229 (**Shipped**) removed the fs-gg-ui template's `.claude/skills/` UI-skill emission, because
**ADR-0011 §3 confines a provider to `.agents/skills/`**: "A provider MUST NOT write `.claude/skills/`
or `.codex/skills/`". The spec's own *Observed today* section describes an asymmetry that looks
identical to the one #1081 was filed about — the template writing `.agents/` *and* `.claude/` while
`.codex/` is untouched — and calls the `.claude/` write **unintended**.

So a reader who arrives at `template/base/` and finds three skill roots, one of them `.codex/`,
reasonably suspects that 229 was reverted.

## Why it is not a contradiction: two different subjects

| | Feature 229's subject | #1081's subject |
|---|---|---|
| **What** | what the template **emits into a generated product's** skill roots | what the **committed base tree in this repository** holds |
| **Governed by** | ADR-0011 §3/§4 — provider confinement, orchestrator-owned fan-out | ADR-0011's three-root union as a property of a tree |
| **Authority for the roots** | the `fsgg-sdd` orchestrator (ADR-0008), which computes the union and fans it out | nothing — the base tree is copied, so it must carry them itself |
| **Verdict** | provider writes `.agents/skills/` **only** | base tree holds `.agents/` ∧ `.claude/` ∧ `.codex/`, byte-identical |

ADR-0011 §3 is a rule about a **write into a scaffolded product**. `template/base/` is not a
scaffolded product; it is the scaffolding **source**. Nothing writes skill roots into it — they are
authored there. Confinement has no subject to bind in a tree that is the provider's own source.

## 229 did not merely permit this — it deferred it, in writing

This is the load-bearing point, and it is stronger than "different subject". Two of Feature 229's own
**Assumptions** (`spec.md`, *Assumptions*) name this exact question and set it aside:

> **The base `fs-gg-project` skill is workspace infrastructure, not a UI product-skill mirror.** It
> ships inside the base `.claude/` workspace tree (`template/base/.claude/`) and is the standalone
> Spec Kit workspace's own authoring skill. It is **exempt** from the "0 UI product skills in
> `.claude/skills/`" invariant (which counts the UI product / sample / feedback set).

> **Standalone `spec-kit` discoverability is the orchestrator's concern.** [...] This feature does
> **not** add an in-template three-root mirror for the standalone `spec-kit` lane; if standalone
> `spec-kit` needs the union in `.claude/skills/`, **that is a separate orchestrator/template concern
> tracked elsewhere.**

#1081 **is** that "elsewhere". Feature 229 scoped itself to the UI product-skill set, explicitly
exempted `template/base/`'s `fs-gg-project` from its invariant, and explicitly declined to answer the
standalone-lane union question. #1081 answers it. **No requirement, success criterion, or invariant
of 229 changes**, and none is weakened:

- **SC-002** counts *template-authored `fs-gg-*` UI skill files* under `.claude/skills/` / `.codex/skills/`
  in a **scaffolded product**. `fs-gg-project` is not in that set (229's own exemption above), and
  under `sdd` / `none` the base `.claude/` **and** `.codex/` trees are not emitted at all — both are
  `lifecycle == "spec-kit"`-gated sources.
- **SC-001** (`0` `scaffold.providerWroteSddTree` intrusions) is what the `.codex/**` entry added to
  the ungated base source's `exclude` list in `.template.config/template.json` **protects**. Adding
  `template/base/.codex/` without that exclusion would have had the ungated source emit
  `.codex/skills/fs-gg-project/` on *every* lifecycle — re-creating exactly the ADR-0011 §3 provider
  write 229 removed, and re-tripping `isSddTree`. Completing the base **tree** must not re-open the
  provider **leak**; the exclude list is where the two are held apart, and it is commented as such.
- **SC-003** (`sdd` and `none` produce identical skill-tree output) holds unchanged, for the same
  reason: neither lane emits either gated root.

## What the standalone lane actually does, stated honestly

The #1081 decision's stated reasoning is that "the standalone lane has no `fsgg-sdd` orchestrator to
compute the union and fan it out, so the base tree must carry the roots itself". That is right about
the **orchestrator**, and it is worth being precise about the rest, because a reader who finds
`template/lifecycle/materialize-skill-roots.fsx` will otherwise think the premise was simply wrong:

- The standalone **spec-kit** lane *does* ship a fan-out — `materialize-skill-roots.fsx` (Feature 231,
  ADR-0014 §Decision 2), emitted to `<product>/.specify/scripts/fs-gg/`, which mirrors
  `.agents/skills/` into `.claude/` and `.codex/` and verifies the invariant. But it runs **at the
  product's first build**, not at scaffold time, and it ships **only** in that lane.
- The **`none`** lane has neither an orchestrator nor that script.
- And none of the three does anything at all for the **committed tree in this repository**, which is
  the subject that was audited by nothing.

So the decision's conclusion stands on its own tree regardless: the repository's `template/base/` had
no fan-out authority of any kind, and the consequence was not hypothetical. See below.

## The drift this closed (the evidence that prose is not a gate)

At `origin/main` before this item, `template/base/.claude/skills/fs-gg-project/SKILL.md` had digest
`4cfdc0f832819986b2ea7131b6fbde09d4faffbe7310829e311e0133be571ba2`, while the canonical
`template/base/.agents/skills/fs-gg-project/SKILL.md` — and the `sha256` the shipped
`template/skill-manifest/skill-manifest.json` declares for `fs-gg-project` — were
`c9fac83fb4ebb1f29f666cd206e4f47a88c0686a0245f21f5fa48dc2209a54a0`.

The `.claude/` copy had missed three commits that updated the canonical body (`1f027841`, `2a5718da`,
`6cd447a9`) and was left partially updated by a fourth (`9b1824e1`). Meanwhile
`.template.config/template.json` asserted, in a comment, that "`copyOnly` keeps the `fs-gg-project`
body byte-identical to the `.agents/` canonical copy (skill-manifest digest)". It was not, and
**nothing could tell**, because the claim lived in prose and the tree was outside every gate's
subject. That is the `FS-GG/.github#266` shape — a green reported over something no check can see.

## What is in each root, and what deliberately is not

| path | in `.agents/` | in `.claude/` | in `.codex/` |
|---|---|---|---|
| `skills/fs-gg-project/SKILL.md` | yes (canonical) | yes | yes |
| `hooks/validate-speckit-project.sh` | **no** | yes | **no** |
| `settings.json` | **no** | yes | **no** |

`hooks/` and `settings.json` are **not** triplicated, and that is a decision rather than an omission.
They are not skill-root content: the skill-union subject is `<root>/skills`, so they are outside the
assertion either way. More to the point, `settings.json` is **Claude Code's own configuration
schema** — `permissions.allow`, a `hooks.UserPromptSubmit` array, and a command referencing
`$CLAUDE_PROJECT_DIR` — and `hooks/validate-speckit-project.sh` exists only because that file points
at it. Codex does not read that schema and the generic `.agents/` surface has no settings contract at
all. Copying them into `.codex/` and `.agents/` would create files no runtime reads, and three copies
of a config that only one runtime honours is a drift source with no reader — the same failure mode as
the drift above, deliberately manufactured. #1081's issue body says the same thing from the other
direction: these files "are **not** skill-root writes and are irrelevant to ADR-0011 §3 either way".

## How the tree is audited now

`.github/workflows/template-base-skill-union.yml` — a `product-path: template/base` caller of
`FS-GG/.github`'s reusable `skill-union-assert.yml`, with `manifest:` supplied so the digest
cross-check is on. Its `pull_request` **and** `push` triggers carry `paths:` covering
`template/base/.claude/**`, `template/base/.codex/**`, `template/base/.agents/**`, the manifest it
reads, and the workflow file itself, so the gate is armed on its own subject
(`FS-GG/.github#332`/`#334`/`#880`).

It is deliberately a **separate workflow** from the `skill-union.yml` that FS.GG.Rendering#1080 wires:
that one is the **receiver** caller (`product-path` defaulted to `.`) auditing this repository's own
committed runtime roots. Two different subjects, and per `FS-GG/.github#1504`/`#628` a generated-product
caller does **not** satisfy the `receives: skill-union` capability. Neither green stands in for the
other.

**The command a reviewer runs, and its expected exit code — `0`:**

```sh
curl -fsSL -o skill-union-assert.sh \
  "https://raw.githubusercontent.com/FS-GG/.github/8481873f9f7b9a6229d3337cb2a738a2f80066a0/dist/skill-union-assert.sh"
bash skill-union-assert.sh --product template/base \
  --manifest template/skill-manifest/skill-manifest.json
```

Fetch `dist/`, never `scripts/` (`FS-GG/.github#843`). Observed on the repaired tree:

```
skill-union-assert: 1 skill(s) — in-every-root=1/1 partitioned=0 | byte-comparable=1 byte-compared=1
  byte-identical=1/1 byte-differing=0 single-root=0 | manifest-declared=1/1 manifest-comparable=1
  manifest-examined=1 manifest-matched=1/1 manifest-no-reference=0 undeclared-rejected=0/0
  co-tenant=0/0 declared-absent=17/18
skill-union-assert: OK — all roots hold the byte-identical union.   # exit 0
```

Both failure modes were verified against the **pre-change** tree rather than asserted: the missing
root exits **2** (`configured root is absent: template/base/.codex/skills`), and the drifted
`.claude/` body exits **1** (`[divergent] skill 'fs-gg-project' differs between root '.claude/skills'
and root(s): .codex/skills .agents/skills`). `declared-absent=17/18` is the manifest's documented
superset-catalog semantics — the other 17 skills are supplied from `template/product-skills/`, not
from `template/base/`.

## Related, and deliberately not merged with this

`FS.GG.Rendering#1082` was decided the **other** way on the same day, for this repository's **own**
committed runtime roots: one byte-identical canonical body per skill, per-surface `Claude-active` /
`Codex-active` wrappers removed as drift. Implemented as `FS.GG.Rendering#1080`. That decision is
about runtime roots; this one is about a scaffolding base tree. **Do not apply either decision's
reasoning to the other's subject.**
