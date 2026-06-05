using System.Text.RegularExpressions;

namespace KnowledgeBot.Infrastructure.Services;

internal static class Tokenizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "not", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "into", "out", "up", "about", "as", "is", "are",
        "was", "were", "be", "been", "have", "has", "had", "do", "does", "did",
        "will", "would", "can", "could", "should", "may", "might", "that", "this",
        "it", "its", "i", "you", "we", "they", "he", "she", "me", "him", "her",
        "us", "them", "my", "your", "our", "their", "all", "if", "how", "what",
        "when", "where", "who", "which", "these", "those", "also", "just", "so"
    };

    // Shared regex compiled once for the process lifetime.
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9\s]", RegexOptions.Compiled);

    internal static IReadOnlyList<string> Tokenize(string text)
    {
        var lower = text.ToLowerInvariant();
        var cleaned = NonAlphanumeric.Replace(lower, " ");
        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .ToList();
    }
}
