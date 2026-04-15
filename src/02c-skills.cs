#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.20.0
#:package Microsoft.Extensions.AI@10.4.0
#:property NoWarn=MAAI001

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Discover skills from the 'skills' directory — progressive disclosure pattern
// For run files, resolve skills relative to the source file location
var sourceDir =
    Path.GetDirectoryName(AppContext.BaseDirectory)
    ?? throw new InvalidOperationException("Cannot determine source directory.");
var skillsDir = Path.Combine(sourceDir, "skills");

// Fallback: when running from source directly, use current directory
if (!Directory.Exists(skillsDir))
{
    skillsDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "skills");
}

var skillsProvider = new AgentSkillsProvider(skillsDir);

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = "SkillsAgent",
            ChatOptions = new ChatOptions { Instructions = "You are a helpful assistant." },
            AIContextProviders = [skillsProvider],
        }
    );

// The agent discovers available skills and loads them on demand
Console.WriteLine("--- Skills Demo: Code Review ---\n");

var code = """
    public string GetUser(string id)
    {
        var query = "SELECT * FROM users WHERE id = '" + id + "'";
        var result = db.Execute(query);
        return result;
    }
    """;

AgentResponse response = await agent.RunAsync($"Review this code:\n```csharp\n{code}\n```");
Console.WriteLine(response.Text);
