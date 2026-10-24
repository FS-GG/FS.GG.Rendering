#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="${1:-$repo/readiness/svg-scale-01-3/scale-qualification.json}"
mode="${SVG_SCENE_PERFORMANCE_MODE:-diagnostic}"
measurement_contract="${SVG_SCALE_MEASUREMENT_CONTRACT:-$repo/readiness/svg-scale-01-1/measurement-contract.json}"
# Hosted coverage is required, and cannot disappear behind the gallery shortcut.
if [[ "${GITHUB_ACTIONS:-}" == true ]]; then
  : "${SVG_SCENE_SELF_RUNNING_OUTPUT:?hosted self-running output required}"
  : "${SVG_SCENE_SELF_RUNNING_CANDIDATE:?hosted candidate identity required}"
  [[ -z "${SVG_SCENE_PERFORMANCE_SMOKE:-}" ]] || { echo 'Hosted self-running coverage cannot use smoke mode' >&2; exit 1; }
fi
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-svg-performance.XXXXXX")"
trap 'rm -rf "$work"' EXIT
rmdir "$work"
mkdir -p "$(dirname "$output")"
bash "$repo/tests/Scene.SvgBrowser.Tests/build-fixture.sh" "$work"
feed="$work/feed"
source_manifest="$work/source-manifest.tsv"
source_manifest_after="$work/source-manifest-after.tsv"
source "$repo/tests/Scene.SvgBrowser.Tests/source-manifest.sh"
source_digest="$(sha256sum "$source_manifest" | cut -d' ' -f1)"
package_digest="$(find "$feed" -name 'FS.GG.UI.*.nupkg' -type f -print0 | sort -z | while IFS= read -r -d '' package; do sha256sum "$package" | cut -d' ' -f1; done | sha256sum | cut -d' ' -f1)"

if [[ "${SVG_SCENE_PERFORMANCE_SMOKE:-}" == "gallery" ]]; then
  node "$work/Browser/performance-test.mjs" --mode diagnostic --gallery-smoke --out "$output" \
    --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest" \
    --measurement-contract "$measurement_contract"
  exit 0
fi

for mutant in unnecessary-rebuild listener-leak excessive-document-acceptance; do
  log="$work/$mutant.log"
  if node "$work/Browser/performance-test.mjs" --mode diagnostic --mutant "$mutant" --out "$work/$mutant.json" \
      --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest" \
      --measurement-contract "$measurement_contract" >"$log" 2>&1; then
    echo "performance mutant survived: $mutant" >&2
    exit 1
  fi
  case "$mutant" in
    unnecessary-rebuild) expected="unnecessary-rebuild gate" ;;
    listener-leak) expected="listener/resource-leak gate" ;;
    excessive-document-acceptance) expected="excessive-document gate" ;;
  esac
  if ! grep -F "$expected" "$log" >/dev/null; then
    cat "$log" >&2
    echo "performance mutant failed for an unexpected reason: $mutant" >&2
    exit 1
  fi
done

if [[ "$mode" == "reference" ]]; then
  reference_ready=false
  pressure_limit="$(jq -r '.referenceHost.cgroupCpuPressureAvg10Maximum' "$measurement_contract")"
  for _ in $(seq 1 120); do
    pressure_avg10="$(sed -n 's/^some avg10=\([0-9.]*\).*/\1/p' /sys/fs/cgroup/cpu.pressure)"
    if awk -v observed="$pressure_avg10" -v limit="$pressure_limit" 'BEGIN { exit !(observed <= limit) }'; then
      reference_ready=true
      break
    fi
    sleep 5
  done
  if [[ "$reference_ready" != true ]]; then
    echo "reference candidate cgroup did not settle below cpu.pressure avg10 $pressure_limit" >&2
    exit 1
  fi
fi

node "$work/Browser/performance-test.mjs" --mode "$mode" --out "$output" \
  --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest" \
  --measurement-contract "$measurement_contract"
write_source_manifest "$source_manifest_after"
if ! cmp "$source_manifest" "$source_manifest_after"; then
  echo "authoritative source manifest changed during performance observation" >&2
  exit 1
fi
echo "svg-scale-performance: result=passed mode=$mode evidence=$output"

