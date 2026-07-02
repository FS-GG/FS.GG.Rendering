# 0012 — Dual-publish FS-GG packages to nuget.org alongside the org feed

**Status**: Accepted (§6 push authentication superseded by [ADR-0013](./0013-trusted-publishing-oidc-for-nuget-org.md); §1–§5 stand) · **Date**: 2026-07-01

> **Pointer stub.** ADR-0012 is an **org-level** decision that lives in the shared
> `FS-GG/.github` decision log, **not** in this repo's local `0001`–`0010` sequence. It is
> surfaced here because it is cited normatively by the rendering release workflow. The
> numbers `0011`–`0014` belong to the org ADR sequence and are **not** a continuation of
> this repo's local decisions.

**Canonical document**:
[`FS-GG/.github` → `docs/adr/0012-dual-publish-to-nuget-org.md`](https://github.com/FS-GG/.github/blob/main/docs/adr/0012-dual-publish-to-nuget-org.md)

## What it decides

Every FS-GG package publishes to **public nuget.org** in addition to the org GitHub Packages
feed, so consumers get frictionless `dotnet tool install -g …` / `dotnet add package …` with
no `--add-source`. The dual-publish set is **byte-identical** to the org-feed artifacts, and
the nuget.org push runs **after** (and is gated by) the org-feed push — publish-before-flip
ordering (§4).

## Where this repo relies on it

- `.github/workflows/release.yml` — the dual-publish step to nuget.org cites ADR-0012 §1–§5
  for scope, byte-identity, and gated ordering after the org-feed push.
