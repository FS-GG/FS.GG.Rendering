# T011 — Org feed serves `V = 0.1.54-preview.1` (SC-002)

`publish-packages` (release run `28468936061`) pushed **18** packages — `Your package was pushed.`
×18 + `Published coherent set 0.1.54-preview.1 to the org feed.`

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'
0.1.54-preview.1     <- NEW (this release)
0.1.53-preview.1
0.1.52-preview.1
```

Exactly one new version `> 0.1.53-preview.1` is served. ✅
