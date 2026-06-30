# T001 — Release preconditions

**Date**: 2026-06-30 · **Branch**: `222-republish-game-template`

| Check | Command | Result |
|---|---|---|
| Branch is the feature branch | `git rev-parse --abbrev-ref HEAD` | `222-republish-game-template` ✅ |
| Feature-220 reachable from HEAD | `git merge-base --is-ancestor b78e72a HEAD` | true ✅ |
| Feature-220 reachable from main | `git merge-base --is-ancestor b78e72a main` | true ✅ |
| Branch == main commit | `git log --oneline main..HEAD` | empty (branch tip == `main` `eb93e89`; feature work is uncommitted working-tree: `specs/222…`, `CLAUDE.md`, `.specify/feature.json`) |
| No `.fs`/`.fsi` change vs main | `git diff --name-only main...HEAD \| grep '\.fsi\?$'` | empty ✅ (docs/registry-only feature, FR-010) |

**gitignore allowlist proof (Feature 168 / merge-skill rule)** — `specs/*/readiness/` is ignored by
default; the allowlist entry was added before staging:

```
$ git check-ignore specs/222-republish-game-template/readiness/preconditions.md
(exit 1 — NOT ignored; allowlist `!specs/222-republish-game-template/readiness/**` active)
```

Preconditions satisfied — the release may be cut from `main` (which contains `b78e72a`).
