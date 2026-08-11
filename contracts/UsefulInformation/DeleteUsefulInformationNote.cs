using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.UsefulInformation;

public sealed record DeleteUsefulInformationNote(string UsefulInformationNoteId) : ICommand<Acknowledgement>;
