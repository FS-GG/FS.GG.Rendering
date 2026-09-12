import { createServer } from "node:http";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { firefox } from "playwright-core";

const fixture = dirname(fileURLToPath(import.meta.url));
const dist = resolve(fixture, "dist");
const outputIndex = process.argv.indexOf("--out");
if (outputIndex < 0) throw new Error("--out is required");
const output = resolve(process.argv[outputIndex + 1]);

function mime(path) {
  if (extname(path) === ".html") return "text/html; charset=utf-8";
  if (extname(path) === ".js") return "text/javascript; charset=utf-8";
  return "application/octet-stream";
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
const browser = await firefox.launch({ headless: false });
const page = await browser.newPage({ viewport: { width: 1024, height: 720 } });
const address = server.address();
const waitForOrca = () => page.waitForTimeout(1200);
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined);

  const root = page.locator("[data-scene-root-id='svg-foundation-root']");
  await root.focus();
  await waitForOrca();
  // A fresh document places Orca back at its ordinary web-document boundary,
  // so the equivalent native controls can be observed independently of the
  // application's focus-mode transition.
  await page.reload({ waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgFoundation !== undefined);
  await waitForOrca();

  const alpha = page.locator("[data-scene-control-id='alpha']");
  await alpha.focus();
  await waitForOrca();
  await page.keyboard.press("Space");
  await waitForOrca();
  const afterAlphaControl = await page.evaluate(() => window.svgFoundation.state());

  const beta = page.locator("[data-scene-control-id='beta']");
  await beta.focus();
  await waitForOrca();
  await page.keyboard.press("Space");
  await waitForOrca();
  const afterHtmlControl = await page.evaluate(() => window.svgFoundation.state());

  await root.focus();
  await waitForOrca();
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("Enter");
  await waitForOrca();
  const afterSvgKeyboard = await page.evaluate(() => window.svgFoundation.state());

  const nonInteractive = page.locator("[data-scene-object-id='label']");
  const decorationFocusable = await nonInteractive.evaluate((node) => node.tabIndex >= 0);

  await page.goto(`http://127.0.0.1:${address.port}/studio.html`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgStudioFixture !== undefined);
  const rectangle = page.getByRole("button", { name: "Rectangle", exact: true });
  await rectangle.focus();
  await waitForOrca();
  await rectangle.click();
  await waitForOrca();
  const studioAfterCreate = await page.evaluate(() => window.svgStudioFixture.observation());
  const translate = page.getByRole("textbox", { name: "Translate X", exact: true });
  await translate.focus();
  await waitForOrca();
  await translate.fill("not-a-number");
  const apply = page.getByRole("button", { name: "Apply translation", exact: true });
  await apply.focus();
  await waitForOrca();
  await apply.click();
  await waitForOrca();
  const validationFeedback = await page.locator("[role='status']").textContent();
  const selectionFeedback = await page.getByLabel("Current selection").textContent();
  writeFileSync(output, JSON.stringify({
    alphaHtmlControl: { selected: afterAlphaControl.selected, focused: afterAlphaControl.focused },
    betaHtmlControl: { selected: afterHtmlControl.selected, focused: afterHtmlControl.focused },
    svgKeyboard: { selected: afterSvgKeyboard.selected, focused: afterSvgKeyboard.focused },
    studio: {
      revision: studioAfterCreate.Revision,
      selectionCount: studioAfterCreate.SelectionCount,
      selectionFeedback,
      validationFeedback,
    },
    negativeControl: { nonInteractiveDecorationFocusable: decorationFocusable },
  }, null, 2) + "\n");
} finally {
  await browser.close();
  server.close();
}
