# T002 — Environment & prerequisites (Feature 212)

Recorded: 2026-06-28 on branch `212-template-root-build`.

## SDK band (SC-006 mismatch case present)

```
$ dotnet --version
10.0.301

$ dotnet --list-sdks
6.0.428 [/usr/share/dotnet/sdk]
10.0.301 [/usr/share/dotnet/sdk]
```

- A **10.0.x** SDK (10.0.301) is present — satisfies the `net10.0` band the product pins.
- A differing **6.0.x** SDK (6.0.428) is also installed. With **no root `global.json`**, the
  resolved default `dotnet --version` is 10.0.301 here, but the presence of 6.0.428 is exactly the
  default-SDK-mismatch case the emitted product `global.json` must neutralize (a host whose default
  resolves to 6.0.x must still build the product against net10). This is the SC-006 scenario.

## Content root present

```
$ ls template/base/Directory.Build.props .template.config/template.json
template/base/Directory.Build.props
.template.config/template.json
```

- `template/base/` exists (the ungated product content root); `.template.config/template.json`
  exists with `sourceName = "Product"`. Confirmed before any artifact authoring.

## Notes

- No root `global.json` exists in the framework repo itself (`cat global.json` → not found); the SDK
  pin authored in T005 is for the **emitted product**, under `template/base/global.json`, not the
  framework root.
</content>
</invoke>
