import { chromium } from "playwright-core";
import { readFileSync, writeFileSync, existsSync, statSync } from "node:fs";
import { createHash } from "node:crypto";
import { fileURLToPath } from "node:url";
import { validateSoldierResult } from "./suite-contract.js";

const argument = (name) => process.argv[process.argv.indexOf(name) + 1];
if (!process.argv.includes("--base-url") || !process.argv.includes("--out")) throw new Error("Usage: drive-suite.mjs --base-url URL --out RESULT [--exercise-resume] [--mutant NAME]");
const url = new URL("suite.html", argument("--base-url"));
const profile = process.argv.includes("--profile") ? argument("--profile") : "controls";
if (!["controls", "soldier-functional"].includes(profile)) throw new Error("Unknown suite profile");
if (profile !== "controls") url.searchParams.set("profile", profile);
const soldierMutants = ["missing-mount", "wrong-entry", "source-hash", "asset-hash", "bundle-hash", "workload-hash", "missing-case", "stale-checkpoint", "stale-epoch-accepted", "incomplete-export", "owner-leak", "artifact-before-download", "artifact-after-download"];
const mutant = process.argv.includes("--mutant") ? argument("--mutant") : null;
if (mutant && !(profile === "soldier-functional" ? soldierMutants : ["unnecessary-rebuild", "listener-leak", "excessive-document-acceptance"]).includes(mutant)) throw new Error("Unknown mutant");
// This driver is explicitly the retained container software profile. A supplied URL
// does not turn its local machine into a host-GPU measurement route.
const deadlineMilliseconds = process.argv.includes("--deadline-ms") ? Number(argument("--deadline-ms")) : 180000;
if (!Number.isFinite(deadlineMilliseconds) || deadlineMilliseconds < 1000 || deadlineMilliseconds > 750000) throw new Error("Invalid driver deadline (1000–750000 milliseconds)");
const executablePath = process.argv.includes("--executable") ? argument("--executable") : undefined;
const sha256 = (value) => `sha256:${createHash("sha256").update(value).digest("hex")}`;
const driverSha256 = sha256(readFileSync(fileURLToPath(import.meta.url)));
const executableSha256 = executablePath ? sha256(readFileSync(executablePath)) : null;
const browserPromise = chromium.launch({ headless: true, args: ["--disable-gpu"], executablePath, timeout: Math.min(30000, deadlineMilliseconds) });
let shutdownPromise;
const closeBrowser = () => shutdownPromise ??= browserPromise.then((browser) => browser.close(), () => {});
const deadline = setTimeout(() => {
  console.error(`Driver deadline exceeded: ${deadlineMilliseconds}ms`);
  closeBrowser().then(() => process.exit(124), (error) => { console.error(error); process.exit(125); });
}, deadlineMilliseconds);
for (const signal of ["SIGTERM", "SIGINT"]) process.once(signal, () => {
  closeBrowser().then(() => process.exit(signal === "SIGTERM" ? 143 : 130), (error) => { console.error(error); process.exit(125); });
});
try {
  const browser = await browserPromise;
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1, acceptDownloads: true });
  if (mutant && profile === "controls") await context.addInitScript((name) => {
    if (window === window.top) return;
    const poll = setInterval(() => {
      const f = window.svgFoundation;
      if (!f) return;
      clearInterval(poll);
      if (name === "unnecessary-rebuild") { const original = f.performanceSelect; f.performanceSelect = (i) => { f.performanceMount("ordinary"); return original(i); }; }
      if (name === "listener-leak") { const original = f.animationDispose; f.animationDispose = () => ({ ...original(), listeners: 1 }); }
      if (name === "excessive-document-acceptance") f.performanceExcessiveDocument = () => ({ rootRetained: false, error: null });
    }, 0);
  }, mutant);
  const page = await context.newPage();
  const worldExports = [];
  page.on("download", (download) => {
    if (profile === "soldier-functional" && download.suggestedFilename() === "soldier-world.svg") worldExports.push((async () => {
      const path = argument("--out") + ".world.svg";
      await download.saveAs(path);
      if (statSync(path).size > 8388608) throw new Error("Full-world export exceeds bounded evidence size");
      return { sha256: sha256(readFileSync(path)), bytes: statSync(path).size };
    })().catch((error) => ({ error: error.message })));
  });
  let inputManifest;
  if (profile === "soldier-functional") {
    if (!process.argv.includes("--manifest-sha256")) throw new Error("Exact staged manifest digest required");
    const response = await context.request.get(new URL("artifact-manifest.json", url).href);
    const raw = await response.body();
    if (!response.ok() || sha256(raw) !== argument("--manifest-sha256")) throw new Error("Staged manifest input identity mismatch");
    inputManifest = JSON.parse(raw.toString());
    if (mutant === "stale-checkpoint") await context.addInitScript(({ manifest, digest }) => {
      if (window === window.top) localStorage.setItem(`svg-suite:${manifest.artifactId}`, JSON.stringify({ artifact: { artifactId: "foreign" }, manifestSha256: digest, cases: [] }));
    }, { manifest: inputManifest, digest: argument("--manifest-sha256") });
    if (["source-hash", "asset-hash", "bundle-hash", "workload-hash", "wrong-entry"].includes(mutant)) {
      await page.route("**/artifact-manifest.json", async (route) => {
        const value = structuredClone(inputManifest);
        if (mutant === "source-hash") value.sourceSha256 = "sha256:" + "0".repeat(64);
        if (mutant === "asset-hash") value.workload.input.assetSha256 = "sha256:" + "0".repeat(64);
        if (mutant === "bundle-hash") value.files.find((file) => file.path.startsWith("soldier-player/")).sha256 = "sha256:" + "0".repeat(64);
        if (mutant === "workload-hash") value.workloadSha256 = "sha256:" + "0".repeat(64);
        if (mutant === "wrong-entry") value.workload.input.actualEntry = "index.html";
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(value) });
      });
    }
    if (["missing-mount", "stale-epoch-accepted", "incomplete-export", "owner-leak"].includes(mutant)) await context.addInitScript((fault) => {
      if (window === window.top) return;
      // Explicit adversarial DOM observation mutations; no product model/gateway replacement.
      if (["stale-epoch-accepted", "incomplete-export"].includes(fault)) document.addEventListener("click", (event) => {
        if (fault === "stale-epoch-accepted" && event.target.textContent === "Complete oldest soldier snapshot") queueMicrotask(() => {
          const output = document.getElementById("soldier-reference-state");
          const value = JSON.parse(output.textContent); value.revision = 999; output.textContent = JSON.stringify(value);
        });
        if (fault === "incomplete-export" && event.target.textContent === "Export full soldier world") queueMicrotask(() => {
          const output = document.getElementById("soldier-reference-export");
          output.textContent = output.textContent.replace(/<use\b[^>]*(?:\/>|>.*?<\/use>)/, "");
        });
      }, true);
      let done = false;
      new MutationObserver((records) => {
        if (fault === "missing-mount") document.getElementById("mount-soldier-reference")?.remove();
        if (fault === "owner-leak") for (const record of records) for (const node of record.removedNodes) {
          if (node.nodeType === 1 && node.id === "soldier-reference") node.setAttribute("data-adapter-listeners", "1");
        }
      }).observe(document, { childList: true, subtree: true, characterData: true });
    }, mutant);
  }
  await page.goto(url.href);
  await page.waitForFunction(() => !document.querySelector("#run").disabled || document.querySelector("#progress").textContent.includes("failed:"), undefined, { timeout: 30000 });
  const capabilityFailed = await page.locator("#run").isDisabled();
  if (!capabilityFailed) await page.locator("#run").click();
  let resumeObserved = false;
  if (!capabilityFailed && process.argv.includes("--exercise-resume")) {
    await page.waitForFunction(() => window.svgSuite.result?.cases.length >= 2, undefined, { timeout: 120000 });
    await page.reload();
    await page.locator("#resume").waitFor();
    await page.waitForFunction(() => !document.querySelector("#resume").disabled, undefined, { timeout: 30000 });
    const saved = await page.evaluate(() => window.svgSuite.result.cases.length);
    await page.locator("#resume").click(); resumeObserved = saved >= 2;
  }
  if (!capabilityFailed) await page.waitForFunction(() => window.svgSuite.result?.completedAtUtc && !window.svgSuite.running, undefined, { timeout: Math.min(750000, deadlineMilliseconds) });
  if (mutant === "artifact-before-download") await page.route("**/artifact-manifest.json", (route) => route.fulfill({ status: 200, body: "altered-before-download" }));
  const downloadEvent = page.waitForEvent("download", { predicate: (download) => download.suggestedFilename().startsWith("svg-controls-") });
  await page.locator("#download").click();
  const download = await downloadEvent;
  await download.saveAs(argument("--out"));
  const result = JSON.parse(readFileSync(argument("--out"), "utf8"));
  const worldExportEvidence = await Promise.all(worldExports);
  result.driver = { fullWorldExports: worldExportEvidence, profile: executablePath ? "container-software-diagnostic-explicit-browser" : "container-software-historical", historicalBaselineEquivalence: false, browser: browser.version(), executablePath: executablePath ?? "Playwright default", executableSha256, driverSha256, args: ["--disable-gpu"], baseUrl: url.href, resumeObserved, downloadObserved: true };
  writeFileSync(argument("--out"), `${JSON.stringify(result, null, 2)}\n`);
  if (profile === "soldier-functional") {
    if (mutant === "missing-case") { result.cases.pop(); result.outcome = "failed"; result.error = "downloaded case removed by explicit negative control"; }
    if (mutant === "artifact-after-download") { result.artifact.files[0].sha256 = "altered"; result.outcome = "failed"; result.error = "downloaded artifact identity altered by explicit negative control"; }
    writeFileSync(argument("--out"), `${JSON.stringify(result, null, 2)}\n`);
    try {
      if (result.manifestSha256 !== argument("--manifest-sha256") || result.artifact?.artifactId !== inputManifest.artifactId || JSON.stringify(result.artifact) !== JSON.stringify(inputManifest)) throw new Error("Downloaded staged input identity mismatch");
      validateSoldierResult(result);
      const full = result.cases.find((row) => row.kind === "soldier-churn").observations.details.fullExport;
      if (worldExportEvidence.length !== 1 || worldExportEvidence[0].sha256 !== full.sha256) throw new Error("Actual full-world download differs from observed export");
      if (mutant) throw new Error(`Soldier mutant survived: ${mutant}`);
      console.log(JSON.stringify({ outcome: result.outcome, cases: result.cases.length, resumeObserved, output: argument("--out") }));
    } catch (error) {
      result.outcome = "failed"; result.verificationError = error.message;
      writeFileSync(argument("--out"), `${JSON.stringify(result, null, 2)}\n`);
      throw error;
    }
  } else if (mutant) {
    const gate = { "unnecessary-rebuild": "unnecessary-rebuild gate", "listener-leak": "listener/resource-leak gate", "excessive-document-acceptance": "excessive-document gate" }[mutant];
    if (!result.cases.some((c) => c.error?.includes(gate))) throw new Error(`Mutant survived: ${mutant}`);
    console.log(JSON.stringify({ mutant, result: "killed", gate, output: argument("--out") }));
  } else {
    if (result.outcome !== "container-controls-completed" || result.integrity !== "verified-before-and-after") throw new Error(`Suite failed: ${JSON.stringify(result.cases.filter((c) => c.outcome === "failed"))} ${result.error ?? ""}`);
    console.log(JSON.stringify({ outcome: result.outcome, cases: result.cases.length, resumeObserved, output: argument("--out") }));
  }
} finally {
  clearTimeout(deadline);
  let cleanup = "unknown";
  try { await closeBrowser(); cleanup = "observed-closed"; }
  finally {
    if (existsSync(argument("--out"))) {
      const value = JSON.parse(readFileSync(argument("--out"), "utf8"));
      value.driver = { ...value.driver, browserCleanup: cleanup };
      writeFileSync(argument("--out"), `${JSON.stringify(value, null, 2)}\n`);
    }
  }
}
