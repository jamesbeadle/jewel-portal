using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// The vehicle accident report, used at the roadside on a phone. The questions are the dashboard's
/// as they stood; the page keeps a half-written report and its photographs on the phone until it is
/// sent, so a dropped signal loses nothing.
/// </summary>
public static class VehicleAccidentReportForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.AccidentReport,
        "Vehicle Accident Report",
        "Report a vehicle accident. Give as much detail as possible, including a statement, photos, and "
            + "the location and time.",
        new[]
        {
            Text("name", "Your Full Name", Required),
            Text("reg", "Vehicle Registration Number", Required),
            LongText("statement", "Accident Statement", Required, "A detailed statement of what happened."),
            Upload("photos", "Photos", Required, "Photos of the scene, vehicles involved, or any relevant evidence."),
            Text("location", "Location of the accident", Required, "Road name, junction or postcode."),
            DateAndTime("when", "Date and time of the accident", Required),
            Choice("third_party", "Do you have third party details?", Required, new[] { "No", "Yes" }),
            LongText("third_party_detail", "If yes: their name, contact details, registration and insurer", Optional)
        });
}
