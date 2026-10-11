using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;

#nullable disable
namespace SupportDesk.Infrastructure.Data.Migrations;
[DbContext(typeof(SupportDbContext))]
[Migration("20261011120000_TicketEscalations")]
public sealed class TicketEscalations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("SlaStartedAtUtc", "Tickets", type: "datetime2(3)", nullable: true);
        migrationBuilder.Sql("UPDATE [Tickets] SET [SlaStartedAtUtc] = [CreatedAtUtc] WHERE [DueAtUtc] IS NOT NULL");
        migrationBuilder.CreateTable("TicketEscalations", table => new
        {
            Id = table.Column<int>("int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            TicketId = table.Column<int>("int", nullable: false),
            FromPriority = table.Column<int>("int", nullable: false),
            ToPriority = table.Column<int>("int", nullable: false),
            FromAgentId = table.Column<int>("int", nullable: true),
            ToAgentId = table.Column<int>("int", nullable: true),
            FromDueAtUtc = table.Column<DateTime>("datetime2(3)", nullable: true),
            ToDueAtUtc = table.Column<DateTime>("datetime2(3)", nullable: true),
            Reason = table.Column<string>("nvarchar(500)", maxLength: 500, nullable: false),
            EscalatedBy = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: false),
            EscalatedAtUtc = table.Column<DateTime>("datetime2(3)", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_TicketEscalations", x => x.Id);
            table.ForeignKey("FK_TicketEscalations_Tickets_TicketId", x => x.TicketId, "Tickets", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_TicketEscalations_TicketId_EscalatedAtUtc", "TicketEscalations", new[] { "TicketId", "EscalatedAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TicketEscalations");
        migrationBuilder.DropColumn("SlaStartedAtUtc", "Tickets");
    }
}
