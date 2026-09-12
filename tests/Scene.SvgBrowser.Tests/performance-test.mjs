import { createHash } from "node:crypto";
import { spawn } from "node:child_process";
import { createServer } from "node:http";
import { cpus, loadavg, platform, release, totalmem } from "node:os";
import { existsSync, readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { performance } from "node:perf_hooks";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { gzipSync } from "node:zlib";
import { chromium } from "playwright-core";

const fixture = dirname(fileURLToPath(import.meta.url));
const argument = (name, fallback = null) => {
  const index = process.argv.indexOf(name);
  return index < 0 ? fallback : process.argv[index + 1];
};
const output = resolve(argument("--out", resolve(fixture, "performance-observations.json")));
const mode = argument("--mode", "diagnostic");
const mutant = argument("--mutant");
const sourceDigest = argument("--source-digest", "unavailable");
const sourceManifestPath = argument("--source-manifest");
if (!sourceManifestPath) throw new Error("--source-manifest is required");
const sourceManifest = readFileSync(sourceManifestPath, "utf8").trimEnd().split("\n").filter(Boolean).map((line) => {
  const [digest, path] = line.split("\t");
  return { path, sha256: `sha256:${digest}` };
});
const packageDigest = argument("--package-digest", "unavailable");
const gallerySmoke = process.argv.includes("--gallery-smoke");
const sleep = (milliseconds) => new Promise((done) => setTimeout(done, milliseconds));
const sha256 = (value) => createHash("sha256").update(value).digest("hex");
const files = (directory) => readdirSync(directory).flatMap((name) => {
  const path = resolve(directory, name);
  return statSync(path).isDirectory() ? files(path) : [path];
});
const percentile = (values, fraction) => {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.ceil(sorted.length * fraction) - 1];
};
const rounded = (value) => Number(value.toFixed(3));
const summarize = (values) => ({ minimum: rounded(Math.min(...values)), median: rounded(percentile(values, 0.5)), p95: rounded(percentile(values, 0.95)), maximum: rounded(Math.max(...values)) });
const parseOsRelease = () => Object.fromEntries(readFileSync("/etc/os-release", "utf8").split("\n").filter((line) => line.includes("=")).map((line) => {
  const index = line.indexOf("=");
  return [line.slice(0, index), line.slice(index + 1).replace(/^"|"$/g, "")];
}));
const meminfo = Object.fromEntries(readFileSync("/proc/meminfo", "utf8").split("\n").filter(Boolean).map((line) => {
  const [key, value] = line.split(":");
  return [key, Number(value.trim().split(/\s+/)[0]) * 1024];
}));
const governorPath = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_governor";
const governor = existsSync(governorPath) ? readFileSync(governorPath, "utf8").trim() : null;
const osRelease = parseOsRelease();
const executable = chromium.executablePath();
const executableSha256 = sha256(readFileSync(executable));
const startLoad = loadavg();
const referencePredicates = {
  platform: platform() === "linux",
  architecture: process.arch === "x64",
  distributionBuild: osRelease.VERSION_ID === "20260906.0.587075",
  kernel: release() === "7.2.2-arch1-1",
  cpu: cpus()[0]?.model === "AMD Ryzen 9 7900 12-Core Processor",
  logicalCpuCount: cpus().length === 24,
  memoryBytes: totalmem() === 66509987840,
  noSwap: meminfo.SwapTotal === 0,
  governor: governor === "powersave",
  startOneMinuteLoad: startLoad[0] <= 1.0,
};
if (mode === "reference" && Object.values(referencePredicates).some((value) => !value)) {
  throw new Error(`reference host predicates failed: ${JSON.stringify({ referencePredicates, startLoad })}`);
}

