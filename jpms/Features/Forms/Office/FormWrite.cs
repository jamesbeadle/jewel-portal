namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// A write in flight on one of the forms' office screens: busy while it runs, and the API's own
/// sentence when it refuses (a 400/409 answer, which the dialog shows beside the fields rather than
/// as a toast). Every forms dialog holds one, so none re-types the same try/catch.
/// </summary>
public sealed class FormWrite
{
    public bool IsBusy { get; private set; }

    public string? Problem { get; private set; }

    public void Forget() => Problem = null;

    public async Task<bool> RunAsync(Func<Task> write)
    {
        IsBusy = true;
        Problem = null;
        try
        {
            await write();
            return true;
        }
        catch (CommandFailedException refusal)
        {
            Problem = refusal.Message;
            return false;
        }
        finally { IsBusy = false; }
    }
}
