# T019 — Combined gate (FR-004, INV-15) — ⛔ NOT YET SATISFIED (half-landed)

**Verdict (2026-06-29):** **One of the two binding conditions holds.** Per FR-004 "no half-landing",
the feature is **NOT done** until both hold for the same `V`.

| Condition | For `V = 0.1.53-preview.1` | Evidence |
|---|---|---|
| **A. No exit 127** (`--productName` honored) | ✅ **PROVEN** | `no-127-scaffold.md` — clean feed install + scaffold, exit 0 |
| **B. No exit 103** (consumer token can install) | ❌ **BLOCKED** | `no-103-install.md` — package still `private`; needs org-admin visibility flip |

**Binding invariant**: a version published-but-private (current state) is **not** done. `V` is
Feature-217-bearing and feed-served (Gate A ✅), but not yet org-readable (Gate B ❌). The conjunction
fails on Gate B alone.

## The one action that closes the gate

An org admin flips `FS.GG.UI.Template` visibility `private → internal` (or grants `FS-GG/FS.GG.Templates`
Read) at `https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`. After
that:
```bash
gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility   # -> internal
# then a foreign packages:read token:
dotnet new install FS.GG.UI.Template@0.1.53-preview.1                 # -> exit 0 (no 103)
```
Both ✅ ⇒ FR-004 satisfied ⇒ close #29/#26, advance the board to Done, land the registry PR.

**Until then the producer half is complete and the feature is one operator action from done.**
