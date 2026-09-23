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
    public static string FormAddress(string companyCode, string slug) =>
        $"/api/public-forms/{Uri.EscapeDataString(companyCode)}/{Uri.EscapeDataString(slug)}";

    public static Task<PublicFormAnswer<PublicFormView>> OpenAsync(
        HttpClient http, string companyCode, string slug, string? inviteToken, string? packToken)
    {
        var query = $"?k={Uri.EscapeDataString(inviteToken ?? "")}&p={Uri.EscapeDataString(packToken ?? "")}";
        return AskAsync<PublicFormView>(() => http.GetAsync(FormAddress(companyCode, slug) + query));
    }

    public static Task<PublicFormAnswer<PublicPackView>> OpenPackAsync(HttpClient http, string companyCode, string token) =>
        AskAsync<PublicPackView>(() =>
            http.GetAsync($"/api/public-form-packs/{Uri.EscapeDataString(companyCode)}/{Uri.EscapeDataString(token)}"));

    public static Task<PublicFormAnswer<PublicFormUploadReceipt>> UploadAsync(
        HttpClient http, string companyCode, string slug, PublicFormUpload upload) =>
        AskAsync<PublicFormUploadReceipt>(() => http.PostAsJsonAsync(FormAddress(companyCode, slug) + "/uploads", upload));

    public static Task<PublicFormAnswer<PublicFormReceipt>> SubmitAsync(
        HttpClient http, string companyCode, string slug, PublicFormSubmission submission) =>
        AskAsync<PublicFormReceipt>(() => http.PostAsJsonAsync(FormAddress(companyCode, slug) + "/submit", submission));

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
