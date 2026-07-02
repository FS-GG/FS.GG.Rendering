# 0013 — Publish to nuget.org via Trusted Publishing (OIDC), not a stored API key

**Status**: Accepted (supersedes [ADR-0012](./0012-dual-publish-to-nuget-org.md) §6 / push auth) · **Date**: 2026-07-01

> **Pointer stub.** ADR-0013 is an **org-level** decision that lives in the shared
> `FS-GG/.github` decision log, **not** in this repo's local `0001`–`0010` sequence. It is
> surfaced here because it is cited normatively by the rendering release workflow. The
> numbers `0011`–`0014` belong to the org ADR sequence and are **not** a continuation of
> this repo's local decisions.

**Canonical document**:
[`FS-GG/.github` → `docs/adr/0013-trusted-publishing-oidc-for-nuget-org.md`](https://github.com/FS-GG/.github/blob/main/docs/adr/0013-trusted-publishing-oidc-for-nuget-org.md)

## What it decides

The nuget.org push authenticates via **Trusted Publishing (OIDC)**, not a long-lived
`NUGET_ORG_API_KEY` secret. A job with `id-token: write` mints a short-lived GitHub OIDC
token; `NuGet/login` exchanges it at nuget.org for a single-use ~1h key. Because nuget.org
matches the trust policy against the **calling** workflow file, the login+push must live in
**each producer's own workflow** — a reusable cross-repo workflow does not match.

## Where this repo relies on it

- `.github/workflows/release.yml` — the release job requests `id-token: write` (ADR-0013) and
  runs `NuGet/login` + push inline rather than delegating to a shared workflow.
