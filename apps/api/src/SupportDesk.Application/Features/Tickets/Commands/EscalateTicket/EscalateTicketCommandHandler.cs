using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
public sealed class EscalateTicketCommandHandler(ITicketRepository tickets, ICustomerRepository customers,
    ICategoryRepository categories, IAgentRepository agents, IUnitOfWork unitOfWork, IClock clock,
    SlaCalculator sla, GetTicketQueryHandler getTicket)
{
    public async Task<EscalateTicketResult> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();
        var actor = request.EscalatedBy?.Trim();
        var errors = new Dictionary<string, string[]>();
        if (reason is null || reason.Length is < 5 or > 500)
            errors["reason"] = ["Reason must contain 5 to 500 characters."];
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100)
            errors["escalatedBy"] = ["An actor of up to 100 characters is required."];
        if (errors.Count > 0) throw new ValidationException(errors);
        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);
        if (!ticket.IsOpen || ticket.Priority == TicketPriority.Critical)
            throw new ConflictException(ticket.Priority == TicketPriority.Critical
                ? "Critical tickets cannot be escalated." : "Resolved or closed tickets cannot be escalated.");
        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);
        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);
        var now = clock.UtcNow;
        var fromPriority = ticket.Priority;
        var fromAgent = ticket.AssignedAgentId;
        var fromDue = ticket.DueAtUtc;
        var nextPriority = (TicketPriority)((int)fromPriority + 1);
        ticket.Escalate(nextPriority, now);
        ticket.SetDueDate(sla.CalculateDueDate(nextPriority, customer.Tier, now), now);
        // Keep current owner only when still active, qualified and below capacity.
        var keep = fromAgent.HasValue && await agents.CanKeepAssignmentAsync(fromAgent.Value, category.Id,
            category.RequiresSpecialist, ct);
        if (!keep)
        {
            var selected = await agents.FindBestAvailableAsync(category.Id, category.RequiresSpecialist, ct);
            ticket.AssignTo(selected?.Id, now);
        }
        var history = new TicketEscalation(ticket.Id, fromPriority, ticket.Priority, fromAgent,
            ticket.AssignedAgentId, fromDue, ticket.DueAtUtc, reason, actor!, now);
        await tickets.AddEscalationAsync(history, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var detail = await getTicket.HandleAsync(id, ct);
        return new EscalateTicketResult(detail, new TicketEscalationDto(history.Id, id, fromPriority,
            ticket.Priority, fromAgent, ticket.AssignedAgentId, fromDue, ticket.DueAtUtc, reason, actor!, now));
    }
}
