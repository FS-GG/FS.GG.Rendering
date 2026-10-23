#!/usr/bin/env bash
# Hosted public-fixture SVG software benchmark (decision FS-GG/.github#4645, comment 6359585794).
#   calibration OUT          A/A: the current checkout's fixture against itself, three interleaved trials.
#   gate OUT BASE CAND       A/B: producer sources of BASE and CAND under this harness, three trials;
#                            requires HOSTED_BENCHMARK_TOLERANCE from a calibration.
#   smoke OUT BASE CAND      A/B shape, one trial, no tolerance and no verdict.
# Public fixture only: licensed geometry and original-candidate custody inputs are refused.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
usage() { echo "Usage: run-hosted-benchmark.sh calibration ABSOLUTE_ABSENT_OUT | (gate|smoke) ABSOLUTE_ABSENT_OUT BASELINE_SHA CANDIDATE_SHA" >&2; exit 2; }
mode="${1:-}"; out="${2:-}"
[[ "$out" = /* && ! -e "$out" ]] || usage
case "$mode" in
  calibration) [[ $# -eq 2 ]] || usage; trials=3; sustained="${HOSTED_BENCHMARK_SUSTAINED_SECONDS:-300}" ;;
  gate) [[ $# -eq 4 ]] || usage; trials=3; sustained="${HOSTED_BENCHMARK_SUSTAINED_SECONDS:-300}"
        [[ -n "${HOSTED_BENCHMARK_TOLERANCE:-}" ]] || { echo "gate requires HOSTED_BENCHMARK_TOLERANCE from an A/A calibration" >&2; exit 2; } ;;
  smoke) [[ $# -eq 4 ]] || usage; trials=1; sustained="${HOSTED_BENCHMARK_SUSTAINED_SECONDS:-10}" ;;
  *) usage ;;
esac
if [[ "$mode" != calibration ]]; then
  # Gate and smoke compare two named producers; a blank or repeated SHA would compare nothing.
  [[ "$3" =~ ^[0-9a-f]{40}$ && "$4" =~ ^[0-9a-f]{40}$ ]] || { echo "$mode requires two full 40-hex producer SHAs" >&2; exit 2; }
  [[ "$3" != "$4" ]] || { echo "$mode requires two different producer SHAs" >&2; exit 2; }
fi
[[ "$sustained" =~ ^[0-9]+$ && "$sustained" -le 600 ]] || { echo "sustained seconds must be 0..600" >&2; exit 2; }
if [[ -n "${SVG_SCENE_ORIGINAL_CANDIDATE_DIR:-}${SVG_SCENE_ORIGINAL_CUSTODY:-}${SVG_SCENE_ORIGINAL_CUSTODY_SHA256:-}${SVG_SCENE_ORIGINAL_SOURCE_SHA:-}" ]]; then
  echo "hosted benchmark refuses original-candidate custody inputs" >&2; exit 2
fi
[[ "$(nproc)" -ge 2 ]] || { echo "hosted benchmark needs two logical CPUs (browser and server pinned apart)" >&2; exit 2; }
node "$here/hosted-benchmark-control-test.mjs"

mkdir -p "$out/runs" "$out/logs"
work="$out/work"
pids=()
cleanup() {
  for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
  for pid in "${pids[@]}"; do wait "$pid" 2>/dev/null || true; done
  for tree in "$work"/tree-*; do [[ -d "$tree" ]] && git -C "$repo" worktree remove --force "$tree" 2>/dev/null || true; done
}
trap cleanup EXIT

# Each arm uses this harness checkout with only the producer sources replaced from its revision.
producers=(src/Scene src/Scene.SvgBrowser src/KeyboardInput)
build_arm() {
  local arm="$1" revision="$2" tree="$work/tree-$1"
  if [[ -z "$revision" ]]; then
    # Calibration builds this checkout; its producer sources must equal the committed HEAD they are labelled with.
    tree="$repo"; revision="$(git -C "$repo" rev-parse HEAD)"
    git -C "$repo" diff --quiet HEAD -- "${producers[@]}" && [[ -z "$(git -C "$repo" ls-files --others --exclude-standard -- "${producers[@]}")" ]] \
      || { echo "calibration producer sources differ from HEAD" >&2; exit 2; }
  else
    [[ "$revision" =~ ^[0-9a-f]{40}$ ]] || { echo "$arm revision must be a full commit SHA" >&2; exit 2; }
    mkdir -p "$work"
    git -C "$repo" worktree add --quiet --detach "$tree" HEAD
    git -C "$tree" rm -r -q --ignore-unmatch -- "${producers[@]}"
    git -C "$tree" checkout "$revision" -- "${producers[@]}"
  fi
  bash "$tree/tests/Scene.SvgBrowser.Tests/build-fixture.sh" "$work/fixture-$arm" > "$out/logs/build-$arm.log" 2>&1 \
    || { tail -n 40 "$out/logs/build-$arm.log" >&2; exit 1; }
  printf '%s\n' "$revision" > "$work/fixture-$arm/producer-revision"
}
serve_arm() {
  local arm="$1" port="$2"
  taskset -c 0 node "$here/serve-fixture.mjs" --dist "$work/fixture-$arm/Browser/dist" --host 127.0.0.1 --port "$port" > "$out/logs/serve-$arm.log" 2>&1 &
  pids+=("$!")
  for _ in $(seq 1 100); do curl --silent --fail --output /dev/null "http://127.0.0.1:$port/artifact-manifest.json" && return 0; sleep 0.1; done
  echo "fixture server for $arm did not start" >&2; exit 1
}
manifest_digest() { echo "sha256:$(sha256sum "$work/fixture-$1/Browser/dist/artifact-manifest.json" | cut -d' ' -f1)"; }

if [[ "$mode" == calibration ]]; then
  build_arm baseline ""
  serve_arm baseline 47811
  declare -A url=([baseline]="http://127.0.0.1:47811/" [candidate]="http://127.0.0.1:47811/")
  declare -A digest=([baseline]="$(manifest_digest baseline)" [candidate]="$(manifest_digest baseline)")
  declare -A producer=([baseline]="$(cat "$work/fixture-baseline/producer-revision")" [candidate]="$(cat "$work/fixture-baseline/producer-revision")")
else
  build_arm baseline "$3"
  build_arm candidate "$4"
  serve_arm baseline 47811
  serve_arm candidate 47812
  declare -A url=([baseline]="http://127.0.0.1:47811/" [candidate]="http://127.0.0.1:47812/")
  declare -A digest=([baseline]="$(manifest_digest baseline)" [candidate]="$(manifest_digest candidate)")
  declare -A producer=([baseline]="$3" [candidate]="$4")
fi

orders=(baseline-first candidate-first baseline-first)
record_orders=(forward reverse forward)
for trial in $(seq 1 "$trials"); do
  if [[ "${orders[$((trial - 1))]}" == baseline-first ]]; then arms=(baseline candidate); else arms=(candidate baseline); fi
  for position in 0 1; do
    arm="${arms[$position]}"
    taskset -c 1 node "$here/hosted-benchmark-driver.mjs" --base-url "${url[$arm]}" --manifest-sha256 "${digest[$arm]}" \
      --trial "$trial" --arm "$arm" --position "$position" --record-order "${record_orders[$((trial - 1))]}" \
      --sustained-seconds "$sustained" --producer-revision "${producer[$arm]}" --out "$out/runs/run-t$trial-$arm.json"
  done
done

compare=(node "$here/hosted-benchmark-compare.mjs" --mode "$mode" --runs "$out/runs" --sustained-seconds "$sustained" --out "$out/summary.json" --markdown "$out/summary.md")
[[ "$mode" == gate ]] && compare+=(--tolerance "$HOSTED_BENCHMARK_TOLERANCE")
"${compare[@]}"
