using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using FluentValidation;

namespace DeskFlow.Application.Features.Tickets.CreateTicket;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(Ticket.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(Ticket.DescriptionMaxLength);

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("O departamento é obrigatório.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Prioridade inválida.");

        RuleFor(x => x.CriticalJustification)
            .NotEmpty().WithMessage("Prioridade crítica exige uma justificativa.")
            .When(x => x.Priority == TicketPriority.Critical);
    }
}
