// The drawn signature on the public forms (/f/…), carried from the JPS Dashboard's pad
// (api/pubforms.js): a canvas signed with a finger or a mouse, drawn at twice the element's width
// so the line stays crisp, ink the colour of the dashboard's. Blazor asks it four things —
// attach to a canvas, whether anything has been drawn, the drawing as a PNG, and to clear it.
// A classic global, so a page served by an older cached shell finds it missing and says so
// rather than failing.
window.jpmsSignaturePad = (function () {
    var pads = new WeakMap();
    var ink = '#13233a';

    function positionOf(canvas, event) {
        var box = canvas.getBoundingClientRect();
        var point = event.touches ? event.touches[0] : event;
        return [(point.clientX - box.left) * canvas.width / box.width, (point.clientY - box.top) * canvas.height / box.height];
    }

    function attach(canvas) {
        if (!canvas || pads.has(canvas)) { return; }
        canvas.width = Math.max(canvas.offsetWidth, 1) * 2;
        canvas.height = 320;
        var context = canvas.getContext('2d');
        context.lineWidth = 3;
        context.lineCap = 'round';
        context.strokeStyle = ink;
        var pad = { drawing: false, drawn: false, context: context };
        function start(event) {
            pad.drawing = true;
            var point = positionOf(canvas, event);
            context.beginPath();
            context.moveTo(point[0], point[1]);
            event.preventDefault();
        }
        function move(event) {
            if (!pad.drawing) { return; }
            var point = positionOf(canvas, event);
            context.lineTo(point[0], point[1]);
            context.stroke();
            pad.drawn = true;
            event.preventDefault();
        }
        function end() { pad.drawing = false; }
        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        document.addEventListener('mouseup', end);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', end);
        pads.set(canvas, pad);
    }

    function hasInk(canvas) {
        var pad = pads.get(canvas);
        return !!(pad && pad.drawn);
    }

    function clear(canvas) {
        var pad = pads.get(canvas);
        if (!pad) { return; }
        pad.context.clearRect(0, 0, canvas.width, canvas.height);
        pad.drawn = false;
    }

    function toPng(canvas) {
        return canvas.toDataURL('image/png').split(',')[1];
    }

    return { attach: attach, hasInk: hasInk, clear: clear, toPng: toPng };
})();
