# Readiness — Tooling versions (T001)

Captured 2026-06-28 on this dev box. Tools were already present; nothing installed.

| Tool | Version | Path | Layer |
|------|---------|------|-------|
| `actionlint` | `1.7.7` (pinned) | `/home/developer/.local/bin/actionlint` | Layer 1 (workflow lint) |
| `gh` | `2.95.0 (2026-06-17)` | on `PATH` | Layer 3 only (live cross-repo evidence) |

```text
$ actionlint --version
1.7.7
installed by downloading from release page
built with go1.23.4 compiler for linux/amd64

$ gh --version
gh version 2.95.0 (2026-06-17)
```

`actionlint` matches the quickstart-pinned `@v1.7.7`, so lint evidence is reproducible. `gh` is used
only for the deferred Layer 3 live send (the reusable workflow itself uses runner-preinstalled
`gh`/`jq`), not by any Layer 1/2 local check.
