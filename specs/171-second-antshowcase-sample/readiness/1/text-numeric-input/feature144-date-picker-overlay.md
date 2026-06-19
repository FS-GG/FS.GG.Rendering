# Feature 144 date-picker overlay evidence

- replay: navigate:text-numeric-input -> open:date-picker-calendar -> focus:calendar -> select:2026-06-17 -> close:date-picker-calendar -> focus:trigger
- product messages: DatePickerOpenChanged:true, DatePickerChanged:2026-06-17, DatePickerOpenChanged:false
- diagnostics: none
- no stale overlay: true
