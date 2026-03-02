#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc2
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a friendly assistant. Keep your answers brief. And always remember the information the user shares with you during the conversation.",
        name: "ConversationAgent"
    );

AgentSession session = await agent.CreateSessionAsync();

// Turn 1
Console.WriteLine("User: My name is Alice and I love hiking.");
Console.WriteLine($"Agent: {await agent.RunAsync("My name is Alice and I love hiking.", session)}");

// Turn 2 — agent should remember context
Console.WriteLine("\nUser: What do you remember about me?");
Console.WriteLine($"Agent: {await agent.RunAsync("What do you remember about me?", session)}");

// Turn 3
Console.WriteLine("\nUser: Suggest a hiking destination for me.");
Console.WriteLine(
    $"Agent: {await agent.RunAsync("Suggest a hiking destination for me.", session)}"
);
