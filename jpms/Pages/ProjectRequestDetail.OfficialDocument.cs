using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // ---- Official document form (itemised queries + narrative sections) ------------------------

    /// <summary>A mutable editor row for one itemised query; positions are re-minted on save.</summary>
    private sealed class FormItemRow
    {
        public string DrawingRef { get; set; } = "";
        public string MemberArea { get; set; } = "";
        public string Query { get; set; } = "";
        public string Response { get; set; } = "";
    }

    private void OpenFormEditor()
    {
        if (record is null) return;
        // The form lives on the official pane; opening its editor from the Actions menu must land
        // the user in front of it rather than editing a panel on the tab they are not looking at.
        if (HasOfficialTab) activeTab = "official";
        formItems = record.ItemList
            .OrderBy(item => item.Position)
            .Select(item => new FormItemRow
            {
                DrawingRef = item.DrawingRef,
                MemberArea = item.MemberArea,
                Query = item.Query,
                Response = item.Response ?? ""
            })
            .ToList();
        if (formItems.Count == 0) formItems.Add(new FormItemRow());
        formBasis = record.BasisOfQueries ?? "";
        formResponseAction = record.ResponseActionRequired ?? "";
        formImpact = record.ImpactIfLate ?? "";
        formError = null;
        editingForm = true;
    }

    private void CancelFormEdit()
    {
        editingForm = false;
        formError = null;
    }

    private void AddFormItem() => formItems.Add(new FormItemRow());

    private void RemoveFormItem(FormItemRow row) => formItems.Remove(row);

    private async Task SaveForm()
    {
        if (record is null || busy || !CanEditDetails) return;
        formError = null;

        var items = formItems
            .Select(row => new RequestItemDraft(
                row.DrawingRef.Trim(),
                row.MemberArea.Trim(),
                row.Query.Trim(),
                NullIfBlank(row.Response)))
            .ToList();

        var command = new UpdateRequestForm(
            record.RequestId,
            NullIfBlank(formBasis),
            NullIfBlank(formResponseAction),
            NullIfBlank(formImpact),
            items);

        try
        {
            busy = true;
            record = await RequestRegister.SaveFormAsync(command);
            editingForm = false;
        }
        catch (CommandFailedException ex)
        {
            formError = ex.Message;
        }
        catch
        {
            formError = "Couldn't save the form. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

}
