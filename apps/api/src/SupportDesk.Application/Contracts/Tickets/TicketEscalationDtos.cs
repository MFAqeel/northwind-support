using SupportDesk.Domain.Aggregates.Tickets;
namespace SupportDesk.Application.Contracts.Tickets;
public sealed record EscalateTicketRequest(string Reason, string EscalatedBy = "");
public sealed record TicketEscalationDto(int Id, int TicketId, TicketPriority FromPriority,
    TicketPriority ToPriority, int? FromAgentId, int? ToAgentId,
    DateTime? FromDueAtUtc, DateTime? ToDueAtUtc, string Reason, string EscalatedBy, DateTime EscalatedAtUtc);
public sealed record EscalateTicketResult(TicketDetailDto Ticket, TicketEscalationDto Escalation);
public sealed record TriageDecisionDto(string PriorityReason, string SlaWindowReason, string AssignmentReason);
