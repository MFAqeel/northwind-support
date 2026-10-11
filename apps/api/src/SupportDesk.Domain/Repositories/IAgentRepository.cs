
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Loads Agent aggregates and finds available agents.
/// </summary>
public interface IAgentRepository
{
    Task<bool> CanKeepAssignmentAsync(int agentId, int categoryId, bool requiresSpecialist, CancellationToken ct);

    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    Task<Agent?> FindBestAvailableAsync(
        int categoryId,
        bool requiresSpecialist,
        CancellationToken ct);
}
