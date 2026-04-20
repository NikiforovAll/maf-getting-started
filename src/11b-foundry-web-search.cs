#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc5
#:package Azure.AI.Projects@2.0.0-beta.2
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true
#:property NoWarn=OPENAI001

using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using Spectre.Console;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

const string AgentName = "WebSearchAgent";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());

// HostedWebSearchTool — Responses API web search
AgentVersion agentVersion = await aiProjectClient.Agents.CreateAgentVersionAsync(
    agentName: AgentName,
    options: new AgentVersionCreationOptions(
        new PromptAgentDefinition(deploymentName)
        {
            Instructions =
                "You are a helpful assistant that searches the web to answer questions accurately. Always cite your sources.",
            Tools = { ResponseTool.CreateWebSearchTool() },
        }
    )
);
AIAgent agent = aiProjectClient.AsAIAgent(agentVersion);

AnsiConsole.Write(new Rule("[bold blue]Web Search[/]").LeftJustified());

AgentResponse response = await agent.RunAsync("What are the latest features in .NET 10?");

AnsiConsole.Write(
    new Panel(Markup.Escape(response.Text))
        .Header("[bold green]Answer[/]")
        .BorderColor(Color.Green)
        .Expand()
);

// Extract URL citations
var citations = response
    .Messages.SelectMany(m => m.Contents)
    .SelectMany(c => c.Annotations ?? [])
    .Where(a => a.RawRepresentation is UriCitationMessageAnnotation)
    .Select(a => (UriCitationMessageAnnotation)a.RawRepresentation!)
    .ToList();

if (citations.Count > 0)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Cyan1)
        .AddColumn("[bold]Title[/]")
        .AddColumn("[bold]URL[/]")
        .Expand();

    foreach (var citation in citations)
    {
        table.AddRow(
            Markup.Escape(citation.Title ?? ""),
            $"[link={citation.Uri}]{Markup.Escape(citation.Uri?.ToString() ?? "")}[/]"
        );
    }

    AnsiConsole.Write(
        new Panel(table).Header("[bold cyan]Sources[/]").BorderColor(Color.Cyan1).Expand()
    );
}

if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/]?"))
{
    await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
    AnsiConsole.MarkupLine("[green]Agent deleted.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Agent kept. Remember to clean up manually.[/]");
}
