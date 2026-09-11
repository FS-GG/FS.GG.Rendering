import { createHash } from "node:crypto";
import { createServer } from "node:http";
import { cpus, platform, release } from "node:os";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { gzipSync } from "node:zlib";
import { chromium } from "playwright-core";

const fixture = dirname(fileURLToPath(import.meta.url));
const dist = resolve(fixture, "dist");
const outputIndex = process.argv.indexOf("--out");
if (outputIndex < 0) throw new Error("--out is required");
const output = resolve(process.argv[outputIndex + 1]);
const sourceDigestIndex = process.argv.indexOf("--source-digest");
const sourceDigest = sourceDigestIndex < 0 ? "unavailable" : process.argv[sourceDigestIndex + 1];
const packageDigestIndex = process.argv.indexOf("--package-digest");
const packageDigest = packageDigestIndex < 0 ? "unavailable" : process.argv[packageDigestIndex + 1];

function mime(path) {
  if (extname(path) === ".html") return "text/html; charset=utf-8";
  if (extname(path) === ".js") return "text/javascript; charset=utf-8";
  return "application/octet-stream";
}
function files(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = resolve(directory, name);
    return statSync(path).isDirectory() ? files(path) : [path];
  });
}
function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.floor(sorted.length / 2)];
}
const server = createServer((request, response) => {
  const target = request.url === "/" ? resolve(dist, "index.html") : resolve(dist, request.url.replace(/^\//, ""));
  if (!target.startsWith(`${dist}/`) || !existsSync(target) || statSync(target).isDirectory()) {
    response.writeHead(404).end(); return;
  }
  response.writeHead(200, { "content-type": mime(target), "cache-control": "no-store" });
  response.end(readFileSync(target));
});
await new Promise((done) => server.listen(0, "127.0.0.1", done));
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 800, height: 600 }, deviceScaleFactor: 1 });
const page = await context.newPage();
const consoleErrors = [];
page.on("console", (message) => { if (message.type() === "error") consoleErrors.push(message.text()); });
page.on("pageerror", (error) => consoleErrors.push(error.stack || error.message));
await page.addInitScript(() => {
  window.__svgListenerBalance = 0;
  const add = EventTarget.prototype.addEventListener;
  const remove = EventTarget.prototype.removeEventListener;
  EventTarget.prototype.addEventListener = function(type, listener, options) {
    if (this instanceof SVGElement && this.hasAttribute("data-scene-root-id")) window.__svgListenerBalance += 1;
    return add.call(this, type, listener, options);
  };
  EventTarget.prototype.removeEventListener = function(type, listener, options) {
    if (this instanceof SVGElement && this.hasAttribute("data-scene-root-id")) window.__svgListenerBalance -= 1;
    return remove.call(this, type, listener, options);
  };
});
const address = server.address();
const mountSamples = [];
const inputPaintSamples = [];
let observations;
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined);
  const root = page.locator("[data-scene-root-id='svg-foundation-root']");
  const box = await root.boundingBox();
  if (!box) throw new Error("SVG root has no rendered bounds");

  await page.mouse.click(box.x + 60, box.y + 50);
  let state = await page.evaluate(() => window.svgFoundation.state());
  if (state.selected !== "alpha" || state.focused !== "alpha") throw new Error(`inverse pointer pick failed: ${JSON.stringify(state)}`);

  await page.evaluate(() => window.svgFoundation.mount());
  await root.focus();
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("Enter");
  state = await page.evaluate(() => window.svgFoundation.state());
  if (state.selected !== "alpha" || state.focused !== "alpha") throw new Error(`keyboard selection disagreed with pointer selection: ${JSON.stringify(state)}`);
  const accessibility = await page.evaluate(() => {
    const svg = document.querySelector("[data-scene-root-id]");
    const alpha = document.querySelector("[data-scene-object-id='alpha']");
    return {
      rootRole: svg?.getAttribute("role"), rootLabel: svg?.getAttribute("aria-label"),
      objectRole: alpha?.getAttribute("role"), objectLabel: alpha?.getAttribute("aria-label"),
      selected: alpha?.getAttribute("aria-selected"),
    };
  });
  if (accessibility.rootRole !== "application" || accessibility.rootLabel !== "SVG foundation scene" || accessibility.objectRole !== "button" || accessibility.objectLabel !== "Alpha unit" || accessibility.selected !== "true") {
    throw new Error(`accessible route failed: ${JSON.stringify(accessibility)}`);
  }

  const camera = await page.evaluate(() => {
    const before = window.svgFoundation.scenePoint(60, 50);
    const zoom = window.svgFoundation.zoomAt(60, 50, 4);
    const after = window.svgFoundation.scenePoint(60, 50);
    const anchoredHit = window.svgFoundation.hit(60, 50);
    const pan = window.svgFoundation.panBy(10, -5);
    const movedHit = window.svgFoundation.hit(70, 45);
    return { before, after, anchoredHit, movedHit, zoom, pan };
  });
  if (camera.before.x !== 20 || camera.before.y !== 20 || camera.after.x !== 20 || camera.after.y !== 20 || camera.anchoredHit !== "alpha" || camera.movedHit !== "alpha" || camera.zoom.error || camera.pan.error) {
    throw new Error(`camera anchor/inverse contract failed: ${JSON.stringify(camera)}`);
  }

  await page.evaluate(() => {
    window.svgFoundation.mount();
    window.__identity = {
      root: document.querySelector("[data-scene-root-id]"),
      units: document.querySelector("[data-scene-layer-id='units']"),
      overlay: document.querySelector("[data-scene-layer-id='overlay']"),
    };
  });
  await page.mouse.click(box.x + 60, box.y + 50);
  const revision = await page.evaluate(() => {
    const accepted = window.svgFoundation.replaceCurrent();
    const retained = window.__identity.root === document.querySelector("[data-scene-root-id]")
      && window.__identity.units === document.querySelector("[data-scene-layer-id='units']")
      && window.__identity.overlay === document.querySelector("[data-scene-layer-id='overlay']");
    const stale = window.svgFoundation.replaceStale();
    return { accepted, stale, retained, domRevision: document.querySelector("[data-scene-root-id]")?.dataset.sceneRevision };
  });
  if (revision.accepted.error || revision.accepted.state.revision !== 2 || revision.accepted.state.selected !== "alpha" || !revision.retained || revision.stale.error !== "non-increasing-revision" || revision.domRevision !== "2") {
    throw new Error(`retained revision/stale refusal failed: ${JSON.stringify(revision)}`);
  }

  const lifecycle = await page.evaluate(() => {
    window.svgFoundation.dispose();
    const afterFirstDispose = { balance: window.__svgListenerBalance, roots: document.querySelectorAll("[data-scene-root-id]").length };
    const cycles = [];
    for (let index = 0; index < 12; index += 1) {
      window.svgFoundation.mount();
      const mounted = { balance: window.__svgListenerBalance, observe: window.svgFoundation.observe() };
      window.svgFoundation.dispose();
      cycles.push({ mounted, balanceAfter: window.__svgListenerBalance, rootsAfter: document.querySelectorAll("[data-scene-root-id]").length });
    }
    window.svgFoundation.mount();
    return { afterFirstDispose, cycles, final: window.svgFoundation.observe(), finalBalance: window.__svgListenerBalance };
  });
  if (lifecycle.afterFirstDispose.balance !== 0 || lifecycle.afterFirstDispose.roots !== 0 || lifecycle.finalBalance !== 6 || lifecycle.final.listeners !== 6 || lifecycle.final.frames !== 0 || lifecycle.cycles.some((cycle) => cycle.mounted.balance !== 6 || cycle.mounted.observe.listeners !== 6 || cycle.mounted.observe.frames !== 0 || cycle.balanceAfter !== 0 || cycle.rootsAfter !== 0)) {
    throw new Error(`mount/dispose leaked owned resources: ${JSON.stringify(lifecycle)}`);
  }

  for (let index = 0; index < 20; index += 1) {
    const mounted = await page.evaluate(() => {
      window.svgFoundation.dispose();
      const start = performance.now();
      window.svgFoundation.mount();
      return performance.now() - start;
    });
    mountSamples.push(mounted);
    const currentBox = await root.boundingBox();
    const started = await page.evaluate(() => performance.now());
    await page.mouse.click(currentBox.x + 60, currentBox.y + 50);
    await page.evaluate(() => new Promise((done) => requestAnimationFrame(() => done())));
    inputPaintSamples.push((await page.evaluate(() => performance.now())) - started);
  }
  observations = await page.evaluate(() => window.svgFoundation.observe());
} finally {
  await browser.close();
  await new Promise((done) => server.close(done));
}
if (consoleErrors.length) throw new Error(`browser console errors:\n${consoleErrors.join("\n")}`);
const productionFiles = files(dist);
const rawBytes = productionFiles.reduce((sum, path) => sum + statSync(path).size, 0);
const gzipBytes = productionFiles.reduce((sum, path) => sum + gzipSync(readFileSync(path)).length, 0);
const round = (value) => Number(value.toFixed(3));
const evidence = {
  schema: "fsgg.svg-foundation.browser-observation/v1",
  result: "pass",
  capturedAtUtc: new Date().toISOString(),
  candidate: { sourceSha256: sourceDigest, packageSetSha256: packageDigest },
  environment: {
    browser: await chromium.launch({ headless: true }).then(async (probe) => { const version = probe.version(); await probe.close(); return version; }),
    node: process.version, os: `${platform()} ${release()}`, logicalCpuCount: cpus().length,
    viewport: { width: 800, height: 600, deviceScaleFactor: 1 }, headless: true,
  },
  sceneCost: observations,
  startup: { productionFiles: productionFiles.length, rawBytes, gzipBytes, coldNetworkCache: false },
  timing: {
    sampleCount: mountSamples.length,
    mountMilliseconds: { median: round(median(mountSamples)), maximum: round(Math.max(...mountSamples)) },
    inputToNextAnimationFrameMilliseconds: { median: round(median(inputPaintSamples)), maximum: round(Math.max(...inputPaintSamples)) },
  },
  functional: {
    pointerKeyboardSameObject: true, accessibleDomRoute: true, anchoredZoomAndInversePicking: true,
    retainedRootAndLayersAcrossRevision: true, staleRevisionRefused: true, mountDisposeCycles: 12,
    finalOwnedListenerCount: observations.listeners, scheduledFrameCount: observations.frames,
  },
  unavailable: [
    "Input timing ends at the next animation-frame callback; no compositor presentation timestamp was captured.",
    "Heap and detached-node measurements were not captured in this focused foundation fixture.",
    "Firefox, WebKit, touch hardware, mobile GPUs and assistive-technology announcements were not observed."
  ],
  claims: { thresholdEstablished: false, completeM9Qualification: false, packagePublished: false },
};
writeFileSync(output, `${JSON.stringify(evidence, null, 2)}\n`);
console.log(JSON.stringify({ result: evidence.result, browser: evidence.environment.browser, sceneCost: observations, rawBytes, gzipBytes, mountMedianMs: evidence.timing.mountMilliseconds.median, inputMedianMs: evidence.timing.inputToNextAnimationFrameMilliseconds.median, output }));
