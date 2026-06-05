using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Application.Interfaces;

public interface IRetriever
{
    IEnumerable<CompanyDocument> Retrieve(string query, int topN);
}
