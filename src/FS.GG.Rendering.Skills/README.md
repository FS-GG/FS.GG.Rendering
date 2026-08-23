# FS.GG.Rendering.Skills

FS.GG.Rendering's owner-authored **product** skill bytes, as one versioned, content-addressed
package.

## Why a package

This repository authors 18 `scope: product, owner: fs-gg-rendering` skills. Until this package they
had exactly one delivery channel: the `fs-gg-ui` `dotnet new` template. That template emits them
correctly and unconditionally — but a product scaffolded through any *other* workspace provider
never restores the template, so those trees received none of them. Measured on a real tree
(`providerName: fable-game`): 42 driver paths, 16 process skills, 2 game-skill paths, and **zero**
Rendering-owned product skills, including `fs-gg-feedback-report`, whose predicate is `always`.

That is the class [ADR-0063](https://github.com/FS-GG/.github/blob/main/docs/adr/0063-scaffold-materializer-sources-skills-from-the-owner-repo.md)
named — *declared, gated in, and supplied from nowhere*. The fix it prescribes is the one taken
here: the owner publishes its own bytes through a delivered, pinned, content-addressed channel.
`.github` carrying them instead was asked for and refuted, because `.github` would need a frozen
copy of another repository's SKILL.md bytes — the restatement
[ADR-0058](https://github.com/FS-GG/.github/blob/main/docs/adr/0058-adopt-one-governing-principle-derive-dont-restate.md)
forbids and [ADR-0062](https://github.com/FS-GG/.github/blob/main/docs/adr/0062-versioned-kit-package-replaces-byte-copy-sync.md)
replaces.

**Nothing is removed from the template.** Trees the `fs-gg-ui` template creates keep receiving these
skills from the template, exactly as before. This is an additional channel for the trees it does not
create.

## What it ships

```
rendering-skills/skill-manifest.json     schema-v2 delivered set + canonical-body and every-file sha256
rendering-skills/skills/<id>/SKILL.md    the body for each product row
rendering-skills/skills/<id>/**          that row's sidecars, where it has any
build/FS.GG.Rendering.Skills.props       a consumer handle: $(FsggRenderingSkillsContentDir)
```

`skill-manifest.json` is byte-identical to this repository's committed
`template/skill-manifest/skill-manifest.json`, which is also what `.github`'s `registry/skills.yml`
is reconciled from. Registry, manifest, and packed bytes are the same three digests.

**Sidecars travel with their body.** Three rows carry files the body depends on —
`fs-gg-symbology` and `fs-gg-symbol-design` each carry a `reference.fsx` and a `reference/` note,
and `fs-gg-feedback-report` carries `scripts/feedback-tool.fsx` and `scripts/FeedbackReportTool.fs`.
[ADR-0014](https://github.com/FS-GG/.github/blob/main/docs/adr/0014-skill-vendoring-one-manifest-one-materialize-verify.md)
clause 4 requires an emitted body to be self-contained, so the unit of delivery is the row's
directory, not a single file.

## Consuming it

1. FS.GG.Rendering **publishes** these bytes plus `skill-manifest.json` as this versioned package.
2. A scaffold materializer **pins** it and **restores** it at its own build time — online.
3. At **scaffold time** — offline — it **materializes** each skill into the product tree's skill
   roots from the bytes it already carries, verifying every schema-v2 declared file against the
   manifest `files` digest set. The retained row `sha256` is the compatible canonical `SKILL.md`
   digest for schema-v1 readers.

There is deliberately **no materialize target in this package**. Laying skills into a tree requires
knowing that tree's skill roots and scaffold parameters, which only the consuming materializer
knows. This package's job ends at *here are the bytes, and the record that says what they hash to*.

`build/FS.GG.Rendering.Skills.props` is auto-imported by NuGet and sets
`$(FsggRenderingSkillsContentDir)` to the content root, so a consumer never hard-codes a
version-shaped path under the package cache.

## Deriving, not restating

The delivered set is a one-line predicate over the producer manifest — `scope == "product"` — and
it is written down in exactly two places, `stage-skills.py` and `verify-package.sh`. There is no
list of skill ids anywhere in this package. A row added to the manifest flows into the nupkg with
no edit here, which `verify-package.sh` step 2 measures with a synthetic row.

The set is read from the manifest rather than globbed from `template/product-skills/` because
3 of the 18 rows are sourced from elsewhere (`template/feedback-report/skill/`,
`template/fragments/samples/skill/`, `template/base/.agents/skills/fs-gg-project/`). A glob ships
15 of 18 and silently drops the `always` row. Each row's `supplied-by` is the authority for where
its bytes live.

## Verifying it

```sh
bash src/FS.GG.Rendering.Skills/verify-package.sh
```

Five numbered assertions: the staged set is exactly the manifest's product rows and the packed
manifest is byte-identical to the committed one; a synthetic out-of-tree row flows through with its
sidecar; the nupkg carries every body, every sidecar, the handle and the README; every **packed**
body digests to its manifest `sha256`; and that same verify **fails** on a tampered byte. The last
one is why the fourth is meaningful — a verify never shown to fail has not been shown to run.
