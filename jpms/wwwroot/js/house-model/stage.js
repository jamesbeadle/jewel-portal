// The stage the house stands on: a WebGL canvas filling its host, a soft sky, the lights, a
// studio environment for the glass to reflect, and orbit controls that keep the camera above
// the ground. Nothing here knows what a house is.
import * as THREE from "three";
import { OrbitControls } from "../vendor/three/OrbitControls.js";
import { RoomEnvironment } from "../vendor/three/RoomEnvironment.js";
import { Palette } from "./palette.js";
import { lights } from "./lighting.js";

const FieldOfViewDegrees = 38;
const NearestMetres = 0.5;
const FarthestMetres = 400;

function aspectOf(host) {
    return Math.max(1, host.clientWidth) / Math.max(1, host.clientHeight);
}

function createRenderer(host) {
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setSize(host.clientWidth, host.clientHeight);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFShadowMap;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.domElement.style.cssText = "display:block;width:100%;height:100%;outline:none;";
    host.appendChild(renderer.domElement);
    return renderer;
}

function createScene(renderer) {
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(Palette.skyZenith);
    scene.fog = new THREE.Fog(Palette.skyHorizon, 70, 180);
    const environment = new THREE.PMREMGenerator(renderer);
    scene.environment = environment.fromScene(new RoomEnvironment(), 0.04).texture;
    scene.environmentIntensity = 0.35;
    environment.dispose();
    scene.add(...lights());
    return scene;
}

function createControls(camera, canvas) {
    const controls = new OrbitControls(camera, canvas);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.maxPolarAngle = Math.PI / 2 - 0.03;
    controls.minDistance = 6;
    controls.maxDistance = 70;
    controls.screenSpacePanning = false;
    return controls;
}

export function createStage(host) {
    const renderer = createRenderer(host);
    const scene = createScene(renderer);
    const camera = new THREE.PerspectiveCamera(FieldOfViewDegrees, aspectOf(host), NearestMetres, FarthestMetres);
    const controls = createControls(camera, renderer.domElement);
    return {
        scene,
        camera,
        controls,
        render: () => renderer.render(scene, camera),
        resize: () => {
            renderer.setSize(host.clientWidth, host.clientHeight);
            camera.aspect = aspectOf(host);
            camera.updateProjectionMatrix();
        },
        dispose: () => {
            controls.dispose();
            scene.environment?.dispose();
            renderer.dispose();
            renderer.forceContextLoss();
            renderer.domElement.remove();
        }
    };
}
