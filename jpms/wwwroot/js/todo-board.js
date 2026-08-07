// The to-do board's one piece of JS. Firefox refuses to start an HTML5 drag unless dragstart
// puts SOMETHING on the dataTransfer, and Blazor's C# dragstart handler has no way to reach it —
// so this document-level listener primes it for any element opting in with data-todo-drag
// (TodoBoard.razor's cards). The payload is empty on purpose: which card is mid-flight is state
// the Blazor component already tracks itself.
document.addEventListener('dragstart', function (e) {
    if (e.target && e.target.closest && e.target.closest('[data-todo-drag]') && e.dataTransfer) {
        e.dataTransfer.setData('text/plain', '');
        e.dataTransfer.effectAllowed = 'move';
    }
});
