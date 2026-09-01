// One home for the usings every page and component shares — instead of the same
// block repeated at the top of every code-behind partial.
global using System.Net.Http;
global using System.Net.Http.Json;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.JSInterop;
global using Jewel.JPMS.Components;
global using Jewel.JPMS.Cqrs;
global using Jewel.JPMS.Models;
global using Jewel.JPMS.Services;
global using Jewel.JPMS.Services.Navigation;
global using Jewel.JPMS.Services.Excel;
global using static Jewel.JPMS.MoneyFormats;
global using static Jewel.JPMS.FileSizeFormat;
global using static Jewel.JPMS.DateFormats;
global using Jewel.JPMS.Contracts.Ai;
global using Jewel.JPMS.Contracts.Commercial;
global using Jewel.JPMS.Contracts.Cqrs;
global using Jewel.JPMS.Contracts.Procurement;
global using Jewel.JPMS.Contracts.RecordLinks;
global using Jewel.JPMS.Contracts.Requests;
global using Jewel.JPMS.Contracts.Xero;
global using Microsoft.Extensions.DependencyInjection;
