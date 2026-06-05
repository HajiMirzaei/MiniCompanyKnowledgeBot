using MediatR;

namespace KnowledgeBot.Application.CompanyKnowledgeModule.Queries;

public record QueryRequest(string Question) : IRequest<QueryResponse>;
