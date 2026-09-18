// The dismissal watcher is the one definition of "a press outside closes it", and since
// 2026-09-18 two components depend on it: DropdownMenu and SearchSelect. These pin the rules
// each of them relies on. Run with: npm run test:js (from jpms/), or node --test tests/js.
const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');

const source = fs.readFileSync(
    path.join(__dirname, '..', '..', 'jpms', 'wwwroot', 'js', 'dropdown-menu.js'), 'utf8');

function listenerBox() {
    const listeners = [];
    return {
        listeners,
        addEventListener: (type, handler, capture) => listeners.push({ type, handler, capture }),
        removeEventListener: (type, handler, capture) => {
            const at = listeners.findIndex(
                one => one.type === type && one.handler === handler && one.capture === capture);
            if (at >= 0) listeners.splice(at, 1);
        },
        fire: (type, event) => listeners
            .filter(one => one.type === type)
            .slice()
            .forEach(one => one.handler(event)),
        countOf: type => listeners.filter(one => one.type === type).length
    };
}

// The page area inside the scrollbar is narrower than the window: a fixed panel's right and
// bottom offsets are measured against the former, so the fake keeps them apart on purpose.
const ScrollbarWidth = 15;
const WindowWidth = 1000;
const WindowHeight = 800;

function loadWatcher() {
    const documentBox = listenerBox();
    const windowBox = listenerBox();
    documentBox.documentElement = {
        clientWidth: WindowWidth - ScrollbarWidth,
        clientHeight: WindowHeight
    };
    windowBox.innerWidth = WindowWidth;
    windowBox.innerHeight = WindowHeight;
    new Function('window', 'document', source)(windowBox, documentBox);
    return { watcher: windowBox.jpmsDropdownMenu, documentBox, windowBox };
}

function menuReference(id) {
    const closes = [];
    return {
        _id: id,
        closes,
        invokeMethodAsync: name => { closes.push(name); return Promise.resolve(); }
    };
}

const root = { contains: target => target === 'inside' };
const pressInside = { composedPath: () => ['inside', root], target: 'inside' };
const pressOutside = { composedPath: () => ['elsewhere'], target: 'elsewhere' };

test('a press outside the root closes the popup', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    documentBox.fire('pointerdown', pressOutside);
    assert.deepStrictEqual(reference.closes, ['CloseFromOutside']);
});

test('a press inside the root does not close it — this is how an option press survives', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    documentBox.fire('pointerdown', pressInside);
    assert.deepStrictEqual(reference.closes, []);
});

test('SearchSelect fixed popup is inside the wrapper, so its option press survives too', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    const wrapper = { contains: () => { throw new Error('composedPath should answer first'); } };
    const optionInFixedPopup = { composedPath: () => ['option', 'popup', wrapper], target: 'option' };
    watcher.watch(wrapper, reference, true);
    documentBox.fire('pointerdown', optionInFixedPopup);
    assert.deepStrictEqual(reference.closes, []);
});

test('Escape closes it', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    documentBox.fire('keydown', { key: 'Escape' });
    assert.deepStrictEqual(reference.closes, ['CloseFromOutside']);
});

test('another key does not', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    documentBox.fire('keydown', { key: 'a' });
    assert.deepStrictEqual(reference.closes, []);
});

test('a fixed popup asks for the scroll close and gets it', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference, true);
    documentBox.fire('scroll', {});
    assert.deepStrictEqual(reference.closes, ['CloseFromOutside']);
});

test('an absolutely positioned panel does not, so its own container scrolling leaves it open', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    assert.strictEqual(documentBox.countOf('scroll'), 0);
    documentBox.fire('scroll', {});
    assert.deepStrictEqual(reference.closes, []);
});

test('unwatch takes down every listener it put up', () => {
    const { watcher, documentBox, windowBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference, true);
    watcher.unwatch(reference);
    assert.strictEqual(documentBox.listeners.length, 0);
    assert.strictEqual(windowBox.listeners.length, 0);
    documentBox.fire('pointerdown', pressOutside);
    assert.deepStrictEqual(reference.closes, []);
});

test('watching twice replaces rather than doubles, so one press closes once', () => {
    const { watcher, documentBox } = loadWatcher();
    const reference = menuReference(1);
    watcher.watch(root, reference);
    watcher.watch(root, reference);
    assert.strictEqual(documentBox.countOf('pointerdown'), 1);
    documentBox.fire('pointerdown', pressOutside);
    assert.deepStrictEqual(reference.closes, ['CloseFromOutside']);
});

test('each popup is keyed by its reference id, not the proxy object', () => {
    const { watcher, documentBox } = loadWatcher();
    const first = menuReference(1);
    const second = menuReference(2);
    watcher.watch(root, first);
    watcher.watch(root, second);
    watcher.unwatch({ _id: 1, invokeMethodAsync: () => Promise.resolve() });
    documentBox.fire('pointerdown', pressOutside);
    assert.deepStrictEqual(first.closes, []);
    assert.deepStrictEqual(second.closes, ['CloseFromOutside']);
});

test('a toggle is measured against the page area, not the window, so a right-anchored panel lands on it', () => {
    const { watcher } = loadWatcher();
    const toggle = { getBoundingClientRect: () => ({ top: 100, left: 300, right: 420, bottom: 130, width: 120 }) };
    const reading = watcher.measure(toggle);
    assert.strictEqual(reading.viewportWidth, WindowWidth - ScrollbarWidth);
    assert.strictEqual(reading.viewportHeight, WindowHeight);
    // What the caller pins the right edge at. Measured against the window it would be out by the
    // scrollbar, which is how every right-aligned menu came to sit beside its toggle.
    assert.strictEqual(reading.viewportWidth - reading.right, 565);
});

test('a toggle reading carries the edges a panel is placed from', () => {
    const { watcher } = loadWatcher();
    const toggle = { getBoundingClientRect: () => ({ top: 10, left: 20, right: 140, bottom: 44, width: 120 }) };
    const reading = watcher.measure(toggle);
    assert.deepStrictEqual(
        { top: reading.top, left: reading.left, right: reading.right, bottom: reading.bottom, width: reading.width },
        { top: 10, left: 20, right: 140, bottom: 44, width: 120 });
});
