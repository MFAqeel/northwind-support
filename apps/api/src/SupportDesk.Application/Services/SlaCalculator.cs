
using Microsoft.Extensions.Options;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Services;

/// <summary>
/// Calculates SLA deadlines using configurable business rules.
/// </summary>
public sealed class SlaCalculator(IOptions<SlaOptions> options)
{
    public TimeSpan CalculateWindow(
        TicketPriority priority,
        CustomerTier customerTier)
    {
        var settings = options.Value;

        if (!settings.BaseWindowHours.TryGetValue(
            priority.ToString(), out var baseHours))
        {
            throw new InvalidOperationException(
                $"No SLA window configured for priority {priority}.");
        }

        var hours = customerTier == CustomerTier.Premium
            ? Math.Max(
                baseHours * settings.PremiumCustomerMultiplier,
                settings.MinimumWindowHours)
            : baseHours;

        return TimeSpan.FromHours(hours);
    }

    public DateTime CalculateDueDate(
        TicketPriority priority,
        CustomerTier customerTier,
        DateTime startUtc)
    {
        return startUtc.Add(CalculateWindow(priority, customerTier));
    }
}
