using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Infrastructure.Services;

public class TfIdfRetriever : IRetriever
{
    private readonly IDocumentLoader _loader;
    private IReadOnlyList<CompanyDocument>? _docs;
    private Dictionary<string, double>? _idfMap;
    private Dictionary<string, int>? _docTokenCounts;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public TfIdfRetriever(IDocumentLoader loader)
    {
        _loader = loader;
    }

    public async Task<IEnumerable<CompanyDocument>> RetrieveAsync(string query, int topN)
    {
        await EnsureInitializedAsync();

        var queryTokens = Tokenizer.Tokenize(query);
        if (queryTokens.Count == 0)
            return [];

        return _docs!
            .Select(doc => new CompanyDocument
            {
                FileName = doc.FileName,
                RawText = doc.RawText,
                Sentences = doc.Sentences,
                TermFrequencies = doc.TermFrequencies,
                Score = ComputeScore(queryTokens, doc)
            })
            .OrderByDescending(d => d.Score)
            .Take(topN);
    }

    private double ComputeScore(IReadOnlyList<string> queryTokens, CompanyDocument doc)
    {
        var totalTokens = _docTokenCounts![doc.FileName];
        if (totalTokens == 0)
            return 0;

        var score = 0.0;
        // Distinct query tokens so repeated words in the question don't double-count.
        foreach (var token in queryTokens.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!doc.TermFrequencies.TryGetValue(token, out var termCount))
                continue;

            var tf = (double)termCount / totalTokens;
            var idf = _idfMap!.GetValueOrDefault(token, 0.0);
            score += tf * idf;
        }

        return score;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_docs is not null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_docs is not null)
                return;

            _docs = await _loader.LoadAllAsync();
            BuildIdfMap(_docs);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void BuildIdfMap(IReadOnlyList<CompanyDocument> docs)
    {
        var n = docs.Count;

        // df[term] = number of documents that contain the term.
        var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tokenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in docs)
        {
            // Total token count per doc is the denominator for normalized TF.
            tokenCounts[doc.FileName] = doc.TermFrequencies.Values.Sum();

            foreach (var term in doc.TermFrequencies.Keys)
            {
                df.TryGetValue(term, out var count);
                df[term] = count + 1;
            }
        }

        // Smoothed IDF: log((N+1) / (df+1)) + 1  — avoids zero for ubiquitous terms.
        var idfMap = new Dictionary<string, double>(df.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (term, docFreq) in df)
            idfMap[term] = Math.Log((double)(n + 1) / (docFreq + 1)) + 1.0;

        _idfMap = idfMap;
        _docTokenCounts = tokenCounts;
    }
}
