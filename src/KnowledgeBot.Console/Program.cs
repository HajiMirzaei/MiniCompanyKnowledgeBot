using FluentValidation;
using KnowledgeBot.Application.CompanyKnowledgeModule.Queries;
using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Application.Options;
using KnowledgeBot.Domain.Enums;
using KnowledgeBot.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// ── Configuration ────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

// ── DI ───────────────────────────────────────────────────────────────────────

var services = new ServiceCollection();

services.AddLogging(lb => lb.AddSerilog(dispose: true));
services.Configure<KnowledgeBotOptions>(config.GetSection("KnowledgeBot"));
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(QueryRequest).Assembly));
services.AddValidatorsFromAssembly(typeof(QueryRequestValidator).Assembly);
services.AddSingleton<IDocumentLoader, DocumentLoader>();
services.AddSingleton<IRetriever, TfIdfRetriever>();
services.AddTransient<IAnswerComposer, AnswerComposer>();

var provider = services.BuildServiceProvider();

// ── Startup ──────────────────────────────────────────────────────────────────

Console.WriteLine("========================================");
Console.WriteLine("  NovaTech Knowledge Bot");
Console.WriteLine("========================================");
Console.Write("Loading documents...");

var loader = provider.GetRequiredService<IDocumentLoader>();
var docs = await loader.LoadAllAsync();

Console.WriteLine($" done ({docs.Count} files)");
Console.WriteLine();
Console.WriteLine("Type your question or 'exit' to quit.");
Console.WriteLine();

// ── Interactive loop ─────────────────────────────────────────────────────────

var mediator = provider.GetRequiredService<IMediator>();
var validator = provider.GetRequiredService<IValidator<QueryRequest>>();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var request = new QueryRequest(input);
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
    {
        Console.WriteLine();
        foreach (var error in validation.Errors)
            Console.WriteLine($"  {error.ErrorMessage}");
        Console.WriteLine();
        continue;
    }

    try
    {
        var response = await mediator.Send(request);
        Console.WriteLine();
        Console.WriteLine(response.Answer);
        Console.WriteLine();

        if (response.Confidence != ConfidenceLevel.None)
        {
            var sources = string.Join(", ", response.SourceFiles);
            Console.WriteLine($"Confidence: {response.Confidence.ToString().ToLower()} | Source: {sources}");
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unexpected error processing query");
        Console.WriteLine();
        Console.WriteLine("  An unexpected error occurred. Please try again.");
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye!");
Log.CloseAndFlush();
