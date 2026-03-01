var builder = DistributedApplication.CreateBuilder(args);

var openai = builder
    .AddAzureOpenAI("openai")
    .AsExisting(
        builder.AddParameterFromConfiguration("AzureOpenAIName", "Azure:OpenAI:Name"),
        builder.AddParameterFromConfiguration(
            "AzureOpenAIResourceGroup",
            "Azure:OpenAI:ResourceGroup"
        )
    )
    .AddDeployment("gpt-4o-mini", "gpt-4o-mini", "2024-07-18");

await builder.Build().RunAsync();
