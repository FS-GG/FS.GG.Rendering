# Overlay Visual Correlation

- run id: 20260617-203538-994
- scenario id: feature144-antshowcase-date-picker-reference
## artifacts/20260617-203538-994/open.png
- input step: open:date-picker-calendar
- expected overlay state: open
- topmost hit target: date-picker-calendar
- focus state: date-picker-calendar
- product dispatch: DatePickerOpenChanged:true
- replay log: Feature144 AntShowcase date-picker reference flow
- behavioral evidence: samples/AntShowcase/AntShowcase.Core/Evidence.fs:datePickerReferenceOverlayEvidence
## artifacts/20260617-203538-994/closed.png
- input step: close:date-picker-calendar
- expected overlay state: closed
- topmost hit target: none
- focus state: date-picker-trigger
- product dispatch: DatePickerOpenChanged:true; DatePickerChanged:2026-06-17; DatePickerOpenChanged:false
- replay log: Feature144 AntShowcase date-picker reference flow
- behavioral evidence: samples/AntShowcase/AntShowcase.Core/Evidence.fs:datePickerReferenceOverlayEvidence
