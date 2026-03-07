#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc2
#:package Azure.AI.Projects@1.2.0-beta.5
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:property EnablePreviewFeatures=true

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

const string AgentName = "WebSearchAgent";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// HostedWebSearchTool — Responses API web search
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: AgentName,
    model: deploymentName,
    instructions: "You are a helpful assistant that searches the web to answer questions accurately. Always cite your sources.",
    tools: [new HostedWebSearchTool()]
);

Console.WriteLine("--- Web Search ---");
AgentResponse response = await agent.RunAsync("What are the latest features in .NET 10?");

Console.WriteLine($"Answer: {response.Text}\n");

// Extract URL citations
Console.WriteLine("--- Sources ---");
foreach (
    var annotation in response
        .Messages.SelectMany(m => m.Contents)
        .SelectMany(c => c.Annotations ?? [])
)
{
    if (annotation.RawRepresentation is UriCitationMessageAnnotation urlCitation)
    {
        Console.WriteLine($"  - {urlCitation.Title}: {urlCitation.Uri}");
    }
}

await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
Console.WriteLine("\nAgent deleted.");
