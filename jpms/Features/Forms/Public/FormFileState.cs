namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// One file a person chose for a question, as its line in the list under the button reads: on its
/// way, arrived (with the id the form is sent with), or the sentence that says why it did not.
/// </summary>
public sealed class FormFileState
{
    public FormFileState(string fileName, string? formUploadId = null)
    {
        FileName = fileName;
        FormUploadId = formUploadId;
    }

    public string FileName { get; private set; }

    public string? FormUploadId { get; private set; }

    public string? Problem { get; private set; }

    public bool IsUploaded => FormUploadId is not null;

    public string Line => Problem ?? (IsUploaded ? FormSheetWording.Uploaded(FileName) : FormSheetWording.Uploading(FileName));

    public void Arrived(string formUploadId, string storedName)
    {
        FormUploadId = formUploadId;
        FileName = storedName;
        Problem = null;
    }

    public void Failed(string problem) => Problem = problem;
}
