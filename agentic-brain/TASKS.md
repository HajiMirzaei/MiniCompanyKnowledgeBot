# TASKS

## Status Legend
- ✅ Done
- 🔲 Remaining / Nice to have

---

## ✅ Completed Tasks

### TASK-01 — Initialize GitHub Repository
Create a new public GitHub repository named `MiniCompanyKnowledgeBot`. Add a `.gitignore` for .NET projects and an initial `README.md` placeholder. This is the foundation for all version-controlled work.

### TASK-02 — Create Company Docs and Agentic Brain Files
Create the `docs/` and `agentic-brain/` folders. Add 5 sample `.txt` files inside `docs/` representing the fictional company NovaTech Solutions:
- `faq.txt` — common user questions and answers
- `leave_policy.txt` — annual, sick, and parental leave rules
- `product_overview.txt` — product features, pricing plans, and integrations
- `support_guide.txt` — how to contact support, SLAs per plan, and bug reporting
- `onboarding.txt` — new employee checklist, system access, equipment policy

Create `agentic-brain/` with `PROJECT_BRIEF.md`, `TASKS.md`, `AGENT_CONTEXT.md`, `MEMORY.md`, and `EVALS.md`.

### TASK-03 — Create .NET Solution (Clean Architecture)
Create the solution and project structure for a .NET 9 Clean Architecture console application named `MiniCompanyKnowledgeBot` in the `src/` folder, plus an xUnit test project in `tests/`. Create all `.csproj` files with the correct project references:

```
Domain  ←  Application  ←  Infrastructure
                         ↑
                       Console

tests/KnowledgeBot.Tests  →  Infrastructure + Application
```

Add NuGet packages:
- `Application`: MediatR, FluentValidation, Microsoft.Extensions.Options
- `Infrastructure`: Microsoft.Extensions.Options, Serilog, Serilog.Sinks.Console
- `Console`: MediatR, FluentValidation.DependencyInjectionExtensions, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration.Json, Serilog, Serilog.Settings.Configuration, Serilog.Sinks.Console
- `Tests`: xUnit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk

### TASK-04 — Implement Domain Layer
Implement the Domain layer with no external dependencies. Create:
- `Entities/CompanyDocument.cs` — `FileName` (string), `Content` (string), `Score` (double)

### TASK-05 — Implement Application Layer
Implement the Application layer using MediatR for CQRS. Create a `CompanyKnowledgeModule/Queries/` folder containing:
- `QueryRequest.cs` — `IRequest<QueryResponse>` with a `Question` (string) property
- `QueryResponse.cs` — `Answer` (string), `SourceFiles` (List\<string\>), `Confidence` (string: "high", "medium", or "low")
- `QueryRequestHandler.cs` — `IRequestHandler` that calls `IRetriever` then `IAnswerComposer`
- `QueryRequestValidator.cs` — FluentValidation rule ensuring `Question` is not empty

Also create:
- `Interfaces/IDocumentLoader.cs` — `IEnumerable<CompanyDocument> LoadAll()`
- `Interfaces/IRetriever.cs` — `IEnumerable<CompanyDocument> Retrieve(string query, int topN)`
- `Interfaces/IAnswerComposer.cs` — `QueryResponse Compose(string question, IEnumerable<CompanyDocument> chunks)`
- `Options/KnowledgeBotOptions.cs` — `DocsPath`, `TopDocuments` (int, default 3), `TopSentences` (int, default 4), `HighConfidenceThreshold` (double), `MediumConfidenceThreshold` (double), `FallbackThreshold` (double)

### TASK-06 — Implement Document Loader
Create `Services/DocumentLoader.cs` in the Infrastructure project implementing `IDocumentLoader`.

Reads all `.txt` files from the configured `DocsPath` at first call, creates one `CompanyDocument` per file, and caches them in memory. Path is resolved from `KnowledgeBotOptions` so it is not hardcoded.

### TASK-07 — Implement TF-IDF Retriever
Create `Services/TfIdfRetriever.cs` implementing `IRetriever`.

1. Tokenizes the query into individual lowercase words, removing punctuation
2. Removes common English stop words (e.g. "the", "is", "a", "how", "do")
3. For each document, computes TF-IDF: `TF(term, doc) × IDF(term)` where `IDF = log(N / df)`
4. Returns the top N documents sorted by descending score

