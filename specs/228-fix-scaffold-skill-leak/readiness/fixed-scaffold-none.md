# T011 — Live observation under `none` (FR-003 / SC-003): `none ≡ sdd`

| profile / lifecycle | `count(.claude/skills/fs-gg-*)` | `count(.agents/skills/fs-gg-*)` |
|---|---|---|
| app / none | **0** | 8 = S(app) |
| game / none | **0** | 8 = S(game) |

- `count(.claude/skills/fs-gg-*) == 0` under `none` — identical to the `sdd` column (C-4, FR-003).
- `set(.agents/skills/fs-gg-*) == S(profile)` — byte-identical skill-tree to `sdd`.

`none` losing its `.claude/skills/` UI-skill copies is the deliberate R1 correction: the discriminator
is `lifecycle == "spec-kit"`, so `none` aligns to `sdd` per the lifecycle contract ("`none` = same
template-level output as `sdd`"). The `sdd` and `none` scaffolds produce identical skill trees.
