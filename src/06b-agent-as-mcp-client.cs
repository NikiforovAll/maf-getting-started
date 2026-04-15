#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:package ModelContextProtocol@1.0.0
#:package Microsoft.Extensions.Logging.Console@10.0.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

Console.WriteLine("Connecting to Microsoft Learn MCP...");
await using var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(
        new()
        {
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
            Name = "Microsoft Learn MCP",
            TransportMode = HttpTransportMode.StreamableHttp,
        }
    ),
    loggerFactory: loggerFactory
);

Console.WriteLine("Listing tools...");
IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();
Console.WriteLine($"Discovered {mcpTools.Count} tools:");
foreach (var tool in mcpTools)
    Console.WriteLine($"  - {tool.Name}");

AIAgent agent = client
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .UseLogging(loggerFactory)
    .Build()
    .AsAIAgent(
        instructions: "You answer questions using Microsoft Learn documentation tools.",
        name: "DocsAgent",
        loggerFactory: loggerFactory,
        tools: [.. mcpTools.Cast<AITool>()]
    );

Console.WriteLine("Running agent...");
Console.WriteLine(await agent.RunAsync("What is Microsoft Agent Framework?"));
