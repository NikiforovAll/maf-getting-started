#:package Microsoft.Agents.AI.Workflows@1.0.0-rc4
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

// Step 1: Function executor — mask emails with regex
Func<string, string> maskEmails = text =>
    Regex.Replace(text, @"[\w.-]+@[\w.-]+\.\w+", "[EMAIL_REDACTED]");
var maskExecutor = maskEmails.BindAsExecutor("MaskEmails");

// Step 2: Adapter — bridge from function output (string) to agent input (ChatMessage + TurnToken)
var toAgentExecutor = new FunctionExecutor<string>(
    "ToAgent",
    async (string text, IWorkflowContext ctx, CancellationToken ct) =>
    {
        await ctx.SendMessageAsync(new ChatMessage(ChatRole.User, text), cancellationToken: ct);
        await ctx.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: ct);
    },
    sentMessageTypes: [typeof(ChatMessage), typeof(TurnToken)]
).BindExecutor();

// Step 3: Agent executor — LLM rewrites text while preserving redactions
AIAgent rewriter = client
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You receive text with [EMAIL_REDACTED] placeholders. "
            + "Rewrite the text to sound natural while keeping all redactions intact. "
            + "Do not invent or restore any redacted information. "
            + "Return only the rewritten text.",
        name: "Rewriter"
    );
var rewriteExecutor = rewriter.BindAsExecutor(
    new AIAgentHostOptions { ForwardIncomingMessages = false }
);

// Step 4: Adapter — bridge from agent output (List<ChatMessage>) back to string
Func<List<ChatMessage>, string> fromAgent = messages =>
    string.Join("", messages.Select(m => m.Text ?? "")).Trim();
var fromAgentExecutor = fromAgent.BindAsExecutor("FromAgent");

// Step 5: Function executor — validate no emails leaked
Func<string, string> validate = text =>
{
    var leaks = Regex.Matches(text, @"[\w.-]+@[\w.-]+\.\w+");
    return leaks.Count > 0
        ? $"VALIDATION FAILED - {leaks.Count} email(s) leaked"
        : $"CLEAN - no emails detected\n\n{text}";
};
var validateExecutor = validate.BindAsExecutor("ValidateNoLeaks");

// Build graph: mask → toAgent → rewrite → fromAgent → validate
WorkflowBuilder builder = new(maskExecutor);
builder.AddEdge(maskExecutor, toAgentExecutor);
builder.AddEdge(toAgentExecutor, rewriteExecutor);
builder.AddEdge(rewriteExecutor, fromAgentExecutor);
builder.AddEdge(fromAgentExecutor, validateExecutor);
builder.WithOutputFrom(validateExecutor);
var workflow = builder.Build();

var input = """
    Hi team, please contact Alice at alice.smith@example.com for the Q3 report.
    Bob (bob.jones@corp.net) will handle the deployment.
    CC: support@acme.io for any issues.
    """;

Console.WriteLine("=== Input ===");
Console.WriteLine(input);

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);

HashSet<string> streamedExecutors = [];
string lastExecutorId = string.Empty;
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e && !string.IsNullOrWhiteSpace(e.Update.Text))
    {
        if (e.ExecutorId != lastExecutorId)
        {
            lastExecutorId = e.ExecutorId;
            streamedExecutors.Add(e.ExecutorId);
            Console.WriteLine();
            Console.WriteLine($"[{e.ExecutorId}]:");
        }

        Console.Write(e.Update.Text);
    }
    else if (
        evt is ExecutorCompletedEvent { Data: not null } executorComplete
        && !streamedExecutors.Contains(executorComplete.ExecutorId)
    )
    {
        Console.WriteLine();
        Console.WriteLine($"[{executorComplete.ExecutorId}]:");
        Console.WriteLine(executorComplete.Data);
        Console.WriteLine();
    }
}
