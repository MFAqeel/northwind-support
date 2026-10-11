using Microsoft.Extensions.Options;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;
public sealed class SlaTriageTests
{
    [Theory]
    [InlineData(TicketPriority.Low, 72)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Critical, 4)]
    public void StandardSlaWindows(TicketPriority priority, int hours)
    {
        var calculator = new SlaCalculator(Options.Create(new SlaOptions()));
        Assert.Equal(TimeSpan.FromHours(hours), calculator.CalculateWindow(priority, CustomerTier.Standard));
        Assert.Equal(TimeSpan.FromHours(hours / 2.0), calculator.CalculateWindow(priority, CustomerTier.Premium));
    }

    [Fact]
    public void PremiumFloorIsApplied()
    {
        var settings = new SlaOptions { PremiumCustomerMultiplier = 0.01 };
        var calculator = new SlaCalculator(Options.Create(settings));
        Assert.Equal(TimeSpan.FromHours(1), calculator.CalculateWindow(TicketPriority.Critical, CustomerTier.Premium));
    }

    [Fact]
    public async Task CategoryForcesCriticalPriority()
    {
        var context = new TicketCommandTestContext();
        context.Categories.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Outage", true, true));
        await context.RaiseTicket.HandleAsync(new CreateTicketRequest("Outage", "System down", 1, 3, TicketPriority.Low), CancellationToken.None);
        Assert.Equal(TicketPriority.Critical, context.AddedTicket!.Priority);
        Assert.NotNull(context.AddedTicket.DueAtUtc);
    }
}
