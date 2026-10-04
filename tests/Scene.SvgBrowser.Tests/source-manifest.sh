#!/usr/bin/env bash
write_source_manifest() {
  local destination="$1"
  git -C "$repo" ls-files -z --cached --others --exclude-standard -- \
    src/KeyboardInput src/Scene src/Scene.SvgBrowser tests/Scene.SvgBrowser.Tests tests/Scene.PortableConsumers/DocumentRoundTrip.fs \
    | sort -z \
    | while IFS= read -r -d '' relative; do
        case "$relative" in
          */bin/*|*/obj/*|*/node_modules/*|*/dist/*|*/packages.lock.json) continue ;;
        esac
        printf '%s\t%s\n' "$(sha256sum "$repo/$relative" | cut -d' ' -f1)" "$relative"
      done > "$destination"
}
