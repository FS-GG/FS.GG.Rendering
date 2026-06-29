# T005 — Early live feed/visibility probe (the smoke run)

**Captured**: 2026-06-29, against the **real org feed** (`nuget.pkg.github.com/FS-GG`) and `gh api`
(account `EHotwagner`, scopes incl. `read:packages`, `read:org`). This replaces the plan's
hypotheses with **observed facts before any tag is pushed**.

## Gate A — feed serves only the pre-217 version (→ exit 127)

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'
0.1.52-preview.1
```
✅ Confirms #29: the only served version is `0.1.52-preview.1`, which predates Feature 217 (`6df0d39`).

Live `--productName` rejection (owner token can read the private package, so the symptom is
observable locally):
```
$ dotnet new fs-gg-ui --productName Acme --output ./Acme
Error: Invalid option(s):
--productName
   '--productName' is not a valid option
... refer to https://aka.ms/templating-exit-codes#127
```
✅ **Exit-127 reproduced** — the installed (pre-217) template has no `productName` symbol.

> **Disclosed confound (honesty, Principle V).** The local working tree registers an in-repo template
> with the same identity `FS.GG.UI.Template` from `/home/developer/projects/FS.GG.Rendering`, which
> `dotnet new` reports as taking precedence over the feed package. This is a local-host registration
> artifact and is exactly why a local scaffold is **not** authoritative (Feature 175/216 lesson). The
> authoritative Gate-A signal is the **feed listing** above (only `0.1.52-preview.1`) plus the live
> #127. The post-publish proof (T018) must install `V` in a clean dir.

## Gate B — package is private (→ exit 103)

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility
private
```
Contrast — a public sibling reads fine for any consumer token:
```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.SDD.Cli --jq .visibility
public
```
✅ Confirms #26: `FS.GG.UI.Template` is `private`. An ordinary org-consumer `GITHUB_TOKEN`
(`packages: read`, no explicit private grant) gets **exit 103** ("could not be authenticated" /
NotFound) on `dotnet new install`.

> **Disclosed limitation (environment-limited).** The honest exit-**103** probe requires a *foreign*
> consumer token (the FS.GG.Templates composition CI job, or a token with `packages: read` and **no**
> private grant). The in-session owner token (`EHotwagner`) *can* read the private package, so it
> **masks** the 103 — re-running an install locally would falsely succeed. The 103 is therefore
> documented from #26's CI symptom and the `visibility: private` fact, not faked with a privileged
> local run.

## Verdict

Both gates reproduced against live state: feed serves only `0.1.52-preview.1` (Gate A / 127),
package is `private` (Gate B / 103). The two-gate hypothesis (root-cause-map.md) is **confirmed, not
assumed**. Release actions may proceed.
