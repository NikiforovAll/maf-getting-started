#:package Microsoft.Agents.AI@1.0.0-rc4
#:package Microsoft.Agents.AI.AGUI@1.0.0-preview.260311.1
#:package Spectre.Console@0.49.1

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Spectre.Console;

var host = args.Length > 0 ? args[0] : "http://localhost:5000";

AnsiConsole.Write(new Rule($"[blue]AG-UI Chat[/] → [link]{host}[/]").LeftJustified());
AnsiConsole.WriteLine();

using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
AGUIChatClient chatClient = new(httpClient, host);

AIAgent agent = chatClient.AsAIAgent(name: "agui-client");
AgentSession session = await agent.CreateSessionAsync();
List<ChatMessage> messages = [];

while (true)
{
    string input = AnsiConsole.Prompt(new TextPrompt<string>("[green]You:[/]").AllowEmpty());

    if (string.IsNullOrWhiteSpace(input) || input is ":q" or "quit")
        break;

    messages.Add(new ChatMessage(ChatRole.User, input));

    var response = new System.Text.StringBuilder();

    AnsiConsole.Markup("[cyan]Assistant:[/] ");
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages, session))
    {
        foreach (AIContent content in update.Contents)
        {
            if (content is TextContent text)
            {
                AnsiConsole.Write(text.Text);
                response.Append(text.Text);
            }
        }
    }

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule().RuleStyle(Style.Parse("dim")).LeftJustified());

    messages.Add(new ChatMessage(ChatRole.Assistant, response.ToString()));
}
