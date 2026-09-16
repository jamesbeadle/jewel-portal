// Renders the works view's play headlessly, frame by frame at a fixed clock, and stitches the
// frames into an mp4 with ffmpeg — the higher-quality route beside the page's Record button.
//
//   node tools/works-model/render-frames.mjs <definition.json> <out.mp4> [--fps 30] [--width 1920] [--height 1200]
//
// Needs Playwright (npm i playwright) with a Chromium it can launch, and ffmpeg on the PATH.
// The definition is the estimate's stored JSON (get_lead → estimates[].houseModel.definition).
import { chromium } from "playwright";
import { execFileSync } from "child_process";
import http from "http";
import fs from "fs";
import path from "path";
import os from "os";

const [definitionPath, outputPath, ...flags] = process.argv.slice(2);
if (!definitionPath || !outputPath) { console.error("usage: render-frames.mjs <definition.json> <out.mp4> [--fps 30] [--width 1920] [--height 1200]"); process.exit(2); }
const flag = (name, fallback) => { const at = flags.indexOf(`--${name}`); return at >= 0 ? Number(flags[at + 1]) : fallback; };
const fps = flag("fps", 30), width = flag("width", 1920), height = flag("height", 1200);

const wwwroot = path.resolve(new URL("../../jpms/wwwroot", import.meta.url).pathname);
const types = { ".js": "text/javascript", ".json": "application/json", ".css": "text/css" };
const pageHtml = `<!doctype html><html><head><script type="importmap">{ "imports": { "three": "/js/vendor/three/three.module.min.js" } }</script>
<style>body{margin:0}#host{width:${width}px;height:${height}px}</style></head><body><div id="host"></div><script src="/js/works-model.js"></script></body></html>`;
const server = http.createServer((request, response) => {
    const url = new URL(request.url, "http://localhost");
    if (url.pathname === "/") { response.setHeader("content-type", "text/html"); return response.end(pageHtml); }
    const file = path.join(wwwroot, url.pathname);
    if (!fs.existsSync(file)) { response.statusCode = 404; return response.end(); }
    response.setHeader("content-type", types[path.extname(file)] ?? "application/octet-stream");
    fs.createReadStream(file).pipe(response);
});
await new Promise(resolve => server.listen(0, resolve));
const port = server.address().port;

const framesFolder = fs.mkdtempSync(path.join(os.tmpdir(), "works-frames-"));
const browser = await chromium.launch({ args: ["--use-gl=angle", "--use-angle=swiftshader", "--ignore-gpu-blocklist"] });
const page = await browser.newPage({ viewport: { width, height } });
await page.goto(`http://localhost:${port}/`);
const json = fs.readFileSync(definitionPath, "utf8");
await page.evaluate(async json => {
    const ref = { invokeMethodAsync: async () => {} };
    await window.jpmsWorksModel.mount(document.getElementById("host"), json, ref);
}, json);

const millisecondsPerFrame = 1000 / fps;
let frame = 0;
for (let playing = true; playing; frame++) {
    playing = await page.evaluate(t => window.jpmsWorksModel.renderAt(document.getElementById("host"), t), frame * millisecondsPerFrame);
    await page.screenshot({ path: path.join(framesFolder, `frame-${String(frame).padStart(5, "0")}.png`) });
}
await browser.close();
server.close();

execFileSync("ffmpeg", ["-y", "-framerate", String(fps), "-i", path.join(framesFolder, "frame-%05d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", outputPath], { stdio: "inherit" });
fs.rmSync(framesFolder, { recursive: true, force: true });
console.log(`${frame} frames → ${outputPath}`);
