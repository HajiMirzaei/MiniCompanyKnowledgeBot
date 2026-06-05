using KnowledgeBot.Application.Options;
using KnowledgeBot.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace KnowledgeBot.Tests;

public class DocumentLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public DocumentLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private DocumentLoader MakeLoader(string docsPath) =>
        new(Options.Create(new KnowledgeBotOptions { DocsPath = docsPath }));

    [Fact]
    public async Task MissingDirectory_ThrowsWithDescriptiveMessage()
    {
        var loader = MakeLoader(Path.Combine(_tempDir, "nonexistent"));

        var ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => loader.LoadAllAsync());

        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public async Task ValidDirectory_LoadsAllTxtFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "policy.txt"), "Annual leave is 25 days per year.");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "faq.txt"), "Support is available 24/7.");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "readme.md"), "This should be ignored.");

        var loader = MakeLoader(_tempDir);
        var docs = await loader.LoadAllAsync();

        Assert.Equal(2, docs.Count);
        Assert.All(docs, d => Assert.EndsWith(".txt", d.FileName));
    }

    [Fact]
    public async Task LoadedDocument_HasCorrectFileName()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "policy.txt"), "Annual leave is 25 days.");

        var loader = MakeLoader(_tempDir);
        var docs = await loader.LoadAllAsync();

        Assert.Equal("policy.txt", docs[0].FileName);
    }

    [Fact]
    public async Task LoadedDocument_HasNonEmptyTermFrequencies()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "policy.txt"),
            "Annual leave is 25 days per year for full-time employees.");

        var loader = MakeLoader(_tempDir);
        var docs = await loader.LoadAllAsync();

        Assert.NotEmpty(docs[0].TermFrequencies);
        Assert.True(docs[0].TermFrequencies.ContainsKey("annual"));
        Assert.True(docs[0].TermFrequencies.ContainsKey("leave"));
    }

    [Fact]
    public async Task LoadedDocument_SentencesExcludeShortLines()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "policy.txt"),
            "Hi\nAnnual leave is 25 days per year for full-time employees.");

        var loader = MakeLoader(_tempDir);
        var docs = await loader.LoadAllAsync();

        // "Hi" is <= 10 chars and should be filtered out.
        Assert.DoesNotContain("Hi", docs[0].Sentences);
        Assert.Single(docs[0].Sentences);
    }

    [Fact]
    public async Task LoadAll_IsCached_ReturnsSameInstance()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "policy.txt"), "Annual leave is 25 days.");

        var loader = MakeLoader(_tempDir);
        var first = await loader.LoadAllAsync();
        var second = await loader.LoadAllAsync();

        Assert.Same(first, second);
    }
}
