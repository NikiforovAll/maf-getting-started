#:package Microsoft.Agents.AI.OpenAI@1.0.0-rc4
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint!), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = "HelloAgent",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a friendly assistant. Keep your answers brief.",
                Temperature = 0.9f,
            },
        }
    );

// Non-streaming
Console.WriteLine("--- Non-streaming ---");
Console.WriteLine(await agent.RunAsync("Tell me a one-sentence fun fact."));

// Streaming
Console.WriteLine("\n--- Streaming ---");
await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact."))
{
    Console.WriteLine(update);
}
Console.WriteLine();
