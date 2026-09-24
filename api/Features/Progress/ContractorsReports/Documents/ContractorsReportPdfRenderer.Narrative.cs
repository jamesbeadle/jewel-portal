using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

public static partial class ContractorsReportPdfRenderer
{
    private static void AddProgress(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportPrintedText.ProgressHeading(ContractorsReportSections.Progress, model.Header.ProgrammeReference));
        BodyLine(section, ContractorsReportPrintedText.ProgressOpening);
        foreach (var day in model.Progress)
        {
            DayHeading(section, day.Heading);
            foreach (var entry in day.Entries)
                foreach (var bullet in ContractorsReportPrintedText.Bullets(entry.Description)) Bullet(section, bullet);
        }
        if (model.InstructionsReceived.Count == 0) return;
        DayHeading(section, ContractorsReportPrintedText.InstructionsHeading);
        foreach (var instruction in model.InstructionsReceived) Bullet(section, instruction);
    }

    private static void BodyLine(Section section, string text)
    {
        var line = section.AddParagraph(text);
        line.Format.Font.Size = 9.5;
        SpaceAfter(line, 2.5);
    }

    private static void Bullet(Section section, string text)
    {
        var line = section.AddParagraph("•  " + text);
        line.Format.Font.Size = 9.5;
        line.Format.LeftIndent = Unit.FromMillimeter(4);
        line.Format.FirstLineIndent = Unit.FromMillimeter(-4);
        SpaceAfter(line, 1);
    }

    private static void AddLookAhead(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.LookAhead);
        var planned = ContractorsReportText.Planned(model);
        if (planned.Count == 0) { MutedLine(section, ContractorsReportText.NoLookAhead); return; }
        foreach (var item in planned) Bullet(section, item.Text);
    }

    private static void AddBuildingControl(Section section, ContractorsReportBuildingControl buildingControl)
    {
        SectionHeading(section, ContractorsReportSections.BuildingControl);
        AddContact(section, buildingControl);
        Panelled(section, ContractorsReportText.OrNothingToReport(buildingControl.Liaison));
        SpaceAfterTable(section);
    }

    private static void AddContact(Section section, ContractorsReportBuildingControl buildingControl)
    {
        if (buildingControl.HasCase()) { AddContactGrid(section, buildingControl); return; }
        if (buildingControl.EnteredContact is not { } contact) { MutedLine(section, ContractorsReportText.NoBuildingControlCase); return; }
        var line = section.AddParagraph(ContractorsReportText.BuildingControlContact(contact));
        line.Format.Font.Size = 9.5;
        SpaceAfter(line, 2.5);
    }

    private static void AddContactGrid(Section section, ContractorsReportBuildingControl buildingControl)
    {
        var table = GridTable(section);
        GridRow(table, "Body", ContractorsReportText.OrDash(buildingControl.BodyName), "Contact", ContractorsReportText.OrDash(buildingControl.ContactName));
        GridRow(table, "Email", ContractorsReportText.OrDash(buildingControl.ContactEmail), "Phone", ContractorsReportText.OrDash(buildingControl.ContactPhone));
        SpaceAfterTable(section);
    }

    private static void DayHeading(Section section, string heading)
    {
        var day = section.AddParagraph(heading);
        day.Format.Font.Size = 9;
        day.Format.Font.Bold = true;
        day.Format.Font.Color = Gold;
        day.Format.KeepWithNext = true;
        SpaceBefore(day, 3);
        SpaceAfter(day, 1.5);
    }
}
