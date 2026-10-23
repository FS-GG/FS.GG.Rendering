// One arm run of the hosted public-fixture software benchmark: fresh Chromium, packed fixture, every record.
import { chromium } from "playwright-core";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { createHash } from "node:crypto";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { runSchema, validateWorkload, workloadRecords } from "./hosted-benchmark-analysis.mjs";

const usage = "Usage: hosted-benchmark-driver.mjs --base-url URL --manifest-sha256 DIGEST --trial N --arm baseline|candidate --position 0|1 --record-order forward|reverse --sustained-seconds S --out RESULT [--executable PATH]";
const argument = (name) => { const i = process.argv.indexOf(name); if (i < 0 || i + 1 >= process.argv.length) throw new Error(`${name} required. ${usage}`); return process.argv[i + 1]; };
const here = dirname(fileURLToPath(import.meta.url));
const sha256 = (bytes) => `sha256:${createHash("sha256").update(bytes).digest("hex")}`;
const workloadBytes = readFileSync(resolve(here, "hosted-benchmark-workload.json"));
const workload = validateWorkload(JSON.parse(workloadBytes));
const pageSource = readFileSync(resolve(here, "hosted-benchmark-page.js"), "utf8");
const harness = { driverSha256: sha256(readFileSync(fileURLToPath(import.meta.url))), pageSha256: sha256(pageSource), analysisSha256: sha256(readFileSync(resolve(here, "hosted-benchmark-analysis.mjs"))), workloadSha256: sha256(workloadBytes) };
const base = new URL(argument("--base-url"));
if (!["127.0.0.1", "localhost"].includes(base.hostname)) throw new Error("Hosted benchmark serves only a loopback fixture");
const expectedManifest = argument("--manifest-sha256"), trial = Number(argument("--trial")), arm = argument("--arm"), position = Number(argument("--position"));
const recordOrder = argument("--record-order"), sustainedSeconds = Number(argument("--sustained-seconds")), out = argument("--out");
if (!/^sha256:[0-9a-f]{64}$/.test(expectedManifest) || ![1, 2, 3].includes(trial) || !["baseline", "candidate"].includes(arm) || ![0, 1].includes(position)
  || !["forward", "reverse"].includes(recordOrder) || !Number.isInteger(sustainedSeconds) || sustainedSeconds < 0 || sustainedSeconds > 600) throw new Error(usage);
// Default: the Playwright-pinned headless shell (Playwright's own headless default), resolved so it can be hashed.
const headlessShell = () => {
  const root = process.env.PLAYWRIGHT_BROWSERS_PATH || resolve(homedir(), ".cache/ms-playwright");
  const found = readdirSync(root).filter((name) => /^chromium_headless_shell-\d+$/.test(name));
  if (found.length !== 1) throw new Error(`expected exactly one installed chromium headless shell under ${root}`);
  return resolve(root, found[0], "chrome-headless-shell-linux64", "chrome-headless-shell");
};
const executablePath = process.argv.includes("--executable") ? argument("--executable") : headlessShell();
const affinity = readFileSync("/proc/self/status", "utf8").match(/^Cpus_allowed_list:\s*(.+)$/m)?.[1] ?? "unknown";
// Twenty records take about two minutes on a software runner; the sustained phase adds its duration.
const deadlineMilliseconds = 900000 + sustainedSeconds * 1000;
const browser = await chromium.launch({ headless: true, args: workload.method.browser.args, executablePath, timeout: 30000 });
const deadline = setTimeout(() => { console.error(`hosted benchmark driver deadline exceeded: ${deadlineMilliseconds}ms`); browser.close().finally(() => process.exit(124)); }, deadlineMilliseconds);
for (const signal of ["SIGTERM", "SIGINT"]) process.once(signal, () => browser.close().finally(() => process.exit(signal === "SIGTERM" ? 143 : 130)));
const result = { schema: runSchema, trial, arm, position, recordOrder, harness, publicFixture: true, outcome: "running", startedAtUtc: new Date().toISOString(),
  claims: { budgetsEvaluated: false, gpuQualified: false, licensedGlyphMeasured: false, performanceAccepted: false } };
try {
  const [width, height] = workload.method.browser.viewport;
  const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: workload.method.browser.deviceScaleFactor });
  const verifyArtifact = async () => {
    const response = await context.request.get(new URL("artifact-manifest.json", base).href);
    const raw = await response.body();
    if (!response.ok() || sha256(raw) !== expectedManifest) throw new Error("served artifact manifest differs from the selected arm");
    const manifest = JSON.parse(raw.toString());
    for (const file of manifest.files) {
      const body = await (await context.request.get(new URL(file.path, base).href)).body();
      if (sha256(body) !== file.sha256) throw new Error(`served artifact file differs: ${file.path}`);
    }
    return manifest;
  };
  const manifest = await verifyArtifact();
  Object.assign(result, { manifestSha256: expectedManifest, artifactId: manifest.artifactId, sourceSha256: manifest.sourceSha256, packages: manifest.packages });
  const page = await context.newPage();
  await page.goto(new URL("index.html", base).href);
  await page.waitForFunction(() => typeof window.svgFoundation?.replaceHostedBenchmarkDocument === "function", undefined, { timeout: 30000 });
  await page.addScriptTag({ type: "module", content: pageSource });
  await page.waitForFunction(() => window.fsggHostedBenchmark, undefined, { timeout: 10000 });
  result.clockResolutionMs = await page.evaluate(() => window.fsggHostedBenchmark.clockResolution(window));
  result.environment = { browser: browser.version(), executablePath, executableSha256: sha256(readFileSync(executablePath)), args: workload.method.browser.args, cpuAffinity: affinity,
    node: process.version, runnerImage: process.env.ImageOS ? `${process.env.ImageOS} ${process.env.ImageVersion ?? ""}`.trim() : "local", userAgent: await page.evaluate(() => navigator.userAgent), hardwareConcurrency: await page.evaluate(() => navigator.hardwareConcurrency) };
  const records = workloadRecords(workload);
  if (recordOrder === "reverse") records.reverse();
  result.records = [];
  for (const record of records) {
    const spec = { ...record, warmupTicks: workload.method.warmupTicks, measuredTicks: workload.method.measuredTicks };
    result.records.push(await page.evaluate((value) => window.fsggHostedBenchmark.runBenchmarkRecord(window, value), spec));
  }
  result.sustained = sustainedSeconds > 0
    ? await page.evaluate((value) => window.fsggHostedBenchmark.runBenchmarkRecord(window, value), { representation: workload.sustainedChurn.representation, count: workload.sustainedChurn.count, motion: workload.sustainedChurn.motion, warmupTicks: 0, measuredTicks: 1, sustainedMilliseconds: sustainedSeconds * 1000 })
    : null;
  await verifyArtifact();
  Object.assign(result, { integrity: "verified-before-and-after", outcome: "completed" });
} catch (error) {
  Object.assign(result, { outcome: "failed", error: error.message });
  process.exitCode = 1;
} finally {
  result.completedAtUtc = new Date().toISOString();
  writeFileSync(out, `${JSON.stringify(result)}\n`);
  await browser.close().catch(() => {});
  clearTimeout(deadline);
  console.log(JSON.stringify({ trial, arm, position, outcome: result.outcome, records: result.records?.length ?? 0, error: result.error ?? null }));
}
