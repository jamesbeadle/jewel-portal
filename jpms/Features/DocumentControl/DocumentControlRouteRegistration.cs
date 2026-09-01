using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Features.DocumentControl;

public static class DocumentControlRouteRegistration
{
    public static void RegisterDocumentControlRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>>(
            QueryRoute.Static("/api/document-control/items"));

        queries.Register<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>>(
            new QueryRoute("/api/finance/payment-certificates",
                query => ((ListPaymentCertificates)query).ProjectId is { Length: > 0 } projectId
                    ? $"/api/finance/payment-certificates?projectId={Uri.EscapeDataString(projectId)}"
                    : "/api/finance/payment-certificates"));

        commands.Register<SendAttachmentsToDocumentControl, IReadOnlyList<DocumentControlItem>>(
            CommandRoute.Post("/api/document-control/send"));

        commands.Register<FileDocumentAsDrawing, DocumentControlItem>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/file-as-drawing",
                command => $"/api/document-control/items/{((FileDocumentAsDrawing)command).DocumentControlItemId}/file-as-drawing"));

        commands.Register<FileDocumentAsPaymentCertificate, DocumentControlItem>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/file-as-payment-certificate",
                command => $"/api/document-control/items/{((FileDocumentAsPaymentCertificate)command).DocumentControlItemId}/file-as-payment-certificate"));

        commands.Register<FileDocumentToSubcontractor, DocumentControlItem>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/file-to-subcontractor",
                command => $"/api/document-control/items/{((FileDocumentToSubcontractor)command).DocumentControlItemId}/file-to-subcontractor"));

        commands.Register<DiscardDocumentControlItem, DocumentControlItem>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/discard",
                command => $"/api/document-control/items/{((DiscardDocumentControlItem)command).DocumentControlItemId}/discard"));

        commands.Register<RestoreDocumentControlItem, DocumentControlItem>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/restore",
                command => $"/api/document-control/items/{((RestoreDocumentControlItem)command).DocumentControlItemId}/restore"));

        commands.Register<ExtractDocumentControlArchive, IReadOnlyList<DocumentControlItem>>(
            new CommandRoute("POST", "/api/document-control/items/{itemId}/extract-archive",
                command => $"/api/document-control/items/{((ExtractDocumentControlArchive)command).DocumentControlItemId}/extract-archive"));
    }
}
