# C3-RENDERING-01 — Ordinary V2 receiver adoption

Status: installed. Dedicated custody and immutable Coordination CLI 0.1.4 are pinned; protected-main pushes run secret-free qualification before the bounded settlement job.

FS.GG.Rendering is the fixed C3 source repository (`FS-GG/FS.GG.Rendering`, repository ID
`1269292235`) under the code-owned `rendering-v1` profile. This change adds only repository-owned
receiver source. It changes no repository setting, environment, secret, branch protection, required
check, generated workspace content, or protected effect.

## Prepared source

- The receiver workflow is bound to protected-main pushes, but its only job has an unconditional
  false guard. It uses read-only GitHub permissions, persists no checkout credential, and contains
  no credential job, environment binding, secret reference, package download, or settlement command.
- The observer comes byte-for-byte from repaired Audio main `257ff5c`; the qualifier replaces only
  the code-owned source profile. Their SHA-256 digests are
  `6e63f6f724fb267e77ef02f41b4c58a833e0dc5b08c003f07fd16c89e4f7fd55` and
  `a5518e59add15e9c4c02d0a9854efcf236bb6c9bfc83b7981002a2f6751c5c9d` respectively.
- The selected settlement checks are `Deterministic gate` and `routine-eligibility`. The five
  separate native required gates are `Deterministic gate`, `API compatibility gate (breaking-change
  → SemVer major)`, `kit / coordination-kit`, `skill-view-check`, and `materialize /
  receiver-validate`. Every check is bound to GitHub Actions App `15368` and its exact workflow ID,
  path, event, PR head, run, job, suite, and attempt.
- The shared policy ID remains `v2-ci-i1-ordinary-settlement-v1`; the shared Authority anchor retains
  App `5064713`, installation `164553252`, repository `FS-GG/FS.GG.Coordination.Authority`
  (`1351660651`), `contents:write`, metadata read, and the existing writer/integrity ruleset pins.
- Rendering's `ordinary-v2` environment exists as ID `22918944124`, restricted to the single `main`
  branch policy ID `61286114`, with no reviewers. The protected custody bridge run `36416756692` succeeded and the environment reads back the exact three dedicated ordinary-v2 secret names.
- Coordination CLI `0.1.4` is installed from the immutable GitHub release asset pinned to SHA-256 `10a51295db43e454b7692196533cceda48508165a8023dc98e87639be89f5c50`; publisher run `36427124428` verified both feeds and anonymous installation.
- Rendering already pins .NET SDK `10.0.401` in the repository's tracked `global.json`. This
  receiver leaves that pin unchanged and invokes no .NET setup while disabled.

## Installation boundary

Do not enable the preflight or add a credential job until one reviewed source change verifies all of:

1. immutable published Coordination CLI `0.1.4` supports the exact `rendering-v1` source profile and
   its independently verified served package SHA-256 is pinned; and
2. Rendering identity, exact current required-check population, producer mappings, and shared
   Authority binding are freshly read back.

The later activation must change policy status, installed state, package evidence, observer guard,
and the bounded credential job together. This disabled source cannot settle work and imports no V1
admission or receiver state.
