using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Domain.Entities;
using KnowledgeBot.Infrastructure.Services;

namespace KnowledgeBot.Tests;

public class TfIdfRetrieverTests
{
    // A minimal IDocumentLoader that returns a fixed in-memory corpus.
    private sealed class FakeLoader(IReadOnlyList<CompanyDocument> docs) : IDocumentLoader
    {
        public Task<IReadOnlyList<CompanyDocument>> LoadAllAsync() => Task.FromResult(docs);
    }

    private static CompanyDocument MakeDoc(string fileName, Dictionary<string, int> tf, string[] sentences)
        => new()
        {
            FileName = fileName,
            RawText = string.Join(" ", sentences),
            Sentences = sentences,
            TermFrequencies = tf
        };

    // Three-document corpus: only leave.txt contains "annual" and "leave".
    private static IReadOnlyList<CompanyDocument> BuildCorpus() =>
    [
        MakeDoc("leave.txt",
            new(StringComparer.OrdinalIgnoreCase) { ["annual"] = 3, ["leave"] = 5, ["days"] = 2, ["employees"] = 1 },
            ["Employees receive 25 annual leave days per year."]),

        MakeDoc("product.txt",
            new(StringComparer.OrdinalIgnoreCase) { ["product"] = 4, ["features"] = 3, ["pricing"] = 2, ["plans"] = 1 },
            ["The product offers flexible pricing plans."]),

        MakeDoc("faq.txt",
            new(StringComparer.OrdinalIgnoreCase) { ["support"] = 3, ["ticket"] = 2, ["urgent"] = 1, ["contact"] = 2 },
            ["Submit a support ticket for urgent issues."])
    ];

    [Fact]
    public async Task Query_MatchingSingleDoc_ReturnsThatDocFirst()
    {
        var retriever = new TfIdfRetriever(new FakeLoader(BuildCorpus()));

        var results = (await retriever.RetrieveAsync("annual leave", topN: 3)).ToList();

        Assert.Equal("leave.txt", results[0].FileName);
        Assert.True(results[0].Score > 0);
    }

    [Fact]
    public async Task Query_MatchingSingleDoc_OtherDocsScoreLower()
    {
        var retriever = new TfIdfRetriever(new FakeLoader(BuildCorpus()));

        var results = (await retriever.RetrieveAsync("annual leave", topN: 3)).ToList();

        Assert.True(results[0].Score > results[1].Score);
        Assert.True(results[0].Score > results[2].Score);
    }

    [Fact]
    public async Task Query_MatchingDifferentDoc_ReturnsCorrectTopDoc()
    {
        var retriever = new TfIdfRetriever(new FakeLoader(BuildCorpus()));

        var results = (await retriever.RetrieveAsync("product pricing features", topN: 3)).ToList();

        Assert.Equal("product.txt", results[0].FileName);
    }

    [Fact]
    public async Task Query_AllStopWords_ReturnsEmpty()
    {
        var retriever = new TfIdfRetriever(new FakeLoader(BuildCorpus()));

        var results = (await retriever.RetrieveAsync("the is a", topN: 3)).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public async Task TopN_LimitsReturnedDocuments()
    {
        var retriever = new TfIdfRetriever(new FakeLoader(BuildCorpus()));

        var results = (await retriever.RetrieveAsync("annual leave", topN: 2)).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ReturnedDocs_AreScoredCopies_NotMutatingCache()
    {
        var corpus = BuildCorpus();
        var retriever = new TfIdfRetriever(new FakeLoader(corpus));

        await retriever.RetrieveAsync("annual leave", topN: 3);

        // Cached originals should have Score == 0 (default); retriever returns copies.
        Assert.Equal(0.0, corpus[0].Score);
    }
}
