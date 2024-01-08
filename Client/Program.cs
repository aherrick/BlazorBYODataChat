using Client;
using Client.Helpers;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSweetAlert2(options =>
{
    options.Theme = SweetAlertTheme.Dark;
});

builder.Services.AddScoped<CustomSweetAlertService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromMinutes(5) });

builder.Services.AddScoped<CustomHttpClient>();

await builder.Build().RunAsync();