// Moves the camera from where it is to a named view over a short flight rather than a cut, so
// the viewer keeps their bearings. `step` is called every frame and says whether it moved.
const FlightMilliseconds = 700;

const easeInOut = t => (t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2);

export class CameraFlight {
    constructor(camera, controls) {
        this.camera = camera;
        this.controls = controls;
        this.destination = null;
    }

    jumpTo(view) {
        this.destination = null;
        this.camera.position.copy(view.position);
        this.controls.target.copy(view.target);
        this.controls.update();
    }

    flyTo(view) {
        this.departure = { position: this.camera.position.clone(), target: this.controls.target.clone() };
        this.destination = view;
        this.departedAt = performance.now();
    }

    step() {
        if (!this.destination) return false;
        const progress = Math.min(1, (performance.now() - this.departedAt) / FlightMilliseconds);
        const eased = easeInOut(progress);
        this.camera.position.lerpVectors(this.departure.position, this.destination.position, eased);
        this.controls.target.lerpVectors(this.departure.target, this.destination.target, eased);
        if (progress >= 1) this.destination = null;
        return true;
    }
}
