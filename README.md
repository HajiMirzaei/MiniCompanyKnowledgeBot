# NovaTech Knowledge Bot

A .NET 9 console application that answers employee and client questions by searching a set of company `.txt` documents using TF-IDF retrieval, then extracting and returning the most relevant sentences.

## Why it was built

NovaTech receives hundreds of daily inquiries about leave policy, product features, support SLAs, and onboarding. This bot lets employees and clients get instant, document-grounded answers without involving the call centre.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Verify your installation:

```bash
dotnet --version
# should print 9.x.x
```

---

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/HajiMirzaei/MiniCompanyKnowledgeBot.git
cd MiniCompanyKnowledgeBot
```

### 2. Build

```bash
dotnet build
```

### 3. Run the tests

```bash
dotnet test
```

### 4. Run the app

```bash
# From the repo root — or from inside src/KnowledgeBot.Console
dotnet run --project src/KnowledgeBot.Console
```

---

## Example session

```
========================================
  NovaTech Knowledge Bot
========================================
Loading documents... done (5 files)

Type your question or 'exit' to quit.

> how many annual leave days do employees get?

All full-time employees receive 25 days of paid annual leave per calendar year.
Up to 5 unused days can be carried over to the following year.

Confidence: high | Source: leave_policy.txt

> what are the enterprise support SLAs?

Enterprise plan customers receive a 1-hour response SLA.
Support is available 24/7 for Enterprise customers via phone, email, and live chat.

Confidence: medium | Source: support_guide.txt

> exit
Goodbye!
```

---

## Configuration

Settings live in `src/KnowledgeBot.Console/appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `DocsPath` | `docs` | Path to the knowledge `.txt` files (relative to the binary's output directory) |
| `TopDocuments` | `3` | Number of top-scoring documents the retriever returns per query |
| `TopSentences` | `4` | Number of sentences extracted across all retrieved documents |
| `HighConfidenceThreshold` | `0.15` | Minimum TF-IDF score to report `high` confidence |
| `MediumConfidenceThreshold` | `0.05` | Minimum TF-IDF score to report `medium` confidence |
| `FallbackThreshold` | `0.01` | Below this score the bot returns a fallback message instead of an answer |

---

## Project structure

```
MiniCompanyKnowledgeBot/
├── docs/                          # Five NovaTech .txt knowledge files
│   ├── faq.txt
│   ├── leave_policy.txt
│   ├── onboarding.txt
│   ├── product_overview.txt
│   └── support_guide.txt
├── src/
│   ├── KnowledgeBot.Domain/       # Core entities and enums — no external dependencies
│   ├── KnowledgeBot.Application/  # CQRS queries, interfaces, validation, options
│   ├── KnowledgeBot.Infrastructure/ # TF-IDF engine, document loading, answer composition
│   └── KnowledgeBot.Console/      # DI wiring, configuration, interactive loop
├── tests/
│   └── KnowledgeBot.Tests/        # xUnit unit tests (in-memory, no disk I/O)
└── agentic-brain/                 # Project brief, tasks, evals, and agent context
```

### KnowledgeBot.Domain

The innermost layer with no external dependencies. Contains:

- **`CompanyDocument`** — the core entity: `FileName`, `RawText`, `Sentences[]`, `TermFrequencies`, and `Score` (set per query by the retriever).
- **`ConfidenceLevel`** — enum with `None`, `Low`, `Medium`, `High`.

### KnowledgeBot.Application

Defines what the system can do, without knowing how. Contains:

- **`QueryRequest` / `QueryResponse`** — MediatR request/response pair. `QueryResponse` carries the answer text, source file names, and confidence level.
- **`QueryRequestHandler`** — receives a question, asks the retriever for the top-N documents, then asks the composer to build the answer.
- **`QueryRequestValidator`** — FluentValidation rule: question must not be empty and must be under 500 characters.
- **`IDocumentLoader`, `IRetriever`, `IAnswerComposer`** — interfaces the Infrastructure layer implements.
- **`KnowledgeBotOptions`** — strongly-typed options class bound from `appsettings.json`.

### KnowledgeBot.Infrastructure

Implements the retrieval and answer-building logic. Contains:

- **`DocumentLoader`** — reads all `*.txt` files from `DocsPath` on first call, tokenizes each file into term-frequency maps and sentence arrays, then caches the result. Subsequent calls return the same in-memory list.
- **`TfIdfRetriever`** — on startup builds a corpus-wide IDF map (`log((N+1)/(df+1)) + 1`). At query time scores each document as the sum of `TF × IDF` for each query token and returns the top-N scored copies.
- **`AnswerComposer`** — scores every sentence across the top-N documents by counting query token occurrences, picks the global top-K, then restores reading order within each source document before joining them into the answer.
- **`Tokenizer`** — shared static helper: lowercases input, strips punctuation, removes ~60 English stop words.

### KnowledgeBot.Console

The entry point and composition root. `Program.cs`:

1. Reads `appsettings.json` and configures Serilog.
2. Wires up the DI container: `DocumentLoader` and `TfIdfRetriever` as singletons, `AnswerComposer` as transient, MediatR, and FluentValidation.
3. Pre-warms the document cache at startup.
4. Runs an interactive loop: validates input, sends a `QueryRequest` through MediatR, and prints the answer with confidence and source metadata.

---

## How Claude Code was used

This project was built end-to-end with [Claude Code](https://claude.ai/code) as the primary development assistant. The workflow followed the task list in `agentic-brain/TASKS.md`:

- **Planning** — the project brief and task list in `agentic-brain/` were drafted collaboratively. Claude asked clarifying questions about confidence thresholds, multi-document retrieval, output format, and test scope before writing any code.
- **Implementation** — each layer (Domain → Application → Infrastructure → Console) was implemented one task at a time. Claude generated all source files, `.csproj` configurations, and DI wiring.
- **Debugging** — path resolution bugs (working-directory sensitivity, spurious empty `docs/` folder) were diagnosed and fixed within the same session.
- **Testing** — all 20 unit tests were written by Claude using plain fake implementations and a temp-directory fixture, with no mocking library.
