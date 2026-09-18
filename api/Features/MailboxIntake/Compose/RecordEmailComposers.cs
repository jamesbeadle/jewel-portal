namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// The records whose email the portal writes for them, by kind. One list, so the preview covers a
/// new record the moment its composer is registered and nothing has to remember to add it twice.
/// </summary>
public sealed class RecordEmailComposers
{
    private readonly IReadOnlyList<IComposesRecordEmail> composers;

    public RecordEmailComposers(IEnumerable<IComposesRecordEmail> composers) =>
        this.composers = composers.ToList();

    public IReadOnlyList<RecordType> Records => composers.Select(composer => composer.Record).ToList();

    public IComposesRecordEmail? Find(RecordType record) =>
        composers.FirstOrDefault(composer => composer.Record == record);

    public IComposesRecordEmail For(RecordType record) =>
        Find(record) ?? throw new InvalidOperationException(
            $"The portal doesn't compose an email for a {record} — it can only preview one it writes itself.");
}
