using System.Net;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>What the public forms' API answered: the value, the sentence to show instead, or that there is no such form.</summary>
public sealed record PublicFormAnswer<TValue>(TValue? Value, string? Problem, bool IsMissing)
{
    public static PublicFormAnswer<TValue> Missing { get; } = new(default, FormSheetWording.NotAvailable, true);

    public static PublicFormAnswer<TValue> Refused(string problem) => new(default, problem, false);
}

/// <summary>
/// The public forms' API as a stranger's page talks to it: a raw HttpClient with no sign-in, as the
/// imagine page does, because nothing here may redirect to /login. A refusal carries the sentence
/// the page shows; a lost connection reads as the dashboard's "No signal".
/// </summary>
public static class PublicFormRequests
{
    public static string FormAddress(string slug) => $"/api/public-forms/{Uri.EscapeDataString(slug)}";

    public static Task<PublicFormAnswer<PublicFormView>> OpenAsync(HttpClient http, string slug, string? inviteToken, string? packToken) =>
        AskAsync<PublicFormView>(() => http.GetAsync(FormAddress(slug) + LinkQuery(inviteToken, packToken)));

    public static string PolicyFileAddress(string slug, string? inviteToken, string? packToken) =>
        FormAddress(slug) + "/policy-file" + LinkQuery(inviteToken, packToken);

    private static string LinkQuery(string? inviteToken, string? packToken) =>
        $"?k={Uri.EscapeDataString(inviteToken ?? "")}&p={Uri.EscapeDataString(packToken ?? "")}";

    public static Task<PublicFormAnswer<PublicPackView>> OpenPackAsync(HttpClient http, string token) =>
        AskAsync<PublicPackView>(() => http.GetAsync($"/api/public-form-packs/{Uri.EscapeDataString(token)}"));

    public static Task<PublicFormAnswer<PublicFormUploadReceipt>> UploadAsync(HttpClient http, string slug, PublicFormUpload upload) =>
        AskAsync<PublicFormUploadReceipt>(() => http.PostAsJsonAsync(FormAddress(slug) + "/uploads", upload));

    public static Task<PublicFormAnswer<PublicFormReceipt>> SubmitAsync(HttpClient http, string slug, PublicFormSubmission submission) =>
        AskAsync<PublicFormReceipt>(() => http.PostAsJsonAsync(FormAddress(slug) + "/submit", submission));

    private static async Task<PublicFormAnswer<TValue>> AskAsync<TValue>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            return await AnswerFromAsync<TValue>(response);
        }
        catch (Exception unreachable) when (unreachable is HttpRequestException or TaskCanceledException)
        {
            return PublicFormAnswer<TValue>.Refused(FormWording.NoSignal);
        }
    }

    private static async Task<PublicFormAnswer<TValue>> AnswerFromAsync<TValue>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound) return PublicFormAnswer<TValue>.Missing;
        if (response.IsSuccessStatusCode) return new PublicFormAnswer<TValue>(await response.Content.ReadFromJsonAsync<TValue>(), null, false);
        var sentence = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
        return PublicFormAnswer<TValue>.Refused(sentence.Length > 0 ? sentence : FormSheetWording.CouldNotSend);
    }
}
