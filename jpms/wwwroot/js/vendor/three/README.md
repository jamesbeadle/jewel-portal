# three.js, vendored

`three@0.186.0` (MIT — https://github.com/mrdoob/three.js/blob/dev/LICENSE), fetched from npm on
2026-09-15 for the 3D model on a lead's page (`js/house-model.js`). Self-hosted rather than a CDN
so the portal has no runtime dependency on a third-party host for a client-facing page.

- `three.module.min.js` — `build/three.module.js` bundled with its `three.core.js` half and
  minified into one ES module: `npx esbuild node_modules/three/build/three.module.js --bundle
  --minify --format=esm --legal-comments=inline --outfile=three.module.min.js` (esbuild 0.25.4).
- `OrbitControls.js` — `examples/jsm/controls/OrbitControls.js`, untouched.
- `RoomEnvironment.js` — `examples/jsm/environments/RoomEnvironment.js`, untouched.

Both example modules import the bare specifier `three`; the import map in `wwwroot/index.html`
resolves it to `three.module.min.js`. To upgrade: bump the version, re-run the esbuild line, copy
the two example files over, and update this note.
