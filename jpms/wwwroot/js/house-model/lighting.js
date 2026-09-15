// How the model is lit. The sun throws the shadows, from high over the garden and a little to
// the party-wall side so they fall across the roof and the garage where the opening view can
// see them; a shadowless fill from the road side keeps the front and the gable end legible
// rather than flat; the hemisphere is the sky's own glow on everything.
import * as THREE from "three";
import { Palette } from "./palette.js";

const SunPosition = new THREE.Vector3(-24, 26, 16);
const FillPosition = new THREE.Vector3(22, 14, -14);
const ShadowMapSize = 2048;
const ShadowReachMetres = 24;

function sun() {
    const light = new THREE.DirectionalLight(Palette.sunlight, 3.2);
    light.position.copy(SunPosition);
    light.castShadow = true;
    light.shadow.mapSize.set(ShadowMapSize, ShadowMapSize);
    light.shadow.camera.left = -ShadowReachMetres;
    light.shadow.camera.right = ShadowReachMetres;
    light.shadow.camera.top = ShadowReachMetres;
    light.shadow.camera.bottom = -ShadowReachMetres;
    light.shadow.camera.near = 1;
    light.shadow.camera.far = 90;
    light.shadow.camera.updateProjectionMatrix();
    light.shadow.bias = -0.0004;
    light.shadow.normalBias = 0.03;
    return light;
}

function fill() {
    const light = new THREE.DirectionalLight(Palette.skyZenith, 0.55);
    light.position.copy(FillPosition);
    return light;
}

function sky() {
    return new THREE.HemisphereLight(Palette.skyZenith, Palette.groundBounce, 0.45);
}

export function lights() {
    return [sky(), sun(), fill()];
}
