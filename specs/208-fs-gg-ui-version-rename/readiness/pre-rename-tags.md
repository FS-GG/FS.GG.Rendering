# T003 — Pre-rename tag state (recorded 2026-06-27)

```
$ git tag -l 'fs-skia-ui/v*'
fs-skia-ui/v0.1.50-preview.1
fs-skia-ui/v0.1.51-preview.1
```

Target commits (these MUST be preserved by the fs-gg-ui/v* re-tag, FR-004):

| legacy tag | commit |
|---|---|
| fs-skia-ui/v0.1.50-preview.1 | 57be86c81436c6adb26f46bbd4a08c02becec25e |
| fs-skia-ui/v0.1.51-preview.1 | d9f4c81ad57903537eb4de116b206676ce1a2dc8 |

Expected short forms per plan: 57be86c, d9f4c81 →
  v0.1.50 short: 57be86c  | v0.1.51 short: d9f4c81

Unrelated namespace left untouched: fs-gg-ui-template/v0.1.50-preview.1
