# C# Style Guide

## Naming
- Use PascalCase for public members and types
- Use camelCase for local variables and parameters
- Prefix interfaces with `I`
- Use meaningful, descriptive names

## Methods
- Keep methods under 30 lines
- Single responsibility per method
- Prefer early returns over deep nesting

## Async
- Always use `Async` suffix for async methods
- Never use `async void` except for event handlers
- Always await or return tasks, never fire-and-forget

## Error Handling
- Catch specific exceptions, not `Exception`
- Log exceptions with context
- Use guard clauses for parameter validation
