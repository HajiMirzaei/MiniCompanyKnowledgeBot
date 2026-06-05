using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Application.Options;
using KnowledgeBot.Domain.Entities;
using Microsoft.Extensions.Options;

namespace KnowledgeBot.Infrastructure.Services;

public class DocumentLoader : IDocumentLoader
{
    private readonly KnowledgeBotOptions _options;
    private IReadOnlyList<CompanyDocument>? _cache;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DocumentLoader(IOptions<KnowledgeBotOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CompanyDocument>> LoadAllAsync()
    {
        if (_cache is not null)
            return _cache;

        await _initLock.WaitAsync();
        try
        {
            // Double-check after acquiring the lock.
            if (_cache is not null)
                return _cache;

            _cache = await LoadFromDiskAsync();
            return _cache;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<IReadOnlyList<CompanyDocument>> LoadFromDiskAsync()
    {
        var docsPath = Path.GetFullPath(_options.DocsPath);
        if (!Directory.Exists(docsPath))
            throw new DirectoryNotFoundException($"Docs folder not found at resolved path: {docsPath}");

        var files = Directory.GetFiles(docsPath, "*.txt");
        var docs = new List<CompanyDocument>(files.Length);

        foreach (var filePath in files)
        {
            var rawText = await File.ReadAllTextAsync(filePath);
            docs.Add(new CompanyDocument
            {
                FileName = Path.GetFileName(filePath),
                RawText = rawText,
                Sentences = SplitSentences(rawText),
                TermFrequencies = ComputeTermFrequencies(rawText)
            });
        }

        return docs.AsReadOnly();
    }

    private static string[] SplitSentences(string text) =>
        text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimStart('-', '*', '•', '\t'))
            .Select(s => s.Trim())
            .Where(s => s.Length > 10 && !s.StartsWith("---"))
            .ToArray();

    private static Dictionary<string, int> ComputeTermFrequencies(string text)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in Tokenizer.Tokenize(text))
        {
            freq.TryGetValue(token, out var count);
            freq[token] = count + 1;
        }
        return freq;
    }
}
