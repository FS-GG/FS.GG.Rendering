# T019 — Quickstart walk (steps 0 → 3), every "Expected outcome" confirmed

| Quickstart step | Expected outcome | Observed | Evidence |
|---|---|---|---|
| **§0 static (before)** | scan prints `9` ungated `.claude/skills/` product sources | `9` | [leak-before.md](./leak-before.md) T004 |
| **§0 live (before)** | `sdd/game` scaffold has `.claude/skills/fs-gg-*` present (8) | `8` present (game/sdd) | [leak-before.md](./leak-before.md) T005 |
| **§1 apply fix** | 9 `.claude/skills/` conditions gain `&& lifecycle == "spec-kit"`; gates + fsx corrected | applied (9 sources; `.agents/` siblings untouched) | [leak-surface-map.md](./leak-surface-map.md), diff |
| **§2 static (after)** | scan now prints `0` | `0` | [leak-before.md](./leak-before.md) T008 |
| **§2 gates** | Feature204 GV-2/GV-4/GV-5 green; Feature219 G-EMIT green | `Passed! 14/14` | [gate-transcripts.md](./gate-transcripts.md) |
| **§2 audit** | `framework=9`, `workspace>=15`, `0` violations | classifier corrected; report `framework product-skill sources (.agents/skills/)` | [gate-transcripts.md](./gate-transcripts.md) |
| **§3 live table sdd** | `count(.claude/skills/fs-gg-*)=0`, `.agents/skills/=S(profile)` | 0 / 8 (app,game) | [fixed-scaffold-sdd.md](./fixed-scaffold-sdd.md) |
| **§3 live table none** | `0` / `S(profile)`, `none ≡ sdd` | 0 / 8 (app,game) | [fixed-scaffold-none.md](./fixed-scaffold-none.md) |
| **§3 live table spec-kit** | both surfaces = `S(profile)` | 8/8 both surfaces (game) | [agents-tree-intact.md](./agents-tree-intact.md) |
| **§3 SDD-orchestrated** | `outcome success`, no `providerWroteSddTree`; proceeds to governance + doctor | **environment-limited** (provider not registered here) + disclosed substitute | [success-criteria.md](./success-criteria.md) T010 |

No deviation from any env-free / live-scaffold "Expected outcome". The only step not directly executed is
the SDD-orchestrated end-to-end run (§3 last row), which is environment-limited with a disclosed substitute
(the fixed-scaffold-sdd `.claude/skills/`=0 observation + the SDD boundary-rule deduction).
