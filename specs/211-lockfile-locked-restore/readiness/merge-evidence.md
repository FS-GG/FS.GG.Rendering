# Merge evidence (Feature 211)

Date: 2026-06-28 · branch `211-lockfile-locked-restore` → `main` (squash).

## Package bump / local feed — deliberate NO-OP

Feature 211 is **Tier 2** (build/restore configuration). It adds committed `packages.lock.json`
files, a locked-mode CI restore step, an `NU1603;NU1608` warnings-as-error policy, and one
deterministic policy test. It introduces **no public API surface change, no new shipped dependency,
and no observable library behavior change** — it pins the *resolution* of the already-pinned graph,
it does not change the graph or any `FS.GG.UI.*` package content/version.

Therefore:
- **No package version bump** (`Directory.Build.props` `<Version>` stays `0.1.0-preview.1`).
- **No local-feed re-pack required** — no packable output changed; the local-feed lanes (4 standalone
  samples + `Package.Tests`) are explicitly EXCLUDED from this feature and untouched (they shadow root
  / opt out). Their package pins are unaffected; no sample pin alignment needed.

## Readiness allowlist — `git check-ignore` proof

`.gitignore` allowlist added for this feature (mirrors prior features):
```
!specs/211-lockfile-locked-restore/readiness/
!specs/211-lockfile-locked-restore/readiness/**
specs/211-lockfile-locked-restore/readiness/**/nuget-cache/
```
Proof the readiness evidence is committable (not ignored):
```
$ git check-ignore specs/211-lockfile-locked-restore/readiness/restore-proof.md  → (no match) exit 1
$ git add -n specs/211-lockfile-locked-restore/readiness/restore-proof.md         → add 'specs/.../restore-proof.md'
```

## Validation summary (all real, no synthetic)

| Check | Result |
|---|---|
| Pre-change baseline | 21/21 green, 0 red (`baseline.md`) |
| Clean locked restore (SC-001) | PASS exit 0 |
| Drift fail-closed (SC-002) | PASS — `NU1004`, exit 1 |
| Silent substitution → error (SC-003) | PASS — `error NU1601: Warning As Error`; props alone |
| Fresh-clone local not blocked (SC-004) | PASS exit 0 |
| One-command regenerate (SC-005) | PASS — reviewable lockfile diff |
| Scope boundary (SC-006) | 38 locked = slnx membership; 0 in excluded lanes; `RestoreLockTests` 10/10 |
| No regression (FR-009) | surface drift none · coherence COHERENT · baseline identical 21/21 |
