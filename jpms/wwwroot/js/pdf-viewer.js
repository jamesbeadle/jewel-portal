// The drawing PDF viewer (Components/PdfViewer.razor). pdf.js renders each page to a canvas
// inside the component's scroll surface, and this module owns everything the browser's built-in
// <iframe> viewer refused to let us control: zoom centred on the cursor, drag-to-pan, pinch on
// touch, fit-width / fit-whole-page, rotation, page tracking and fullscreen — the operations that
// matter on a wide landscape A1 drawing sheet squeezed into a portal column.
window.jpmsPdfViewer = (() => {
    // Pinned pdf.js build, dynamically imported the first time a PDF preview opens, so the
    // library (~1.3 MB with its worker) never taxes app boot and non-drawing pages never pay for
    // it. To vendor instead of using the CDN: copy build/pdf.min.mjs + build/pdf.worker.min.mjs
    // from the pdfjs-dist npm package into wwwroot/js/vendor/pdfjs/ and point these two constants
    // there (the service worker already precaches .mjs).
    const PDFJS_URL = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.min.mjs";
    const PDFJS_WORKER_URL = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.worker.min.mjs";

    const MIN_SCALE = 0.25;
    const MAX_SCALE = 8;
    const ZOOM_STEP = 1.25;
    const PAGE_GAP = 12;        // px between pages, and the surface's own padding
    // Backing-store budget per page canvas. Drawing sheets are vector A1/A0 — an uncapped canvas
    // at 800% zoom would exhaust canvas memory on mobile Safari. Beyond the cap the canvas is
    // rendered smaller and CSS-scaled up, so deep zoom costs sharpness rather than crashes.
    const MAX_CANVAS_PIXELS = 4096 * 4096;

    let libPromise = null;
    const loadLib = () => libPromise ??= import(PDFJS_URL).then(lib => {
        lib.GlobalWorkerOptions.workerSrc = PDFJS_WORKER_URL;
        return lib;
    });

    const states = new Map(); // keyed by the component's root element

    // ---- geometry ----------------------------------------------------------------------------

    const baseViewport = (state, page) =>
        page.getViewport({ scale: 1, rotation: (page.rotate + state.rotation) % 360 });

    const fitScale = state => {
        const availW = state.viewport.clientWidth - PAGE_GAP * 2;
        const availH = state.viewport.clientHeight - PAGE_GAP * 2;
        let maxW = 1, maxH = 1;
        for (const entry of state.pages) {
            const vp = baseViewport(state, entry.page);
            maxW = Math.max(maxW, vp.width);
            maxH = Math.max(maxH, vp.height);
        }
        const widthScale = availW / maxW;
        return state.mode === "page" ? Math.min(widthScale, availH / maxH) : widthScale;
    };

    const clamp = value => Math.min(MAX_SCALE, Math.max(MIN_SCALE, value));

    // Size every wrapper/canvas for the current scale immediately (cheap CSS — the already-drawn
    // bitmap stretches, briefly soft), then redraw crisp bitmaps for the visible pages.
    const layout = state => {
        if (state.mode !== "manual") state.scale = clamp(fitScale(state));
        state.generation++;
        for (const entry of state.pages) {
            const vp = baseViewport(state, entry.page);
            const cssW = Math.round(vp.width * state.scale);
            const cssH = Math.round(vp.height * state.scale);
            entry.wrapper.style.width = cssW + "px";
            entry.wrapper.style.height = cssH + "px";
            entry.canvas.style.width = cssW + "px";
            entry.canvas.style.height = cssH + "px";
        }
        renderVisible(state);
        notify(state);
    };

    // ---- rendering ---------------------------------------------------------------------------

    const renderVisible = state => {
        const top = state.viewport.scrollTop;
        const bottom = top + state.viewport.clientHeight;
        const margin = state.viewport.clientHeight; // pre-render one screen either side
        for (const entry of state.pages) {
            const start = entry.wrapper.offsetTop;
            const end = start + entry.wrapper.offsetHeight;
            if (end < top - margin || start > bottom + margin) continue;
            if (entry.renderedGeneration === state.generation || entry.rendering) continue;
            renderPage(state, entry);
        }
    };

    const renderPage = async (state, entry) => {
        entry.rendering = true;
        const generation = state.generation;
        try {
            const dpr = Math.min(window.devicePixelRatio || 1, 2);
            let renderScale = state.scale * dpr;
            const vp1 = baseViewport(state, entry.page);
            const pixels = vp1.width * renderScale * vp1.height * renderScale;
            if (pixels > MAX_CANVAS_PIXELS) renderScale *= Math.sqrt(MAX_CANVAS_PIXELS / pixels);

            const vp = entry.page.getViewport({
                scale: renderScale,
                rotation: (entry.page.rotate + state.rotation) % 360
            });
            // Draw off-screen, swap in when done — resizing the live canvas would blank the page
            // for the whole render.
            const scratch = document.createElement("canvas");
            scratch.width = Math.floor(vp.width);
            scratch.height = Math.floor(vp.height);
            await entry.page.render({ canvasContext: scratch.getContext("2d"), viewport: vp }).promise;
            if (state.disposed) return;
            entry.canvas.width = scratch.width;
            entry.canvas.height = scratch.height;
            entry.canvas.getContext("2d").drawImage(scratch, 0, 0);
            entry.renderedGeneration = generation;
        } catch (error) {
            if (error?.name !== "RenderingCancelledException") console.warn("jpmsPdfViewer render:", error);
        } finally {
            entry.rendering = false;
            // The scale moved on while this draw was in flight — go again at the current one.
            if (!state.disposed && entry.renderedGeneration !== state.generation) renderVisible(state);
        }
    };

    // ---- state reporting ---------------------------------------------------------------------

    const currentPage = state => {
        const middle = state.viewport.scrollTop + state.viewport.clientHeight / 2;
        let best = 1, bestDistance = Infinity;
        for (let i = 0; i < state.pages.length; i++) {
            const wrapper = state.pages[i].wrapper;
            const centre = wrapper.offsetTop + wrapper.offsetHeight / 2;
            const distance = Math.abs(centre - middle);
            if (distance < bestDistance) { bestDistance = distance; best = i + 1; }
        }
        return best;
    };

    const notify = state => {
        if (state.disposed) return;
        state.dotnetRef.invokeMethodAsync(
            "ViewerState",
            currentPage(state),
            state.pages.length,
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
        const ratio = scale / state.scale;
        const scrollX = (state.viewport.scrollLeft + x) * ratio - x;
        const scrollY = (state.viewport.scrollTop + y) * ratio - y;
        state.mode = "manual";
        state.scale = scale;
        layout(state);
        state.viewport.scrollLeft = scrollX;
        state.viewport.scrollTop = scrollY;
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

        // Pointer events cover both mouse drag-to-pan and touch: one pointer pans, two pinch.
        state.pointers = new Map();
        state.onPointerDown = event => {
            if (event.button !== 0) return;
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

        // Scroll drives lazy rendering and the "2 / 5" indicator, coalesced to one per frame.
        state.onScroll = () => {
            if (state.scrollScheduled) return;
            state.scrollScheduled = true;
            requestAnimationFrame(() => {
                state.scrollScheduled = false;
                if (state.disposed) return;
                renderVisible(state);
                notify(state);
            });
        };
        viewport.addEventListener("scroll", state.onScroll, { passive: true });

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
            // h-[70vh] is right in the column; in fullscreen the surface takes the screen.
            state.viewport.style.height = fullscreen ? "auto" : "";
            state.viewport.style.flex = fullscreen ? "1 1 0%" : "";
            if (state.mode !== "manual") layout(state); else notify(state);
        };
        document.addEventListener("fullscreenchange", state.onFullscreenChange);
    };

    // ---- public api --------------------------------------------------------------------------

    const get = root => states.get(root);

    return {
        init: async (root, viewport, pagesEl, url, dotnetRef) => {
            if (!root || states.has(root)) return false;
            const state = {
                root, viewport, pagesEl, dotnetRef,
                pages: [], scale: 1, mode: "width", rotation: 0,
                generation: 0, disposed: false, pinchDistance: 0
            };
            states.set(root, state);
            try {
                const lib = await loadLib();
                state.doc = await lib.getDocument({ url }).promise;
                if (state.disposed) return false;
                pagesEl.replaceChildren();
                for (let number = 1; number <= state.doc.numPages; number++) {
                    const page = await state.doc.getPage(number);
                    if (state.disposed) return false;
                    // Inline styles throughout — Tailwind's JIT never scans this file, so any
                    // class named here would simply not exist in the built CSS.
                    const wrapper = document.createElement("div");
                    wrapper.style.cssText =
                        `position:relative;margin:0 auto ${PAGE_GAP}px;background:#fff;` +
                        "box-shadow:0 2px 8px rgb(0 0 0 / 0.35)";
                    const canvas = document.createElement("canvas");
                    canvas.style.cssText = "display:block;width:100%;height:100%";
                    wrapper.appendChild(canvas);
                    pagesEl.appendChild(wrapper);
                    state.pages.push({ page, wrapper, canvas, renderedGeneration: -1, rendering: false });
                }
                attachInteraction(state);
                layout(state);
                return true;
            } catch (error) {
                states.delete(root);
                dotnetRef.invokeMethodAsync("ViewerFailed", String(error?.message ?? error)).catch(() => {});
                return false;
            }
        },

        zoomIn: root => { const s = get(root); if (s) setScale(s, s.scale * ZOOM_STEP); },
        zoomOut: root => { const s = get(root); if (s) setScale(s, s.scale / ZOOM_STEP); },

        fitWidth: root => { const s = get(root); if (s) { s.mode = "width"; layout(s); } },
        fitPage: root => {
            const s = get(root);
            if (!s) return;
            s.mode = "page";
            layout(s);
            // Fit-whole-page means "show me the page", so line its top up too.
            const entry = s.pages[currentPage(s) - 1];
            if (entry) s.viewport.scrollTop = Math.max(0, entry.wrapper.offsetTop - PAGE_GAP);
        },

        rotate: root => {
            const s = get(root);
            if (!s) return;
            s.rotation = (s.rotation + 90) % 360;
            layout(s);
        },

        goToPage: (root, delta) => {
            const s = get(root);
            if (!s) return;
            const target = s.pages[Math.min(s.pages.length, Math.max(1, currentPage(s) + delta)) - 1];
            if (target) s.viewport.scrollTo({ top: Math.max(0, target.wrapper.offsetTop - PAGE_GAP) });
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
            state.viewport.removeEventListener("wheel", state.onWheel);
            state.viewport.removeEventListener("pointerdown", state.onPointerDown);
            state.viewport.removeEventListener("pointermove", state.onPointerMove);
            state.viewport.removeEventListener("pointerup", state.onPointerEnd);
            state.viewport.removeEventListener("pointercancel", state.onPointerEnd);
            state.viewport.removeEventListener("scroll", state.onScroll);
            state.resizeObserver?.disconnect();
            clearTimeout(state.resizeTimer);
            document.removeEventListener("fullscreenchange", state.onFullscreenChange);
            state.doc?.destroy().catch(() => {});
            states.delete(root);
        }
    };
})();
