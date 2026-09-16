// Record plays the phases from the start and captures the canvas to a webm the browser
// downloads when the play finishes — the quick record from a laptop. The higher-quality route
// renders frames headlessly and stitches them with ffmpeg: tools/works-model/render-frames.mjs.
const FramesPerSecond = 30;
const PreferredTypes = ["video/webm;codecs=vp9", "video/webm;codecs=vp8", "video/webm"];

function supportedType() {
    return PreferredTypes.find(type => window.MediaRecorder?.isTypeSupported(type)) ?? null;
}

export function startRecording(state, playback) {
    const type = supportedType();
    if (!type) throw new Error("this browser cannot record the canvas");
    const stream = state.stage.canvas.captureStream(FramesPerSecond);
    const chunks = [];
    const recorder = new MediaRecorder(stream, { mimeType: type });
    recorder.ondataavailable = event => { if (event.data.size > 0) chunks.push(event.data); };
    recorder.onstop = () => download(new Blob(chunks, { type }), `${fileStem(state.definition.name)}-works.webm`);
    state.recorder = recorder;
    state.isRecordingUntilFinished = true;
    state.phase = 0;
    recorder.start();
    playback.play();
    state.dotnetRef.invokeMethodAsync("RecordingChanged", true).catch(() => {});
}

export function stopRecording(state) {
    if (!state.recorder) return;
    state.recorder.stop();
    state.recorder = null;
    state.isRecordingUntilFinished = false;
    state.dotnetRef.invokeMethodAsync("RecordingChanged", false).catch(() => {});
}

function download(blob, filename) {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

function fileStem(name) {
    return (name ?? "model").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
}
