# T017 — No exit 103 (SC-002, INV-8) — ⛔ environment-limited (BLOCKED on visibility)

**Status (2026-06-29):** ❌ **Cannot pass yet — the package is still `private`.**

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility
private
```

The honest exit-103 probe requires a **foreign org-consumer token** holding only `packages: read`
with **no** explicit private-package grant (the FS.GG.Templates composition CI job that #26 reports,
or an equivalent token). While `FS.GG.UI.Template` is `private`, that token gets **exit 103** ("could
not be authenticated" / NotFound) on `dotnet new install FS.GG.UI.Template@0.1.53-preview.1`.

Two reasons this is not provable in-session:
1. **The package is private** — the precondition for "no 103" (visibility `internal`, or a Templates
   Read grant) has **not** been met. That flip is an org-admin UI action with no REST endpoint
   (`visibility-internal.md`).
2. **The in-session token masks it** — `EHotwagner` *can* read the private package, so a local install
   would falsely return 0. Reproducing 103 with a privileged token would be fake evidence (Principle V).

**Acceptance (after the operator flips visibility):** re-run the Templates composition install (or a
foreign `packages: read` token) → `dotnet new install FS.GG.UI.Template@0.1.53-preview.1` exits 0.

This is the **single remaining blocker** for the FR-004 combined gate.
