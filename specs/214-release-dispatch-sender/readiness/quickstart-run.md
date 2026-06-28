# Quickstart consolidated run (T019) — 2026-06-28T11:54:12Z

## Layer 1 — actionlint (exit 0 expected)
actionlint exit=0

## Layer 2 — dispatch dry-run harness (all four scenarios)
== Feature 214 dispatch dry-run harness ==
PASS  happy-path derives version + payload (exit 0)
PASS  empty version fails, no payload (exit 1)
PASS  malformed version fails (exit 1)
PASS  missing credential fails (even dry-run) (exit 1)

ALL PASS
harness exit=0

## Regression guard (FR-008) — release.yml byte-unchanged vs origin/main
git diff --stat origin/main -- .github/workflows/release.yml -> [<no output>]

## Layer 3 — live cross-repo send: DEFERRED (org credential FS-GG/.github#21/#22). See deferred-live-check.md.
