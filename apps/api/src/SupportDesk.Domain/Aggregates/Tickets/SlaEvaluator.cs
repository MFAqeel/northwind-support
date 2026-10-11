namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        double atRiskThresholdPercent = 25,
        TimeSpan? originalWindow = null)
    {
        if (dueAtUtc is null)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is not null)
        {
            return resolvedAtUtc <= dueAtUtc
                ? SlaStatus.Met
                : SlaStatus.Breached;
        }

        if (nowUtc > dueAtUtc)
        {
            return SlaStatus.Breached;
        }

        if (originalWindow is null || originalWindow.Value <= TimeSpan.Zero)
        {
            return SlaStatus.WithinSla;
        }

        var remainingTime = dueAtUtc.Value - nowUtc;
        var remainingPercent =
            remainingTime.TotalSeconds / originalWindow.Value.TotalSeconds * 100;

        return remainingPercent <= atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}