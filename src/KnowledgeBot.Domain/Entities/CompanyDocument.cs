namespace KnowledgeBot.Domain.Entities;

public class CompanyDocument
{
    public required string FileName { get; init; }
    public required string RawText { get; init; }
    public required string[] Sentences { get; init; }
    public required Dictionary<string, int> TermFrequencies { get; init; }
    public double Score { get; set; }
}
