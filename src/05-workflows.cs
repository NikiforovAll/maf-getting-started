#:package Microsoft.Agents.AI.Workflows@1.0.0-rc2
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OpenAI.Chat;

// --- Part 1: Pure function workflow (no LLM) ---

Console.WriteLine("=== Part 1: Text Processing Workflow ===\n");

Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

Func<string, string> reverseFunc = s => string.Concat(s.Reverse());
var reverse = reverseFunc.BindAsExecutor("ReverseTextExecutor");

WorkflowBuilder builder = new(uppercase);
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();

await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}

// --- Part 2: Agent-based sequential workflow ---

Console.WriteLine("\n=== Part 2: Agent Workflow ===\n");

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

AIAgent writer = client
    .GetChatClient(deploymentName)
    .AsAIAgent(instructions: "You write short creative stories in 2-3 sentences.", name: "Writer");

AIAgent critic = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You review stories and give brief constructive feedback in 1-2 sentences.",
        name: "Critic"
    );

var agentWorkflow = AgentWorkflowBuilder.BuildSequential("story-pipeline", [writer, critic]);

await using Run agentRun = await InProcessExecution.RunAsync(
    agentWorkflow,
    "Write a story about a robot learning to paint."
);

foreach (WorkflowEvent evt in agentRun.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"[{executorComplete.ExecutorId}]: {executorComplete.Data}");
    }
}
