#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.AI.Projects.OpenAI@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:package OpenAI@2.8.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true
#:property NoWarn=OPENAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Assistants;
using OpenAI.Files;
using Spectre.Console;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

const string AgentName = "RAGAgent";

AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());
var projectOpenAIClient = aiProjectClient.GetProjectOpenAIClient();
var filesClient = projectOpenAIClient.GetProjectFilesClient();
var vectorStoresClient = projectOpenAIClient.GetProjectVectorStoresClient();

// 1. Create and upload a knowledge base file
string tempFile = Path.Combine(Path.GetTempPath(), "contoso-products.txt");
File.WriteAllText(
    tempFile,
    """
    Contoso Product Catalog:

    - Contoso CloudSync Pro ($29/month)
      Enterprise file synchronization with end-to-end encryption.
      Supports up to 500 users. Includes 1TB shared storage.

    - Contoso DevOps Suite ($99/month)
      CI/CD pipeline management with built-in testing frameworks.
      Integrates with GitHub, Azure DevOps, and GitLab.

    - Contoso AI Assistant ($49/month)
      AI-powered customer support chatbot.
      Supports 50+ languages. Custom training on your knowledge base.

    - Contoso SecureVault ($19/month)
      Password management and secrets storage for teams.
      Hardware key support. SOC 2 Type II certified.
    """
);

AnsiConsole.Write(new Rule("[bold blue]RAG with File Search[/]").LeftJustified());

OpenAIFile uploaded = await AnsiConsole
    .Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync(
        "Uploading knowledge base...",
        async _ =>
        {
            var file = filesClient.UploadFile(tempFile, FileUploadPurpose.Assistants);
            AnsiConsole.MarkupLine($"[dim]File uploaded:[/] {file.Value.Filename}");
            return file;
        }
    );

// 2. Create vector store
var vectorStore = await AnsiConsole
    .Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync(
        "Creating vector store...",
        async _ =>
        {
            var vs = await vectorStoresClient.CreateVectorStoreAsync(
                options: new() { FileIds = { uploaded.Id }, Name = "contoso-products" }
            );
            AnsiConsole.MarkupLine($"[dim]Vector store created:[/] {vs.Value.Id}");
            return vs;
        }
    );
string vectorStoreId = vectorStore.Value.Id;

// 3. Create agent with HostedFileSearchTool
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: AgentName,
    instructions: "You are a Contoso sales assistant. Answer questions using the product catalog. Always cite the source.",
    tools: [new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStoreId)] }]
);

// 4. Multi-turn Q&A
AnsiConsole.Write(new Rule("[bold blue]Multi-turn Q&A[/]").LeftJustified());
var session = await agent.CreateSessionAsync();

string[] questions =
[
    "What's the cheapest product?",
    "Which product supports CI/CD?",
    "Compare CloudSync Pro and SecureVault features.",
];

foreach (var question in questions)
{
    AnsiConsole.Write(
        new Panel(Markup.Escape(question))
            .Header("[bold yellow]Question[/]")
            .BorderColor(Color.Yellow)
            .Expand()
    );

    AgentResponse response = await agent.RunAsync(question, session);

    AnsiConsole.Write(
        new Panel(Markup.Escape(response.Text))
            .Header("[bold green]Answer[/]")
            .BorderColor(Color.Green)
            .Expand()
    );

    // Show file citations
    var citations = response
        .Messages.SelectMany(m => m.Contents)
        .SelectMany(c => c.Annotations ?? [])
        .Where(a => a.RawRepresentation is TextAnnotationUpdate)
        .Select(a => (TextAnnotationUpdate)a.RawRepresentation!)
        .ToList();

    if (citations.Count > 0)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn("[bold]File ID[/]")
            .Expand();

        foreach (var citation in citations)
        {
            table.AddRow(Markup.Escape(citation.OutputFileId ?? ""));
        }

        AnsiConsole.Write(
            new Panel(table).Header("[bold cyan]Citations[/]").BorderColor(Color.Cyan1).Expand()
        );
    }
}

// 5. Cleanup
if (AnsiConsole.Confirm($"Delete agent [bold]{agent.Name}[/] and all resources?"))
{
    await Task.WhenAll(
        aiProjectClient.Agents.DeleteAgentAsync(agent.Name),
        vectorStoresClient.DeleteVectorStoreAsync(vectorStoreId),
        filesClient.DeleteFileAsync(uploaded.Id)
    );
    File.Delete(tempFile);
    AnsiConsole.MarkupLine("[green]All resources cleaned up.[/]");
}
else
{
    AnsiConsole.MarkupLine("[yellow]Resources kept. Remember to clean up manually.[/]");
}
