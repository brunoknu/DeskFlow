using DeskFlow.Application.Features.Tickets.AddAttachment;
using DeskFlow.Application.Features.Tickets.AddInternalNote;
using DeskFlow.Application.Features.Tickets.AddPublicComment;
using DeskFlow.Application.Features.Tickets.AssignTicket;
using DeskFlow.Application.Features.Tickets.CancelTicket;
using DeskFlow.Application.Features.Tickets.ChangeTicketStatus;
using DeskFlow.Application.Features.Tickets.CreateTicket;
using DeskFlow.Application.Features.Tickets.GetAttachment;
using DeskFlow.Application.Features.Tickets.GetTicketById;
using DeskFlow.Application.Features.Tickets.RateTicket;
using DeskFlow.Application.Features.Tickets.ReopenTicket;
using DeskFlow.Application.Features.Tickets.ResolveTicket;
using DeskFlow.Application.Features.Tickets.SearchTickets;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DeskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<CreateTicketHandler>();
        services.AddScoped<GetTicketByIdHandler>();
        services.AddScoped<SearchTicketsHandler>();
        services.AddScoped<AssignTicketHandler>();
        services.AddScoped<ChangeTicketStatusHandler>();
        services.AddScoped<ResolveTicketHandler>();
        services.AddScoped<ReopenTicketHandler>();
        services.AddScoped<CancelTicketHandler>();
        services.AddScoped<AddPublicCommentHandler>();
        services.AddScoped<AddInternalNoteHandler>();
        services.AddScoped<AddAttachmentHandler>();
        services.AddScoped<GetAttachmentHandler>();
        services.AddScoped<RateTicketHandler>();

        return services;
    }
}
