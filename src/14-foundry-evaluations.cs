#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc5
#:package Azure.AI.Projects@2.0.0-beta.2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.Evaluation@10.3.0
#:package Microsoft.Extensions.AI.Evaluation.Quality@10.3.0
#:package Microsoft.Extensions.AI.Evaluation.Safety@10.3.0-preview.1.26109.11
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true

using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Safety;
using Spectre.Console;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var openAiEndpoint =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var evaluatorDeployment =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

DefaultAzureCredential credential = new();
AIProjectClient aiProjectClient = new(new Uri(endpoint), credential);

// Evaluator LLM (judge)
IChatClient chatClient = new AzureOpenAIClient(new Uri(openAiEndpoint), credential)
    .GetChatClient(evaluatorDeployment)
    .AsIChatClient();

// Safety config uses Azure AI Foundry content safety endpoint
ContentSafetyServiceConfiguration safetyConfig = new(
    credential: credential,
    endpoint: new Uri(endpoint)
);
ChatConfiguration chatConfiguration = safetyConfig.ToChatConfiguration(
    originalChatConfiguration: new ChatConfiguration(chatClient)
);

const string AgentName = "EvalAgent";
const string Question = "What are the main benefits of using Azure AI Foundry?";
const string Context = """
    Azure AI Foundry is a platform for building, deploying, and managing AI applications.
    Benefits include: unified dev environment, built-in safety features (content filtering, red teaming),
    scalable infrastructure, integration with Azure services, evaluation tools for quality and safety,
    support for RAG patterns with vector search, and enterprise compliance features.
    """;

AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: AgentName,
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions =
                "You are a helpful assistant. Answer questions accurately based on the provided context.",
        }
    )
);
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

try
{
    AnsiConsole.Write(new Rule("[bold blue]Evaluations[/]").LeftJustified());

    AnsiConsole.Write(
        new Panel(Markup.Escape(Context.Trim()))
            .Header("[bold blue]Grounding Context (passed to agent prompt & evaluator)[/]")
            .BorderColor(Color.Blue)
            .Expand()
    );
    AnsiConsole.Write(
        new Panel(Markup.Escape(Question))
            .Header("[bold yellow]Question[/]")
            .BorderColor(Color.Yellow)
            .Expand()
    );

    // Get agent response
    var session = await agent.CreateSessionAsync();
    AgentResponse agentResponse = await agent.RunAsync(
        $"Context: {Context}\n\nQuestion: {Question}",
        session
    );

    AnsiConsole.Write(
        new Panel(Markup.Escape(agentResponse.Text))
            .Header("[bold green]Response[/]")
            .BorderColor(Color.Green)
            .Expand()
    );

    // Evaluate: groundedness + quality + safety
    AnsiConsole.Write(new Rule("[bold blue]Running Evaluators[/]").LeftJustified());

    CompositeEvaluator evaluator = new([
        new GroundednessEvaluator(),
        new RelevanceEvaluator(),
        new CoherenceEvaluator(),
        new ContentHarmEvaluator(),
    ]);

    List<ChatMessage> messages = [new(ChatRole.User, Question)];
    ChatResponse chatResponse = new(new ChatMessage(ChatRole.Assistant, agentResponse.Text));
    GroundednessEvaluatorContext groundingContext = new(Context);

    EvaluationResult result = await evaluator.EvaluateAsync(
        messages,
        chatResponse,
        chatConfiguration,
        additionalContext: [groundingContext]
    );

    // Display results
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Cyan1)
        .AddColumn("[bold]Metric[/]")
        .AddColumn("[bold]Score[/]")
        .AddColumn("[bold]Rating[/]")
        .AddColumn("[bold]Failed[/]")
        .Expand();

    foreach (var metric in result.Metrics.Values)
    {
        if (metric is NumericMetric n)
        {
            Color color =
                (n.Value ?? 0) >= 4.0 ? Color.Green
                : (n.Value ?? 0) >= 2.0 ? Color.Yellow
                : Color.Red;
            table.AddRow(
                Markup.Escape(n.Name),
                $"[{color}]{n.Value:F1}/5[/]",
                n.Interpretation?.Rating.ToString() ?? "",
                (n.Interpretation?.Failed ?? false) ? "[red]Yes[/]" : "[green]No[/]"
            );
        }
        else if (metric is BooleanMetric b)
        {
            table.AddRow(
                Markup.Escape(b.Name),
                b.Value?.ToString() ?? "",
                b.Interpretation?.Rating.ToString() ?? "",
                (b.Interpretation?.Failed ?? false) ? "[red]Yes[/]" : "[green]No[/]"
            );
        }
    }

    AnsiConsole.Write(
        new Panel(table)
            .Header("[bold cyan]Evaluation Results[/]")
            .BorderColor(Color.Cyan1)
            .Expand()
    );
}
finally
{
    if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/]?"))
    {
        await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
        AnsiConsole.MarkupLine("[green]Agent deleted.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]Agent kept. Remember to clean up manually.[/]");
    }
}