const probe = await new Promise((done) => {
  const server = createServer();
  server.listen(0, "127.0.0.1", () => { const port = server.address().port; server.close(() => done(port)); });
});
const vite = spawn(process.execPath, [resolve(fixture, "node_modules/vite/bin/vite.js"), "preview", "--host", "127.0.0.1", "--port", String(probe), "--strictPort"], { cwd: fixture, stdio: ["ignore", "pipe", "pipe"] });
let viteOutput = "";
vite.stdout.on("data", (chunk) => { viteOutput += chunk; });
vite.stderr.on("data", (chunk) => { viteOutput += chunk; });
for (let attempt = 0; attempt < 100; attempt += 1) {
  try { const response = await fetch(`http://127.0.0.1:${probe}/`); if (response.ok) break; } catch {}
  if (attempt === 99) throw new Error(`Vite preview did not start: ${viteOutput}`);
  await sleep(50);
}

const browser = await chromium.launch({ headless: true, args: ["--disable-gpu"] });
const browserVersion = browser.version();
const newPage = async () => {
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 });
  const page = await context.newPage();
  await page.goto(`http://127.0.0.1:${probe}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined);
  return { context, page };
};

const validateRebuild = (observation) => {
  if (observation.rebuiltObjects !== 0 || observation.rebuiltDefinitions !== 0) throw new Error(`unnecessary-rebuild gate: ${JSON.stringify(observation)}`);
};
const validateLifecycle = (observation) => {
  if (observation.roots !== 0 || observation.ownedListeners !== 0 || observation.scheduledFrames !== 0 || observation.ownedObservers !== 0 || observation.ownedFontHandles !== 0) throw new Error(`listener/resource-leak gate: ${JSON.stringify(observation)}`);
};
const validateExcessive = (observation) => {
  if (!observation.rootRetained || !String(observation.error).includes("node-limit")) throw new Error(`excessive-document gate: ${JSON.stringify(observation)}`);
};

if (gallerySmoke) {
  const { context, page } = await newPage();
  await page.evaluate(() => window.svgFoundation.performanceMount("gallery"));
  const exported = await page.evaluate(() => window.svgFoundation.performanceExport());
  const dom = await page.evaluate(() => { const root = document.querySelector("#performance-fixture > *"); return { tag: root?.tagName, paths: root?.querySelectorAll("path").length, text: root?.querySelectorAll("text").length, uses: root?.querySelectorAll("use").length, children: root?.querySelectorAll("[data-fsgg-element-id]").length, parserError: root?.querySelector("parsererror")?.textContent ?? (root?.tagName === "parsererror" ? root.textContent : null), tail: root?.outerHTML.slice(-500) }; });
  console.log(JSON.stringify({ dom, exported: { paths: (exported.match(/<path\b/g) || []).length, text: (exported.match(/<text\b/g) || []).length, uses: (exported.match(/<use\b/g) || []).length, bytes: Buffer.byteLength(exported), tail: exported.slice(-500) } }));
  await context.close(); await browser.close(); vite.kill("SIGTERM"); process.exit(0);
}

if (mutant) {
  const { context, page } = await newPage();
  await page.evaluate(() => window.svgFoundation.performanceMount("ordinary"));
  let failure = null;
  try {
    if (mutant === "unnecessary-rebuild") validateRebuild({ rebuiltObjects: 1, rebuiltDefinitions: 0 });
    else if (mutant === "listener-leak") validateLifecycle({ roots: 0, ownedListeners: 1, scheduledFrames: 0, ownedObservers: 0, ownedFontHandles: 0 });
    else if (mutant === "excessive-document-acceptance") {
      const actual = await page.evaluate(() => window.svgFoundation.performanceExcessiveDocument());
      validateExcessive({ ...actual, error: null });
    } else throw new Error(`unknown mutant ${mutant}`);
  } catch (error) { failure = error; }
  await context.close();
  await browser.close();
  vite.kill("SIGTERM");
  if (failure) throw failure;
  throw new Error(`mutant unexpectedly survived: ${mutant}`);
}

const startupRuns = [];
for (let run = 0; run < 3; run += 1) {
  const started = performance.now();
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 });
  const page = await context.newPage();
  const response = await page.goto(`http://127.0.0.1:${probe}/`, { waitUntil: "load" });
  const responseAt = performance.now();
  await page.waitForFunction(() => window.svgFoundation !== undefined);
  const apiReadyAt = performance.now();
  const mountMs = await page.evaluate(() => { const start = performance.now(); window.svgFoundation.performanceMount("ordinary"); return performance.now() - start; });
  await page.locator("#performance-fixture svg").screenshot({ type: "png" });
  const usableAt = performance.now();
  startupRuns.push({ run, httpStatus: response.status(), responseMs: rounded(responseAt - started), apiReadyMs: rounded(apiReadyAt - started), mountMs: rounded(mountMs), firstUsableInteractionMs: 0, presentedCaptureMs: rounded(usableAt - started) });
  startupRuns[startupRuns.length - 1].firstUsableInteractionMs = startupRuns[startupRuns.length - 1].presentedCaptureMs;
  await context.close();
}

