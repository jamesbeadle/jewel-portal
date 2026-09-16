// What the build-up does to the rest of the viewer as it runs: a new stage flies the camera to
// its viewpoint and tells the page its caption; a change of state hands the camera to the
// viewer only once the works are finished, and tells the page so it can change its buttons.
import { PlaybackState } from "./playback-state.js";
import { applyPhase } from "../phases.js";

export function timelineHooks(state) {
    return {
        stageEntered: (index, stage) => {
            const view = state.views[stage.view];
            if (view) state.flight.flyTo(view);
            state.dotnetRef.invokeMethodAsync("StageChanged", index, state.timeline.stages.length, stage.title, stage.detail)
                .catch(() => {});
        },
        stateChanged: playback => {
            const isFinished = playback === PlaybackState.finished;
            state.stage.controls.enabled = isFinished;
            state.isDrifting = !isFinished;
            state.needsFrame = true;
            if (isFinished) { applyPhase(state.house, true); state.mode = "proposed"; }
            state.dotnetRef.invokeMethodAsync("PlaybackChanged", playback).catch(() => {});
        },
        beforeReplay: () => {
            applyPhase(state.house, false);
            state.mode = "existing";
            state.flight.flyTo(state.views.overview);
        }
    };
}
