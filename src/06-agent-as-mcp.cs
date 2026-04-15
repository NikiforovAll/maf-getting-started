#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.Hosting@10.0.0
#:package ModelContextProtocol@1.0.0

using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

AIAgent joker = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are good at telling jokes.",
        name: "Joker",
        description: "An agent that tells jokes on any topic."
    );

AIAgent weatherAgent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful weather assistant.",
        name: "WeatherAgent",
        description: "An agent that answers weather questions.",
        tools: [AIFunctionFactory.Create(GetWeather)]
    );

var jokerTool = McpServerTool.Create(joker.AsAIFunction());
var weatherTool = McpServerTool.Create(weatherAgent.AsAIFunction());

var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools([jokerTool, weatherTool]);

await builder.Build().RunAsync();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location) =>
    $"The weather in {location} is cloudy with a high of 15°C.";
