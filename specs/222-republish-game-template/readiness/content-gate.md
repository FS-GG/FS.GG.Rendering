# T013 — Content gate: `V` carries Feature 220 (FR-002, SC-002)

Not just the version string — the **content** ancestry and the packed `game` choice.

```
$ git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.54-preview.1 && echo true
true            # the release tag contains b78e72a (Feature 220)
```

The packed template's `game` choice is confirmed live by the scaffold probe (T015,
`game-scaffold.md`): installing `FS.GG.UI.Template@0.1.54-preview.1` from the feed and running
`dotnet new fs-gg-ui --profile game` is **accepted** and emits the Pong-style starter — i.e. the
packed `template.json` carries the `game` choice. ✅
