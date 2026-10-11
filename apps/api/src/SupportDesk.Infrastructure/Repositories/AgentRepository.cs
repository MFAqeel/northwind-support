
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    public Task<bool> CanKeepAssignmentAsync(int agentId, int categoryId, bool requiresSpecialist, CancellationToken ct) =>
        db.Set<Agent>().AsNoTracking().Where(a => a.Id == agentId && a.IsActive)
            .Where(a => !requiresSpecialist || a.Specializations.Any(s => s.CategoryId == categoryId))
            .AnyAsync(a => db.Set<Ticket>().Count(t => t.AssignedAgentId == a.Id &&
                t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed) <= a.MaxOpenTickets, ct);

    /// <summary>Loads the whole aggregate, specializations included.</summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>
    /// Finds the eligible agent with the fewest open tickets.
    /// Ties are resolved by agent ID for deterministic assignment.
    /// </summary>
    public Task<Agent?> FindBestAvailableAsync(
        int categoryId,
        bool requiresSpecialist,
        CancellationToken ct)
    {
        return db.Set<Agent>()
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Where(a => !requiresSpecialist ||
                a.Specializations.Any(s => s.CategoryId == categoryId))
            .Select(a => new
            {
                Agent = a,
                OpenCount = db.Set<Ticket>().Count(t =>
                    t.AssignedAgentId == a.Id &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed)
            })
            .Where(x => x.OpenCount < x.Agent.MaxOpenTickets)
            .OrderBy(x => x.OpenCount)
            .ThenBy(x => x.Agent.Id)
            .Select(x => x.Agent)
            .FirstOrDefaultAsync(ct);
    }
}
