// The build-up's clock: stages in order, each holding its steps at offsets from the stage's
// start, played, paused, skipped to the end or reset. `step(now)` is called every frame and
// says whether anything is moving. Entering a stage and every change of state are reported
// through the hooks — that is how the camera flies and the page learns the caption.
import { PlaybackState } from "./playback-state.js";

export class Timeline {
    constructor(stages, hooks) {
        this.stages = stages;
        this.hooks = hooks;
        this.reset();
    }

    reset() {
        for (const stage of this.stages) for (const step of stage.steps) step.animator.reset();
        this.elapsed = 0;
        this.lastTick = null;
        this.stageIndex = -1;
        this.state = PlaybackState.ready;
    }

    play() {
        if (this.state === PlaybackState.finished) {
            this.hooks.beforeReplay();
            this.reset();
        }
        this.lastTick = null;
        this.changeState(PlaybackState.playing);
    }

    pause() {
        if (this.state !== PlaybackState.playing) return;
        this.changeState(PlaybackState.paused);
    }

    skipToEnd() {
        if (this.state === PlaybackState.finished) return;
        this.elapsed = this.totalDuration();
        this.advance();
    }

    step(now) {
        if (this.state !== PlaybackState.playing) return false;
        if (this.lastTick !== null) this.elapsed += now - this.lastTick;
        this.lastTick = now;
        this.advance();
        return true;
    }

    totalDuration() {
        return this.stages.reduce((total, stage) => total + stage.duration, 0);
    }

    advance() {
        let stageStart = 0;
        this.stages.forEach((stage, index) => {
            const sinceStageStart = this.elapsed - stageStart;
            stageStart += stage.duration;
            if (sinceStageStart < 0) return;
            if (index > this.stageIndex) this.enter(index);
            for (const step of stage.steps) {
                const progress = (sinceStageStart - step.at) / step.duration;
                if (progress >= 0) step.animator.apply(Math.min(1, progress));
            }
        });
        if (this.elapsed >= stageStart) this.changeState(PlaybackState.finished);
    }

    enter(index) {
        this.stageIndex = index;
        this.hooks.stageEntered(index, this.stages[index]);
    }

    changeState(state) {
        if (this.state === state) return;
        this.state = state;
        this.hooks.stateChanged(state);
    }
}
