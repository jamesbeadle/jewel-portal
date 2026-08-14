// The to-do board's JS shims — the pieces of HTML5 drag-and-drop that Blazor cannot do reliably
// from C#. Both are document-level listeners, scoped by data attribute to TodoBoard.razor's markup.
//
// 1. dragstart: Firefox refuses to start an HTML5 drag unless dragstart puts SOMETHING on the
//    dataTransfer, and Blazor's C# dragstart handler has no way to reach it — so this primes it
//    for any element opting in with data-todo-drag (the board's cards). The payload is empty on
//    purpose: which card is mid-flight is state the Blazor component already tracks itself.
//
// 2. dragover: a browser only allows a drop where the dragover default is cancelled — on EVERY
//    dragover event, synchronously. Blazor's standalone @ondragover:preventDefault directive did
//    not deliver that in practice (Chrome rejected the drop: the card animated back to its column
//    and @ondrop never fired), so the board's columns opt in with data-todo-dropzone and the
//    default is cancelled here instead, only while one of the board's own cards is mid-drag. The
//    drop itself stays a Blazor @ondrop handler on the column.
let todoDragActive = false;

document.addEventListener('dragstart', function (e) {
    if (e.target && e.target.closest && e.target.closest('[data-todo-drag]') && e.dataTransfer) {
        e.dataTransfer.setData('text/plain', '');
        e.dataTransfer.effectAllowed = 'move';
        todoDragActive = true;
    }
});

// Cleared on BOTH ends of a drag: dragend fires on the source card even when the drop landed
// nowhere, and drop covers the case where the accepted drop's re-render removes the source card
// before its dragend can bubble this far. A stray true would let file drags onto a column be
// swallowed by the dragover listener below.
document.addEventListener('dragend', function () { todoDragActive = false; });
document.addEventListener('drop', function () { todoDragActive = false; });

document.addEventListener('dragover', function (e) {
    if (todoDragActive && e.target && e.target.closest && e.target.closest('[data-todo-dropzone]')) {
        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
    }
});
