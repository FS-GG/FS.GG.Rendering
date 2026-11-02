---
name: initialize-sdd-workspace
description: Initialize a newly scaffolded FS.GG SDD workspace from inside its repository. Verify local readiness, configure only applicable provider or GitHub integrations, and clear the pending initialization warning.
---

# Initialize SDD workspace

Finish configuration from inside the generated repository, where the real git remote, available tools,
and the user's intended collaboration model can be observed. Creation deliberately does not guess
these facts.

## Boundary

- Work only in the current scaffolded workspace. Refuse if `.fsgg/` is absent.
- Read `.fsgg/workspace-initialization.json` first. If its `status` is `initialized`, report the
  recorded choices and offer to repair observed drift; do not repeat the interview automatically.
- Do not require GitHub, a repository remote, a Project board, collaborators, or npm for a local-only
  workspace. Do not invent any of them.
- Ask one concise question at a time only when a choice cannot be learned from the repository. Explain
  why the answer is needed. Prefer current local evidence and safe defaults.
- Obtain explicit user confirmation immediately before creating or mutating any GitHub resource or
  access policy. Initialization is not authorization to create a repository, board, issue, team, or
  collaborator grant.
- Never claim initialization is complete while a required selected action failed or remains
  unverified. Optional integrations may remain skipped when the marker records that choice.

## Initialize

1. Inspect the workspace without changing it:
   - read `.fsgg/providers.yml`, `.fsgg/scaffold-provenance.json`, `.fsgg/security-obligations.json` (the
     Wizard-owned pending security state; absent when no coordination was selected) and the pending marker;
   - inspect the git worktree and configured `origin`, if any;
   - check the commands the selected provider actually needs, then run `dotnet tool restore`,
     `fsgg-sdd doctor --root .`, and the provider's cheapest meaningful build or validation;
   - report actionable local failures before asking about optional integrations.
2. Resolve local configuration only when the selected provider needs it. Read its checked-in
   descriptor and generated docs for the exact parameters; do not ask generic npm questions in a
   provider that has no npm closure. Preserve existing user-authored configuration.
3. Ask whether this workspace should remain local-only or use GitHub coordination. Local-only is a
   complete, supported initialization mode.
4. For GitHub coordination, derive the exact repository identity from `origin`. Select that
   product's explicit organization-owned V2 project and reviewed closed Binding version 3; do not
   attach a product implicitly to the organization project or discover a project by title. Obtain
   the actual immutable producer revision carrying the published adapter/tool pin and both skill
   manifests. Missing publication or binding evidence keeps coordination pending; local-only remains
   a complete supported choice. Do not construct project/field/node IDs from placeholders.
5. Prepare the existing workspace's local integration through the creator's read-only preview:

   ```sh
   new-sdd-workspace retrofit . --repo OWNER/REPO --board-binding /private/selected-binding.json --board-kit-ref IMMUTABLE_SHA --preview
   ```

   Supply actual selected values. The canonical parser validates the binding; preparation verifies
   adapter identity and published consumer digests, preserves unrelated JSON and refuses conflicting
   owner-authored files, legacy wiring or adapter pins before writes. Review the concrete file list,
   then run the same command without `--preview` within the user's authorized local configuration
   scope. Repeating coherent integration is a no-op. This installs local configuration/provenance;
   it creates no GitHub resource, grants no credential/access policy and performs no board write.
   Existing legacy workspaces retain their history until a separately qualified adoption operation
   handles migration; never remove conflicting owner settings to force this installation.

   GitHub resource creation, issue-policy/access changes, selected membership and actual board
   refresh are separate operations. Preserve their existing authorization and independent readback
   requirements. A source fixture or local integration result cannot establish installed adoption.
6. Re-run the local doctor/build checks and verify any selected coordination by inspecting the
   materialized skills, selected binding and its `productBoard` provenance, `.claude/settings.json`
   environment, exact tool restore and work-board's read-only `board-v2 inspect` report. Surface the pending
   obligations in `.fsgg/security-obligations.json` (the SDD-owned `.fsgg/scaffold-provenance.json` carries none);
   a pending security obligation is not a verified receipt. If the doctor reports `scaffold.provenanceMalformed`
   on a workspace created by Wizard 0.23.0, run `new-sdd-workspace secure . --repo OWNER/REPO` or
   `new-sdd-workspace retrofit .`, which moves the legacy Wizard members out of the provenance.
7. On success, replace `.fsgg/workspace-initialization.json` with valid JSON containing at least:
   `schemaVersion: 1`, `status: "initialized"`, an ISO-8601 UTC `initializedAt`, `mode` (`local` or
   `github-coordinated`), and a `checks` object recording the commands and exit codes actually
   observed. For GitHub mode, also record the confirmed repository, immutable project ID, binding digest and exact adapter version. Do not put
   tokens, credentials, collaborator node ids, or other secrets in the marker.

End with a compact receipt: mode, local checks, optional integrations configured or skipped, and any
remaining non-blocking obligations. The persistent warning in `AGENTS.md`/`CLAUDE.md` is conditional
on the marker, so it becomes silent once `status` is `initialized`.
