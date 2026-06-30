# T025 — Quickstart end-to-end "Done When" (steps 0–7)

Each quickstart step run live against `V = 0.1.54-preview.1`; "Done When" checklist mapped to evidence.

| Step | Done When | Evidence | ✅ |
|---|---|---|---|
| 0 | Pre-publish gap confirmed (feed serves 0.1.53 without `b78e72a`) | `pre-publish-probe.md` | ✅ |
| 1–2 | Feed serves exactly one new coherent-set `V > 0.1.53-preview.1` whose contents carry `b78e72a` (SC-002) | `feed-listing.md`, `coherent-set.md`, `content-gate.md` | ✅ |
| 3 | A consumer installs `V` (no 103) and scaffolds the `game` profile (SC-001) | `consumer-install.md`, `game-scaffold.md` | ✅ |
| 4–5 | Generated `game` builds + governance green, zero `GovernanceTests` edits; non-game unaffected (SC-003) | `game-governance.md`, `non-game-parity.md` | ✅ |
| 6 | Registry + compatibility name `V`, `game` released, after the feed listing (SC-004) | `registry-pr.md` | ✅ |
| 7 | #33 closed (+`V`,+registry PR), board `Done`, #31 unblocked, SDD#44 notified (SC-005) | `board-closure.md` | ✅ |

All "Done When" items satisfied with live cross-repo evidence.
