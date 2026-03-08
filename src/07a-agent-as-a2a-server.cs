#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Microsoft.Agents.AI.Hosting.A2A.AspNetCore@1.0.0-preview.260225.1
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using System.ComponentModel;
using A2A;
using A2A.AspNetCore;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient().AddLogging();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolverChain.Add(
        A2AJsonUtilities.DefaultOptions.TypeInfoResolver!
    );
    o.SerializerOptions.TypeInfoResolverChain.Add(
        A2AJsonUtilities.DefaultOptions.TypeInfoResolver!
    );
});

var app = builder.Build();

AIAgent agent = client
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful assistant.",
        name: "A2AAssistant",
        tools: [AIFunctionFactory.Create(GetWeather)]
    );

AgentCard agentCard = new()
{
    Name = "A2AAssistant",
    Description = "A helpful assistant exposed via A2A protocol.",
    Version = "1.0.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Capabilities = new() { Streaming = false },
    Skills =
    [
        new()
        {
            Id = "general",
            Name = "General Assistant",
            Description = "Answers general questions and checks weather.",
        },
    ],
};

app.MapA2A(
    agent,
    path: "/",
    agentCard: agentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/")
);

await app.RunAsync();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location) =>
    $"The weather in {location} is cloudy with a high of 15°C.";
