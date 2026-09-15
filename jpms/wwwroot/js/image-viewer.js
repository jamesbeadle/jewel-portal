// The photo viewer (Components/ImageViewer.razor). A high-resolution site photo dropped into a
// bare <img> or <iframe> renders at its native size — a 12-megapixel snap is a scroll-in-every-
// direction fight, with no way to see the whole picture at once. This module gives an image the
// same surface the PDF viewer gives a drawing sheet: fit-whole / fit-width / actual size, zoom
// centred on the cursor, drag-to-pan, pinch on touch, rotation and fullscreen. Same shape as
// pdf-viewer.js on purpose — one set of habits for both kinds of preview.
window.jpmsImageViewer = (() => {
    const MIN_SCALE = 0.05;
    const MAX_SCALE = 8;
    const ZOOM_STEP = 1.25;
    const PAD = 12; // the surface's own padding, px

    const states = new Map(); // keyed by the component's root element

    // ---- geometry ----------------------------------------------------------------------------

    // The picture's footprint at scale 1 once rotated — a portrait photo turned 90° is landscape.
    const rotatedSize = state => {
        const swap = state.rotation % 180 !== 0;
        return {
            width: swap ? state.naturalHeight : state.naturalWidth,
            height: swap ? state.naturalWidth : state.naturalHeight
        };
    };

    const fitScale = state => {
        const { width, height } = rotatedSize(state);
        const availW = Math.max(1, state.viewport.clientWidth - PAD * 2);
        const availH = Math.max(1, state.viewport.clientHeight - PAD * 2);
        const widthScale = availW / width;
        return state.mode === "page" ? Math.min(widthScale, availH / height) : widthScale;
    };

    const clamp = value => Math.min(MAX_SCALE, Math.max(MIN_SCALE, value));

    // Size the stage to the rotated footprint and place the (unrotated-size) image in its centre,
    // turned by CSS. When the picture is smaller than the surface it sits centred rather than
    // pinned to the top-left corner — fit-whole means "show me the photo", not "show me a corner".
    const layout = state => {
        if (state.mode !== "manual") state.scale = clamp(fitScale(state));
        const { width, height } = rotatedSize(state);
        const stageW = Math.round(width * state.scale);
        const stageH = Math.round(height * state.scale);
        const availH = state.viewport.clientHeight - PAD * 2;
        state.stage.style.width = stageW + "px";
        state.stage.style.height = stageH + "px";
        state.stage.style.marginTop = Math.max(0, Math.floor((availH - stageH) / 2)) + "px";
        state.img.style.width = Math.round(state.naturalWidth * state.scale) + "px";
        state.img.style.height = Math.round(state.naturalHeight * state.scale) + "px";
        state.img.style.transform = `translate(-50%, -50%) rotate(${state.rotation}deg)`;
        notify(state);
    };

    // ---- state reporting ---------------------------------------------------------------------

    const notify = state => {
        if (state.disposed) return;
        state.dotnetRef.invokeMethodAsync(
            "ViewerState",
            Math.round(state.scale * 100),
            document.fullscreenElement === state.root
        ).catch(() => { /* circuit gone — dispose is on its way */ });
    };

    // ---- zoom / pan --------------------------------------------------------------------------

    // Re-scale keeping the surface point under (clientX, clientY) stationary, so zooming works
    // like a map: the detail you pointed at stays put and grows.
    const setScale = (state, nextScale, clientX, clientY) => {
        const scale = clamp(nextScale);
        if (scale === state.scale) return;
        const rect = state.viewport.getBoundingClientRect();
        const x = (clientX ?? rect.left + rect.width / 2) - rect.left;
        const y = (clientY ?? rect.top + rect.height / 2) - rect.top;
        // Measure against the stage, not the scroll offset: the centring margin above the stage
        // shrinks as the picture grows, and the scroll position has to follow it. The stage's
        // place is read from the rectangles (offsetTop would measure against whichever ancestor
        // happens to be positioned — the panel header's height would leak into the sum).
        const stageOffset = () => {
            const stageRect = state.stage.getBoundingClientRect();
            return {
                left: stageRect.left - rect.left + state.viewport.scrollLeft,
                top: stageRect.top - rect.top + state.viewport.scrollTop
            };
        };
        const before = stageOffset();
        const stageX = state.viewport.scrollLeft + x - before.left;
        const stageY = state.viewport.scrollTop + y - before.top;
        const ratio = scale / state.scale;
        state.mode = "manual";
        state.scale = scale;
        layout(state);
        const after = stageOffset();
        state.viewport.scrollLeft = after.left + stageX * ratio - x;
        state.viewport.scrollTop = after.top + stageY * ratio - y;
    };

    const attachInteraction = state => {
        const viewport = state.viewport;

        // Ctrl/Cmd + wheel zooms (matching every browser's own convention — trackpad pinch
        // arrives as exactly this event); a plain wheel keeps scrolling.
        state.onWheel = event => {
            if (!event.ctrlKey && !event.metaKey) return;
            event.preventDefault();
            setScale(state, state.scale * Math.exp(-event.deltaY * 0.002), event.clientX, event.clientY);
        };
        viewport.addEventListener("wheel", state.onWheel, { passive: false });

        // Double-click hops between "whole photo" and "actual pixels" at the point clicked — the
        // gesture every photo app has taught people to expect.
        state.onDoubleClick = event => {
            if (state.mode === "manual" && Math.abs(state.scale - 1) < 0.01) {
                state.mode = "page";
                layout(state);
            } else {
                setScale(state, 1, event.clientX, event.clientY);
            }
        };
        viewport.addEventListener("dblclick", state.onDoubleClick);

        // Pointer events cover both mouse drag-to-pan and touch: one pointer pans, two pinch.
        state.pointers = new Map();
        state.onPointerDown = event => {
            if (event.button !== 0) return;
            event.preventDefault(); // no native image drag ghost
            state.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
            viewport.setPointerCapture(event.pointerId);
            // Inline style rather than a Tailwind class: the JIT build never scans this file.
            if (state.pointers.size === 1) viewport.style.cursor = "grabbing";
            if (state.pointers.size === 2) {
                const [a, b] = [...state.pointers.values()];
                state.pinchDistance = Math.hypot(a.x - b.x, a.y - b.y);
            }
        };
        state.onPointerMove = event => {
            const previous = state.pointers.get(event.pointerId);
            if (!previous) return;
            const point = { x: event.clientX, y: event.clientY };
            state.pointers.set(event.pointerId, point);
            if (state.pointers.size === 1) {
                viewport.scrollLeft -= point.x - previous.x;
                viewport.scrollTop -= point.y - previous.y;
            } else if (state.pointers.size === 2) {
                const [a, b] = [...state.pointers.values()];
                const distance = Math.hypot(a.x - b.x, a.y - b.y);
                if (state.pinchDistance > 0) {
                    setScale(state, state.scale * (distance / state.pinchDistance),
                        (a.x + b.x) / 2, (a.y + b.y) / 2);
                }
                state.pinchDistance = distance;
            }
        };
        state.onPointerEnd = event => {
            state.pointers.delete(event.pointerId);
            if (state.pointers.size < 2) state.pinchDistance = 0;
            if (state.pointers.size === 0) viewport.style.cursor = "";
        };
        viewport.addEventListener("pointerdown", state.onPointerDown);
        viewport.addEventListener("pointermove", state.onPointerMove);
        viewport.addEventListener("pointerup", state.onPointerEnd);
        viewport.addEventListener("pointercancel", state.onPointerEnd);

        // Fit modes re-fit when the surface itself resizes (side nav collapse, window resize,
        // entering fullscreen). Manual zoom is left exactly where the user put it.
        state.resizeObserver = new ResizeObserver(() => {
            clearTimeout(state.resizeTimer);
            state.resizeTimer = setTimeout(() => {
                if (!state.disposed && state.mode !== "manual") layout(state);
            }, 100);
        });
        state.resizeObserver.observe(viewport);

        state.onFullscreenChange = () => {
            const fullscreen = document.fullscreenElement === state.root;
            // The height class is right in the column; in fullscreen the surface takes the screen.
            state.viewport.style.height = fullscreen ? "auto" : "";
            state.viewport.style.flex = fullscreen ? "1 1 0%" : "";
            if (state.mode !== "manual") layout(state); else notify(state);
        };
        document.addEventListener("fullscreenchange", state.onFullscreenChange);
    };

    // ---- public api --------------------------------------------------------------------------

    const get = root => states.get(root);

    return {
        init: (root, viewport, stage, img, url, dotnetRef) => {
            if (!root || states.has(root)) return false;
            const state = {
                root, viewport, stage, img, dotnetRef,
                naturalWidth: 1, naturalHeight: 1,
                scale: 1, mode: "page", rotation: 0,
                disposed: false, pinchDistance: 0
            };
            states.set(root, state);
            // Inline styles throughout — Tailwind's JIT never scans this file, so any class named
            // here would simply not exist in the built CSS.
            stage.style.cssText = "position:relative;margin:0 auto;";
            img.style.cssText =
                "position:absolute;left:50%;top:50%;max-width:none;display:block;" +
                "transform-origin:center;user-select:none;-webkit-user-drag:none;" +
                "box-shadow:0 2px 8px rgb(0 0 0 / 0.35)";
            img.draggable = false;
            img.onload = () => {
                if (state.disposed) return;
                state.naturalWidth = img.naturalWidth || 1;
                state.naturalHeight = img.naturalHeight || 1;
                attachInteraction(state);
                layout(state);
            };
            img.onerror = () => {
                states.delete(root);
                dotnetRef.invokeMethodAsync("ViewerFailed", "the image could not be loaded").catch(() => {});
            };
            img.src = url;
            return true;
        },

        zoomIn: root => { const s = get(root); if (s) setScale(s, s.scale * ZOOM_STEP); },
        zoomOut: root => { const s = get(root); if (s) setScale(s, s.scale / ZOOM_STEP); },
        actualSize: root => { const s = get(root); if (s) setScale(s, 1); },

        fitWidth: root => {
            const s = get(root);
            if (!s) return;
            s.mode = "width";
            layout(s);
            s.viewport.scrollTop = 0;
        },
        fitPage: root => {
            const s = get(root);
            if (!s) return;
            s.mode = "page";
            layout(s);
            s.viewport.scrollTop = 0;
            s.viewport.scrollLeft = 0;
        },

        rotate: root => {
            const s = get(root);
            if (!s) return;
            s.rotation = (s.rotation + 90) % 360;
            layout(s);
        },

        toggleFullscreen: root => {
            const s = get(root);
            if (!s) return;
            if (document.fullscreenElement === root) document.exitFullscreen();
            else root.requestFullscreen?.();
        },

        dispose: root => {
            const state = states.get(root);
            if (!state) return;
            state.disposed = true;
            state.img.onload = null;
            state.img.onerror = null;
            state.viewport.removeEventListener("wheel", state.onWheel);
            state.viewport.removeEventListener("dblclick", state.onDoubleClick);
            state.viewport.removeEventListener("pointerdown", state.onPointerDown);
            state.viewport.removeEventListener("pointermove", state.onPointerMove);
            state.viewport.removeEventListener("pointerup", state.onPointerEnd);
            state.viewport.removeEventListener("pointercancel", state.onPointerEnd);
            state.resizeObserver?.disconnect();
            clearTimeout(state.resizeTimer);
            document.removeEventListener("fullscreenchange", state.onFullscreenChange);
            states.delete(root);
        }
    };
})();
