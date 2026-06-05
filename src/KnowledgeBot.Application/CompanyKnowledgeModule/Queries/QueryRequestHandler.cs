using MediatR;
using Microsoft.Extensions.Options;
using KnowledgeBot.Application.Interfaces;
using KnowledgeBot.Application.Options;

namespace KnowledgeBot.Application.CompanyKnowledgeModule.Queries;

public class QueryRequestHandler : IRequestHandler<QueryRequest, QueryResponse>
{
    private readonly IRetriever _retriever;
    private readonly IAnswerComposer _composer;
    private readonly KnowledgeBotOptions _options;

    public QueryRequestHandler(IRetriever retriever, IAnswerComposer composer, IOptions<KnowledgeBotOptions> options)
    {
        _retriever = retriever;
        _composer = composer;
        _options = options.Value;
    }

    public async Task<QueryResponse> Handle(QueryRequest request, CancellationToken cancellationToken)
    {
        var docs = await _retriever.RetrieveAsync(request.Question, _options.TopDocuments);
        return _composer.Compose(request.Question, docs);
    }
}