# Reuse the packaged fixture only AFTER measurement and its source-stasis check.
# This fixed caller adds no build, restore, pack, Fable or Vite invocation.
if [[ -n "${SVG_SCENE_SELF_RUNNING_OUTPUT:-}" ]]; then
  node --input-type=module - "$work" "$SVG_SCENE_SELF_RUNNING_OUTPUT" "${SVG_SCENE_SELF_RUNNING_CANDIDATE:-local}" "$(git -C "$repo" rev-parse HEAD)" "$repo" <<'SELF_RUNNING_CALLER'
import assert from 'node:assert/strict';
import * as fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';

// Pure acceptance and retirement functions are also exercised by suite-control-test.
function requireSelfRunningResult(value, manifest, manifestHash, expected, executableHash) {
  assert.equal(value.schema, 'fsgg.svg-coherence.page-results/v1');
  assert.equal(value.outcome, 'container-controls-completed');
  assert.equal(value.integrity, 'verified-before-and-after');
  assert.deepEqual(value.artifact, manifest);
  assert.equal(value.manifestSha256, manifestHash);
  assert.equal(expected.length, 17);
  assert.equal(value.cases.length, expected.length);
  value.cases.forEach((row, index) => {
    assert.equal(row.kind, expected[index].kind);
    assert.equal(row.repetition, expected[index].repetition);
    assert.equal(row.outcome, 'pass');
    assert.equal(row.integrity, 'verified-before-and-after');
  });
  for (const key of ['gpuQualified', 'performanceAccepted', 'representativeSoldierQualified']) assert.equal(value.claims[key], false);
  assert.equal(value.driver.downloadObserved, true);
  assert.equal(value.driver.browserCleanup, 'observed-closed');
  assert.equal(value.driver.executableSha256, executableHash);
}
function requireFixtureReady(value, pid, root, url) {
  assert.equal(value.pid, pid);
  assert.equal(value.staticRoot, root);
  assert.equal(value.url, url);
}
async function retireBoth(driver, server, retire, noteFailure) {
  for (const child of [driver, server]) if (child) {
    try { await retire(child); } catch (error) { noteFailure(error); }
  }
}
// END_SELF_RUNNING_PURE_CONTROLS

