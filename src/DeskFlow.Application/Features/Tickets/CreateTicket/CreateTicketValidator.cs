using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using FluentValidation;

namespace DeskFlow.Application.Features.Tickets.CreateTicket;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(Ticket.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(Ticket.DescriptionMaxLength);

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority.");

        RuleFor(x => x.CriticalJustification)
            .NotEmpty().WithMessage("Critical priority requires a justification.")
            .When(x => x.Priority == TicketPriority.Critical);
    }
}
