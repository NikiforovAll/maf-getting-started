#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore@1.1.0-preview.260410.1
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0

using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient().AddLogging();
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
);
builder.Services.AddAGUI();

WebApplication app = builder.Build();
app.UseCors();

AIAgent agent = client
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful assistant.",
        name: "AGUIAssistant",
        description: "A helpful assistant that can answer questions and check weather.",
        tools: [AIFunctionFactory.Create(GetWeather)]
    );

app.MapAGUI("/", agent);

await app.RunAsync();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location) =>
    $"The weather in {location} is cloudy with a high of 15°C.";