### TASK-08 — Implement Answer Composer
Create `Services/AnswerComposer.cs` implementing `IAnswerComposer`.

1. If the top-scored document's score is below `FallbackThreshold`, returns a graceful fallback: "I don't have information about that in the company docs."
2. Otherwise, splits each of the top-N retrieved documents into sentences and scores each sentence using the TF-IDF weights of query terms that appear in it
3. Selects the top-K sentences across all retrieved documents (configurable via `TopSentences`, default 4), preserving reading order within each source document
4. Assembles the sentences into a readable answer, printed as: `Answer: …\nConfidence: <level> | Source: <filename(s)>`
5. Maps the top document score to confidence: `"high"` if score ≥ `HighConfidenceThreshold`, `"medium"` if score ≥ `MediumConfidenceThreshold`, `"low"` otherwise

### TASK-09 — Implement Console Interactive Loop
Create `Program.cs` in the Console project.

1. Builds a `ServiceCollection` with all DI registrations:
   - `IDocumentLoader` → `DocumentLoader` as Singleton
   - `IRetriever` → `TfIdfRetriever` as Singleton
   - `IAnswerComposer` → `AnswerComposer` as Transient
   - MediatR registered from the Application assembly
   - FluentValidation validators registered from the Application assembly
2. Reads configuration from `appsettings.json` and binds `KnowledgeBotOptions`
3. Pre-loads all documents at startup
4. Runs an interactive `while` loop: prints `> ` prompt, reads input, validates, sends `QueryRequest` via MediatR, prints the answer and source/confidence metadata

### TASK-10 — Add Configuration
In `appsettings.json`, add a `KnowledgeBot` section with:
- `DocsPath` — relative path to the `docs/` folder (default: `"../../docs"`)
- `TopDocuments` — how many top documents the retriever returns (default: `3`)
- `TopSentences` — how many sentences to extract across all retrieved documents (default: `4`)
- `HighConfidenceThreshold` — score at or above which confidence is `"high"` (default: `0.15`)
- `MediumConfidenceThreshold` — score at or above which confidence is `"medium"` (default: `0.05`)
- `FallbackThreshold` — score below which a fallback message is returned instead of an answer (default: `0.01`)

Bind this section to `KnowledgeBotOptions` using the Options pattern.

### TASK-11 — Write README.md
Write the top-level `README.md` covering:
- What the project does and why it was built
- How to clone and run it locally (`dotnet run`)
- Example session showing a question and answer
- Folder structure explained
- How Claude Code was used during development

### TASK-12 — Add Unit Tests Project
Create a `tests/KnowledgeBot.Tests/` xUnit project (already scaffolded in TASK-03). Write unit tests using in-memory `CompanyDocument` instances — no disk I/O:
- `TfIdfRetrieverTests`: given a known 3-document corpus, assert the correct document ranks first for a targeted query; assert multi-document ranking order
- `AnswerComposerTests`: score above `HighConfidenceThreshold` → `"high"`; score between Medium and High → `"medium"`; score below `FallbackThreshold` → fallback message returned
- `DocumentLoaderTests`: assert files parse correctly; assert a missing `DocsPath` throws a descriptive exception

---

## 🔲 Remaining / Nice to Have

### TASK-13 — Improve Sentence Extraction in AnswerComposer
Currently the composer picks sentences by simple term frequency. A future improvement is to score sentences using the same TF-IDF weights already computed during retrieval, so the answer is guaranteed to use the highest-signal text from each document.

### TASK-14 — Add Pagination / Multi-turn Support
Add an optional `context` field to `QueryRequest` so callers can pass in the previous question and answer. The retriever would then combine current query terms with terms from the prior context, enabling basic multi-turn conversation flow.

### TASK-15 — Dockerize the Application
Add a `Dockerfile` and `docker-compose.yml` so the app can be run in a container with a single command. The `docs/` folder should be mounted as a volume so knowledge files can be updated without rebuilding the image.

### TASK-16 — Add Request Logging Middleware
Add a simple logger call that records each incoming query, which documents were retrieved, the confidence level, and the response time in milliseconds.
