---
theme: default
layout: default
---

# What did we cover?

- `AddAIAgent()` registers agents as keyed services in DI
- `WithAITool(sp => ...)` resolves tools from the container
- Class-based tools can have their own injected dependencies
- Agents are resolved with `GetRequiredKeyedService<AIAgent>("name")`

---
layout: section
---

# Next up: agent skills