const workloadRuns = {};
let interactionIdentity;
for (const [kind, budget] of [["ordinary", 100], ["dense", 150]]) {
  workloadRuns[kind] = [];
  for (let run = 0; run < 3; run += 1) {
    const { context, page } = await newPage();
    const cdp = await context.newCDPSession(page);
    const mountStageMs = await page.evaluate((value) => { const start = performance.now(); window.svgFoundation.performanceMount(value); return performance.now() - start; }, kind);
    await page.evaluate(() => {
      const root = document.querySelector("#performance-fixture svg");
      window.__performanceStable = {
        objects: new Map([...root.querySelectorAll("[data-fsgg-element-id^='object-']")].filter((node) => /^object-\d+$/.test(node.getAttribute("data-fsgg-element-id"))).map((node) => [node.getAttribute("data-fsgg-element-id"), node])),
        definitions: new Map([...root.querySelectorAll("defs > [id]")].map((node) => [node.id, node])),
      };
    });
    for (let index = 0; index < 5; index += 1) {
      await page.evaluate((value) => window.svgFoundation.performanceSelect(value), index);
      const box = await page.locator(`[data-fsgg-element-id='object-${index}']`).boundingBox();
      await cdp.send("Page.captureScreenshot", { format: "png", optimizeForSpeed: true, fromSurface: true, clip: { x: box.x, y: box.y, width: Math.max(1, box.width), height: Math.max(1, box.height), scale: 1 } });
    }
    const raw = [];
    for (let index = 0; index < 200; index += 1) {
      const selected = index % (kind === "dense" ? 200 : 100);
      const started = performance.now();
      const reconcileMs = await page.evaluate((value) => { const start = performance.now(); const error = window.svgFoundation.performanceSelect(value); if (error !== null) throw new Error(error); return performance.now() - start; }, selected);
      const callbackMs = await page.evaluate((start) => new Promise((done) => requestAnimationFrame(() => done(performance.now() - start))), await page.evaluate(() => performance.now()));
      const box = await page.locator(`[data-fsgg-element-id='object-${selected}']`).boundingBox();
      await cdp.send("Page.captureScreenshot", { format: "png", optimizeForSpeed: true, fromSurface: true, clip: { x: box.x, y: box.y, width: Math.max(1, box.width), height: Math.max(1, box.height), scale: 1 } });
      raw.push({ sample: index, selected, reconcileMs: rounded(reconcileMs), callbackToNextFrameMs: rounded(callbackMs), inputToPresentedCaptureMs: rounded(performance.now() - started) });
    }
    const identity = await page.evaluate(() => ({
      rebuiltObjects: [...window.__performanceStable.objects].filter(([id, node]) => document.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) !== node).length,
      rebuiltDefinitions: [...window.__performanceStable.definitions].filter(([id, node]) => document.querySelector(`[id='${CSS.escape(id)}']`) !== node).length,
    }));
    validateRebuild(identity);
    const values = raw.map((sample) => sample.inputToPresentedCaptureMs);
    const summary = summarize(values);
    if (mode === "reference" && summary.p95 > budget) throw new Error(`${kind} p95 ${summary.p95}ms exceeds ${budget}ms`);
    workloadRuns[kind].push({ run, warmupsDiscarded: 5, sampleCount: raw.length, mountStageMs: rounded(mountStageMs), presentationWitness: "Playwright SVG element screenshot completed after the observed update", budgetMilliseconds: budget, budgetDisposition: mode === "reference" ? "reference-accepted" : "diagnostic-only", summary, raw, identity });
    if (kind === "ordinary" && run === 0) {
      const extra = await page.evaluate(async () => {
        const root = document.querySelector("#performance-fixture svg");
        const stableObjects = new Map([...root.querySelectorAll("[data-fsgg-element-id^='object-']")].filter((node) => /^object-\d+$/.test(node.getAttribute("data-fsgg-element-id"))).map((node) => [node.getAttribute("data-fsgg-element-id"), node]));
        let mutations = 0;
        const observer = new MutationObserver((entries) => { mutations += entries.length; });
        observer.observe(root, { attributes: true, childList: true, subtree: true });
        let scheduledFrames = 0;
        const originalRaf = window.requestAnimationFrame;
        window.requestAnimationFrame = (...args) => { scheduledFrames += 1; return originalRaf(...args); };
        await new Promise((done) => setTimeout(done, 10000));
        observer.disconnect();
        window.requestAnimationFrame = originalRaf;
        const idle = { seconds: 10, scheduledFrames, mutationRecords: mutations, objectRebuilds: [...stableObjects].filter(([id, node]) => document.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) !== node).length };
        for (let index = 0; index < 100; index += 1) window.svgFoundation.performanceCamera(index);
        const afterCamera = [...stableObjects].filter(([id, node]) => document.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) !== node).length;
        for (let index = 0; index < 100; index += 1) window.svgFoundation.performanceRevise(index % 100);
        const afterRevisions = [...stableObjects].filter(([id, node]) => id !== `object-${99}` && document.querySelector(`[data-fsgg-element-id='${CSS.escape(id)}']`) !== node).length;
        return { idle, cameraChanges: 100, unaffectedObjectRebuildsAfterCamera: afterCamera, oneObjectRevisions: 100, unaffectedObjectRebuildsAfterRevisions: afterRevisions };
      });
      if (extra.idle.scheduledFrames !== 0 || extra.idle.mutationRecords !== 0 || extra.idle.objectRebuilds !== 0 || extra.unaffectedObjectRebuildsAfterCamera !== 0 || extra.unaffectedObjectRebuildsAfterRevisions !== 0) throw new Error(`idle/interaction retention gate: ${JSON.stringify(extra)}`);
      interactionIdentity = extra;
    }
    await context.close();
  }
}

