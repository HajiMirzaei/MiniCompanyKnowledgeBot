# NovaTech Knowledge Bot

## Problem
We receive hundreds of phone calls and emails daily from our clients and employees asking questions about support guides, company products, leave policy, onboarding, and more. The company call centre must respond to each message individually, which is slow and leads to impersonal, canned responses.

## Solution
Build a console application that accepts a question from the user, retrieves and ranks company documents (from the `docs/` folder) using a TF-IDF approach, extracts the most relevant sentences, and responds with the most relevant information.

## Features
- Receive a question interactively from the user via the console
- Load and cache all company `.txt` documents at startup
- Rank documents using TF-IDF (term frequency × inverse document frequency); retrieve top-N documents (default 3), not just the best match — required for cross-document questions
- Extract the top-K most query-relevant sentences across all retrieved documents
- Return an answer in the format: `Answer: … \nConfidence: high | Source: filename.txt`
- Return an answer with a confidence level: `high`, `medium`, or `low` — thresholds are configurable via `appsettings.json`
- Return a graceful fallback message when no document scores above the fallback threshold
- Interactive loop — user can ask multiple questions in one session; type `exit` to quit

## Tech Stack
- **Runtime:** .NET 9 console application
- **Architecture:** Clean Architecture with CQRS
  - `Domain` — `CompanyDocument` entity (no external dependencies)
  - `Application` — MediatR handlers, interfaces, FluentValidation, options
  - `Infrastructure` — `DocumentLoader`, `TfIdfRetriever`, `AnswerComposer` (Serilog)
  - `Console` — DI wiring, configuration, interactive loop
- **Key packages:** MediatR, FluentValidation, Serilog, Microsoft.Extensions.DependencyInjection
- **Tests:** `KnowledgeBot.Tests` xUnit project covering `TfIdfRetriever` and `AnswerComposer` logic (in-memory, no disk I/O)
- **No database** — documents are `.txt` files read from disk and cached in memory

## Verification
Acceptance is defined by the 6 evaluation questions in [`agentic-brain/EVALS.md`](EVALS.md). Each question targets a specific knowledge file and has explicit pass conditions (key phrases that must appear in the answer). A cross-file question (Q6) tests retrieval across multiple documents.
