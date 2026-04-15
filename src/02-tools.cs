#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0

using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

// --- Part 1: Function tools ---

Console.WriteLine("=== Function Tools ===\n");

AIAgent weatherAgent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful weather assistant.",
        name: "WeatherAgent",
        description: "An agent that answers weather questions.",
        tools: [AIFunctionFactory.Create(GetWeather)]
    );

Console.WriteLine(await weatherAgent.RunAsync("What is the weather like in Amsterdam?"));

// --- Part 2: Agent-as-tool ---

Console.WriteLine("\n=== Agent as Tool ===\n");

AIAgent orchestrator = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful assistant. Use the weather agent when asked about weather.",
        tools: [weatherAgent.AsAIFunction()]
    );

Console.WriteLine(await orchestrator.RunAsync("What's the weather in Amsterdam and Paris?"));

// --- Tool definitions ---

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location) =>
    $"The weather in {location} is cloudy with a high of 15°C.";
