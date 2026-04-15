#:package Microsoft.Agents.AI.A2A@1.1.0-preview.260410.1

using A2A;
using Microsoft.Agents.AI;

var host = args.Length > 0 ? args[0] : "http://localhost:5000";

A2ACardResolver resolver = new(new Uri(host));

AIAgent agent = await resolver.GetAIAgentAsync();

Console.WriteLine(await agent.RunAsync("What is the weather in Amsterdam?"));
