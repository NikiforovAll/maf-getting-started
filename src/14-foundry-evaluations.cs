#:package Microsoft.Agents.AI.AzureAI@1.0.0-rc2
#:package Azure.AI.Projects@1.2.0-beta.5
#:package Azure.AI.OpenAI@2.8.0-beta.1
#:package Azure.Identity@1.18.0
#:package Microsoft.Extensions.AI@10.3.0
#:package Microsoft.Extensions.AI.Evaluation@10.3.0
#:package Microsoft.Extensions.AI.Evaluation.Quality@10.3.0
#:package Microsoft.Extensions.AI.Evaluation.Safety@10.3.0-preview.1.26109.11
#:property EnablePreviewFeatures=true

using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Safety;
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
AIAgent agent = await aiProjectClient.CreateAIAgentAsync(
    name: AgentName,
    model: deploymentName,
    instructions: "You are a helpful assistant. Answer questions accurately based on the provided context."
);

try
{
    const string Question = "What are the main benefits of using Azure AI Foundry?";
    const string Context = """
        Azure AI Foundry is a platform for building, deploying, and managing AI applications.
        Benefits include: unified dev environment, built-in safety features (content filtering, red teaming),
        scalable infrastructure, integration with Azure services, evaluation tools for quality and safety,
        support for RAG patterns with vector search, and enterprise compliance features.
        """;

    // 1. Self-Reflection with Groundedness
    Console.WriteLine("=== Self-Reflection with Groundedness ===\n");
    await RunSelfReflection(agent, Question, Context, chatConfiguration);

    // 2. Combined Quality + Safety
    Console.WriteLine("\n=== Quality + Safety Evaluation ===\n");
    await RunQualityAndSafety(agent, Question, chatConfiguration);
}
finally
{
    await aiProjectClient.Agents.DeleteAgentAsync(agent.Name);
    Console.WriteLine("\nAgent deleted.");
}

static async Task RunSelfReflection(
    AIAgent agent,
    string question,
    string context,
    ChatConfiguration config
)
{
    GroundednessEvaluator evaluator = new();
    GroundednessEvaluatorContext groundingContext = new(context);

    string currentPrompt = $"Context: {context}\n\nQuestion: {question}";

    for (int i = 0; i < 3; i++)
    {
        Console.WriteLine($"Iteration {i + 1}/3:");

        var session = await agent.CreateSessionAsync();
        AgentResponse agentResponse = await agent.RunAsync(currentPrompt, session);
        string responseText = agentResponse.Text;

        Console.WriteLine($"  Response: {responseText[..Math.Min(120, responseText.Length)]}...");

        List<ChatMessage> messages = [new(ChatRole.User, currentPrompt)];
        ChatResponse chatResponse = new(new ChatMessage(ChatRole.Assistant, responseText));

        EvaluationResult result = await evaluator.EvaluateAsync(
            messages,
            chatResponse,
            config,
            additionalContext: [groundingContext]
        );

        NumericMetric groundedness = result.Get<NumericMetric>(
            GroundednessEvaluator.GroundednessMetricName
        );
        double score = groundedness.Value ?? 0;
        Console.WriteLine(
            $"  Groundedness: {score:F1}/5 ({groundedness.Interpretation?.Rating})\n"
        );

        if (score >= 4.0 || i == 2)
            break;

        currentPrompt = $"""
            Context: {context}
            Your previous answer scored {score}/5 on groundedness.
            Previous answer: {responseText}
            Improve your answer to be more grounded in the context.
            Question: {question}
            """;
    }
}

static async Task RunQualityAndSafety(AIAgent agent, string question, ChatConfiguration config)
{
    CompositeEvaluator evaluator = new([
        new RelevanceEvaluator(),
        new CoherenceEvaluator(),
        new ContentHarmEvaluator(),
    ]);

    var session = await agent.CreateSessionAsync();
    AgentResponse agentResponse = await agent.RunAsync(question, session);

    Console.WriteLine(
        $"Response: {agentResponse.Text[..Math.Min(120, agentResponse.Text.Length)]}...\n"
    );

    List<ChatMessage> messages = [new(ChatRole.User, question)];
    ChatResponse chatResponse = new(new ChatMessage(ChatRole.Assistant, agentResponse.Text));

    EvaluationResult result = await evaluator.EvaluateAsync(messages, chatResponse, config);

    foreach (var metric in result.Metrics.Values)
    {
        if (metric is NumericMetric n)
        {
            Console.WriteLine(
                $"  {n.Name, -25} Score: {n.Value:F1}/5  Rating: {n.Interpretation?.Rating}  Failed: {n.Interpretation?.Failed}"
            );
        }
        else if (metric is BooleanMetric b)
        {
            Console.WriteLine(
                $"  {b.Name, -25} Value: {b.Value}  Rating: {b.Interpretation?.Rating}  Failed: {b.Interpretation?.Failed}"
            );
        }
    }
}
