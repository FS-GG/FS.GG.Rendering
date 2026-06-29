# T003 — Feature-217 `productName` symbol is on `main` and is packed (READ-ONLY)

**Captured**: 2026-06-29 · branch `218-publish-readable-template` (cut from `main`).

## Commit `6df0d39` is in history

```
$ git log --oneline | grep 6df0d39
6df0d39 217: add fs-gg-ui --productName scaffold symbol (additive; resolves #27 exit-127)
```

✅ Present.

## `.template.config/template.json` declares the `productName` symbol

```
$ grep -n "productName" .template.config/template.json
81:    "productName": {
87:    "productNameTrimmed": {
92:        "source": "productName",
105:        "sourceVariableName": "productNameTrimmed",
```

✅ The `productName` parameter symbol (and its derived `productNameTrimmed`) is declared in the
template config that `.template.package/FS.GG.UI.Template.fsproj` packs. So a coherent-set release
cut from `main` carries the Feature-217 `--productName` capability — the producer half of exit-127's
fix. The only thing missing pre-release is that the published feed version (`0.1.52-preview.1`)
predates this commit; publishing a `> 0.1.52-preview.1` version exposes it.

**Verdict**: Feature 217 is on `main` and packed. No `.fs`/`.fsi`/template change is needed in this
feature — it is publish + visibility only.
