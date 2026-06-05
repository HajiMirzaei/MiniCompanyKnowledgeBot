namespace KnowledgeBot.Application.Options;

public class KnowledgeBotOptions
{
    public string DocsPath { get; init; } = "../../docs";
    public int TopDocuments { get; init; } = 3;
    public int TopSentences { get; init; } = 4;
    public double HighConfidenceThreshold { get; init; } = 0.15;
    public double MediumConfidenceThreshold { get; init; } = 0.05;
    public double FallbackThreshold { get; init; } = 0.01;
}
