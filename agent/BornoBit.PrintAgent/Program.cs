using BornoBit.PrintAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// BornoBit Local Print Agent — runs on the restaurant counter PC, dials into the cloud staff
// console's print hub, and drives the thermal receipt/kitchen printer. Windows-only (raw spooler).

// Root config/content at the binary's own folder, NOT the working directory — so appsettings.json is
// found whether launched via `dotnet run` (cwd = solution root) or as a Windows service (cwd = System32).
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.Configure<PrintAgentConfig>(builder.Configuration.GetSection(PrintAgentConfig.SectionName));
builder.Services.AddSingleton<JobProcessor>();
builder.Services.AddHostedService<HubWorker>();

// Lets the same exe run as a console app (double-click / debugging) OR an installed Windows service.
builder.Services.AddWindowsService(o => o.ServiceName = "BornoBit Print Agent");

var host = builder.Build();
await host.RunAsync();
