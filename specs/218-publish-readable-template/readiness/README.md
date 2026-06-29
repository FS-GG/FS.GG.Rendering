# Feature 218 — readiness summary

**Publish & Make-Readable the productName-Enabled Template** · `V = 0.1.53-preview.1` ·
captured 2026-06-29 → 2026-06-30.

## Bottom line — ✅ COMPLETE

Both gates hold for the **same** `V = 0.1.53-preview.1` (FR-004, no half-landing), **proven live**:
the coherent set is published, carries Feature-217 `--productName` (no exit 127), and the whole set
is org-readable (no exit 103). #29 + #26 are **closed**, the board items are **Done**, and the
registry contract-change PR is filed (**FS-GG/.github#66**).

## Evidence map

| Item | Status | Evidence |
|---|---|---|
| **SC-001 / FR-005 / INV-3** feed serves `V` | 🟢 PASS | `feed-serves-V.md` |
| **FR-006 / INV-2** coherent set at one `V` | 🟢 PASS | `feed-serves-V.md`, `publish-run.md` |
| **SC-003 / INV-6** no exit 127 (`--productName`) | 🟢 PASS | `no-127-scaffold.md` |
| **INV-1** `V > 0.1.52-preview.1` | 🟢 PASS | `preconditions.md`, `feed-serves-V.md` |
| **FR-010 / INV-4** template-released dispatch | 🟢 PASS | `dispatch-fired.md`; Templates#33 opened |
| **SC-002 / INV-8** no exit 103 (readable) | 🟢 PASS | `no-103-install.md` — whole set public; install+restore+build green |
| **FR-002/003** visibility | 🟢 PASS | `visibility-internal.md` — flipped to **public** (internal unavailable on free org) |
| **FR-004 / INV-15** combined gate | 🟢 PASS | `combined-gate.md` — Gate A ✅ + Gate B ✅ for one `V` |
| **FR-008 / INV-10/11/12** registry landing | 🟢 FILED | FS-GG/.github#66; `registry-delta.md` |
| **FR-007 / INV-14** #29 reply with `V` | 🟢 DONE | replied on #29 |
| **FR-007** close #29/#26 | 🟢 DONE | both closed |
| **SC-004 / INV-16** downstream `29/29` | ◐ IN FLIGHT | `downstream-2929.md` — Templates#33 opened; `29/29` lands on Templates merge |
| board reflects reality | 🟢 DONE | #29 → Done, #26 → Done (Coordination Projects v2 #1) |
| no-regression baseline | 🟢 PASS | `baseline.md` (21/21 green) |

## Corrections discovered during implementation

1. **`internal` is unavailable on `FS-GG`** (free, non-enterprise org) — the plan/research preferred
   `internal`, but the only org-wide-read option is **`public`** (matches the public `FS.GG.*.Cli`).
2. **The whole coherent set had to be flipped, not just the template** — the scaffolded build restores
   all 17 `FS.GG.UI.*` libraries; flipping only the template would have left a latent second exit-103.
3. **Tag ordering matters** — pushing `main` before the `fs-gg-ui/v<V>` snapshot tag briefly red-ed
   the Feature-209 version-coherence gate (`DRIFT [pin-no-tag]`); push the snapshot tag first/atomically.

## Remaining (Templates-owned, linked not blocking)

- FS.GG.Templates#33 (pin-bump to `0.1.53-preview.1`) merges → `FSGG_COMPOSITION_FULL=1` → `29/29`;
  link that run on #32. Tracked in `downstream-2929.md`.
- Registry PR **FS-GG/.github#66** awaits review/merge.

## Disclosed limitations (Principle V)

- **No REST endpoint for package visibility** — the flip was an org-admin UI action (done by the operator).
- **GitHub Packages requires auth even for public packages** — a truly anonymous request 103s by
  design; `public` means any *authenticated* GitHub identity can read (the consumer-CI case).
- **In-session owner token can't distinguish private vs public by itself** — the authoritative
  non-member proof is the Templates composition CI (#33); the in-session proof is the full
  install→restore→build chain from the feed.
