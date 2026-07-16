using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The emails currently tagged to one to-do item ("JPMS/TODO-####", the item's reference as tag
// stem), read live by tag via the record-link layer. The tag is the only association — nothing is
// stored — so this reflects whatever is tagged to the item now. Feeds the linked-mail list in the
// to-do detail modal.
public sealed record ListTodoEmails(string TodoItemId) : IQuery<IReadOnlyList<MailboxMessage>>;
