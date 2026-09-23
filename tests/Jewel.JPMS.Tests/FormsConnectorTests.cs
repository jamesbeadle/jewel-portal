using System.Text.Json;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The forms moved from Jeremy's forms dashboard (2026-09-23): every button on the /forms screens
// reaches the connector, anything that emails a stranger says so and confirms first, the right-to-work
// register is its readers' alone, nobody on site or outside the business reaches any of it, and a form
// read over the connector keeps its health and sensitive answers on the page.
public sealed class FormsConnectorTests
{
    private static readonly string[] Buttons =
    {
        "send_form_invite", "resend_form_invite", "cancel_form_invite", "send_form_pack", "chase_form_pack", "cancel_form_pack",
        "set_form_submission_status", "file_form_to_directory", "record_form_folder_dates", "save_right_to_work_check",
        "send_right_to_work_confirmation", "accept_training_certificate", "set_training_record_details",
        "resolve_workstation_action", "record_driving_licence_check"
    };

    private static readonly string[] Reads =
    {
        "list_form_submissions", "get_form_submission", "list_form_packs", "list_form_invites", "list_form_folders",
        "list_right_to_work_checks", "list_training_records", "list_workstation_actions", "list_driving_licence_checks",
        "list_emergency_contacts"
    };

    private static readonly string[] EmailsAStranger =
    {
        "send_form_invite", "resend_form_invite", "send_form_pack", "chase_form_pack", "send_right_to_work_confirmation"
    };

    private static SignedInUser UserWith(params Role[] roles) => new("test@jewelbb.co.uk", "Test User", roles);

    private static List<string> ToolsFor(Role role) => AiToolCatalogue.ForConnector(UserWith(role)).Select(tool => tool.Name).ToList();

    [Fact]
    public void EveryFormsButtonAndRead_reachesTheConnector()
    {
        Assert.All(Buttons, name => Assert.Equal("Forms", AiActionRegistry.Find(name)?.Area));
        Assert.All(Reads, name => Assert.Equal(AiToolKind.Read, AiToolCatalogue.Find(name)?.Kind));
        var coordinator = ToolsFor(Role.OfficeComplianceCoordinator);
        Assert.All(Reads, name => Assert.Contains(name, coordinator));
    }

    [Fact]
    public void AnythingThatEmailsAStranger_saysSo_confirmsFirst_andNamesWhoSentIt()
    {
        foreach (var name in EmailsAStranger)
        {
            var action = AiActionRegistry.Find(name)!;
            Assert.StartsWith("SENDS EMAIL", action.Description, StringComparison.Ordinal);
            Assert.True(action.RequiresConfirmation, $"{name} emails someone outside the business — confirm-first.");
            Assert.Contains("SentByEmail", action.EmailStamps);
        }
    }

    [Fact]
    public void TheRightToWorkRegister_isItsReadersAlone()
    {
        var accounts = UserWith(Role.Accounts);
        var coordinator = UserWith(Role.OfficeComplianceCoordinator);
        foreach (var name in new[] { "save_right_to_work_check", "send_right_to_work_confirmation" })
        {
            var action = AiActionRegistry.Find(name)!;
            Assert.True(action.VisibleTo.IncludesAny(coordinator.Roles));
            Assert.False(action.VisibleTo.IncludesAny(accounts.Roles));
        }
        Assert.Contains("list_right_to_work_checks", ToolsFor(Role.OfficeComplianceCoordinator));
        Assert.DoesNotContain("list_right_to_work_checks", ToolsFor(Role.Accounts));
    }

    [Theory]
    [InlineData(Role.Subcontractor)]
    [InlineData(Role.Client)]
    [InlineData(Role.Architect)]
    [InlineData(Role.Foreman)]
    [InlineData(Role.SiteOperative)]
    public void NoOneOnSiteOrOutsideTheBusiness_reachesTheForms(Role role)
    {
        var tools = ToolsFor(role);
        Assert.All(Reads, name => Assert.DoesNotContain(name, tools));
        Assert.All(Buttons, name => Assert.False(AiActionRegistry.Find(name)!.VisibleTo.Includes(role)));
    }

    // 2026-09-23: the site manager and the H&S officer read the Received list for the health and
    // safety forms alone — their site checks and the site's incident reports — and nothing else of
    // the office's: no packs, no links, no registers, no filing.
    [Theory]
    [InlineData(Role.SiteManager)]
    [InlineData(Role.HealthSafetyOfficer)]
    public void TheSiteRoles_readTheHealthAndSafetyFormsAlone(Role role)
    {
        var tools = ToolsFor(role);
        Assert.Contains("list_form_submissions", tools);
        Assert.Contains("get_form_submission", tools);
        Assert.All(Reads.Except(new[] { "list_form_submissions", "get_form_submission", "list_emergency_contacts" }), name => Assert.DoesNotContain(name, tools));
        Assert.All(Buttons, name => Assert.False(AiActionRegistry.Find(name)!.VisibleTo.Includes(role)));
        Assert.True(FormRoleSets.ReadersOf(FormEvidenceStore.HealthAndSafety).Includes(role));
        Assert.False(FormRoleSets.ReadersOf(FormEvidenceStore.General).Includes(role));
    }

    [Fact]
    public void AFormReadOverTheConnector_keepsTheSensitiveAndTheHealthAnswersOnThePage()
    {
        var vehicle = Read(FormSlugs.CompanyVehicle, new() { ["name"] = "Sam Smith", ["points"] = FormWording.Yes, ["points_detail"] = "SP30 in 2025" });
        var emergency = Read(FormSlugs.EmergencyContact, new() { ["name"] = "Sam Smith", ["ec_name"] = "Alex Smith" }, "medical_detail");

        Assert.Contains("Sam Smith", vehicle);
        Assert.DoesNotContain("SP30", vehicle);
        Assert.Contains("withheld from the connector", vehicle);
        Assert.Contains("Alex Smith", emergency);
        Assert.Contains("a health answer, hidden until revealed", emergency);
        Assert.DoesNotContain(AiToolCatalogue.ForConnector(UserWith(Role.Admin)), tool => tool.Name.Contains("health", StringComparison.OrdinalIgnoreCase));
    }

    private static string Read(string formSlug, Dictionary<string, string> answers, params string[] withheldKeys)
    {
        var submission = new FormSubmission("form-1", formSlug, "Sam Smith", "Sam Smith", null, true,
            "sam@example.com", "Jeremy", null, FormSubmissionStatus.New, DateTimeOffset.UtcNow, "", null);
        var view = new FormSubmissionView(submission, answers, Array.Empty<FormUploadedFile>(), withheldKeys);
        return JsonSerializer.Serialize(AiFormReading.Of(view, DateTimeOffset.UtcNow));
    }
}
