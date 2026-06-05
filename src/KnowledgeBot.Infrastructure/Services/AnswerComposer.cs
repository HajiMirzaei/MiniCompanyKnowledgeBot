using KnowledgeBot.Application.CompanyKnowledgeModule.Queries;
using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Application.Options;
using KnowledgeBot.Domain.Entities;
using KnowledgeBot.Domain.Enums;
using Microsoft.Extensions.Options;

namespace KnowledgeBot.Infrastructure.Services;

public class AnswerComposer : IAnswerComposer
{
    private readonly KnowledgeBotOptions _options;

    public AnswerComposer(IOptions<KnowledgeBotOptions> options)
    {
        _options = options.Value;
    }

    public QueryResponse Compose(string question, IEnumerable<CompanyDocument> docs)
    {
        var docList = docs.ToList();

        if (docList.Count == 0 || docList[0].Score < _options.FallbackThreshold)
            return Fallback();

        var queryTokens = Tokenizer.Tokenize(question);
        var confidence = MapConfidence(docList[0].Score);

        // Score every sentence across all retrieved documents.
        var scored = docList
            .SelectMany((doc, _) =>
                doc.Sentences.Select((sentence, idx) => new
                {
                    doc.FileName,
                    Index = idx,
                    Sentence = sentence,
                    Score = ScoreSentence(queryTokens, sentence)
                }))
            .ToList();

        // Pick top-K globally, then restore reading order within each source document.
        var selected = scored
            .OrderByDescending(s => s.Score)
            .Take(_options.TopSentences)
            .GroupBy(s => s.FileName)
            .SelectMany(g => g.OrderBy(s => s.Index))
            .ToList();

        // Fallback if nothing scored above zero (query tokens absent from all sentences).
        if (selected.Count == 0 || selected.Max(s => s.Score) == 0)
            return Fallback();

        var answer = string.Join("\n", selected.Select(s => s.Sentence));
        var sourceFiles = selected.Select(s => s.FileName).Distinct().ToList();

        return new QueryResponse(answer, sourceFiles, confidence);
    }

    // Score = number of query token occurrences in the sentence (after tokenization).
    // Using sentence-level counts rather than doc-level TF avoids rewarding
    // sentences from term-dense docs that don't actually mention the query.
    private static double ScoreSentence(IReadOnlyList<string> queryTokens, string sentence)
    {
        var sentenceTokens = Tokenizer.Tokenize(sentence);
        var score = 0.0;
        foreach (var token in queryTokens.Distinct(StringComparer.OrdinalIgnoreCase))
            score += sentenceTokens.Count(t => string.Equals(t, token, StringComparison.OrdinalIgnoreCase));
        return score;
    }

    private ConfidenceLevel MapConfidence(double topScore) =>
        topScore >= _options.HighConfidenceThreshold ? ConfidenceLevel.High :
        topScore >= _options.MediumConfidenceThreshold ? ConfidenceLevel.Medium :
        ConfidenceLevel.Low;

    private static QueryResponse Fallback() =>
        new("I don't have information about that in the company docs.", [], ConfidenceLevel.None);
}
