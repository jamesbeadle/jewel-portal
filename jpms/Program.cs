using Jewel.JPMS;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Agents;
using Jewel.JPMS.Features.Architects;
using Jewel.JPMS.Features.Boq;
using Jewel.JPMS.Features.ValuationInvoices;
using Jewel.JPMS.Features.Cashflow;
using Jewel.JPMS.Features.Clients;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Requests;
using Jewel.JPMS.Features.Retention;
using Jewel.JPMS.Features.Closeout;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Labour;
using Jewel.JPMS.Features.CommercialInputs;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Features.Directory;
using Jewel.JPMS.Features.Drawings;
using Jewel.JPMS.Features.Hs;
using Jewel.JPMS.Features.Lads;
using Jewel.JPMS.Features.Leads;
using Jewel.JPMS.Features.Mobilisation;
using Jewel.JPMS.Features.Parties;
using Jewel.JPMS.Features.Portal;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Rates;
using Jewel.JPMS.Features.Site;
using Jewel.JPMS.Features.Subcontractors;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Xero;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(serviceProvider => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddCqrsTransport();
builder.Services.AddDirectoryReadModels();
builder.Services.AddProjectsReadModels();
builder.Services.AddLeadsReadModels();
builder.Services.AddBoqReadModels();
builder.Services.AddRatesReadModels();
builder.Services.AddDrawingsReadModels();
builder.Services.AddProcurementReadModels();
builder.Services.AddSubcontractorsReadModels();
builder.Services.AddPortalReadModels();
builder.Services.AddHsReadModels();
builder.Services.AddMobilisationReadModels();
builder.Services.AddSiteReadModels();
builder.Services.AddCommercialReadModels();
builder.Services.AddLabourReadModels();
builder.Services.AddCashflowReadModels();
builder.Services.AddCvrReadModels();
builder.Services.AddCloseoutReadModels();
builder.Services.AddRequestsReadModels();
builder.Services.AddClientsReadModels();
builder.Services.AddArchitectsReadModels();
builder.Services.AddCostCentersReadModels();
builder.Services.AddAgentsReadModels();
builder.Services.AddXeroReadModels();

builder.Services.AddScoped<IUserDirectory, HttpUserDirectory>();
builder.Services.AddScoped<IAccessRequestStore, HttpAccessRequestStore>();

builder.Services.AddScoped<ILeadStore, HttpLeadStore>();
builder.Services.AddScoped<IRateLibrary, HttpRateLibrary>();
builder.Services.AddScoped<IBoqStore, HttpBoqStore>();
builder.Services.AddScoped<IDrawingStore, HttpDrawingStore>();
builder.Services.AddScoped<ISubcontractorStore, HttpSubcontractorStore>();
builder.Services.AddScoped<IPortalStore, HttpPortalStore>();
builder.Services.AddScoped<IHsRegister, HttpHsRegister>();
builder.Services.AddScoped<IProcurementStore, HttpProcurementStore>();
builder.Services.AddScoped<IMobilisationStore, HttpMobilisationStore>();
builder.Services.AddScoped<IRequestRegister, HttpRequestRegister>();
builder.Services.AddScoped<IClientStore, HttpClientStore>();
builder.Services.AddScoped<IArchitectStore, HttpArchitectStore>();
builder.Services.AddScoped<ICorrespondenceStore, HttpCorrespondenceStore>();
builder.Services.AddScoped<IVariationStore, HttpVariationStore>();
builder.Services.AddScoped<IValuationInvoiceStore, HttpValuationInvoiceStore>();
builder.Services.AddScoped<IIntakeQueue, HttpIntakeQueue>();
builder.Services.AddScoped<ITodoStore, HttpTodoStore>();
builder.Services.AddScoped<ISiteStore, HttpSiteStore>();
builder.Services.AddScoped<ICommercialStore, HttpCommercialStore>();
builder.Services.AddScoped<ILabourStore, HttpLabourStore>();
builder.Services.AddScoped<IValuationReportStore, HttpValuationReportStore>();
builder.Services.AddScoped<ICvrStore, HttpCvrStore>();
builder.Services.AddScoped<ICommercialInputsStore, HttpCommercialInputsStore>();
builder.Services.AddScoped<IProjectRetentionStore, HttpProjectRetentionStore>();
builder.Services.AddScoped<ICloseoutStore, HttpCloseoutStore>();
builder.Services.AddScoped<ICostCenterStore, HttpCostCenterStore>();
builder.Services.AddScoped<IAgentDesk, HttpAgentDesk>();
builder.Services.AddScoped<IXeroTransactionStore, HttpXeroTransactionStore>();
builder.Services.AddScoped<IXeroLedgerStore, HttpXeroLedgerStore>();

builder.Services.AddScoped<StoreChangeHub>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserInviteService>();
builder.Services.AddScoped<ActiveRoleStorage>();
builder.Services.AddScoped<AllocationTabStorage>();
builder.Services.AddScoped<SessionService>();

var app = builder.Build();

using (var routeScope = app.Services.CreateScope())
{
    var queryRoutes = routeScope.ServiceProvider.GetRequiredService<QueryRouteTable>();
    var commandRoutes = routeScope.ServiceProvider.GetRequiredService<CommandRouteTable>();
    DirectoryRouteRegistration.RegisterDirectoryRoutes(queryRoutes, commandRoutes);
    ProjectsRouteRegistration.RegisterProjectsRoutes(queryRoutes, commandRoutes);
    LeadsRouteRegistration.RegisterLeadsRoutes(queryRoutes, commandRoutes);
    BoqRouteRegistration.RegisterBoqRoutes(queryRoutes, commandRoutes);
    RatesRouteRegistration.RegisterRatesRoutes(queryRoutes, commandRoutes);
    DrawingsRouteRegistration.RegisterDrawingsRoutes(queryRoutes, commandRoutes);
    ProcurementRouteRegistration.RegisterProcurementRoutes(queryRoutes, commandRoutes);
    SubcontractorsRouteRegistration.RegisterSubcontractorsRoutes(queryRoutes, commandRoutes);
    PortalRouteRegistration.RegisterPortalRoutes(queryRoutes, commandRoutes);
    HsRouteRegistration.RegisterHsRoutes(queryRoutes, commandRoutes);
    MobilisationRouteRegistration.RegisterMobilisationRoutes(queryRoutes, commandRoutes);
    SiteRouteRegistration.RegisterSiteRoutes(queryRoutes, commandRoutes);
    CommercialRouteRegistration.RegisterCommercialRoutes(queryRoutes, commandRoutes);
    LabourRouteRegistration.RegisterLabourRoutes(queryRoutes, commandRoutes);
    CommercialInputsRouteRegistration.RegisterCommercialInputsRoutes(queryRoutes, commandRoutes);
    RetentionRouteRegistration.RegisterRetentionRoutes(queryRoutes, commandRoutes);
    CashflowRouteRegistration.RegisterCashflowRoutes(queryRoutes, commandRoutes);
    CvrRouteRegistration.RegisterCvrRoutes(queryRoutes, commandRoutes);
    CloseoutRouteRegistration.RegisterCloseoutRoutes(queryRoutes, commandRoutes);
    RequestsRouteRegistration.RegisterRequestsRoutes(queryRoutes, commandRoutes);
    ClientsRouteRegistration.RegisterClientsRoutes(queryRoutes, commandRoutes);
    ArchitectsRouteRegistration.RegisterArchitectsRoutes(queryRoutes, commandRoutes);
    PartiesRouteRegistration.RegisterPartiesRoutes(queryRoutes, commandRoutes);
    VariationsRouteRegistration.RegisterVariationsRoutes(queryRoutes, commandRoutes);
    ValuationInvoicesRouteRegistration.RegisterValuationInvoicesRoutes(queryRoutes, commandRoutes);
    RecordLinksRouteRegistration.RegisterRecordLinksRoutes(queryRoutes, commandRoutes);
    TodosRouteRegistration.RegisterTodosRoutes(queryRoutes, commandRoutes);
    LadsRouteRegistration.RegisterLadsRoutes(queryRoutes, commandRoutes);
    CostCentersRouteRegistration.RegisterCostCentersRoutes(queryRoutes, commandRoutes);
    AgentsRouteRegistration.RegisterAgentsRoutes(queryRoutes, commandRoutes);
    XeroRouteRegistration.RegisterXeroRoutes(queryRoutes, commandRoutes);
}

await app.RunAsync();
