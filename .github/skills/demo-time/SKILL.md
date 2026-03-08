---
name: demo-time
description: "Use when creating, updating, reviewing, or validating Demo Time act files and companion slides for this repository, especially .demo/*.yaml files, code highlight chunks, scene flow, and alignment with slides and src samples."
---

# Demo Time

## Purpose

Create or refine Demo Time assets for this repository's MAF presentation flow.

Focus on:
- Act files under `.demo/`
- Companion slides under `.demo/slides/`
- Alignment between presentation decks in `slides/` and samples in `src/`

## Repository Conventions

1. Match the numbering used by the source sample and slide deck.
   Example: `src/01-hello-agent.cs` maps to `.demo/01-hello-agent-demo.yaml` and `.demo/slides/01-hello-agent-start.md`.

2. Align the demo narrative with the corresponding deck chapter.
   Example: `slides/01-foundations.md` drives the flow for the `01-hello-agent` demo.

3. Preserve the structure already used in the target act file.
   If the existing file uses `demos` and `steps`, keep that shape.
   If the existing file uses `scenes` and `moves`, keep that shape.
   Do not rewrite the whole act just to normalize terminology.

4. Keep act files focused on one sample or one talk segment.

5. Use repo-relative paths in Demo Time actions.

6. Prefer YAML for Demo Time assets in this repository.

## Workflow

### 1. Gather Context

Read these before editing:
- The target source file in `src/`
- The related presentation deck in `slides/`
- The current act file in `.demo/`, if it already exists
- Any existing companion slides in `.demo/slides/`

When highlights are involved, inspect exact line numbers before editing.

### 2. Build the Presentation Story

Structure the act in presentation order:
1. Intro or framing slide
2. Code walkthrough in teaching order
3. Wrap-up slide or transition

Keep each demo section focused on one visible idea.
Examples:
- Package directives
- Imports
- Environment setup
- Agent construction
- Non-streaming execution
- Streaming execution

### 3. Choose Demo Time Moves Carefully

For code walkthroughs, prefer these actions first:
- `openSlide`
- `open`
- `highlight`

Only add more advanced actions if the requested demo actually needs them and the Demo Time docs confirm the syntax.

### 4. Highlight by Chunks, Not Just Anchors

Do not default to single-line highlights.

Prefer chunk-based ranges that match how a presenter explains the code:
- `position: "1:3"` for a block of lines
- `position: "10,5:20,10"` only when partial-line emphasis is necessary

Use smaller chunks when the audience needs to follow the code step by step.
Use larger chunks when a whole construct should stay visible together.

### 5. Keep the Flow Logical

Avoid jumpy walkthroughs.

Good order:
1. Dependencies and imports
2. Inputs and configuration
3. Construction pipeline
4. Options and prompt shaping
5. Execution code

Avoid reopening the same file in every step unless the editor context actually changed.

### 6. Companion Slides

When the demo needs framing, add or update slides in `.demo/slides/`:
- `*-start.md` for intro/context
- `*-end.md` for summary/transition

Keep these slides short and presenter-oriented.

## Decision Points

### Existing act already present

If the act file already exists, update it in place.
Preserve its naming, structure, and style unless the user explicitly asks for a rewrite.

### Numbering matters

If the sample is numbered, keep the number in all related demo assets.

### Highlight granularity

If the current highlight is too narrow, widen it to a chunk.
If it is too broad for live explanation, split it into multiple sequential chunks.

### Docs uncertainty

If you are unsure about an action property, check Demo Time documentation or schema before editing.
Do not invent unsupported fields.

## Validation Checklist

Before finishing:
1. Confirm every referenced path exists.
2. Confirm the act file parses without YAML errors.
3. Confirm highlight positions match the intended code chunks.
4. Confirm the story order matches the related deck section.
5. Confirm the act structure matches the file's current style.

## Completion Criteria

The work is complete when:
- The act file is named consistently with the sample and deck
- The walkthrough progresses in a logical presentation order
- Highlights emphasize meaningful chunks instead of arbitrary single lines
- Companion slides are present when needed
- No schema or YAML issues remain

## Example Prompts

- Create a Demo Time act for `src/02-tools.cs` based on `slides/01-foundations.md`.
- Refine `.demo/01-hello-agent-demo.yaml` so the highlights move by logical code chunks.
- Add start and end companion slides for the `03-multi-turn` demo.
- Review this Demo Time act and point out jumpy or overly broad highlight ranges.