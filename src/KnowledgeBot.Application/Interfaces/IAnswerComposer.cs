using KnowledgeBot.Application.CompanyKnowledgeModule.Queries;
using KnowledgeBot.Domain.Entities;

namespace KnowledgeBot.Application.Interfaces;

public interface IAnswerComposer
{
    QueryResponse Compose(string question, IEnumerable<CompanyDocument> docs);
}
