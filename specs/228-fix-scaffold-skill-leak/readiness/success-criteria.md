# Success-criteria evidence index (SC-001 … SC-005)

| Criterion | Statement | Evidence | Status |
|---|---|---|---|
| **SC-001** | SDD-orchestrated scaffold (`fsgg-sdd scaffold --provider rendering`) returns `outcome: success` with no `providerWroteSddTree`, and the full-stack path proceeds to governance-overlay + `doctor`. | T010 (below) — **environment-limited**; disclosed substitute: [fixed-scaffold-sdd.md](./fixed-scaffold-sdd.md) + boundary-rule reasoning. | ⚠️ environment-limited |
| **SC-002** | Under `sdd`, `count(.claude/skills/fs-gg-*) == 0` and `set(.agents/skills/fs-gg-*) == S(profile)`. | [fixed-scaffold-sdd.md](./fixed-scaffold-sdd.md) (app/game live: 0 and 8) + report `sdd/<p>: claude-product-skills=0` & `framework-skills-present=ok`. | ✅ accepted |
| **SC-003** | Provider surface never shrinks; `spec-kit` byte-identical to today; `sdd ≡ none`. | [agents-tree-intact.md](./agents-tree-intact.md) + Feature 204 **GV-3** (`diff-vs-today=none`) + [fixed-scaffold-none.md](./fixed-scaffold-none.md). | ✅ accepted |
| **SC-004** | Full-stack path (scaffold → governance-overlay → doctor) completes; composition matrix unaffected. | Report `composition-matrix: 12/12 generate`; SC-001 end-to-end continuation is the environment-limited part of T010. | ⚠️ partial (matrix ✅; end-to-end environment-limited) |
| **SC-005** | Repo-owned gate fails on the pre-fix template and passes on the fixed one, across every profile that ships product skills. | [gate-transcripts.md](./gate-transcripts.md) — pre-fix RED (2 failures naming all 9 `.claude/skills/` paths), post-fix GREEN (14/14). | ✅ accepted |

## T010 — end-to-end SDD-orchestrated acceptance (environment-limited disclosure)

`fsgg-sdd` **is** installed (`~/.dotnet/tools/fsgg-sdd`), but a bare scratch project has no
`.fsgg/providers.yml` registering the `rendering` provider, so `fsgg-sdd scaffold --provider rendering`
returns `outcome: blocked` with `scaffold.providerUnknown` (**not** `providerWroteSddTree`). Registering
the rendering provider + the `new-sdd-fullstack` wrapper is SDD-side setup (the SDD repo's TestSpec
tutorial Part A) and is **not provisioned in this rendering-repo environment** — no `new-sdd-fullstack`
script and no rendering-provider `providers.yml` are checked out here.

**Disclosed substitute (per tasks.md T010 fallback):**

1. **The observed artifact.** SDD's boundary check raises `scaffold.providerWroteSddTree` **iff** the
   invoked provider writes files under an SDD-owned tree (`.claude/skills/`, `.codex/skills/`, `.fsgg/`,
   `work/`, `readiness/`). The rendering template already wrote none of the latter four; the only
   offending write was `.claude/skills/fs-gg-*`. [fixed-scaffold-sdd.md](./fixed-scaffold-sdd.md) proves
   the fixed template writes **0** files under `.claude/skills/` under `sdd` (app and game, live scaffold
   of the dev-installed fixed template). With zero writes into any SDD-owned tree, the boundary check has
   nothing to flag → no `providerWroteSddTree` → the provider step returns success and the full-stack
   script (previously aborted by `set -e` on the block) proceeds to governance-overlay + `doctor`.
2. **The SDD gate exists and is exercised** by the SDD-repo fixtures `skills-intrusion-claude` /
   `lifecycle-intrusion` (`/home/developer/projects/FS.GG.SDD/tests/fixtures/scaffold-provider/`), which
   assert the boundary check fires on a `.claude/skills/` write — the same diagnostic. This feature
   removes the write that would trip it.
3. **Transitive coverage.** The TestSpec tutorial Part A step 2 (FR-005) exercises this exact
   SDD-orchestrated scaffold path; it lives in the SDD repo and needs no separate rendering-side task —
   noted here as transitive coverage.

This keeps a visible `environment-limited` caveat on SC-001/SC-004's end-to-end leg (not summarized as
fully green); SC-002/SC-003/SC-005 are fully accepted on live + gate evidence.
