using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record PostRequestMessage(
    string RequestId,
    string Body,
    MessageVisibility Visibility,
    string AuthorEmail,
    string AuthorName) : ICommand<RequestMessage>;
