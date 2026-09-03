// Helpers for the RecipientInput component (the composers' To/Cc/Bcc chip pickers): the typed
// text lives in the DOM between renders, so committing it to a chip has to clear the box
// explicitly (re-rendering value="" is a no-op when the last render already said ""), and a
// click on the chip row's padding focuses the real input.
window.jpmsRecipientInput = {
    clear: element => { if (element) element.value = ""; },
    focus: element => element?.focus()
};
