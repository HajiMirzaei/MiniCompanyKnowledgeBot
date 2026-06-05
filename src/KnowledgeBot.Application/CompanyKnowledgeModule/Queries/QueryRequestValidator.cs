using FluentValidation;

namespace KnowledgeBot.Application.CompanyKnowledgeModule.Queries;

public class QueryRequestValidator : AbstractValidator<QueryRequest>
{
    public QueryRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question cannot be empty.")
            .MaximumLength(500).WithMessage("Question cannot exceed 500 characters.");
    }
}
