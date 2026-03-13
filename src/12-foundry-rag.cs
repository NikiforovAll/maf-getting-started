#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc4
#:package Azure.AI.Projects@2.0.0-beta.1
#:package Azure.AI.Projects.OpenAI@2.0.0-beta.1
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:property EnablePreviewFeatures=true

using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Files;
using OpenAI.Responses;

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

Console.WriteLine("Uploading knowledge base...");
OpenAIFile uploaded = filesClient.UploadFile(tempFile, FileUploadPurpose.Assistants);
Console.WriteLine($"File uploaded: {uploaded.Id}");

// 2. Create vector store
var vectorStore = await vectorStoresClient.CreateVectorStoreAsync(
    options: new() { FileIds = { uploaded.Id }, Name = "contoso-products" }
);
string vectorStoreId = vectorStore.Value.Id;
Console.WriteLine($"Vector store created: {vectorStoreId}");

// 3. Create agent with HostedFileSearchTool
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    model: deploymentName,
    name: AgentName,
    instructions: "You are a Contoso sales assistant. Answer questions using the product catalog. Always cite the source.",
    tools: [new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStoreId)] }]
);

// 4. Multi-turn Q&A
Console.WriteLine("\n--- RAG Q&A ---");
var session = await agent.CreateSessionAsync();

string[] questions =
[
    "What's the cheapest product?",
    "Which product supports CI/CD?",
    "Compare CloudSync Pro and SecureVault features.",
];

foreach (var question in questions)
{
    Console.WriteLine($"\nQ: {question}");
    AgentResponse response = await agent.RunAsync(question, session);
    Console.WriteLine($"A: {response.Text}");

    // Show file citations
    foreach (
        var annotation in response
            .Messages.SelectMany(m => m.Contents)
            .SelectMany(c => c.Annotations ?? [])
    )
    {
        if (annotation.RawRepresentation is TextAnnotationUpdate citation)
        {
            Console.WriteLine($"   [Citation: file {citation.OutputFileId}]");
        }
    }
}

// 5. Cleanup
Console.WriteLine("\n--- Cleanup ---");
await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
await vectorStoresClient.DeleteVectorStoreAsync(vectorStoreId);
await filesClient.DeleteFileAsync(uploaded.Id);
File.Delete(tempFile);
Console.WriteLine("All resources cleaned up.");
