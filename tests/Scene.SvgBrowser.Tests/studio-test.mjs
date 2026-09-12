import { createServer } from "node:http";
import { readFileSync, statSync, existsSync } from "node:fs";
import { resolve, dirname, extname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium, firefox, webkit } from "playwright-core";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "dist");
const family = process.argv[process.argv.indexOf("--browser") + 1];
const kind = {chromium,firefox,webkit}[family];
const server=createServer((request,response)=>{const path=resolve(root,(request.url==="/"?"studio.html":request.url.slice(1)));if(!path.startsWith(root)||!existsSync(path)||statSync(path).isDirectory()){response.writeHead(404).end();return;}const type=extname(path)===".js"?"text/javascript":extname(path)===".html"?"text/html":"application/octet-stream";response.writeHead(200,{"content-type":type});response.end(readFileSync(path));});
await new Promise(done=>server.listen(0,"127.0.0.1",done));
const browser=await kind.launch({headless:true});
try {
  const page=await browser.newPage();
  await page.goto(`http://127.0.0.1:${server.address().port}/studio.html`);
  await page.waitForFunction(()=>window.svgStudioFixture);
  const geometry={};
  for(const operation of ["union","intersection","difference","xor","curve"])
    geometry[operation]=await page.evaluate(operation=>window.svgStudioFixture.geometry(operation),operation);
  const evidence=await page.evaluate(()=>{
    const labels=[...document.querySelectorAll("button")].map(x=>x.getAttribute("aria-label"));
    const created=window.svgStudioFixture.addRectangle();
    const cancelled=window.svgStudioFixture.cancelGesture();
    const disposed=window.svgStudioFixture.dispose();
    const remounted=window.svgStudioFixture.mount();
    const disposedAgain=window.svgStudioFixture.dispose();
    const resource=window.svgStudioFixture.resourceRoundtrip();
    const geometryCancelled=window.svgStudioFixture.cancelGeometry();
    return {labels,created,cancelled,resource,geometryCancelled,disposed,remounted,disposedAgain,roots:document.querySelectorAll("[data-fsgg-document-id]").length};
  });
  await page.waitForFunction(()=>window.svgStudioFixture.fontStatus().ready||window.svgStudioFixture.fontStatus().diagnostic,{timeout:5000});
  const font=await page.evaluate(()=>window.svgStudioFixture.fontStatus());
  await page.evaluate(()=>window.svgStudioFixture.disposeFont());
  if(evidence.labels.join(",")!=="Rectangle,Ellipse,Polygon,Path,Undo,Redo"||evidence.created.Revision!==1||evidence.created.SelectionCount!==1||evidence.cancelled[0]!==evidence.cancelled[1]||!evidence.resource.embedded||evidence.resource.fonts!==1||evidence.resource.children!==1||!evidence.geometryCancelled||Object.values(geometry).some(value=>value.operations!==1||value.contours<1)||geometry.difference.contours<2||!font.ready||evidence.disposed!==0||evidence.disposedAgain!==0||evidence.roots!==0)throw new Error(JSON.stringify({evidence,font,geometry}));
  console.log(`svg-studio-browser: ${family}=passed repeated-mount-dispose=passed native-controls=passed cancellation=passed offline-font=passed`);
} finally { await browser.close(); await new Promise(done=>server.close(done)); }
