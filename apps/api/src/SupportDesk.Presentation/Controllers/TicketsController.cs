
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Commands.AssignTicket;
using SupportDesk.Application.Features.Tickets.Commands.ChangeTicketStatus;
using SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Queries.SearchTickets;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public sealed class TicketsController : ControllerBase
{
    /// <summary>Searches tickets with filters, sorting and paging.</summary>
    [HttpGet]
    public Task<PagedResult<TicketListItemDto>> Search(
        [FromQuery] TicketQuery query,
        [FromServices] SearchTicketsQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(query, ct);

    /// <summary>Returns one ticket.</summary>
    [HttpGet("{id:int}")]
    public Task<TicketDetailDto> Get(
        int id,
        [FromServices] GetTicketQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, ct);

    /// <summary>Raises a new ticket.</summary>
    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> Create(
        CreateTicketRequest request,
        [FromServices] RaiseTicketCommandHandler handler,
        CancellationToken ct)
    {
        var created = await handler.HandleAsync(request, ct);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Escalates a ticket and records who performed the escalation.</summary>
    [HttpPost("{id:int}/escalate")]
    public Task<EscalateTicketResult> Escalate(
        int id,
        EscalateTicketRequest request,
        [FromServices] EscalateTicketCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(
            id,
            request with
            {
                EscalatedBy = User.FindFirstValue(ClaimTypes.Name) ?? ""
            },
            ct);

    /// <summary>Returns the escalation history for a ticket.</summary>
    [HttpGet("{id:int}/escalations")]
    public async Task<ActionResult<IReadOnlyList<TicketEscalationDto>>> Escalations(
        int id,
        [FromServices] TicketEscalationQueries queries,
        CancellationToken ct)
    {
        var result = await queries.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Moves a ticket to another status.</summary>
    [HttpPatch("{id:int}/status")]
    public Task<TicketDetailDto> UpdateStatus(
        int id,
        UpdateTicketStatusRequest request,
        [FromServices] ChangeTicketStatusCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, request, ct);

    /// <summary>Assigns a ticket to an agent, or unassigns it.</summary>
    [HttpPatch("{id:int}/assignment")]
    public Task<TicketDetailDto> Assign(
        int id,
        AssignTicketRequest request,
        [FromServices] AssignTicketCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, request, ct);
}
