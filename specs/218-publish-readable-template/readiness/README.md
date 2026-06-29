# Feature 218 — readiness summary

**Publish & Make-Readable the productName-Enabled Template** · `V = 0.1.53-preview.1` ·
captured 2026-06-29 → 2026-06-30.

## Bottom line

**Producer half (US2 publish) is complete and proven live. The feature is ONE org-admin action from
done** — the `#26` visibility flip (`private → internal`) has **no REST endpoint** and could not be
performed in-session. Per FR-004 "no half-landing", the combined gate (US1) is therefore **not yet
satisfied**, so #29/#26 stay **open**, the board items are **In progress / Blocked** (not Done), and
the registry PR is **prepared but held**.

## Evidence map

| Item | Status | Evidence |
|---|---|---|
| **SC-001 / FR-005 / INV-3** feed serves `V` | 🟢 PASS | `feed-serves-V.md` (feed lists `0.1.53-preview.1`) |
| **FR-006 / INV-2** coherent set at one `V` | 🟢 PASS | `feed-serves-V.md` (sampled set all `0.1.53-preview.1`); `publish-run.md` |
| **SC-003 / INV-6** no exit 127 (`--productName`) | 🟢 PASS | `no-127-scaffold.md` (clean feed install + scaffold, exit 0) |
| **INV-1** `V > 0.1.52-preview.1` | 🟢 PASS | `preconditions.md`, `feed-serves-V.md` |
| **FR-010 / INV-4** template-released dispatch | 🟢 PASS | `dispatch-fired.md` (run green); Templates#33 PR auto-opened |
| publish gates green before push | 🟢 PASS | `publish-run.md` (run `28404668485`) |
| no-regression baseline | 🟢 PASS | `baseline.md` (21/21 green) |
| **SC-002 / INV-8** no exit 103 (readable) | ⛔ BLOCKED | `no-103-install.md` — package still `private` (org-admin flip, no API) |
| **FR-002/003** visibility `internal` | ⛔ OPERATOR | `visibility-internal.md` — manual UI step disclosed |
| **FR-004 / INV-15** combined gate | ⛔ NOT YET | `combined-gate.md` — Gate A ✅, Gate B ❌ |
| **FR-008 / INV-10/11/12** registry landing | ⏸ DEFERRED | `registry-delta.md` — held until visibility holds |
| **FR-007 / INV-14** #29 reply with `V` | 🟢 DONE | replied on #29 (version string posted) |
| **FR-007** close #29/#26 | ⏸ HELD | gated on FR-004 (both gates) — not closed |
| **SC-004 / INV-16** downstream `29/29` | ⛔ ENV-LIMITED | `downstream-2929.md` — Templates#33 opened; #32 still blocked on visibility |
| board reflects reality | 🟢 DONE | #29 → In progress, #26 → Blocked (Coordination Projects v2 #1) |

## The single remaining action (operator / org admin)

Flip `FS.GG.UI.Template` visibility **`private → internal`** at
`https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`
(or grant `FS-GG/FS.GG.Templates` repo Read). Then the follow-up is mechanical:

1. `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility` → `internal`
2. Foreign `packages: read` token: `dotnet new install FS.GG.UI.Template@0.1.53-preview.1` → exit 0 (no 103)
3. Land the `FS-GG/.github` registry PR (`registry-delta.md`)
4. `gh issue close 29 26 --repo FS-GG/FS.GG.Rendering`
5. Board #29/#26 → Done; confirm Templates#32 unblocked + link the `29/29` composition run

## Disclosed limitations (Principle V)

- **No REST endpoint for package visibility** — the #26 gate is org-admin-UI-only.
- **In-session owner token masks exit-103** — a privileged local install would falsely pass; the 103
  is documented from the visibility fact + #26's CI symptom, not faked.
- **Local in-repo template registration** shadows the feed package in `dotnet new`; the no-127 proof
  uninstalled it first and installed `V` cleanly from the feed.
