using KnowledgeBot.Application.Options;
using KnowledgeBot.Domain.Entities;
using KnowledgeBot.Domain.Enums;
using KnowledgeBot.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace KnowledgeBot.Tests;

public class AnswerComposerTests
{
    private static AnswerComposer MakeComposer(
        double high = 0.15,
        double medium = 0.05,
        double fallback = 0.01,
        int topSentences = 4) =>
        new(Options.Create(new KnowledgeBotOptions
        {
            HighConfidenceThreshold = high,
            MediumConfidenceThreshold = medium,
            FallbackThreshold = fallback,
            TopSentences = topSentences
        }));

    private static CompanyDocument MakeDoc(string fileName, double score, string[] sentences) =>
        new()
        {
            FileName = fileName,
            RawText = string.Join(" ", sentences),
            Sentences = sentences,
            TermFrequencies = [],
            Score = score
        };

    [Fact]
    public void ScoreAboveHighThreshold_ReturnsHighConfidence()
    {
        var composer = MakeComposer();
        var doc = MakeDoc("leave.txt", score: 0.20,
            ["Employees receive 25 annual leave days per year."]);

        var response = composer.Compose("annual leave days", [doc]);

        Assert.Equal(ConfidenceLevel.High, response.Confidence);
    }

    [Fact]
    public void ScoreBetweenMediumAndHigh_ReturnsMediumConfidence()
    {
        var composer = MakeComposer();
        var doc = MakeDoc("leave.txt", score: 0.08,
            ["Employees receive 25 annual leave days per year."]);

        var response = composer.Compose("annual leave days", [doc]);

        Assert.Equal(ConfidenceLevel.Medium, response.Confidence);
    }

    [Fact]
    public void ScoreBetweenFallbackAndMedium_ReturnsLowConfidence()
    {
        var composer = MakeComposer();
        var doc = MakeDoc("leave.txt", score: 0.03,
            ["Employees receive 25 annual leave days per year."]);

        var response = composer.Compose("annual leave days", [doc]);

        Assert.Equal(ConfidenceLevel.Low, response.Confidence);
    }

    [Fact]
    public void ScoreBelowFallbackThreshold_ReturnsFallback()
    {
        var composer = MakeComposer();
        var doc = MakeDoc("leave.txt", score: 0.005,
            ["Employees receive 25 annual leave days."]);

        var response = composer.Compose("annual leave", [doc]);

        Assert.Equal(ConfidenceLevel.None, response.Confidence);
        Assert.Contains("don't have information", response.Answer);
    }

    [Fact]
    public void EmptyDocsList_ReturnsFallback()
    {
        var composer = MakeComposer();

        var response = composer.Compose("annual leave", []);

        Assert.Equal(ConfidenceLevel.None, response.Confidence);
        Assert.Contains("don't have information", response.Answer);
    }

    [Fact]
    public void SourceFiles_ContainsDocFileName()
    {
        var composer = MakeComposer();
        var doc = MakeDoc("leave.txt", score: 0.20,
            ["Annual leave policy grants 25 days to full-time employees."]);

        var response = composer.Compose("annual leave days", [doc]);

        Assert.Contains("leave.txt", response.SourceFiles);
    }

    [Fact]
    public void MultipleDocSentences_AreReturnedInReadingOrder()
    {
        var composer = MakeComposer(topSentences: 3);
        // Doc has 4 sentences; sentences 0, 2, 3 all mention "leave days".
        // After top-3 selection and reading-order restore, expect original index order.
        var doc = MakeDoc("leave.txt", score: 0.20,
        [
            "Annual leave days are granted each year.",    // idx 0 — mentions leave days
            "Sick leave is separate from annual leave.",   // idx 1 — lower score (only "leave")
            "Unused leave days carry over up to five.",    // idx 2 — mentions leave days
            "Leave days expire on March 31 each year."    // idx 3 — mentions leave days
        ]);

        var response = composer.Compose("leave days annual", [doc]);
        var sentences = response.Answer.Split('\n');

        // All returned sentences should be in the order they appear in the doc.
        var indices = sentences
            .Select(s => Array.IndexOf(doc.Sentences, s))
            .Where(i => i >= 0)
            .ToList();

        Assert.Equal(indices.OrderBy(x => x).ToList(), indices);
    }

    [Fact]
    public void MultipleSourceDocs_AreAllListedInSourceFiles()
    {
        var composer = MakeComposer(topSentences: 4);
        var docA = MakeDoc("leave.txt", score: 0.20,
            ["Annual leave policy grants 25 leave days to employees."]);
        var docB = MakeDoc("faq.txt", score: 0.10,
            ["Questions about leave days can be sent to HR."]);

        var response = composer.Compose("leave days", [docA, docB]);

        Assert.Contains("leave.txt", response.SourceFiles);
        Assert.Contains("faq.txt", response.SourceFiles);
    }
}
