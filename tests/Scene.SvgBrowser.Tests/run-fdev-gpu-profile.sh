#!/usr/bin/env bash
# fdev virtio-gpu functional-GPU and early-baseline session (FS-GG/.github#4645). Not B1-B8 evidence.
# Usage: run-fdev-gpu-profile.sh ABSOLUTE_ABSENT_OUT SEALED_DIST CHROMIUM_EXECUTABLE [PORT]
# Interleaves three profiles over three trials, runs the maintained 17-case controls suite plus the public-circle
# records and a short raster trace under each, then kills the three maintained suite mutants on both GPU profiles.
# Run it under: systemd-run --user --scope -p CPUQuota=200% -p MemoryMax=4G
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
out="${1:?absent output directory}"; dist="${2:?sealed fixture dist}"; exe="${3:?chromium executable}"; port="${4:-47820}"
[[ "$out" = /* && ! -e "$out" ]] || { echo "output must be absolute and absent" >&2; exit 2; }
mkdir -p "$out/suite" "$out/profile" "$out/mutants" "$out/logs"
{ date -u +%FT%TZ; cat /proc/loadavg; uname -r; vulkaninfo --summary 2>/dev/null | sed -n '/^Devices/,$p' | head -20; } > "$out/session-start.txt" 2>&1 || true
node "$here/serve-fixture.mjs" --dist "$dist" --host 127.0.0.1 --port "$port" > "$out/logs/serve.log" 2>&1 & server=$!
trap 'kill "$server" 2>/dev/null || true; wait "$server" 2>/dev/null || true' EXIT
for _ in $(seq 1 100); do curl -fsS -o /dev/null "http://127.0.0.1:$port/artifact-manifest.json" && break; sleep .1; done
url="http://127.0.0.1:$port/"
args() { case "$1" in
  software) echo '["--disable-gpu"]';;
  gpu-angle-vulkan) echo '["--use-angle=vulkan","--enable-features=Vulkan","--ignore-gpu-blocklist","--enable-gpu-rasterization"]';;
  gpu-angle-gl-egl) echo '["--use-angle=gl-egl","--ignore-gpu-blocklist","--enable-gpu-rasterization"]';; esac; }
orders=("software gpu-angle-vulkan gpu-angle-gl-egl" "gpu-angle-vulkan gpu-angle-gl-egl software" "gpu-angle-gl-egl software gpu-angle-vulkan")
trial=0
for order in "${orders[@]}"; do
  trial=$((trial + 1))
  for profile in $order; do
    cat /proc/loadavg > "$out/logs/load-t$trial-$profile.txt"
    timeout 240 node "$here/drive-suite.mjs" --base-url "$url" --profile controls --deadline-ms 200000 --executable "$exe" \
      --browser-args-json "$(args "$profile")" --profile-label "fdev-virtio-gpu-$profile-not-b1-b8" --out "$out/suite/t$trial-$profile.json" > "$out/logs/suite-t$trial-$profile.log" 2>&1
    timeout 420 node "$here/fdev-gpu-profile.mjs" --profile "$profile" --executable "$exe" --base-url "$url" --benchmark --trace \
      --out "$out/profile/t$trial-$profile.json" > "$out/logs/profile-t$trial-$profile.log" 2>&1
  done
done
for profile in gpu-angle-vulkan gpu-angle-gl-egl; do
  for mutant in unnecessary-rebuild listener-leak excessive-document-acceptance; do
    if timeout 240 node "$here/drive-suite.mjs" --base-url "$url" --profile controls --deadline-ms 200000 --executable "$exe" --mutant "$mutant" \
      --browser-args-json "$(args "$profile")" --profile-label "fdev-virtio-gpu-$profile-not-b1-b8" --out "$out/mutants/$profile-$mutant.json" > "$out/logs/mutant-$profile-$mutant.log" 2>&1; then
      echo "mutant survived: $profile $mutant" | tee -a "$out/mutants/SURVIVED"; else echo "killed $profile $mutant" >> "$out/mutants/KILLED"; fi
  done
done
timeout 120 node "$here/fdev-gpu-profile.mjs" --profile default-flags --executable "$exe" --base-url "$url" --out "$out/profile/default-flags.json" > "$out/logs/profile-default-flags.log" 2>&1 || true
date -u +%FT%TZ > "$out/session-end.txt"
(cd "$out" && find . -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 sha256sum > SHA256SUMS)
