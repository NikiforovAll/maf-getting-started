var builder = DistributedApplication.CreateBuilder(args);

var session = builder.Configuration["session"] ?? "openai";

if (session is "openai" or "all")
{
    builder
        .AddAzureOpenAI("openai")
        .AsExisting(
            builder.AddParameterFromConfiguration("AzureOpenAIName", "Azure:OpenAI:Name"),
            builder.AddParameterFromConfiguration(
                "AzureOpenAIResourceGroup",
                "Azure:OpenAI:ResourceGroup"
            )
        )
        .AddDeployment("gpt-4o-mini", "gpt-4o-mini", "2024-07-18");
}

if (session is "foundry" or "all")
{
    builder
        .AddAzureAIFoundry("foundry")
        .AsExisting(
            builder.AddParameterFromConfiguration("AzureAIFoundryName", "Azure:AIFoundry:Name"),
            builder.AddParameterFromConfiguration(
                "AzureAIFoundryResourceGroup",
                "Azure:AIFoundry:ResourceGroup"
            )
        );
}

await builder.Build().RunAsync();
