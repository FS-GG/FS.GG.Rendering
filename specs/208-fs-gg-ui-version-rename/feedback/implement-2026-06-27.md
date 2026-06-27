---
phase: implement
date: 2026-06-27
severity: minor            # none | minor | major | blocker
---

## Process friction

Four friction points, all resolved in-phase (none blocking):

1. **Version-bump file mismatch (T011).** tasks.md/plan named `template/base/Directory.Build.props`
   for the FR-006 version bump, but that file carries no `<Version>`. The effective, published
   template version lives in `.template.package/FS.GG.UI.Template.fsproj` (`0.1.50-preview.1`). Bumped
   there → `0.1.51-preview.1`. Plan should target the package fsproj.

2. **SC-001 ∧ FR-008 tension.** FR-008 wants the UPGRADING migration note to tell authors to rename
   the property; SC-001 demands *zero* `FsSkiaUiVersion` anywhere in the generated tree (and
   UPGRADING ships in that tree). Reconciled by wording the note to instruct the rename *to*
   `FsGgUiVersion` without reproducing the old literal — authors already have the old name in their
   own file. A future spec should call this tension out so the note isn't written with the old token.

3. **`sourceName=Product` substitution mangles prose.** The word "product"/"Product" is rewritten to
   the generated name ("acme"/"Acme"), so a migration note saying "pre-rename product" rendered as
   "pre-rename acme". Fix: use the name-neutral word "project" in template docs (matches existing
   `template/base` convention). What would have helped: a documented list of substituted tokens.

4. **Verifying template edits requires re-pack+reinstall.** `dotnet new fs-gg-ui` resolves the
   *installed packed* template, not the working tree. Each verification round needed
   `dotnet pack .template.package` → `dotnet new uninstall/install` → generate. A one-liner helper
   script for "repack+reinstall the local template" would remove the boilerplate. Also: the generated
   test project is `tests/Acme.Tests` (sourceName Product→Acme), not the literal `Product.Tests`
   quickstart/T012 named.

## Generalizable code

none (this feature is a rename across MSBuild/CPM props, git tags, docs, and a cross-repo registry —
no F# library surface; the only `.fs` touched is the template's `GovernanceTests` assertion string).
A candidate **tooling** helper (not library code): a repo script `repack-and-reinstall-template.sh`
wrapping pack→uninstall→install for the local template verification loop.

## Skill gaps

none new — `cross-repo-coordination` covered the registry/ADR/board work and `fs-gg-feedback-capture`
covered this record. A small doc addition to the template-authoring guidance listing the
`sourceName` substitution tokens (and the "use 'project' not 'product' in shipped docs" rule) would
have pre-empted friction #3.

## Research links

none required — no hard external problem; all frictions resolved against repo evidence and live
generate→restore→build runs. F# / .NET template engine reference:
<https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates>
