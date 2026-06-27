# Reproducible snapshot evidence — US2 (SM-B / SC-002)

**Pinned version:** `0.1.50-preview.1`

## Locked restore enabled in the template (SM-3)

`template/base/Directory.Build.props` now sets:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode Condition="'$(ContinuousIntegrationBuild)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>
```

It is the single host applying to **every** generated project, so each profile writes its own
**profile-correct** `packages.lock.json` on first restore and resolves the identical graph thereafter.

### Why the lockfile is per-generated-profile, not a committed `template/base` lock

`template/base/src/Product/Product.fsproj` carries every profile's `<PackageReference>` under
`//#if` *comments*. MSBuild (which does not honour `//#if`) sees the **union** of all profiles inside
`template/base`, so a lockfile generated/committed there would lock the union (10 packages). Shipping
that union lock into a stripped profile (e.g. `headless-scene`, which references only `Scene`) and then
running `RestoreLockedMode` would **fail** — a coherence regression, the very thing this feature fixes.
`RestorePackagesWithLockFile` instead makes each generated profile mint its own correct lock. The
recorded artifact below is a real generated-profile lock (the `app` profile), not a union.

## Byte-reproducibility proof (SM-B / SC-002)

Restored the `app` product (host props above applied) **twice**, each into a **fresh, isolated
global-packages cache**:

```
restore #1  --packages /tmp/fsgg-cacheA   → restore1_exit=0   packages.lock.json = 322 lines
restore #2  --packages /tmp/fsgg-cacheB   → restore2_exit=0   packages.lock.json = 322 lines
diff lockA lockB                          → IDENTICAL ✓ (byte-for-byte)
```

- The resolved `FS.GG.UI.*` set in the lock is the profile-correct 10: `Controls`, `Controls.Elmish`,
  `DesignSystem`, `Diagnostics`, `Elmish`, `KeyboardInput`, `Layout`, `Scene`, `SkiaViewer`,
  `Themes.Default` — **every one `@0.1.50-preview.1`** (the only FS.GG.UI version in the graph).
- No phantom `FS.GG.UI.Color` / `FS.GG.UI.SkillSupport` in the resolved graph.
- Recorded lock: [`app-packages.lock.json`](app-packages.lock.json).

## Immutable source (SM-1 / SM-D)

Annotated tag `fs-skia-ui/v0.1.50-preview.1` at the resolution commit; re-checkout + re-pack
reproduces the 16-package set (16/16 `Successfully created package`, recorded in
[`baseline.md`](baseline.md) T005). Manifest: [`../contracts/snapshot-manifest.md`](../contracts/snapshot-manifest.md).

**US2 holds:** pin == tag == every manifest row == every resolved lock entry, and restores are
byte-reproducible.
