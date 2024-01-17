using Microsoft.SemanticKernel.ChatCompletion;
using Server;
using Server.Helpers;
using Server.Models;
using Server.Services;
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(builder.Configuration.GetSection(nameof(Configuration.AzureOpenAI)).Get<Configuration.AzureOpenAI>());
builder.Services.AddSingleton(builder.Configuration.GetSection(nameof(Configuration.AzureAISearch)).Get<Configuration.AzureAISearch>());

builder.Services.AddSingleton<AzureAIChatCompletionService>();
builder.Services.AddSingleton<AzureAIChatCompletionWithDataService>();

builder.Services.AddSingleton<AzureAIMemoryService>();
builder.Services.AddSingleton<AzureAISearchService>();

builder.Services.AddSingleton(new ChatHistory(
            /*
                    """
                    You are an AI assistant that helps people find information.
                    You NEVER respond about topics other than provided additional information and context.
                    Your job is to answer  questions.
                    You try to be concise and only provide longer responses if necessary.
                    If someone asks a question about anything other than provided additional information and context,
                    you refuse to answer, and you instead ask if there's a topic related to the additional information and context you can assist with.
                    """
            */
            ));

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