using Server;
using Server.Helpers;
using Server.Models;
using Server.Services;
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
});

builder.Services.AddSingleton(builder.Configuration.GetSection(nameof(Configuration.AzureOpenAI)).Get<Configuration.AzureOpenAI>());
builder.Services.AddSingleton(builder.Configuration.GetSection(nameof(Configuration.AzureAISearch)).Get<Configuration.AzureAISearch>());

builder.Services.AddSingleton<AzureAIChatCompletionService>();
builder.Services.AddSingleton<AzureAIMemoryService>();
builder.Services.AddSingleton<AzureAISearchService>();

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.RegisterChatEndpoints();

app.Run();