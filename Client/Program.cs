using Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromMinutes(5) });

builder.Services.AddSingleton(sp =>
{
    return new HubConnectionBuilder()
      .WithUrl($"{builder.HostEnvironment.BaseAddress}chat/byodatahub")
      .WithAutomaticReconnect()
      .Build();
});

await builder.Build().RunAsync();