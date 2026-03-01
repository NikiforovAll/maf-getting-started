#:package Microsoft.Agents.AI.Workflows@1.0.0-rc2
#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

var client = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential());

// Step 1: Function executor — mask emails with regex
Func<string, string> maskEmails = text =>
    Regex.Replace(text, @"[\w.-]+@[\w.-]+\.\w+", "[EMAIL_REDACTED]");
var maskExecutor = maskEmails.BindAsExecutor("MaskEmails");

// Step 2: Agent executor — wrap LLM agent call as a function executor
AIAgent rewriter = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You receive text with [EMAIL_REDACTED] placeholders. "
            + "Rewrite the text to sound natural while keeping all redactions intact. "
            + "Do not invent or restore any redacted information. "
            + "Return only the rewritten text.",
        name: "Rewriter"
    );
Func<string, ValueTask<string>> rewriteFunc = async text =>
{
    var response = await rewriter.RunAsync(text);
    return response.ToString();
};
var rewriteExecutor = rewriteFunc.BindAsExecutor("Rewriter");

// Step 3: Function executor — validate no emails leaked
Func<string, string> validate = text =>
{
    var leaks = Regex.Matches(text, @"[\w.-]+@[\w.-]+\.\w+");
    return leaks.Count > 0
        ? $"VALIDATION FAILED - {leaks.Count} email(s) leaked"
        : $"CLEAN - no emails detected\n\n{text}";
};
var validateExecutor = validate.BindAsExecutor("ValidateNoLeaks");

// Build graph: mask → rewrite → validate
WorkflowBuilder builder = new(maskExecutor);
builder.AddEdge(maskExecutor, rewriteExecutor);
builder.AddEdge(rewriteExecutor, validateExecutor);
builder.WithOutputFrom(validateExecutor);
var workflow = builder.Build();

var input = """
    Hi team, please contact Alice at alice.smith@example.com for the Q3 report.
    Bob (bob.jones@corp.net) will handle the deployment.
    CC: support@acme.io for any issues.
    """;

Console.WriteLine("=== Input ===");
Console.WriteLine(input);

await using Run run = await InProcessExecution.RunAsync(workflow, input);

foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"[{executorComplete.ExecutorId}]:");
        Console.WriteLine(executorComplete.Data);
        Console.WriteLine();
    }
}
