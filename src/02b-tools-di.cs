#:package Microsoft.Agents.AI.Hosting@1.1.0-preview.260410.1
#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.Hosting@10.0.0

using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

var builder = Host.CreateApplicationBuilder(args);

IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();
builder.Services.AddSingleton(chatClient);

// Register tool dependencies
builder.Services.AddSingleton<WeatherService>();

// Register agent with DI and add class-based tool from the container
builder
    .AddAIAgent(
        "weather-agent",
        instructions: "You are a helpful weather assistant.",
        description: "An agent that answers weather questions.",
        chatClientServiceKey: null
    )
    .WithAITool(sp => sp.GetRequiredService<WeatherService>().AsAITool());

using var host = builder.Build();

// Resolve and use the agent
var agent = host.Services.GetRequiredKeyedService<AIAgent>("weather-agent");

Console.WriteLine("--- DI-based Agent with class tool ---\n");
Console.WriteLine(await agent.RunAsync("What's the weather in Amsterdam and Paris?"));

// --- Tool as a class resolved from DI ---

internal sealed class WeatherService
{
    [Description("Get the weather for a given location.")]
    public string GetWeather([Description("The location")] string location) =>
        $"The weather in {location} is cloudy with a high of 15°C.";

    public AITool AsAITool() => AIFunctionFactory.Create(GetWeather);
}