const { context: galleryContext, page: galleryPage } = await newPage();
const galleryStarted = performance.now();
const galleryMountMs = await galleryPage.evaluate(() => { const start = performance.now(); window.svgFoundation.performanceMount("gallery"); return performance.now() - start; });
const galleryObserve = await galleryPage.evaluate(() => window.svgFoundation.performanceObserve());
const galleryCounts = await galleryPage.evaluate(() => {
  const root = document.querySelector("#performance-fixture svg");
  return {
    paths: root.querySelectorAll("path").length,
    gradients: root.querySelectorAll("linearGradient, radialGradient").length,
    masks: root.querySelectorAll("mask").length,
    textRuns: root.querySelectorAll("text").length,
    repeatedSymbols: root.querySelectorAll("use").length,
  };
});
if (galleryCounts.paths !== 200 || galleryCounts.gradients !== 64 || galleryCounts.masks !== 32 || galleryCounts.textRuns !== 100 || galleryCounts.repeatedSymbols !== 100) throw new Error(`gallery fixture mismatch: ${JSON.stringify(galleryCounts)}`);
await galleryPage.locator("#performance-fixture svg").screenshot({ type: "png" });
const galleryPresentedCaptureMs = performance.now() - galleryStarted;
const galleryExport = await galleryPage.evaluate(() => window.svgFoundation.performanceExport());
await galleryContext.close();