const [work, output, candidate, checkout, repo] = process.argv.slice(2);
const started = performance.now(), usefulEnd = started + 180000, end = started + 240000;
const sha256 = bytes => `sha256:${createHash('sha256').update(bytes).digest('hex')}`;
const root = fs.realpathSync(path.join(work, 'Browser/dist'));
const prefix = output.replace(/\.json$/, '');
const children = [], secondary = [];
let firstCause = null, server, driver, interrupted, retiring = false;
const noteFailure = error => { const message = String(error?.stack ?? error); if (firstCause === null) firstCause = message; else secondary.push(message); };
const json = (suffix, value) => {
  const fd = fs.openSync(`${prefix}.${suffix}.json`, 'wx', 0o600);
  try { fs.writeFileSync(fd, JSON.stringify(value, null, 2) + '\n'); fs.fsyncSync(fd); } finally { fs.closeSync(fd); }
  const directory = fs.openSync(path.dirname(output), 'r');
  try { fs.fsyncSync(directory); } finally { fs.closeSync(directory); }
};
const birth = pid => {
  const stat = fs.readFileSync(`/proc/${pid}/stat`, 'utf8');
  return stat.slice(stat.lastIndexOf(')') + 2).split(' ')[19];
};
const stop = new Promise((_, reject) => { interrupted = reject; });
// This rejection is observed by every useful await, and by cleanup after failure.
stop.catch(() => {});
const fail = error => { noteFailure(error); interrupted(error); };
const signalHandlers = ['SIGTERM', 'SIGINT'].map(signal => {
  const handler = () => fail(new Error(`Caller interrupted: ${signal}`));
  process.on(signal, handler); return [signal, handler];
});
const usefulTimer = setTimeout(() => fail(new Error('Self-running useful deadline: 180 seconds')), 180000);
const signal = (entry, name) => {
  if (entry.closed) return;
  if (!entry.birth || birth(entry.pid) !== entry.birth) throw new Error(`${entry.label} birth no longer verified; retirement unknown`);
  entry.child.kill(name); entry.signals.push(name);
};
const hardTimer = setTimeout(() => {
  noteFailure(new Error('Self-running total deadline: retirement unknown'));
  for (const entry of children) try { signal(entry, 'SIGKILL'); } catch (error) { noteFailure(error); }
  try { json('deadline', { firstCause, secondary, children: children.map(row => row.record()) }); } catch (error) { console.error(error); }
  process.exit(124);
}, 240000);
const within = async (promise, milliseconds) => {
  let timer;
  try { return await Promise.race([promise, new Promise((_, reject) => { timer = setTimeout(() => reject(new Error('Caller wait deadline')), Math.max(1, milliseconds)); })]); }
  finally { clearTimeout(timer); }
};
const launch = (label, argv) => {
  const logs = ['stdout', 'stderr'].map(stream => fs.openSync(`${prefix}.${label}.${stream}.log`, 'wx', 0o600));
  const entry = { label, argv, pid: null, birth: null, closed: false, eof: [false, false], signals: [], stdout: '', bytes: [0, 0], logs };
  entry.record = () => ({ label, argv, pid: entry.pid, birth: entry.birth, closed: entry.closed, eof: entry.eof, code: entry.code, signal: entry.exitSignal, signals: entry.signals, bytes: entry.bytes, logsClosed: entry.logsClosed === true });
  children.push(entry);
  // Durable fixed intent and exclusive stream destinations precede the effect.
  json(`${label}.intent`, { executable: process.execPath, argv, elapsedMilliseconds: performance.now() - started });
  entry.child = spawn(process.execPath, argv, { stdio: ['ignore', 'pipe', 'pipe'] });
  entry.pid = entry.child.pid;
  entry.done = new Promise(resolve => {
    entry.child.once('error', error => fail(error));
    entry.child.once('close', (code, exitSignal) => { entry.closed = true; entry.code = code; entry.exitSignal = exitSignal; resolve(); });
  });
  [entry.child.stdout, entry.child.stderr].forEach((stream, index) => {
    stream.on('data', bytes => {
      try {
        entry.bytes[index] += bytes.length;
        if (entry.bytes[index] > 1048576) throw new Error(`${label} ${index} output bound`);
        fs.writeSync(logs[index], bytes);
        if (index === 0 && label === 'server') entry.stdout += bytes.toString('utf8');
      } catch (error) { fail(error); }
    });
    stream.once('end', () => { entry.eof[index] = true; });
    stream.once('error', error => fail(error));
  });
  entry.birth = birth(entry.pid);
  json(`${label}.birth`, entry.record());
  return entry;
};
const retire = async entry => {
  if (!entry.closed) {
    try { signal(entry, 'SIGTERM'); } catch (error) { noteFailure(error); }
    try { await within(entry.done, Math.min(15000, end - performance.now() - 10000)); }
    catch (error) { noteFailure(error); signal(entry, 'SIGKILL'); await within(entry.done, Math.min(5000, end - performance.now() - 1000)); }
  }
  assert(entry.closed && entry.eof.every(Boolean), `${entry.label} direct wait/pipe EOF unknown`);
};
try {
  assert(path.isAbsolute(output) && !fs.existsSync(output), 'Fresh absolute self-running output required');
  assert(/^[0-9a-f]{40}$/.test(checkout), 'Actual checkout identity');
  assert(candidate === 'local' || /^[0-9a-f]{40}$/.test(candidate), 'Candidate identity');
  json('intent', { pid: process.pid, birth: birth(process.pid), candidate, checkout, startedAtUtc: new Date().toISOString(), usefulMilliseconds: 180000, totalMilliseconds: 240000, workflowSha256: sha256(fs.readFileSync(path.join(repo, '.github/workflows/svg-browser.yml'))) });
  const rawManifest = fs.readFileSync(path.join(root, 'artifact-manifest.json'));
  const manifest = JSON.parse(rawManifest), manifestHash = sha256(rawManifest);
  const source = fs.readFileSync(path.join(work, 'source-manifest.tsv'));
  assert.equal(manifest.sourceSha256, sha256(source), 'Packaged source identity');
  assert(source.equals(fs.readFileSync(path.join(work, 'source-manifest-after.tsv'))), 'Post-measurement source identity');
  const { suiteContract } = await import(pathToFileURL(path.join(work, 'Browser/suite-contract.js')));
  const expected = suiteContract.cases.flatMap(kind => Array.from({ length: ['ordinary', 'dense', 'extent-near', 'extent-far'].includes(kind) ? suiteContract.method.repetitions : 1 }, (_, repetition) => ({ kind, repetition })));
  const { chromium } = await import(pathToFileURL(path.join(work, 'Browser/node_modules/playwright-core/index.mjs')));
  const executable = chromium.executablePath(), executableHash = sha256(fs.readFileSync(executable));
  json('artifact-manifest', manifest);
  json('source', { candidate, checkout, sourceSha256: sha256(source), sourceManifest: source.toString('utf8'), manifestSha256: manifestHash, executable, executableSha256: executableHash, driverSha256: sha256(fs.readFileSync(path.join(work, 'Browser/drive-suite.mjs'))), serverSha256: sha256(fs.readFileSync(path.join(work, 'Browser/serve-fixture.mjs'))) });
  const url = 'http://127.0.0.1:47813/suite.html';
  server = launch('server', [path.join(work, 'Browser/serve-fixture.mjs'), '--dist', root, '--host', '127.0.0.1', '--port', '47813']);
  server.done.then(() => { if (!retiring) fail(new Error('Fixture server exited before owned retirement')); });
  await Promise.race([stop, within((async () => {
    while (!server.stdout.includes('\n')) {
      assert(!server.closed, 'Fixture server exited before readiness');
      await new Promise(resolve => setTimeout(resolve, 25));
    }
    requireFixtureReady(JSON.parse(server.stdout.split('\n')[0]), server.pid, root, url);
    const response = await fetch(new URL('artifact-manifest.json', url), { signal: AbortSignal.timeout(5000), redirect: 'error' });
    assert(response.ok, 'Fixture manifest HTTP status');
    assert.equal(sha256(Buffer.from(await response.arrayBuffer())), manifestHash, 'Served fixture identity');
  })(), 10000)]);
  const remaining = Math.floor(usefulEnd - performance.now());
  assert(remaining >= 1000 && !firstCause, 'No useful stage budget remains');
  driver = launch('driver', [path.join(work, 'Browser/drive-suite.mjs'), '--base-url', url, '--out', output, '--profile', 'controls', '--deadline-ms', String(remaining), '--executable', executable]);
  await Promise.race([stop, driver.done]);
  assert.equal(driver.code, 0, 'Self-running driver exit');
  assert(driver.eof.every(Boolean), 'Driver pipe EOF');
  assert(fs.statSync(output).size <= 8388608, 'Result output bound');
  requireSelfRunningResult(JSON.parse(fs.readFileSync(output, 'utf8')), manifest, manifestHash, expected, executableHash);
  assert.equal(sha256(fs.readFileSync(path.join(root, 'artifact-manifest.json'))), manifestHash, 'Artifact changed during self-running suite');
  for (const line of source.toString('utf8').trimEnd().split('\n')) {
    const [digest, relative] = line.split('\t');
    assert.equal(sha256(fs.readFileSync(path.join(repo, relative))), `sha256:${digest}`, `Source changed during self-running suite: ${relative}`);
  }
  json('source-after', { sourceSha256: sha256(source), boundSourceBytesUnchanged: true });
} catch (error) { noteFailure(error); }
finally {
  retiring = true;
  clearTimeout(usefulTimer);
  await retireBoth(driver ?? children.find(row => row.label === 'driver'), server ?? children.find(row => row.label === 'server'), retire, noteFailure);
  for (const entry of children) {
    try { for (const fd of entry.logs) { fs.fsyncSync(fd); fs.closeSync(fd); } entry.logsClosed = true; json(`${entry.label}.terminal`, entry.record()); }
    catch (error) { noteFailure(error); }
  }
  if (performance.now() >= end) noteFailure(new Error('Total stage budget expired'));
  try { json('runner', { accepted: firstCause === null, firstCause, secondary, elapsedMilliseconds: performance.now() - started, usefulMilliseconds: 180000, totalMilliseconds: 240000, children: children.map(row => row.record()), scope: 'Existing 17-case compiled self-running controls; no GPU or performance acceptance' }); }
  catch (error) { noteFailure(error); console.error(error); }
  // A failed retirement must not remove the original total-deadline backstop.
  if (children.every(entry => entry.closed)) {
    clearTimeout(hardTimer);
    for (const [name, handler] of signalHandlers) process.removeListener(name, handler);
  }
}
if (firstCause) { console.error(firstCause); process.exitCode = 1; }
else console.log(JSON.stringify({ outcome: 'self-running-controls-passed', cases: 17, output }));
SELF_RUNNING_CALLER
  write_source_manifest "$work/source-manifest-after-self-running.tsv"
  cmp "$source_manifest" "$work/source-manifest-after-self-running.tsv"
fi
