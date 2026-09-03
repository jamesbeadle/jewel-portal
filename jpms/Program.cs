using Jewel.JPMS;
using Jewel.JPMS.Features.Architects;
using Jewel.JPMS.Features.ArchitectInstructions;
using Jewel.JPMS.Features.Audit;
using Jewel.JPMS.Features.Boq;
using Jewel.JPMS.Features.ValuationInvoices;
using Jewel.JPMS.Features.Cashflow;
using Jewel.JPMS.Features.WeeklyCashflow;
using Jewel.JPMS.Features.Clients;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Requests;
using Jewel.JPMS.Features.Retention;
using Jewel.JPMS.Features.Closeout;
using Jewel.JPMS.Features.Calendar;
using Jewel.JPMS.Features.BuildingControl;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Labour;
using Jewel.JPMS.Features.Registers;
using Jewel.JPMS.Features.CommercialInputs;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Features.Directory;
using Jewel.JPMS.Features.DocumentControl;
using Jewel.JPMS.Features.Drawings;
using Jewel.JPMS.Features.Hs;
using Jewel.JPMS.Features.Inventory;
using Jewel.JPMS.Features.Kpi;
using Jewel.JPMS.Features.Lads;
using Jewel.JPMS.Features.Mobilisation;
using Jewel.JPMS.Features.Parties;
using Jewel.JPMS.Features.Platform;
using Jewel.JPMS.Features.ClientPortal;
using Jewel.JPMS.Features.Portal;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Progress;
using Jewel.JPMS.Features.Ai;
using Jewel.JPMS.Features.ProjectContracts;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Rates;
using Jewel.JPMS.Features.Site;
using Jewel.JPMS.Features.Subcontractors;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.UsefulInformation;
using Jewel.JPMS.Features.Xero;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(serviceProvider => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Error reporting is wired before anything else so that a failure during start-up still has
// somewhere to go. The sink is created by hand rather than resolved, because builder.Logging is
// configured before there is a service provider to resolve from.
var globalErrorSink = new GlobalErrorSink();
builder.Services.AddSingleton(globalErrorSink);
builder.Logging.AddProvider(new ErrorReportingLoggerProvider(globalErrorSink));
builder.Services.AddScoped<ErrorReporter>();
builder.Services.AddScoped<IErrorSink>(services => services.GetRequiredService<ErrorReporter>());

builder.Services.AddCqrsTransport();
builder.Services.AddDirectoryReadModels();
builder.Services.AddProjectsReadModels();
builder.Services.AddBoqReadModels();
builder.Services.AddRatesReadModels();
builder.Services.AddDrawingsReadModels();
builder.Services.AddProgressReadModels();
builder.Services.AddProcurementReadModels();
builder.Services.AddSubcontractorsReadModels();
builder.Services.AddPortalReadModels();
builder.Services.AddHsReadModels();
builder.Services.AddMobilisationReadModels();
builder.Services.AddSiteReadModels();
builder.Services.AddCommercialReadModels();
builder.Services.AddLabourReadModels();
builder.Services.AddRegistersReadModels();
builder.Services.AddCashflowReadModels();
builder.Services.AddWeeklyCashflowReadModels();
builder.Services.AddCvrReadModels();
builder.Services.AddCloseoutReadModels();
builder.Services.AddInventoryReadModels();
builder.Services.AddKpiReadModels();
builder.Services.AddCalendarReadModels();
builder.Services.AddBuildingControlReadModels();
builder.Services.AddRequestsReadModels();
builder.Services.AddClientsReadModels();
builder.Services.AddArchitectsReadModels();
builder.Services.AddCostCentersReadModels();
builder.Services.AddXeroReadModels();
builder.Services.AddRecordLinksReadModels();

builder.Services.AddScoped<IUserDirectory, HttpUserDirectory>();
builder.Services.AddScoped<IAccessRequestStore, HttpAccessRequestStore>();

builder.Services.AddScoped<IRateLibrary, HttpRateLibrary>();
builder.Services.AddScoped<IBoqStore, HttpBoqStore>();
builder.Services.AddScoped<IDrawingStore, HttpDrawingStore>();
builder.Services.AddScoped<BluebeamStatusStore>();
builder.Services.AddScoped<IDocumentControlStore, HttpDocumentControlStore>();
builder.Services.AddScoped<IPaymentCertificateStore, HttpPaymentCertificateStore>();
builder.Services.AddScoped<IProjectContractStore, HttpProjectContractStore>();
builder.Services.AddScoped<IProgressStore, HttpProgressStore>();
builder.Services.AddScoped<ISubcontractorStore, HttpSubcontractorStore>();
builder.Services.AddScoped<IPortalStore, HttpPortalStore>();
builder.Services.AddClientPortalServices();
builder.Services.AddScoped<IHsRegister, HttpHsRegister>();
builder.Services.AddScoped<IProcurementStore, HttpProcurementStore>();
builder.Services.AddScoped<IMobilisationStore, HttpMobilisationStore>();
builder.Services.AddScoped<IRequestRegister, HttpRequestRegister>();
builder.Services.AddScoped<IRequestAttachmentStore, HttpRequestAttachmentStore>();
builder.Services.AddScoped<IWorkOrderAttachmentStore, HttpWorkOrderAttachmentStore>();
builder.Services.AddScoped<IBidPackageAttachmentStore, HttpBidPackageAttachmentStore>();
builder.Services.AddScoped<ITenderEnquiryAttachmentStore, HttpTenderEnquiryAttachmentStore>();
builder.Services.AddScoped<IBuildingControlAttachmentClient, HttpBuildingControlAttachmentClient>();
builder.Services.AddScoped<ICompanyTenderTermsStore, HttpCompanyTenderTermsStore>();
builder.Services.AddScoped<IArchitectInstructionStore, HttpArchitectInstructionStore>();
builder.Services.AddScoped<IClientStore, HttpClientStore>();
builder.Services.AddScoped<IArchitectStore, HttpArchitectStore>();
builder.Services.AddScoped<ICorrespondenceStore, HttpCorrespondenceStore>();
builder.Services.AddScoped<IVariationStore, HttpVariationStore>();
builder.Services.AddScoped<IValuationInvoiceStore, HttpValuationInvoiceStore>();
builder.Services.AddScoped<IIntakeQueue, HttpIntakeQueue>();
builder.Services.AddScoped<ITodoStore, HttpTodoStore>();
builder.Services.AddScoped<IUsefulInformationStore, HttpUsefulInformationStore>();
builder.Services.AddScoped<ISiteStore, HttpSiteStore>();
builder.Services.AddScoped<ICommercialStore, HttpCommercialStore>();
builder.Services.AddScoped<ILabourStore, HttpLabourStore>();
builder.Services.AddScoped<IValuationReportStore, HttpValuationReportStore>();
builder.Services.AddScoped<IClientCostReferenceStore, HttpClientCostReferenceStore>();
builder.Services.AddScoped<ICvrStore, HttpCvrStore>();
builder.Services.AddScoped<ICommercialInputsStore, HttpCommercialInputsStore>();
builder.Services.AddScoped<IProjectRetentionStore, HttpProjectRetentionStore>();
builder.Services.AddScoped<ICloseoutStore, HttpCloseoutStore>();
builder.Services.AddScoped<ICostCenterStore, HttpCostCenterStore>();
builder.Services.AddScoped<IXeroTransactionStore, HttpXeroTransactionStore>();
builder.Services.AddScoped<IXeroCashSummaryStore, HttpXeroCashSummaryStore>();
builder.Services.AddScoped<IXeroAgedPayablesStore, HttpXeroAgedPayablesStore>();
builder.Services.AddScoped<IXeroAgedReceivablesStore, HttpXeroAgedReceivablesStore>();
builder.Services.AddScoped<IXeroLedgerStore, HttpXeroLedgerStore>();
builder.Services.AddScoped<IXeroTrackingCategoriesStore, HttpXeroTrackingCategoriesStore>();
// Admin → System: the announced app version and its publish button.
builder.Services.AddScoped<ISystemStore, HttpSystemStore>();

builder.Services.AddScoped<StoreChangeHub>();
// Watches the build number the API stamps on every response; the CQRS transport reports each
// sighting and the UpdateToast renders the "new version available" prompt it raises.
builder.Services.AddScoped<AppVersionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserInviteService>();
builder.Services.AddScoped<ActiveRoleStorage>();
builder.Services.AddScoped<AllocationTabStorage>();
builder.Services.AddScoped<WorkOrderGroupingStorage>();
builder.Services.AddScoped<TriageSortStorage>();
builder.Services.AddScoped<TodoViewStorage>();
// The "open this email in the Control Centre" handoff from the to-do searches' email results.
builder.Services.AddScoped<ControlCentreOpenEmail>();
// INTERIM (2026-08-11): the Cash Forecast's per-browser overheads figure — replaced by an
// FD-owned server setting once the forecast's phasing rules are signed off.
builder.Services.AddScoped<ForecastOverheadsStorage>();
builder.Services.AddScoped<CurrentProjectService>();
builder.Services.AddScoped<ProjectStageFilter>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ExcelExportService>();

var app = builder.Build();

using (var routeScope = app.Services.CreateScope())
{
    var queryRoutes = routeScope.ServiceProvider.GetRequiredService<QueryRouteTable>();
    var commandRoutes = routeScope.ServiceProvider.GetRequiredService<CommandRouteTable>();
    DirectoryRouteRegistration.RegisterDirectoryRoutes(queryRoutes, commandRoutes);
    ProjectsRouteRegistration.RegisterProjectsRoutes(queryRoutes, commandRoutes);
    ProjectContractsRouteRegistration.RegisterProjectContractsRoutes(queryRoutes, commandRoutes);
    AiRouteRegistration.RegisterAiRoutes(queryRoutes, commandRoutes);
    BoqRouteRegistration.RegisterBoqRoutes(queryRoutes, commandRoutes);
    RatesRouteRegistration.RegisterRatesRoutes(queryRoutes, commandRoutes);
    DrawingsRouteRegistration.RegisterDrawingsRoutes(queryRoutes, commandRoutes);
    Jewel.JPMS.Features.Bluebeam.BluebeamRouteRegistration.RegisterBluebeamRoutes(queryRoutes, commandRoutes);
    DocumentControlRouteRegistration.RegisterDocumentControlRoutes(queryRoutes, commandRoutes);
    ProgressRouteRegistration.RegisterProgressRoutes(queryRoutes, commandRoutes);
    ProcurementRouteRegistration.RegisterProcurementRoutes(queryRoutes, commandRoutes);
    Jewel.JPMS.Features.TenderEnquiries.TenderEnquiriesRouteRegistration.RegisterTenderEnquiriesRoutes(queryRoutes, commandRoutes);
    SubcontractorsRouteRegistration.RegisterSubcontractorsRoutes(queryRoutes, commandRoutes);
    PortalRouteRegistration.RegisterPortalRoutes(queryRoutes, commandRoutes);
    Jewel.JPMS.Features.ClientPortal.ClientPortalRouteRegistration.RegisterClientPortalRoutes(queryRoutes, commandRoutes);
    HsRouteRegistration.RegisterHsRoutes(queryRoutes, commandRoutes);
    MobilisationRouteRegistration.RegisterMobilisationRoutes(queryRoutes, commandRoutes);
    SiteRouteRegistration.RegisterSiteRoutes(queryRoutes, commandRoutes);
    CommercialRouteRegistration.RegisterCommercialRoutes(queryRoutes, commandRoutes);
    LabourRouteRegistration.RegisterLabourRoutes(queryRoutes, commandRoutes);
    Jewel.JPMS.Features.Registers.RegistersRouteRegistration.RegisterRegistersRoutes(queryRoutes, commandRoutes);
    CommercialInputsRouteRegistration.RegisterCommercialInputsRoutes(queryRoutes, commandRoutes);
    RetentionRouteRegistration.RegisterRetentionRoutes(queryRoutes, commandRoutes);
    CashflowRouteRegistration.RegisterCashflowRoutes(queryRoutes, commandRoutes);
    WeeklyCashflowRouteRegistration.RegisterWeeklyCashflowRoutes(queryRoutes, commandRoutes);
    CvrRouteRegistration.RegisterCvrRoutes(queryRoutes, commandRoutes);
    CloseoutRouteRegistration.RegisterCloseoutRoutes(queryRoutes, commandRoutes);
    InventoryRouteRegistration.RegisterInventoryRoutes(queryRoutes, commandRoutes);
    KpiRouteRegistration.RegisterKpiRoutes(queryRoutes, commandRoutes);
    CalendarRouteRegistration.RegisterCalendarRoutes(queryRoutes, commandRoutes);
    BuildingControlRouteRegistration.RegisterBuildingControlRoutes(queryRoutes, commandRoutes);
    RequestsRouteRegistration.RegisterRequestsRoutes(queryRoutes, commandRoutes);
    ArchitectInstructionsRouteRegistration.RegisterArchitectInstructionsRoutes(queryRoutes, commandRoutes);
    ClientsRouteRegistration.RegisterClientsRoutes(queryRoutes, commandRoutes);
    ArchitectsRouteRegistration.RegisterArchitectsRoutes(queryRoutes, commandRoutes);
    PartiesRouteRegistration.RegisterPartiesRoutes(queryRoutes, commandRoutes);
    VariationsRouteRegistration.RegisterVariationsRoutes(queryRoutes, commandRoutes);
    ValuationInvoicesRouteRegistration.RegisterValuationInvoicesRoutes(queryRoutes, commandRoutes);
    RecordLinksRouteRegistration.RegisterRecordLinksRoutes(queryRoutes, commandRoutes);
    AuditRouteRegistration.RegisterAuditRoutes(queryRoutes, commandRoutes);
    TodosRouteRegistration.RegisterTodosRoutes(queryRoutes, commandRoutes);
    UsefulInformationRouteRegistration.RegisterUsefulInformationRoutes(queryRoutes, commandRoutes);
    LadsRouteRegistration.RegisterLadsRoutes(queryRoutes, commandRoutes);
    CostCentersRouteRegistration.RegisterCostCentersRoutes(queryRoutes, commandRoutes);
    XeroRouteRegistration.RegisterXeroRoutes(queryRoutes, commandRoutes);
    PlatformRouteRegistration.RegisterPlatformRoutes(queryRoutes, commandRoutes);
}

await app.RunAsync();
