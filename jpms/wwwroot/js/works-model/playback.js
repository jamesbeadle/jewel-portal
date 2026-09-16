// Play eases through the phases in order, each held long enough to read, the camera orbiting
// slowly the while; the elements that join in a phase rise into place over its first moments.
// Pause stops where it is; the scrubber can move the phase by hand at any time.
import { standingOf } from "./phase-state.js";
import { elementMeshes } from "./elements.js";
import { present } from "./presentation.js";

const MillisecondsPerPhase = 2600;
const RiseFraction = 0.55;
const MinimumScale = 0.001;
const easeOut = t => 1 - Math.pow(1 - t, 3);

export class Playback {
    constructor(state) {
        this.state = state;
        this.isPlaying = false;
        this.startedAt = null;
        this.startedFrom = 0;
    }

    play(now = performance.now()) {
        const last = this.state.definition.phases.length - 1;
        this.startedFrom = this.state.phase >= last ? 0 : Math.floor(this.state.phase);
        this.startedAt = now;
        this.isPlaying = true;
        this.announce("playing");
    }

    pause() {
        if (!this.isPlaying) return;
        this.isPlaying = false;
        this.state.phase = Math.floor(this.state.phase);
        this.settle();
        this.announce("paused");
    }

    step(now) {
        if (!this.isPlaying) return false;
        const last = this.state.definition.phases.length - 1;
        const elapsed = Math.max(0, now - this.startedAt) / MillisecondsPerPhase;
        const phase = Math.min(last, this.startedFrom + elapsed);
        const reached = Math.floor(phase);
        if (reached !== Math.floor(this.state.phase)) { this.state.phase = reached; present(this.state); this.state.onPhase(reached); }
        this.state.phase = phase;
        this.rise(reached, phase - reached);
        if (phase >= last) { this.isPlaying = false; this.settle(); this.announce("finished"); }
        return true;
    }

    rise(reached, fraction) {
        const progress = Math.min(1, fraction / RiseFraction);
        for (const mesh of elementMeshes(this.state.works)) {
            const standing = standingOf(mesh.userData.element, this.state.definition.phases, reached);
            if (standing.joinsAt === reached && mesh.visible) mesh.scale.y = Math.max(MinimumScale, easeOut(progress));
        }
    }

    settle() {
        for (const mesh of elementMeshes(this.state.works)) mesh.scale.y = 1;
        present(this.state);
    }

    announce(playback) {
        this.state.dotnetRef.invokeMethodAsync("PlaybackChanged", playback).catch(() => {});
    }
}
