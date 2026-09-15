// Turns a model's written programme of works into a Timeline's stages: every step names a
// part of the built house and the move it makes, and a stage lasts until its last step is done
// plus a hold for the viewer to take it in.
import { grow, shrink, drop, lift, appear, vanish, fade } from "./animators.js";
import { anchorAt } from "./anchor.js";

const animatorsByKind = { grow, shrink, drop, lift, appear, vanish, fade };
const DefaultStepMilliseconds = 1;

function compileStep(step, house, materials) {
    const part = house.getObjectByName(step.part);
    if (!part) throw new Error(`the programme names a part that is not in the model: "${step.part}"`);
    if (step.anchor) anchorAt(part, step.anchor);
    const animator = animatorsByKind[step.kind](part, step, materials);
    return { at: step.at ?? 0, duration: step.duration ?? DefaultStepMilliseconds, animator };
}

function compileStage(stage, house, materials) {
    const steps = (stage.steps ?? []).map(step => compileStep(step, house, materials));
    const stepsEnd = steps.reduce((latest, step) => Math.max(latest, step.at + step.duration), 0);
    return { title: stage.title, detail: stage.detail, view: stage.view, steps, duration: stepsEnd + stage.hold };
}

export function compileProgramme(programme, house, materials) {
    return programme.stages.map(stage => compileStage(stage, house, materials));
}
