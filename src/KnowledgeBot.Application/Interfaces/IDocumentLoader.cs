using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Application.Interfaces;

public interface IDocumentLoader
{
    IReadOnlyList<CompanyDocument> LoadAll();
}
