---
phase: implement
date: 2026-06-30
severity: minor            # none | minor | major | blocker
---

## Process friction

1. **Comprehensive baseline was 17/21 RED — but on a NuGet cache/lockfile artifact, not real
   regressions.** Every in-solution `tests/*` project failed restore with `NU1403: content hash
   validation failed for FSharp.Core.10.1.301 — the package is different than the last restore`. The
   committed `packages.lock.json` files pin a stale FSharp.Core hash the configured feeds no longer
   serve. Resolved for the run with `dotnet restore --force-evaluate /p:RestoreLockedMode=false`
   (recomputes the lockfile hashes); the lockfile churn was then reverted so the feature diff stays
   clean. Lesson: a green env-free verdict-core (`dotnet fsi`) and `dotnet new` (instantiation only)
   say nothing about whether the *build* graph restores — the baseline runner surfaced this up front,
   exactly its purpose (Feature 175 T3). The one residual red (`Surface baselines.FS.GG.UI.Build
   engine baseline …` — "FS.GG.UI.Build assembly has been built. Actual value was false") is a
   pre-existing env artifact (the Build engine assembly isn't built in the Debug test lane),
   unrelated to this feature; it is red at baseline and after, identically.

2. **The symbology "wire it" plan reversed to "not-vendored" on the verify-on-implement check.**
   Research R5 defaulted to *wire* `fs-gg-symbology`. At implementation the byte-equality check
   (T005 branch decision) showed the product-vendored variant (12788-byte full skill) is NOT
   byte-equal to the spec-kit blanket-copy wrapper (506-byte Codex pointer). Adding the source would
   overwrite the wrapper under `spec-kit` and break the byte-identical guarantee (GV-3 / FR-004). The
   other six skills already overwrite their wrapper today, so dropping *their* lifecycle clause is
   spec-kit-neutral; symbology had no existing overwrite, so wiring it would be a *new* spec-kit
   change. The tasks anticipated this exact fork (T010 guard: "do NOT add a source that would red
   GV-3") — the conservative default in the spec was the correct landing. Lesson worth generalizing:
   "wire a present-but-orphaned skill" is only spec-kit-neutral when its product variant is already
   the variant emitted under spec-kit; otherwise it is a *content reconciliation* task, not a wiring
   task, and belongs in a follow-up.

3. **G-CATALOG (FR-005 "every entry resolves") cannot be asserted literally under spec-kit.** The
   `docs/skillist-reference.md` catalog enumerates the FULL framework registry (ids resolving to
   `src/X/skill/…` and framework-only `.agents/skills/…` that are NOT vendored into a product even
   under spec-kit). A strict per-id resolution check false-fails. The feature's true scope (research
   R4, spec line 108) is *emission gating* — suppress the catalog where it would dangle (`sdd`/`none`)
   — with per-id scoping deferred. The validator/test assert the gating (catalog absent under
   sdd/none, present under spec-kit, `catalog-dangling: none`), not literal resolution. Lesson: when a
   guarantee's literal reading exceeds the chosen implementation scope, encode the *operational*
   guarantee and record the deferral, rather than authoring an assertion the artifact can't satisfy.

## Generalizable-code candidates

- The 3-category source classifier (`isFrameworkSkillSource` / lifecycle-workspace / product, with
  the named `docs/skillist-reference.md` exception) is duplicated between
  `scripts/validate-lifecycle-template.fsx` and the `Feature204LifecycleTemplateTests.fs` mirror — by
  design (the test re-derives independently), but a shared `TestSupport` classifier consumed by both
  would remove the drift risk if a 4th category ever appears.

## What went well

- Front-loading the live `dotnet new` smoke (T004) against the installed 0.1.53 package captured the
  exact before-state (sdd = 0 SKILL.md, catalog present) and let the after-state (`spec-kit/app`
  `diff -r` vs pre-change = 0 lines) be a clean byte-identical proof, independent of the env-gated
  matrix. Reinstalling the edited template from the checkout (`dotnet new install . --force`) gave
  real per-profile live evidence even though the build graph couldn't restore.
