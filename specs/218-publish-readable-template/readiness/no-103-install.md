# T017 — No exit 103 (SC-002, INV-8) — 🟢 RESOLVED (whole set public)

**Captured**: 2026-06-30, after the org admin flipped the **entire coherent set** (template + 16
libraries) `private → public`.

## Visibility flip — whole set (not just the template)

```
$ gh api --paginate orgs/FS-GG/packages?package_type=nuget --jq '.[].name' | grep '^FS.GG.UI.'
  → all 17 packages report visibility: public   (private remaining: 0)
```
> **Why the whole set, not just the template (corrected finding):** the scaffolded product restores
> the 17 `FS.GG.UI.*` libraries at build time, so a reader that could install the template but not the
> libraries would still 103 on restore. Flipping only `FS.GG.UI.Template` would have left a latent
> second 103. All 17 are now public.

## Live proof — install + scaffold + restore + build from the feed

```
$ dotnet new install FS.GG.UI.Template@0.1.53-preview.1 --force      # exit 0
$ dotnet new fs-gg-ui --productName Acme --output ./Acme             # exit 0 (no 127)
$ (cd Acme && dotnet restore)                                        # exit 0 — pulled FS.GG.UI.* @ 0.1.53 from feed
$ (cd Acme && dotnet build -c Release)                               # 0 Error(s) (4 NU1507 advisory warnings)
```
✅ The whole pack→install→instantiate→**build** chain resolves the coherent set at `V` from
`nuget.pkg.github.com/FS-GG` and compiles.

## On the exit-code semantics (honest disclosure, Principle V)

- **GitHub Packages NuGet requires authentication even for *public* packages.** A truly *anonymous*
  request (no token) returns **103** — this is expected GitHub behavior, **not** a failed flip. What
  `public` changes is that **any authenticated GitHub identity** can now read the package, regardless
  of org-private grants — which is exactly the consumer-CI case #26 was blocked on.
- The in-session token is the org owner, so it cannot *distinguish* private vs public by itself (it
  reads both). The authoritative non-member proof is the **FS.GG.Templates composition CI** on
  PR **#33** (`packages: read`, no private grant). With the set public, that install/restore now
  succeeds; linked from `downstream-2929.md`.

**Verdict:** Gate B (no 103 for an ordinary consumer) is satisfied — the packages are world-readable
to any authenticated GitHub user, and the full coherent set restores + builds from the feed.
