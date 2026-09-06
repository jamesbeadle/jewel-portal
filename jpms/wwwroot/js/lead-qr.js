// The QR code on a lead's page (Sales → Leads → lead → Imagine, 2026-09-06): encodes the lead's
// private /imagine/{token} link so the letter or brochure carries it. The encoder (qrcodejs,
// MIT) is fetched from cdnjs the first time a code is drawn — nobody pays for it on pages that
// never show one — and if it can't be fetched the caller falls back to showing the address as
// text. The PNG download is the drawn canvas, so print and screen carry the same code.
window.jpmsLeadQr = (function () {
    const libraryUrl = "https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js";
    let loading = null;

    function ensureLibrary() {
        if (window.QRCode) return Promise.resolve(true);
        if (loading) return loading;
        loading = new Promise(function (resolve) {
            const script = document.createElement("script");
            script.src = libraryUrl;
            script.async = true;
            script.onload = function () { resolve(!!window.QRCode); };
            script.onerror = function () { loading = null; resolve(false); };
            document.head.appendChild(script);
        });
        return loading;
    }

    return {
        // Draw the code for `text` into the element with `id`. Resolves false when the encoder
        // couldn't be loaded (offline, blocked), so the page can show the link instead.
        draw: async function (id, text, size) {
            const host = document.getElementById(id);
            if (!host) return false;
            if (!(await ensureLibrary())) return false;
            host.innerHTML = "";
            new window.QRCode(host, {
                text: text,
                width: size || 220,
                height: size || 220,
                colorDark: "#101111",
                colorLight: "#ffffff",
                correctLevel: window.QRCode.CorrectLevel.M
            });
            return true;
        },
        // Save the drawn code as a PNG named for the lead.
        download: function (id, fileName) {
            const host = document.getElementById(id);
            const canvas = host ? host.querySelector("canvas") : null;
            if (!canvas) return false;
            const anchor = document.createElement("a");
            anchor.href = canvas.toDataURL("image/png");
            anchor.download = fileName || "imagine-qr.png";
            document.body.appendChild(anchor);
            anchor.click();
            anchor.remove();
            return true;
        },
        copy: async function (text) {
            try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
        }
    };
})();
