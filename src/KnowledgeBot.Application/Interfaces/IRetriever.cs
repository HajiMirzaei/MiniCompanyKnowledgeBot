using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Application.Interfaces;

public interface IRetriever
{
    Task<IEnumerable<CompanyDocument>> RetrieveAsync(string query, int topN);
}
