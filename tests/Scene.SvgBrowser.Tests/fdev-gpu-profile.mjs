// fdev virtio-gpu functional-GPU and early-baseline profile observer (FS-GG/.github#4645).
// Labelled "not B1-B8 evidence": it launches one named browser profile against a served sealed fixture,
// records GPU diagnostics, optionally runs the public-circle benchmark records and a short trace, and
// writes raw numbers. It computes no budget verdict and holds no licensed geometry.
import { chromium } from "playwright-core";
import { readFileSync, writeFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { validateWorkload, workloadRecords, median, percentile } from "./hosted-benchmark-analysis.mjs";

const usage = "Usage: fdev-gpu-profile.mjs --profile software|gpu-angle-vulkan|gpu-angle-gl-egl|default-flags --executable PATH --base-url URL --out RESULT [--benchmark] [--trace] [--record-filter SUBSTRING]";
const argument = (name, fallback) => { const i = process.argv.indexOf(name); if (i < 0) { if (fallback !== undefined) return fallback; throw new Error(`${name} required. ${usage}`); } return process.argv[i + 1]; };
const has = (name) => process.argv.includes(name);
export const PROFILES = {
  software: ["--disable-gpu"],
  "gpu-angle-vulkan": ["--use-angle=vulkan", "--enable-features=Vulkan", "--ignore-gpu-blocklist", "--enable-gpu-rasterization"],
  "gpu-angle-gl-egl": ["--use-angle=gl-egl", "--ignore-gpu-blocklist", "--enable-gpu-rasterization"],
  "default-flags": []
};
const here = dirname(fileURLToPath(import.meta.url));
const sha256 = (bytes) => `sha256:${createHash("sha256").update(bytes).digest("hex")}`;
const profile = argument("--profile");
if (!(profile in PROFILES)) throw new Error(usage);
const executablePath = argument("--executable"), out = argument("--out"), base = new URL(argument("--base-url"));
if (!["127.0.0.1", "localhost"].includes(base.hostname)) throw new Error("Loopback fixture only");
const recordFilter = argument("--record-filter", "");
const args = PROFILES[profile];
const workloadBytes = readFileSync(resolve(here, "hosted-benchmark-workload.json"));
const workload = validateWorkload(JSON.parse(workloadBytes));
const pageSource = readFileSync(resolve(here, "hosted-benchmark-page.js"), "utf8");
const label = "fdev virtio-gpu functional-GPU and early-baseline profile; not B1-B8 evidence; not bare-metal Home; r1.3 stays Unknown";
const result = { schema: "fsgg.fdev-gpu-profile/v1", label, profile, args, startedAtUtc: new Date().toISOString(), publicFixture: true,
  harness: { driverSha256: sha256(readFileSync(fileURLToPath(import.meta.url))), pageSha256: sha256(pageSource), workloadSha256: sha256(workloadBytes) },
  claims: { budgetsEvaluated: false, b1ToB8Evidence: false, bareMetalHome: false, licensedGlyphMeasured: false } };
const quantiles = (values) => values.length ? { n: values.length, p50: median(values), p95: percentile(values, 0.95), p99: percentile(values, 0.99), max: Math.max(...values) } : null;
const featureText = (text) => {
  // Keep the status table and driver block; drop the extension list, the command line and long bug lists.
  const lines = text.split("\n").map((line) => line.trim()).filter(Boolean);
  const keep = [];
  for (let i = 0; i < lines.length; i++) {
    if (/^(Canvas|Direct Rendering Display Compositor|Compositing|Multiple Raster Threads|OpenGL|Rasterization|Raw Draw|Skia Graphite|TreesInViz|Video Decode|Video Encode|Vulkan|WebGL|WebGPU|WebGPU interop|WebNN):$/.test(lines[i])) keep.push(`${lines[i]} ${lines[i + 1] ?? ""}`);
    if (/^(Skia Backend|In-process GPU|Sandboxed|Passthrough Command Decoder|Display type|GL implementation parts|GL_VENDOR|GL_RENDERER|GL_VERSION|GPU0|GPU1|2D graphics backend|Chrome version|ANGLE commit id)$/.test(lines[i])) keep.push(`${lines[i]}: ${lines[i + 2] ?? ""}`);
  }
  return keep;
};
const browser = await chromium.launch({ headless: true, args, executablePath, timeout: 30000 });
const deadline = setTimeout(() => { console.error("fdev-gpu-profile deadline exceeded"); browser.close().finally(() => process.exit(124)); }, 600000);
try {
  result.browser = { version: browser.version(), executablePath, executableSha256: sha256(readFileSync(executablePath)) };
  // Diagnostics: chrome://gpu text, CDP SystemInfo and the page-visible WebGL adapter.
  const diagPage = await browser.newPage();
  await diagPage.goto("chrome://gpu");
  await diagPage.waitForTimeout(3000);
  const text = await diagPage.evaluate(() => { const walk = (node, out = []) => { if (node.shadowRoot) walk(node.shadowRoot, out); for (const child of node.childNodes) { if (child.nodeType === 3) out.push(child.textContent.trim()); else walk(child, out); } return out; }; return walk(document.body).filter(Boolean).join("\n"); });
  result.chromeGpu = { featureLines: featureText(text), fullTextSha256: sha256(Buffer.from(text)), fullText: has("--keep-full-text") ? text : undefined };
  const cdp = await browser.newBrowserCDPSession();
  const info = await cdp.send("SystemInfo.getInfo");
  result.systemInfo = { featureStatus: info.gpu.featureStatus, devices: info.gpu.devices.map((d) => ({ vendorId: d.vendorId.toString(16), deviceId: d.deviceId.toString(16), vendorString: d.vendorString, deviceString: d.deviceString, driverVendor: d.driverVendor, driverVersion: d.driverVersion })),
    auxAttributes: Object.fromEntries(Object.entries(info.gpu.auxAttributes ?? {}).filter(([key]) => /glRenderer|glVendor|glVersion|glImplementationParts|displayType|skiaBackend|inProcessGpu|passthrough|sandboxed|initializationTime/i.test(key))) };
  const processes = await cdp.send("SystemInfo.getProcessInfo");
  result.processes = processes.processInfo.map((p) => ({ type: p.type, cpuTime: p.cpuTime }));
  await diagPage.close();
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 });
  const page = await context.newPage();
  await page.goto(new URL("index.html", base).href);
  await page.waitForFunction(() => typeof window.svgFoundation?.replaceHostedBenchmarkDocument === "function", undefined, { timeout: 30000 });
  result.pageGl = await page.evaluate(() => { const canvas = document.createElement("canvas"); const gl = canvas.getContext("webgl2") ?? canvas.getContext("webgl"); if (!gl) return { available: false };
    const ext = gl.getExtension("WEBGL_debug_renderer_info");
    return { available: true, vendor: ext ? gl.getParameter(ext.UNMASKED_VENDOR_WEBGL) : null, renderer: ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : null, version: gl.getParameter(gl.VERSION) }; });
  result.environment = { userAgent: await page.evaluate(() => navigator.userAgent), hardwareConcurrency: await page.evaluate(() => navigator.hardwareConcurrency),
    loadAverageAtStart: readFileSync("/proc/loadavg", "utf8").trim(), kernel: readFileSync("/proc/sys/kernel/osrelease", "utf8").trim() };
  await page.addScriptTag({ type: "module", content: pageSource });
  await page.waitForFunction(() => window.fsggHostedBenchmark, undefined, { timeout: 10000 });
  const run = (spec) => page.evaluate((value) => window.fsggHostedBenchmark.runBenchmarkRecord(window, value), { ...spec, warmupTicks: workload.method.warmupTicks, measuredTicks: workload.method.measuredTicks });
  const records = workloadRecords(workload).filter((record) => JSON.stringify(record).includes(recordFilter));
  if (has("--benchmark")) {
    result.records = [];
    for (const record of records) {
      const value = await run(record);
      result.records.push({ ...record, updateMs: quantiles(value.updateMs), frameIntervalMs: quantiles(value.frameIntervalMs), oracleTicks: value.oracleTicks, nodes: value.nodes,
        framesOver25ms: value.frameIntervalMs.filter((x) => x > 25).length, rawUpdateMs: value.updateMs });
    }
  }
  if (has("--trace")) {
    // Short separate trace window so instrumentation does not enter the timed records above.
    const spec = records.find((record) => record.count === 250 && record.motion === "sparse-10") ?? records[0];
    await browser.startTracing(page, { categories: ["cc", "gpu", "viz", "disabled-by-default-cc.debug"], screenshots: false });
    await run(spec);
    const trace = JSON.parse((await browser.stopTracing()).toString());
    const processNames = {}, threadNames = {};
    for (const event of trace.traceEvents) { if (event.ph === "M" && event.name === "process_name") processNames[event.pid] = event.args.name; if (event.ph === "M" && event.name === "thread_name") threadNames[`${event.pid}:${event.tid}`] = event.args.name; }
    const counts = {};
    for (const event of trace.traceEvents) if (/RasterDecoderImpl::DoRasterCHROMIUM$|GpuRasterBuffer::Playback|RasterBufferImpl::Playback|ZeroCopyRasterBuffer|OneCopyRasterBuffer|SoftwareImage|ImageDecodeTask|RasterTask$/.test(event.name ?? "") && event.ph !== "M") {
      const key = `${processNames[event.pid] ?? event.pid}/${event.name}`; counts[key] = (counts[key] ?? 0) + 1; }
    result.trace = { record: spec, categories: ["cc", "gpu", "viz", "disabled-by-default-cc.debug"], eventCount: trace.traceEvents.length, processes: [...new Set(Object.values(processNames))], rasterEventCounts: counts,
      gpuProcessRasterDecoderEvents: Object.entries(counts).filter(([key]) => key.startsWith("GPU Process/") && key.includes("DoRasterCHROMIUM")).reduce((sum, [, n]) => sum + n, 0) };
  }
  result.outcome = "completed";
} catch (error) {
  result.outcome = "failed"; result.error = error.message; process.exitCode = 1;
} finally {
  result.completedAtUtc = new Date().toISOString();
  writeFileSync(out, `${JSON.stringify(result, null, 1)}\n`);
  await browser.close().catch(() => {});
  clearTimeout(deadline);
  console.log(JSON.stringify({ profile, outcome: result.outcome, records: result.records?.length ?? 0, error: result.error ?? null }));
}
