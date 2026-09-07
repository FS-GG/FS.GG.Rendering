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
   - read `.fsgg/providers.yml`, `.fsgg/scaffold-provenance.json`, and the pending marker;
   - inspect the git worktree and configured `origin`, if any;
   - check the commands the selected provider actually needs, then run `dotnet tool restore`,
     `fsgg-sdd doctor --root .`, and the provider's cheapest meaningful build or validation;
   - report actionable local failures before asking about optional integrations.
2. Resolve local configuration only when the selected provider needs it. Read its checked-in
   descriptor and generated docs for the exact parameters; do not ask generic npm questions in a
   provider that has no npm closure. Preserve existing user-authored configuration.
3. Ask whether this workspace should remain local-only or use GitHub coordination. Local-only is a
   complete, supported initialization mode.
4. For GitHub coordination, derive the repository identity from `origin` when possible and show the
   detected value for confirmation. If no usable remote exists, ask whether the user wants to add one;
   do not require it for local-only mode. Then collect only the chosen board's owner/title and, if the
   board is non-FS-GG and chores will be scheduled, its real closed chore-lock issue reference.
5. Before applying the confirmed coordination choice, ensure `new-sdd-workspace` is available. Run:

   ```sh
   new-sdd-workspace retrofit . --repo OWNER/REPO --board OWNER/TITLE
   ```

   Add `--chore-locks OWNER/REPO#N` only when a real required lock exists. Never pass placeholders.
   If the repository exists and the user authorizes the access-policy mutation, separately run:

   ```sh
   new-sdd-workspace secure . --repo OWNER/REPO
   ```

   Project visibility and writer configuration are optional advanced setup. Ask about them only when
   the user selected a Project and wants access managed now; use the tool's explicit `secure`
   recovery route and preserve every pending human-verification obligation it records.
6. Re-run the local doctor/build checks and verify any selected coordination by inspecting the
   materialized skills, `.claude/settings.json` environment, and tool restore. Surface warnings from
   `.fsgg/scaffold-provenance.json`; a pending security obligation is not a verified receipt.
7. On success, replace `.fsgg/workspace-initialization.json` with valid JSON containing at least:
   `schemaVersion: 1`, `status: "initialized"`, an ISO-8601 UTC `initializedAt`, `mode` (`local` or
   `github-coordinated`), and a `checks` object recording the commands and exit codes actually
   observed. For GitHub mode, also record the confirmed repository and board identities. Do not put
   tokens, credentials, collaborator node ids, or other secrets in the marker.

End with a compact receipt: mode, local checks, optional integrations configured or skipped, and any
remaining non-blocking obligations. The persistent warning in `AGENTS.md`/`CLAUDE.md` is conditional
on the marker, so it becomes silent once `status` is `initialized`.