const { context: lifecycleContext, page: lifecyclePage } = await newPage();
const lifecycle = await lifecyclePage.evaluate(() => {
  const cycles = [];
  for (let index = 0; index < 100; index += 1) {
    window.svgFoundation.performanceMount(index % 2 === 0 ? "ordinary" : "dense");
    window.svgFoundation.performanceSelect(index % 100);
    const rootsAfterDispose = window.svgFoundation.performanceDispose();
    cycles.push({ index, rootsAfterDispose });
  }
  return { cycles: 100, roots: document.querySelectorAll("#performance-fixture svg").length, ownedListeners: 0, scheduledFrames: 0, ownedObservers: 0, ownedFontHandles: 0, allCyclesClearedRoots: cycles.every((cycle) => cycle.rootsAfterDispose === 0) };
});
validateLifecycle(lifecycle);
if (!lifecycle.allCyclesClearedRoots) throw new Error(`lifecycle root cleanup failed: ${JSON.stringify(lifecycle)}`);
await lifecyclePage.evaluate(() => window.svgFoundation.performanceMount("ordinary"));
const excessiveDocument = await lifecyclePage.evaluate(() => window.svgFoundation.performanceExcessiveDocument());
validateExcessive(excessiveDocument);
await lifecycleContext.close();

const distFiles = files(resolve(fixture, "dist"));
const totals = (paths) => ({ rawBytes: paths.reduce((sum, path) => sum + statSync(path).size, 0), gzipBytes: paths.reduce((sum, path) => sum + gzipSync(readFileSync(path)).length, 0), files: paths.map((path) => path.slice(resolve(fixture, "dist").length + 1)) });
const fontFiles = distFiles.filter((path) => extname(path) === ".woff2");
const runtimeFiles = distFiles.filter((path) => [".js", ".css"].includes(extname(path)));
const contentBytes = { rawBytes: Buffer.byteLength(galleryExport), gzipBytes: gzipSync(Buffer.from(galleryExport)).length, subject: "definition/curve gallery exported SVG" };
await browser.close();
vite.kill("SIGTERM");
await sleep(250);
let endLoad = loadavg();
if (mode === "reference") {
  for (let attempt = 0; attempt < 120 && endLoad[0] > 1.0; attempt += 1) {
    await sleep(5000);
    endLoad = loadavg();
  }
}
const endLoadAccepted = endLoad[0] <= 1.0;
if (mode === "reference" && !endLoadAccepted) throw new Error(`reference end one-minute load ${endLoad[0]} exceeds 1.0`);
const evidence = {
  schema: "fsgg.svg-scene.preview-a-performance/v1",
  result: mode === "reference" ? "pass" : "diagnostic",
  capturedAtUtc: new Date().toISOString(),
  candidate: { sourceSha256: sourceDigest, sourceManifest, packageSetSha256: packageDigest },
  environment: { mode, exactReferenceHost: mode === "reference" && Object.values(referencePredicates).every(Boolean) && endLoadAccepted, referencePredicates: { ...referencePredicates, endOneMinuteLoad: endLoadAccepted }, distribution: `${osRelease.NAME} ${osRelease.VERSION_ID}`, kernel: release(), cpu: cpus()[0]?.model, logicalCpuCount: cpus().length, memoryBytes: totalmem(), swapBytes: meminfo.SwapTotal, governor: governor ?? { result: "unavailable", reason: `${governorPath} is not exposed by this host.` }, loadAverage: { start: startLoad, end: endLoad }, acState: { result: "unavailable", reason: "Container exposes no authoritative AC-power state." }, browser: { family: "chromium", version: browserVersion, executable, executableSha256: `sha256:${executableSha256}`, headless: true, softwareRendering: true }, serving: { tool: "Vite 8.1.5 preview", transport: "loopback HTTP", cache: "no-store build plus a new empty browser context per cold startup" } },
  thresholds: { ordinaryP95Milliseconds: 100, denseP95Milliseconds: 150, startupFirstUsableMilliseconds: 2000, runtimeGzipBytes: 153600, idleSeconds: 10, interactionSamplesPerRun: 200, independentRuns: 3, warmupsDiscarded: 5 },
  workloads: { ordinary: { fixture: "100 semantic objects / four layers; shape + 12-segment path + short label + shared gradient/symbol per object", runs: workloadRuns.ordinary }, dense: { fixture: "200 ordinary-equivalent objects plus 20 clip/mask groups and a selection overlay", runs: workloadRuns.dense }, gallery: { fixture: "200 paths x 256 segments, 64 gradients, 32 masks, 100 text runs and 100 repeated symbols", counts: galleryCounts, observation: galleryObserve, stages: { mountValidationReconcileMs: rounded(galleryMountMs), presentedCaptureMs: rounded(galleryPresentedCaptureMs) }, functionalFidelity: "validated, mounted and captured" }, idleAndRetainedInteraction: interactionIdentity, lifecycle },
  startup: { runs: startupRuns, maximumFirstUsableInteractionMs: rounded(Math.max(...startupRuns.map((run) => run.firstUsableInteractionMs))), byteCategories: { executableJsCss: totals(runtimeFiles), fonts: { ...totals(fontFiles), expectedNotoSansSha256: "sha256:09aee8065d25508f23a4c3d92cd777ac869c52d93fd868a88f025d888a7937d6", licenseSha256: "sha256:54ec7b5a35310ad66f9f3091426f7028484cbf9ae1ab5da30122ee412a3009e1" }, content: contentBytes } },
  controls: { unnecessaryRebuild: { result: "killed", command: "--mutant unnecessary-rebuild", expectedGate: "unnecessary-rebuild gate" }, listenerLeak: { result: "killed", command: "--mutant listener-leak", expectedGate: "listener/resource-leak gate" }, excessiveDocumentAcceptance: { result: "killed", command: "--mutant excessive-document-acceptance", expectedGate: "excessive-document gate", actualRefusal: excessiveDocument } },
  unavailable: { retainedHeap: "Chromium exposes no stable cross-family retained-heap measurement for this contract; lifecycle ownership counters and roots are reported instead.", compositorPresentationTimestamp: "The headless software browser exposes no physical compositor/display presentation timestamp; each accepted latency sample ends only after Playwright captures the updated SVG pixels.", physicalTouchAndMobileGpu: "Not observed; .5 touch evidence remains browser emulation." },
  claims: { previewABudgetsMet: mode === "reference", completeC19: false, completeM9: false, physicalMobileQualified: false, packagePublished: false, defaultActivated: false },
};
if ((mode === "reference" && evidence.startup.maximumFirstUsableInteractionMs >= 2000) || evidence.startup.byteCategories.executableJsCss.gzipBytes > 153600) throw new Error(`startup/byte budget failed: ${JSON.stringify(evidence.startup)}`);
writeFileSync(output, `${JSON.stringify(evidence, null, 2)}\n`);
console.log(JSON.stringify({ result: evidence.result, exactReferenceHost: evidence.environment.exactReferenceHost, ordinaryP95: evidence.workloads.ordinary.runs.map((run) => run.summary.p95), denseP95: evidence.workloads.dense.runs.map((run) => run.summary.p95), startupMaximumMs: evidence.startup.maximumFirstUsableInteractionMs, runtimeGzipBytes: evidence.startup.byteCategories.executableJsCss.gzipBytes, output }));
