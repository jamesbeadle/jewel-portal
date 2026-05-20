using Jewel.JPMS;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Mock auth + RBAC services. Replace with real OAuth + API calls later.
// Scoped == singleton in Blazor WebAssembly (one client per session), which is
// what we want here so in-memory state survives across page navigations.
builder.Services.AddScoped<IUserDirectory, AllowListUserDirectory>();
builder.Services.AddScoped<IAccessRequestStore, InMemoryAccessRequestStore>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
