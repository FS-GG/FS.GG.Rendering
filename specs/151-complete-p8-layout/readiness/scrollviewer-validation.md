# Feature151 ScrollViewer Corpus

Status: `accepted`

| Case Id | Viewport | Content Extent | Max Offset | Extent Source | Diagnostics | Verdict | Evidence |
|---|---|---|---|---|---|---|---|
| empty-content | 240x120 | 240x120 | 0x0 | empty content | none | `accepted` | `Feature151ScrollViewerCorpus` |
| smaller-than-viewport | 240x120 | at least 240x120 | 0x0 | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| exact-fit | 240x120 | at least 240x120 | 0x0 | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| barely-overflowing | 240x120 | larger than viewport | positive vertical max | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| substantially-overflowing | 240x120 | larger than viewport | positive vertical max | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| nested-scroll | 240x120 | nested content classified | non-negative | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| clipped-parent | 240x120 | parent clip does not drive extent | non-negative | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| layered-parent | 240x120 | layer does not drive extent | non-negative | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| text-natural-size | 240x120 | text/content natural size | non-negative | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| dynamic-content-change | 240x120 | changed content increases extent | changed max offset | intrinsic content extent | none | `accepted` | `Feature151ScrollViewerCorpus` |
| invalid-intrinsic-fallback | 240x120 | fallback remains finite | non-negative | diagnostic fallback classified | fallback diagnostics | `accepted` | `Feature151Diagnostics` |

Accepted ScrollViewer cases derive extent through `Layout.contentExtent` and the
layout intrinsic/content extent protocol rather than a rendered descendant-bounds
walk.
