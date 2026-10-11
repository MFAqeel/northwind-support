using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
namespace SupportDesk.Infrastructure.Queries;
public sealed class TicketEscalationQueries(SupportDbContext db)
{
    public async Task<IReadOnlyList<TicketEscalationDto>?> GetAsync(int id, CancellationToken ct)
    {
        if (!await db.Set<Ticket>().AnyAsync(t => t.Id == id, ct)) return null;
        return await db.Set<TicketEscalation>().AsNoTracking().Where(x => x.TicketId == id)
            .OrderByDescending(x => x.EscalatedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new TicketEscalationDto(x.Id, x.TicketId, x.FromPriority, x.ToPriority,
                x.FromAgentId, x.ToAgentId, x.FromDueAtUtc, x.ToDueAtUtc,
                x.Reason, x.EscalatedBy, x.EscalatedAtUtc)).ToListAsync(ct);
    }
}
