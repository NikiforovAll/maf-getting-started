#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package OpenAI@2.8.0
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:package Spectre.Console@0.50.0
#:property EnablePreviewFeatures=true
#:property NoWarn=MEAI001;OPENAI001

using System.Text;
using System.Text.RegularExpressions;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Assistants;
using Spectre.Console;

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
    instructions: "You are a math tutor. Write and run Python code to solve problems. Do not generate plots or charts. IMPORTANT: Always use print() to display results, never use bare expressions. Use plain text formatting, never use LaTeX or markdown.",
    tools: [new HostedCodeInterpreterTool() { Inputs = [] }]
);

AnsiConsole.Write(new Rule("[bold blue]Code Interpreter[/]").LeftJustified());

AgentResponse response = await agent.RunAsync(
    "Solve the equation: x^3 - 6x^2 + 11x - 6 = 0. Show the roots."
);

// Walk contents in order to show the full execution flow
bool seenToolCall = false;
foreach (var content in response.Messages.SelectMany(m => m.Contents))
{
    switch (content)
    {
        case TextContent text:
            string label = seenToolCall ? "Answer" : "Thinking";
            Color color = seenToolCall ? Color.Green : Color.Grey;
            AnsiConsole.Write(
                new Panel(Markup.Escape(StripLatex(text.Text)))
                    .Header($"[bold {color}]{label}[/]")
                    .BorderColor(color)
                    .Expand()
            );
            break;

        case CodeInterpreterToolCallContent toolCall:
            seenToolCall = true;
            var codeInput = toolCall.Inputs?.OfType<DataContent>().FirstOrDefault();
            if (codeInput?.HasTopLevelMediaType("text") ?? false)
            {
                string code = Encoding.UTF8.GetString(codeInput.Data.ToArray());
                AnsiConsole.Write(
                    new Panel(Markup.Escape(code))
                        .Header("[bold yellow]Python Code[/]")
                        .BorderColor(Color.Yellow)
                        .Expand()
                );
            }
            break;

        case CodeInterpreterToolResultContent toolResult:
            foreach (var output in toolResult.Outputs ?? [])
            {
                if (output is TextContent tc)
                {
                    AnsiConsole.Write(
                        new Panel(Markup.Escape(tc.Text))
                            .Header("[bold cyan]Code Output[/]")
                            .BorderColor(Color.Cyan1)
                            .Expand()
                    );
                }
            }
            break;
    }

    // Show annotations (file citations, generated files)
    foreach (var annotation in content.Annotations ?? [])
    {
        if (annotation.RawRepresentation is TextAnnotationUpdate citation)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Generated file:[/] {Markup.Escape(citation.OutputFileId)}"
            );
        }
    }
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

static string StripLatex(string text) =>
    Regex.Replace(Regex.Replace(text, @"\\\[(.+?)\\\]", "$1"), @"\\\((.+?)\\\)", "$1");
