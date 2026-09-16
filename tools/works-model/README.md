# Works view — headless render

`render-frames.mjs` plays an estimate's works view (the phase-by-phase walk through the
internal works by trade, `jpms/wwwroot/js/works-model/`) frame by frame on a fixed clock and
stitches the frames with ffmpeg, so the video is the same on any machine — the higher-quality
route beside the page's Record button, which captures the live canvas to a webm.

```
node tools/works-model/render-frames.mjs est-0001-house-model.json est-0001-works.mp4 --fps 30 --width 1920 --height 1200
```

The definition is the estimate's stored JSON (`get_lead` → `estimates[].houseModel.definition`).
Needs Playwright with a Chromium it can launch and ffmpeg on the PATH.
