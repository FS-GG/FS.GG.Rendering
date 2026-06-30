# T005 — Early live feed probe (BEFORE any tag)

Confirms the gap is real on the **actual org feed** before a release tag is pushed (quickstart §0,
plan Standing assumption). Run 2026-06-30 as `EHotwagner` (org member token).

## Feed listing — `FS.GG.UI.Template`

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'
0.1.53-preview.1
0.1.52-preview.1
```

→ No `0.1.54-preview.1` (no `V`) yet. ✅ expected.

## Content gap — the served `0.1.53` lacks Feature 220

```
$ git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.53-preview.1 && echo HAS || echo LACKS
LACKS
$ git merge-base --is-ancestor b78e72a main && echo "on main" || echo "missing"
on main
```

→ The feed's current template (`0.1.53-preview.1`) does **not** contain `b78e72a`; `main` does. The
`game` profile is therefore not feed-selectable today — the work is real. ✅

## Consumer reachability path (carried from Feature 218)

The org feed's `FS.GG.UI.Template` package is org-readable (Feature 218 resolved exit-103). A full
foreign `packages: read`-only-token `dotnet new install` is the **post-publish** probe (T014) against
`V`; the pre-publish run only establishes the listing gap above. The consumer install of `V` is
captured in `consumer-install.md` after CI publishes.

**Conclusion**: deterministic-local checks aside, the live feed confirms `0.1.53-preview.1` is served
**without** `game`, and `main` carries the fix — the publish (US2) is warranted.
