#:package Microsoft.Agents.AI.Workflows@1.0.0-rc2
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var chatClient = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();

AIAgent writer = chatClient.AsAIAgent(
    instructions: "You write short creative stories in 2-3 sentences.",
    name: "Writer"
);

AIAgent critic = chatClient.AsAIAgent(
    instructions: "You review stories and give brief constructive feedback in 1-2 sentences.",
    name: "Critic"
);

var agentWorkflow = AgentWorkflowBuilder.BuildSequential("story-pipeline", [writer, critic]);

List<ChatMessage> input = [new(ChatRole.User, "Write a story about a robot learning to paint.")];

await using StreamingRun agentRun = await InProcessExecution.RunStreamingAsync(
    agentWorkflow,
    input
);
await agentRun.TrySendMessageAsync(new TurnToken(emitEvents: true));

string lastExecutorId = string.Empty;
await foreach (WorkflowEvent evt in agentRun.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
    {
        if (e.ExecutorId != lastExecutorId)
        {
            lastExecutorId = e.ExecutorId;
            Console.WriteLine();
            Console.WriteLine($"[{e.ExecutorId}]:");
        }

        Console.Write(e.Update.Text);
    }
    else if (evt is WorkflowOutputEvent output)
    {
        Console.WriteLine();
    }
}
