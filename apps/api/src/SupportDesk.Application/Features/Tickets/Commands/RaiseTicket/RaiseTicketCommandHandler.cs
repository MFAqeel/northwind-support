
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;

/// <summary>
/// Raises a ticket and automatically calculates its priority,
/// SLA due date, and assigned agent.
/// </summary>
public sealed class RaiseTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    IUnitOfWork unitOfWork,
    IClock clock,
    SlaCalculator slaCalculator,
    GetTicketQueryHandler getTicket,
    ILogger<RaiseTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">
    /// The customer or category does not exist.
    /// </exception>
    public async Task<TicketDetailDto> HandleAsync(
        CreateTicketRequest request,
        CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct);

        if (customer is null)
        {
            throw new NotFoundException("Customer", request.CustomerId);
        }

        var category = await categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        var now = clock.UtcNow;

        // BR-1: Force Critical for categories configured to require it.
        var priority = category.ForcesCriticalPriority
            ? TicketPriority.Critical
            : request.RequestedPriority ?? TicketPriority.Medium;

        // BR-2 and BR-3: Calculate SLA deadline.
        var dueAtUtc = slaCalculator.CalculateDueDate(
            priority,
            customer.Tier,
            now);

        // BR-4 and BR-5: Find the least busy eligible agent.
        var agent = await agents.FindBestAvailableAsync(
            category.Id,
            category.RequiresSpecialist,
            ct);

        var ticket = Ticket.Raise(
            await tickets.NextReferenceAsync(ct),
            request.Title,
            request.Description,
            request.CustomerId,
            request.CategoryId,
            priority,
            now);

        ticket.SetDueDate(dueAtUtc, now);

        if (agent is not null)
        {
            ticket.AssignTo(agent.Id, now);
        }

        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} created with priority {Priority}, due {DueAtUtc}, agent {AgentId}.",
            ticket.Reference,
            ticket.Priority,
            ticket.DueAtUtc,
            ticket.AssignedAgentId);

        var detail = await getTicket.HandleAsync(ticket.Id, ct);
        detail.Triage = new TriageDecisionDto(
            category.ForcesCriticalPriority ? "Category forces Critical priority." :
                request.RequestedPriority.HasValue ? "Requested priority applied." : "Defaulted to Medium priority.",
            $"{slaCalculator.CalculateWindow(priority, customer.Tier).TotalHours:g} hours for {customer.Tier} customer.",
            agent is null ? "Unassigned: no active, qualified agent with available capacity." :
                $"Assigned to {agent.FullName} based on specialization, capacity and workload.");
        return detail;
    }
}
