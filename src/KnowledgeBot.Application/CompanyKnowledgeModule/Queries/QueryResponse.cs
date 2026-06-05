using KnowledgeBot.Domain.Enums;

namespace KnowledgeBot.Application.CompanyKnowledgeModule.Queries;

public record QueryResponse(
    string Answer,
    List<string> SourceFiles,
    ConfidenceLevel Confidence);
