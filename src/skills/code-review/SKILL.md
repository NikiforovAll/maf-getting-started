---
name: code-review
description: Review code for quality, security, and best practices. Use when asked to review code, find bugs, suggest improvements, or check for security issues.
metadata:
  author: demo
  version: "1.0"
---

# Code Review

## Review Checklist

| Area | What to Check |
|---|---|
| Security | SQL injection, XSS, hardcoded secrets, input validation |
| Performance | N+1 queries, unnecessary allocations, missing caching |
| Readability | Naming, method length, single responsibility |
| Error handling | Missing try/catch, swallowed exceptions, proper logging |
| Testing | Testability, edge cases, missing assertions |

## Review Process

1. Read the code carefully and understand its purpose.
2. Check each area from the checklist above.
3. For each issue found, provide:
   - **Severity**: Critical / Warning / Suggestion
   - **Location**: Where in the code
   - **Issue**: What's wrong
   - **Fix**: How to fix it
4. Summarize with an overall assessment.

## Style Guidelines

For detailed style guidelines, consult: [references/STYLE_GUIDE.md](references/STYLE_GUIDE.md)
