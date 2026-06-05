using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Application.Interfaces;

public interface IDocumentLoader
{
    Task<IReadOnlyList<CompanyDocument>> LoadAllAsync();
}
