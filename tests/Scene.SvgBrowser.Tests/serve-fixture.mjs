import { createServer } from "node:http";
import { readFileSync, realpathSync, statSync } from "node:fs";
import { extname, resolve, sep } from "node:path";

const argument = (name) => process.argv[process.argv.indexOf(name) + 1];
if (!["--dist", "--host", "--port"].every((name) => process.argv.includes(name))) throw new Error("Usage: serve-fixture.mjs --dist IMMUTABLE_DIST --host SELECTED_HOST --port SELECTED_UNUSED_PORT");
const root = realpathSync(argument("--dist"));
if (!statSync(resolve(root, "artifact-manifest.json")).isFile()) throw new Error("Only a packaged fixture directory can be served");
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css", ".json": "application/json", ".woff2": "font/woff2", ".svg": "image/svg+xml" };
const server = createServer((request, response) => {
  try {
    if (!["GET", "HEAD"].includes(request.method)) { response.writeHead(405); response.end(); return; }
    const pathname = decodeURIComponent(new URL(request.url, "http://fixture.invalid").pathname);
    const path = realpathSync(resolve(root, `.${pathname === "/" ? "/suite.html" : pathname}`));
    if (!path.startsWith(`${root}${sep}`) || !statSync(path).isFile()) throw new Error("Invalid path");
    response.writeHead(200, { "content-type": types[extname(path)] ?? "application/octet-stream", "cache-control": "no-store" });
    response.end(request.method === "HEAD" ? undefined : readFileSync(path));
  } catch { response.writeHead(404); response.end(); }
});
server.listen(Number(argument("--port")), argument("--host"), () => console.log(JSON.stringify({ url: `http://${argument("--host")}:${argument("--port")}/suite.html`, pid: process.pid, staticRoot: root })));
for (const signal of ["SIGTERM", "SIGINT"]) process.on(signal, () => server.close());
