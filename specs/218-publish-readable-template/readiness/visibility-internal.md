# T015 / T016 — Visibility flip (#26) — 🟢 DONE (public, whole set)

**Status (2026-06-30):** ✅ **Resolved by org admin.** The whole coherent set (template + 16
libraries) was flipped `private → public`.

## Corrected mechanism (the plan said `internal`; that wasn't available)

- `internal` visibility (org-wide read) requires the org to be owned by a **GitHub Enterprise**
  account. `FS-GG` is on the **`free` plan** (`gh api orgs/FS-GG --jq .plan.name → free`,
  `is_enterprise_managed: false`), so `internal` is **not offered** — the only org-wide-read option is
  **`public`** (matching the already-public `FS.GG.*.Cli`).
- The flip had to cover the **whole set**, not just `FS.GG.UI.Template`: the scaffolded product
  restores all 17 `FS.GG.UI.*` libraries, so leaving any one private re-introduces exit-103 at build.

## Acceptance — verified

```
$ for p in <all 17 FS.GG.UI.*>; do gh api orgs/FS-GG/packages/nuget/$p --jq .visibility; done
  → public ×17   (private remaining: 0)
```
✅ INV-8 satisfied. Note: GitHub Packages still requires an *authenticated* token even for public
packages (anonymous requests 103 by design); `public` means any authenticated GitHub identity can
read — which is the consumer-CI case #26 needed. See `no-103-install.md`.
