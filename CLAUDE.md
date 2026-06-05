# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

NovaTech Knowledge Bot — a .NET 9 console application that answers employee and client questions by retrieving and ranking company `.txt` documents using TF-IDF, then extracting the most query-relevant sentences.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~TfIdfRetrieverTests"

# Run the app (from repo root, so the relative docs path resolves)
dotnet run --project src/KnowledgeBot.Console
```

## Solution Structure

```
src/
  KnowledgeBot.Domain/         # CompanyDocument entity, ConfidenceLevel enum — no external deps
  KnowledgeBot.Application/    # MediatR query/handler, FluentValidation, interfaces, KnowledgeBotOptions
  KnowledgeBot.Infrastructure/ # DocumentLoader, TfIdfRetriever, AnswerComposer, Serilog
  KnowledgeBot.Console/        # DI wiring, appsettings.json, interactive loop
tests/
  KnowledgeBot.Tests/          # xUnit — in-memory only, no disk I/O
docs/                          # 5 .txt knowledge files (faq, leave_policy, product_overview, support_guide, onboarding)
agentic-brain/                 # PROJECT_BRIEF.md, TASKS.md, EVALS.md, AGENT_CONTEXT.md, MEMORY.md
```

Dependency direction: `Console → Infrastructure → Application → Domain`

## Architecture Notes

**CQRS flow:** `Program.cs` sends `GetAnswerQuery` via MediatR → `GetAnswerQueryHandler` calls `IDocumentRepository.LoadAllAsync` (cached singleton) then `IAnswerService.GetAnswer` → returns `AnswerResult`.

**TF-IDF pipeline (Infrastructure):**
1. `DocumentLoader` reads `docs/*.txt` at first call, tokenizes each (lowercase → strip punctuation → remove stop words), builds per-document `TermFrequencies`, and caches the list.
2. `TfIdfRetriever` pre-computes corpus-wide IDF at startup: `IDF(term) = log(N / df(term))`. At query time it scores each document as the sum of `TF × IDF` for each query term, returning the top-N documents (default 3).
3. `AnswerComposer` scores every sentence in the top-N docs using the same TF-IDF weights, selects the top-K sentences across all docs (default 4), and maps the leading document score to a confidence level.

**Confidence thresholds** (all configurable in `appsettings.json` under `KnowledgeBot`):
- `HighConfidenceThreshold` (default 0.15) → `"high"`
- `MediumConfidenceThreshold` (default 0.05) → `"medium"`
- Below medium but above `FallbackThreshold` (default 0.01) → `"low"`
- Below fallback → graceful message, no answer returned

**Output format:**
```
Answer: <extracted sentences>

Confidence: high | Source: leave_policy.txt
```

**Multi-document retrieval** is intentional — EVAL Q6 ("support outside business hours") requires combining sentences from `faq.txt` and `support_guide.txt`.

## Configuration (`appsettings.json`)

```json
{
  "KnowledgeBot": {
    "DocsPath": "../../docs",
    "TopDocuments": 3,
    "TopSentences": 4,
    "HighConfidenceThreshold": 0.15,
    "MediumConfidenceThreshold": 0.05,
    "FallbackThreshold": 0.01
  }
}
```

## Acceptance Criteria

Six evaluation questions in `agentic-brain/EVALS.md` define acceptance. Each targets a specific doc and has required key phrases. Q6 is a cross-file question that tests multi-document retrieval. Run all 6 manually after any change to retrieval or scoring logic.
