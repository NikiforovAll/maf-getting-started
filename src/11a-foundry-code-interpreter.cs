#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc2
#:package Azure.AI.Projects@1.2.0-beta.5
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:property EnablePreviewFeatures=true

using System.Text;
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

const string AgentName = "CodeInterpreterAgent";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// HostedCodeInterpreterTool — Python sandbox execution on the server
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: AgentName,
    instructions: "You are a math tutor. Write and run Python code to solve problems.",
    tools: [new HostedCodeInterpreterTool() { Inputs = [] }]
);

Console.WriteLine("--- Code Interpreter ---");
AgentResponse response = await agent.RunAsync(
    "Solve the equation: x^3 - 6x^2 + 11x - 6 = 0. Plot the function and mark the roots."
);

Console.WriteLine($"Answer: {response.Text}\n");

// Extract the Python code that was executed
var toolCall = response
    .Messages.SelectMany(m => m.Contents)
    .OfType<CodeInterpreterToolCallContent>()
    .FirstOrDefault();

if (toolCall?.Inputs is not null)
{
    var codeInput = toolCall.Inputs.OfType<DataContent>().FirstOrDefault();
    if (codeInput?.HasTopLevelMediaType("text") ?? false)
    {
        Console.WriteLine("--- Python Code ---");
        Console.WriteLine(Encoding.UTF8.GetString(codeInput.Data.ToArray()));
    }
}

// Extract code execution result
var toolResult = response
    .Messages.SelectMany(m => m.Contents)
    .OfType<CodeInterpreterToolResultContent>()
    .FirstOrDefault();

if (toolResult?.Outputs?.OfType<TextContent>().FirstOrDefault() is { } result)
{
    Console.WriteLine($"\n--- Code Output ---\n{result.Text}");
}

// Check for generated file annotations (e.g., plot images)
foreach (
    var annotation in response
        .Messages.SelectMany(m => m.Contents)
        .SelectMany(c => c.Annotations ?? [])
)
{
    if (annotation.RawRepresentation is TextAnnotationUpdate citation)
    {
        Console.WriteLine($"\nGenerated file: {citation.OutputFileId}");
    }
}

await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
Console.WriteLine("\nAgent deleted.");
